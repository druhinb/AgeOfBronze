using RTSEngine.Cameras;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Model;
using RTSEngine.Terrain;
using RTSEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Search
{
    public class GridSearchHandler : MonoBehaviour, IGridSearchHandler
    {
        #region Attributes
        [SerializeField, Tooltip("Defines the lower-left corner of the search grid where search cells will be generated.")]
        private Int2D lowerLeftCorner = new Int2D { x = 0, y = 0 };
        [SerializeField, Tooltip("Defines the upper-right corner of the search grid where search cells will be generated.")]
        private Int2D upperRightCorner = new Int2D { x = 100, y = 100 };

        [SerializeField, Tooltip("The size of each individual cell."), Min(1)]
        private int cellSize = 10;
        /// <summary>
        /// Gets the fixed size of each cell in the grid.
        /// </summary>
        public int CellSize => cellSize;

        //holds all generated cells according to their positions.
        private Dictionary<Int2D, SearchCell> gridDict = new Dictionary<Int2D, SearchCell>();
        private IReadOnlyList<SearchCell> allSearchCells;

        [SerializeField, Tooltip("Enable to make sure that any search request has its position inside the search grid by clamping it. This ensures that even if an external component is attempting a search outside of the grid, which causes a warning, the search position remains bound to the search grid.")]
        private bool clampSearchPosition = true;
        [SerializeField, Tooltip("Caching search results would reduce the time complexity of the grid search algorithm in case the same search requests are launched on unchanging cells. When enabled, there is a space complexity and garbage collection penalty so make sure to test whether this option is beneficial for your case or not.")]
        private bool cacheSearchResults = true;
        private struct CachedSearchResult 
        {
            public List<SearchCell> searchedCells;
        }
        private struct CachedSearchSource : IEquatable<CachedSearchSource>
        {
            public Type entityType;

            public SearchCell sourceCell;

            public FloatRange radiusSqr;

            public override int GetHashCode()
            {
                var hashCode = 43270662;
                hashCode = hashCode * -1521134295 + entityType.GetHashCode();
                hashCode = hashCode * -1521134295 + sourceCell.GetHashCode();
                hashCode = hashCode * -1521134295 + radiusSqr.min.GetHashCode();
                hashCode = hashCode * -1521134295 + radiusSqr.max.GetHashCode();
                return hashCode;
            }

            public bool Equals(CachedSearchSource other)
            {
                return entityType == other.entityType &&
                sourceCell == other.sourceCell &&
                radiusSqr.min == other.radiusSqr.min &&
                radiusSqr.max == other.radiusSqr.max;
            }
        }

        private Dictionary<CachedSearchSource, CachedSearchResult> cachedSearches = new Dictionary<CachedSearchSource, CachedSearchResult>();
        private Dictionary<SearchCell, List<CachedSearchSource>> cachedSearchCells = new Dictionary<SearchCell, List<CachedSearchSource>>();

        // Cached Models: Only when the IModelCacheManager allows caching to be done via search cells.
        // Search cells that were rendererd during the last frame
        private HashSet<SearchCell> lastRendererdCells;
        private HashSet<SearchCell> nextRendererdCells;

        private const int DEFAULT_SEARCH_TARGETS_CAPACITY = 50;
        private const int DEFAULT_SEARCH_CELL_LIST_CAPACITY = 20;

        public struct SearchLists 
        {
            public List<SearchCell> currentSearchCells;
            public HashSet<SearchCell> alreadySearchedCells;
            public List<SearchCell> nextSearchCells;
            public List<KeyValuePair<float, IEntity>> currTargetsList;
        }
        private SearchLists posResLists;
        private SearchLists findLists;
        private SearchLists addObstacleLists;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IModelCacheManager modelCacheMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            cachedSearches = new Dictionary<CachedSearchSource, CachedSearchResult>();
            cachedSearchCells = new Dictionary<SearchCell, List<CachedSearchSource>>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 

            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.modelCacheMgr = gameMgr.GetService<IModelCacheManager>();

            posResLists = new SearchLists
            {
                nextSearchCells = new List<SearchCell>(DEFAULT_SEARCH_CELL_LIST_CAPACITY),
                currentSearchCells = new List<SearchCell>(DEFAULT_SEARCH_CELL_LIST_CAPACITY),
                alreadySearchedCells = new HashSet<SearchCell>()
            };

            findLists = new SearchLists
            {
                nextSearchCells = new List<SearchCell>(DEFAULT_SEARCH_CELL_LIST_CAPACITY),
                currentSearchCells = new List<SearchCell>(DEFAULT_SEARCH_CELL_LIST_CAPACITY),
                alreadySearchedCells = new HashSet<SearchCell>(),

                currTargetsList = new List<KeyValuePair<float, IEntity>>(DEFAULT_SEARCH_TARGETS_CAPACITY)
            };

            addObstacleLists = new SearchLists
            {
                nextSearchCells = new List<SearchCell>(DEFAULT_SEARCH_CELL_LIST_CAPACITY),
                currentSearchCells = new List<SearchCell>(DEFAULT_SEARCH_CELL_LIST_CAPACITY),
                alreadySearchedCells = new HashSet<SearchCell>()
            };

            GenerateCells();

            // The Update function is only used to handle grid search model caching.
            lastRendererdCells = new HashSet<SearchCell>();
            nextRendererdCells = new HashSet<SearchCell>();

            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;
            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;

            globalEvent.CachedModelEnabledGlobal += HandleCachedModelEnabledGlobal;

            globalEvent.SearchObstacleEnabledGlobal += HandleSearchObstacleEnabledGlobal;

            mainCameraController.CameraPositionUpdated += HandleCameraPositionUpdated;
        }

        private void OnDestroy()
        {
            globalEvent.EntityInitiatedGlobal -= HandleEntityInitiatedGlobal;
            globalEvent.EntityDeadGlobal -= HandleEntityDeadGlobal;

            globalEvent.CachedModelEnabledGlobal -= HandleCachedModelEnabledGlobal;

            mainCameraController.CameraPositionUpdated -= HandleCameraPositionUpdated;
        }
        #endregion

        #region Handling Search Obstacle Events
        private void HandleSearchObstacleEnabledGlobal(ISearchObstacle sender, EventArgs args)
        {
            TryAddSearchObstacle(sender);
        }

        private ErrorMessage TryAddSearchObstacle (ISearchObstacle newObstacle)
        {
            ErrorMessage errorMessage;
            // Only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(newObstacle.Center, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            Vector2 circleCenter = new Vector2(newObstacle.Center.x, newObstacle.Center.z);

            // Cells to search initially are the source cell and its direct neighbors.
            addObstacleLists.currentSearchCells.Clear();
            addObstacleLists.currentSearchCells.Add(sourceCell);
            addObstacleLists.currentSearchCells.AddRange(sourceCell.Neighbors);

            // We include the source cell and its direct neighbors in the already searched cells so that...
            //... we do not add them twice when looking at the current search cells neighbors that have not been considered for search in the loop below
            addObstacleLists.alreadySearchedCells.Clear();
            for(int i = 0; i < sourceCell.Neighbors.Count; i++)
                addObstacleLists.alreadySearchedCells.Add(sourceCell.Neighbors[i]);
            addObstacleLists.alreadySearchedCells.Add(sourceCell);

            // The size of the covered surface in terms of cell size
            int coveredSurface = 0; 

            // As long as there cells to search
            while(addObstacleLists.currentSearchCells.Count > 0)
            {
                // Holds te neighbor cells of the current cells to search so they would be searched in the next round.
                addObstacleLists.nextSearchCells.Clear();
                float radiusSqr = newObstacle.Size * newObstacle.Size;

                // Go through all the cells that are next to search
                for (int i = 0; i < addObstacleLists.currentSearchCells.Count; i++)
                {
                    SearchCell cell = addObstacleLists.currentSearchCells[i];

                    // Temporary variables to set edges for testing
                    float testX = cell.Position.x + (circleCenter.x <= cell.Position.x ? 0.0f : CellSize);
                    float testY = cell.Position.y + (circleCenter.y <= cell.Position.y ? 0.0f : CellSize);

                    // Get distance from closest edges
                    float distX = circleCenter.x - testX;
                    float distY = circleCenter.y - testY;
                    float nextDistance = (distX * distX) + (distY * distY);

                    // If the distance is less than the radius, the obstacle would be present in the current search cell.
                    if (nextDistance <= radiusSqr)
                        cell.AddObstacle(newObstacle);

                    // Go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    foreach (SearchCell neighborCell in cell.Neighbors)
                        if (!addObstacleLists.nextSearchCells.Contains(neighborCell))
                        {
                            addObstacleLists.nextSearchCells.Add(neighborCell);
                            addObstacleLists.alreadySearchedCells.Add(neighborCell);
                        }
                }

                // No potential target found? Increase the search surface
                coveredSurface += cellSize;

                // As long as the covered search surface has not got beyond the allowed search radius
                if (coveredSurface < newObstacle.Size)
                // Every search round, we go one cell size (or search cell) further.
                // The next cells to search are now the yet-unsearched neighbor cells
                {
                    addObstacleLists.currentSearchCells.Clear();
                    addObstacleLists.currentSearchCells.AddRange(posResLists.nextSearchCells);
                }
                else //we have already gone through the allowed search radius
                    break;
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Cached Model Events
        private void HandleCachedModelEnabledGlobal(ICachedModel sender, EventArgs args)
        {
            TryAddCachedModel(sender);
        }

        private ErrorMessage TryAddCachedModel (ICachedModel cachedModel)
        {
            ErrorMessage errorMessage;
            // Only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(cachedModel.Center, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            sourceCell.AddCachedModel(cachedModel);

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Entity Events
        private void HandleEntityInitiatedGlobal(IEntity entity, EventArgs args)
        {
            if (TryGetSearchCell(entity.transform.position, out SearchCell cell) == ErrorMessage.none) 
                cell.Add(entity); 
        }

        private void HandleEntityDeadGlobal(IEntity entity, DeadEventArgs args)
        {
            if (TryGetSearchCell(entity.transform.position, out SearchCell cell) == ErrorMessage.none)
                cell.Remove(entity);
        }
        #endregion

        #region Generating/Finding Cells
        private void GenerateCells ()
        {
            if (!logger.RequireTrue(cellSize > 0,
              $"[{GetType().Name}] The search grid cell size must be >= 0."))
                return;

            gridDict = new Dictionary<Int2D, SearchCell>();

            // According to the start and end position coordinates, create the required search cells
            for(int x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    // Each search cell instance is added to the dictionary after it is created for easier direct access using coordinates in the future.
                    Int2D nextPosition = new Int2D
                    {
                        x = x,
                        y = y
                    };

                    gridDict.Add(nextPosition, new SearchCell());
                }

            allSearchCells = gridDict.Values.ToList();

            // Go through all generated cells, init them and assign their neighbors
            foreach (Int2D position in gridDict.Keys) 
                gridDict[position].Init(gameMgr, position, FindNeighborCells(position).ToArray());
        }

        public IEnumerable<SearchCell> FindNeighborCells (Int2D sourcePosition)
        {
            // To store the found neighbor cells
            List<SearchCell> neighbors = new List<SearchCell>(); 

            // Maximum amount of potential neighboring cells
            int maxNeighborAmount = 8;
            Int2D nextPosition = new Int2D();

            while(maxNeighborAmount > 0)
            {
                switch(maxNeighborAmount)
                {
                    case 1: //right
                        nextPosition = new Int2D { x = sourcePosition.x + cellSize, y = sourcePosition.y };
                        break;
                    case 2: //left
                        nextPosition = new Int2D { x = sourcePosition.x - cellSize, y = sourcePosition.y };
                        break;
                    case 3: //up
                        nextPosition = new Int2D { x = sourcePosition.x, y = sourcePosition.y + cellSize };
                        break;
                    case 4: //down
                        nextPosition = new Int2D { x = sourcePosition.x, y = sourcePosition.y - cellSize };
                        break;

                    case 5: //upper-right
                        nextPosition = new Int2D { x = sourcePosition.x + cellSize, y = sourcePosition.y + cellSize };
                        break;
                    case 6: //upper-left
                        nextPosition = new Int2D { x = sourcePosition.x - cellSize, y = sourcePosition.y + cellSize };
                        break;
                    case 7: //lower-right
                        nextPosition = new Int2D { x = sourcePosition.x + cellSize, y = sourcePosition.y - cellSize };
                        break;
                    case 8: //lower-left
                        nextPosition = new Int2D { x = sourcePosition.x - cellSize, y = sourcePosition.y - cellSize };
                        break;
                }

                if (gridDict.TryGetValue(nextPosition, out SearchCell neighborCell))
                    neighbors.Add(neighborCell);

                maxNeighborAmount--;
            }

            return neighbors;
        }

        public ErrorMessage TryGetSearchCell (Vector3 position, out SearchCell cell)
        {
            Vector3 clampedPosition = clampSearchPosition
                ? new Vector3(Mathf.Clamp(position.x, lowerLeftCorner.x, upperRightCorner.x),
                position.y,
                Mathf.Clamp(position.z, lowerLeftCorner.y, upperRightCorner.y))
                : position;

            // Find the coordinates of the potential search cell where the input position is in
            Int2D nextPosition = new Int2D
            {
                x = ( ((int)clampedPosition.x - lowerLeftCorner.x) / cellSize) * cellSize + lowerLeftCorner.x,
                y = ( ((int)clampedPosition.z - lowerLeftCorner.y) / cellSize) * cellSize + lowerLeftCorner.y
            };

            if(gridDict.TryGetValue(nextPosition, out cell)) 
                return ErrorMessage.none;

            logger.Log(
                $"[{GetType().Name}] No search cell has been defined to contain position: {position}!",
                source: this,
                type: LoggingType.warning);
            return ErrorMessage.searchCellNotFound;
        }
        #endregion

        #region Handling Search
        public ErrorMessage SearchVisible<T>(RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand,
            out IReadOnlyList<T> targets) where T : IEntity
        {
            List<T> targetsList = new List<T>();

            // The amount that is confirmed to fit the search request
            int confirmedAmount = 0;

            for (int i = 0; i < allSearchCells.Count; i++)
            {
                SearchCell cell = allSearchCells[i];

                if (!cell.IsRenderering)
                    continue;

                for (int j = 0; j < cell.Entities.Count; j++)
                {
                    IEntity entity = cell.Entities[j];

                    if (!entity.IsValid()
                        || !entity.IsSearchable
                        || !(entity is T))
                        continue;

                    if (IsTargetValid(RTSHelper.ToTargetData(entity), playerCommand) == ErrorMessage.none)
                    {
                        targetsList.Add((T)entity);
                        confirmedAmount++;
                    }
                }
            }

            targets = targetsList;

            return ErrorMessage.none;
        }

        public ErrorMessage Search<T>(Vector3 sourcePosition, FloatRange radius, RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out T potentialTarget, bool findClosest = true) where T : IEntity
        {
            ErrorMessage errorMessage = Find<T>(
                sourcePosition,
                radius,
                1,
                IsTargetValid,
                findClosest,
                playerCommand);

            potentialTarget = findLists.currTargetsList.Count > 0 ? (T)findLists.currTargetsList[0].Value : default;

            return errorMessage;
        }

        public ErrorMessage Search<T>(
            Vector3 sourcePosition, float radius, RTSHelper.IsTargetValidDelegate IsTargetValid,
            bool playerCommand, out T potentialTarget, bool findClosest = true) where T : IEntity
            => Search(sourcePosition,
                new FloatRange(0.0f, radius),
                IsTargetValid,
                playerCommand,
                out potentialTarget,
                findClosest);


        public ErrorMessage Search<T>(Vector3 sourcePosition, float radius, int amount,
            RTSHelper.IsTargetValidDelegate IsTargetValid, bool playerCommand, out IReadOnlyList<T> potentialTargets, bool findClosest = true) where T : IEntity
        {
            ErrorMessage errorMessage = Find<T>(
                sourcePosition,
                new FloatRange(0.0f, radius),
                amount,
                IsTargetValid,
                findClosest,
                playerCommand);

            List<T> lastSearchTargets = new List<T>(amount > 0 ? amount : DEFAULT_SEARCH_TARGETS_CAPACITY);

            int targetCount = amount < 0 || findLists.currTargetsList.Count < amount ? findLists.currTargetsList.Count : amount;

            for (int i = 0; i < findLists.currTargetsList.Count; i++)
            {
                KeyValuePair<float, IEntity> elem = findLists.currTargetsList[i];

                lastSearchTargets.Add((T)elem.Value);

                if (i == targetCount - 1)
                    break;
            }

            potentialTargets = lastSearchTargets;

            return errorMessage;
        }

        // A negative integer in the "amount" parameter -> find all entities that satisfy the search conditions
        private ErrorMessage Find<T> (Vector3 sourcePosition, FloatRange radius, int originalAmount,
            RTSHelper.IsTargetValidDelegate IsTargetValid, bool findClosest, bool playerCommand) where T : IEntity
        {
            //targets = new List<T>(originalAmount);

            ErrorMessage errorMessage;
            // Only continue if a valid source search cell is found in the input position.
            if ((errorMessage = TryGetSearchCell(sourcePosition, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            FloatRange radiusSqr = new FloatRange(radius.min * radius.min, radius.max * radius.max);
            float nextDistance = 0.0f;
            // The size of the covered surface in terms of cell size
            int coveredSurface = 0;

            // The amount that is confirmed to fit the search request
            int confirmedAmount = 0;

            // Using a sorted list allows to sort potential targets depending on their distance from the search source position
            findLists.currTargetsList.Clear();

            CachedSearchSource cachedSearchSource = new CachedSearchSource
            {
                entityType = typeof(T),
                radiusSqr = radiusSqr,
                sourceCell = sourceCell
            };

            if(cacheSearchResults 
                    && findClosest 
                    && cachedSearches.TryGetValue(cachedSearchSource, out CachedSearchResult cachedSearchResult))
            {
                for (int i = 0; i < cachedSearchResult.searchedCells.Count; i++)
                {
                    SearchCell cell = cachedSearchResult.searchedCells[i];
                    for (int j = 0; j < cell.Entities.Count; j++)
                    {
                        IEntity entity = cell.Entities[j];
                        if (!entity.IsValid() 
                            || !entity.IsSearchable
                            || !(entity is T))
                            continue;

                        nextDistance = (entity.transform.position - sourcePosition).sqrMagnitude;

                        if ((nextDistance >= radiusSqr.min && nextDistance <= radiusSqr.max)
                            && IsTargetValid(RTSHelper.ToTargetData(entity), playerCommand) == ErrorMessage.none)
                        {
                            findLists.currTargetsList.Add(new KeyValuePair<float, IEntity>(nextDistance, entity));
                            confirmedAmount++;
                        }
                    }
                }

                findLists.currTargetsList.Sort((elem1, elem2) => (elem1.Key.CompareTo(elem2.Key)));

                // After going through all the current cells to search
                // See if we have a potential target or if we have already found all of our required targets
                if (confirmedAmount >= originalAmount)
                    return ErrorMessage.none;
                else
                    return ErrorMessage.searchTargetNotFound;
            }

            // Cells to search initially are the source cell and its direct neighbors.
            findLists.currentSearchCells.Clear();
            findLists.currentSearchCells.Add(sourceCell);
            findLists.currentSearchCells.AddRange(sourceCell.Neighbors);

            // We include the source cell and its direct neighbors in the already searched cells so that...
            //... we do not add them twice when looking at the current search cells neighbors that have not been considered for search in the loop below
            findLists.alreadySearchedCells.Clear();
            for(int i = 0; i < sourceCell.Neighbors.Count; i++)
                findLists.alreadySearchedCells.Add(sourceCell.Neighbors[i]);
            findLists.alreadySearchedCells.Add(sourceCell);

            // As long as there cells to search
            while(findLists.currentSearchCells.Count > 0)
            {
                // Holds te neighbor cells of the current cells to search so they would be searched in the next round.
                findLists.nextSearchCells.Clear();

                // Go through all the cells that are next to search
                for (int i = 0; i < findLists.currentSearchCells.Count; i++)
                {
                    SearchCell cell = findLists.currentSearchCells[i];

                    if (cacheSearchResults)
                    {
                        if (cachedSearchCells.ContainsKey(cell))
                            cachedSearchCells[cell].Add(cachedSearchSource);
                        else
                        {
                            cachedSearchCells.Add(cell, new List<CachedSearchSource>() { cachedSearchSource });
                            cell.SearchCellUpdated += HandleSearchCellUpdated;
                        }
                    }

                    for (int j = 0; j < cell.Entities.Count; j++)
                    {
                        IEntity entity = cell.Entities[j];
                        if (!entity.IsValid() 
                            || !entity.IsSearchable
                            || !(entity is T))
                            continue;

                        nextDistance = (entity.transform.position - sourcePosition).sqrMagnitude;

                        if ((nextDistance >= radiusSqr.min && nextDistance <= radiusSqr.max)
                            && IsTargetValid(RTSHelper.ToTargetData(entity), playerCommand) == ErrorMessage.none)
                        {
                            findLists.currTargetsList.Add(new KeyValuePair<float, IEntity>(nextDistance, entity));
                            confirmedAmount++;

                            if (!findClosest && confirmedAmount >= originalAmount)
                                return ErrorMessage.none;
                        }
                    }

                    // Go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    for (int k = 0; k < cell.Neighbors.Count; k++)
                    {
                        SearchCell neighborCell = cell.Neighbors[k];

                        if (!findLists.nextSearchCells.Contains(neighborCell))
                        {
                            findLists.nextSearchCells.Add(neighborCell);
                            findLists.alreadySearchedCells.Add(neighborCell);
                        }
                    }
                }

                findLists.currTargetsList.Sort((elem1, elem2) => (elem1.Key.CompareTo(elem2.Key)));

                if(cacheSearchResults && findClosest && !cachedSearches.ContainsKey(cachedSearchSource))
                    cachedSearches.Add(
                        cachedSearchSource,
                        new CachedSearchResult
                        {
                            searchedCells = new List<SearchCell>(findLists.alreadySearchedCells)
                        });

                // After going through all the current cells to search
                // See if we have a potential target or if we have already found all of our required targets
                if (confirmedAmount >= originalAmount) 
                    return ErrorMessage.none; 
                else 
                {
                    // No potential target found? Increase the search surface
                    coveredSurface += cellSize;

                    // As long as the covered search surface has not got beyond the allowed search radius
                    if (coveredSurface < radius.max)
                    {
                        // Every search round, we go one cell size (or search cell) further.
                        // The next cells to search are now the yet-unsearched neighbor cells
                        findLists.currentSearchCells.Clear();
                        findLists.currentSearchCells.AddRange(posResLists.nextSearchCells);
                    }
                    else //we have already gone through the allowed search radius
                        break;
                }
            }

            return ErrorMessage.searchTargetNotFound;
        }

        private void HandleSearchCellUpdated(SearchCell sourceCell, EventArgs args)
        {
            if(cachedSearchCells.ContainsKey(sourceCell))
            {
                foreach(CachedSearchSource searchSource in cachedSearchCells[sourceCell])
                {
                    cachedSearches.Remove(searchSource);
                }
            }

            cachedSearchCells.Remove(sourceCell);
            sourceCell.SearchCellUpdated -= HandleSearchCellUpdated;
        }

        public ErrorMessage IsPositionReserved (Vector3 testPosition, float radius, TerrainAreaMask areasMask, bool playerCommand)
        {
            ErrorMessage errorMessage;
            if ((errorMessage = TryGetSearchCell(testPosition, out SearchCell sourceCell)) != ErrorMessage.none)
                return errorMessage;

            // Cells to search initially are the source cell and its direct neighbors.
            posResLists.currentSearchCells.Clear();
            posResLists.currentSearchCells.Add(sourceCell);
            posResLists.currentSearchCells.AddRange(sourceCell.Neighbors);

            // We include the source cell and its direct neighbors in the already searched cells so that...
            //... we do not add them twice when looking at the current search cells neighbors that have not been considered for search in the loop below
            posResLists.alreadySearchedCells.Clear();
            for(int i = 0; i < sourceCell.Neighbors.Count; i++)
                posResLists.alreadySearchedCells.Add(sourceCell.Neighbors[i]);
            posResLists.alreadySearchedCells.Add(sourceCell);

            // The size of the covered surface in terms of cell size
            int coveredSurface = 0;

            // Since we're comparing squarred distances we need the squarred value of the radius
            float sqrRadius = radius * radius;

            // As long as there cells to search
            while(posResLists.currentSearchCells.Count > 0)
            {
                // Holds te neighbor cells of the current cells to search so they would be searched in the next round.
                posResLists.nextSearchCells.Clear();

                for (int i = 0; i < posResLists.currentSearchCells.Count; i++)
                {
                    SearchCell cell = posResLists.currentSearchCells[i];
                    for (int j = 0; j < cell.UnitTargetPositionMarkers.Count; j++)
                    {
                        Movement.IMovementTargetPositionMarker marker = cell.UnitTargetPositionMarkers[j];
                        if (marker.Enabled
                            && marker.AreasMask.Intersect(areasMask)
                            //&& marker.IsIn(testPosition))
                            && (marker.Position - testPosition).sqrMagnitude <= sqrRadius)
                            return ErrorMessage.mvtPositionMarkerReserved;
                    }

                    for (int j = 0; j < cell.Obstacles.Count; j++)
                    {
                        ISearchObstacle obstacle = cell.Obstacles[j];
                        if (obstacle.IsReserved(testPosition, areasMask, playerCommand))
                            return ErrorMessage.mvtPositionObstacleReserved;
                    }

                    // Go through each searched cell's neighbors and see which ones haven't been searched yet or marked for search yet and add them.
                    for (int j = 0; j < cell.Neighbors.Count; j++)
                    {
                        SearchCell neighborCell = cell.Neighbors[j];
                        if (!posResLists.alreadySearchedCells.Contains(neighborCell))
                        {
                            posResLists.nextSearchCells.Add(neighborCell);
                            posResLists.alreadySearchedCells.Add(neighborCell);
                        }
                    }
                }

                // After going through all the current cells to search
                // Increase the search surface
                coveredSurface += cellSize;

                // As long as the covered search surface has not got beyond the allowed search radius
                // Every search round, we go one cell size (or search cell) further.
                if (coveredSurface < radius)
                {
                    // The next cells to search are now the yet-unsearched neighbor cells
                    posResLists.currentSearchCells.Clear();
                    posResLists.currentSearchCells.AddRange(posResLists.nextSearchCells);
                }
                else
                    // We have already gone through the allowed search radius
                    break;
            }

            // No target position marker is present in the searched range then the position is not reserved.
            return ErrorMessage.none; 
        }
        #endregion

        #region Handling Cached Models
        private void HandleCameraPositionUpdated(IMainCameraController sender, EventArgs args)
        {
            UpdateCacheHandling();
        }

        private ErrorMessage UpdateCacheHandling()
        {
            var terrainPositions = terrainMgr.BaseTerrainCameraBounds.Get();

            nextRendererdCells.Clear();

            for (int i = 0; i < allSearchCells.Count; i++)
            {
                SearchCell cell = allSearchCells[i];

                if (nextRendererdCells.Contains(cell))
                    continue;

                if (terrainPositions.IsInsidePolygon(new Vector2(cell.Position.x, cell.Position.y)))
                {
                    cell.OnUpdateRendering(isRenderering: true);
                    nextRendererdCells.Add(cell);
                    lastRendererdCells.Remove(cell);

                    foreach(SearchCell neighborCell in cell.Neighbors)
                    {
                        if (nextRendererdCells.Contains(neighborCell))
                            continue;

                        neighborCell.OnUpdateRendering(isRenderering: true);
                        nextRendererdCells.Add(neighborCell);
                        lastRendererdCells.Remove(neighborCell);
                    }
                }
            }

            foreach(SearchCell cell in lastRendererdCells)
                cell.OnUpdateRendering(isRenderering: false);

            lastRendererdCells.Clear();
            foreach (SearchCell cell in nextRendererdCells)
                lastRendererdCells.Add(cell);

            return ErrorMessage.none;
        }
        #endregion

        #region Displaying Cells
#if UNITY_EDITOR
        [Header("Gizmos")]
        public Color defaultCellColor = Color.yellow;
        public Color renderedCellColor = Color.green;
        public Color cachedCellColor = Color.red;
        [Min(1.0f)]
        public float gizmoHeight = 1.0f;

        private void OnDrawGizmosSelected()
        {
            if (cellSize <= 0)
                return;

            Vector3 size = new Vector3(cellSize, gizmoHeight, cellSize);

            if (gridDict.IsValid() && gridDict.Count > 0)
            {
                foreach (SearchCell cell in gridDict.Values)
                {
                    if (cell.IsRenderering)
                    {
                        Gizmos.color = renderedCellColor;
                        Gizmos.DrawWireCube(new Vector3(cell.Position.x + cellSize / 2.0f, 0.0f, cell.Position.y + cellSize / 2.0f), size);
                    }
                    else
                    {
                        Gizmos.color = cachedCellColor;
                        Gizmos.DrawWireCube(new Vector3(cell.Position.x + cellSize / 2.0f, 0.0f, cell.Position.y + cellSize / 2.0f), size);
                    }
                }

                return;
            }

            Gizmos.color = defaultCellColor;
            for(int x = lowerLeftCorner.x; x < upperRightCorner.x; x += cellSize)
                for (int y = lowerLeftCorner.y; y < upperRightCorner.y; y += cellSize)
                {
                    Gizmos.DrawWireCube(new Vector3(x + cellSize/2.0f, 0.0f, y + cellSize/2.0f), size);
                }
        }
#endif
        #endregion
    }
}
