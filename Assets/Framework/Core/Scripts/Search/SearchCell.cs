using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Utilities;
using RTSEngine.Event;
using RTSEngine.Model;

namespace RTSEngine.Search
{
    public class SearchCell
    {
        #region Attributes
        /// <summary>
        /// Gets the lower-left corner position of the search cell.
        /// </summary>
        public Int2D Position { private set; get; }

        /// <summary>
        /// Gets the set of neighboring cells.
        /// </summary>
        public IReadOnlyList<SearchCell> Neighbors { get; private set; } = null;

        private List<ISearchObstacle> obstacles = null; 
        public IReadOnlyList<ISearchObstacle> Obstacles => obstacles;

        private List<IEntity> entities = null; 
        /// <summary>
        /// Gets the entities that are positioned within the search cell.
        /// </summary>
        public IReadOnlyList<IEntity> Entities => entities;

        private List<ICachedModel> cachedModels = null;

        // Holds the coroutine that periodically checks whether moving entities inside the search cell are still in the cell or have left it
        private IEnumerator entityPositionCheckCoroutine;
        // List of entities in the cell that are actively moving
        private List<IEntity> movingEntities = null;

        //holds all the unit target positions inside the bounds of the search cell.
        private List<IMovementTargetPositionMarker> unitTargetPositionMarkers = null; 
        /// <summary>
        /// Gets the tracked UnitTargetPositionMarker instances inside the search cell.
        /// </summary>
        public IReadOnlyList<IMovementTargetPositionMarker> UnitTargetPositionMarkers => unitTargetPositionMarkers;

        // Game services
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IModelCacheManager modelCacheMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, Int2D position, SearchCell[] neighbors)
        {
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.modelCacheMgr = gameMgr.GetService<IModelCacheManager>(); 

            this.Position = position;
            this.Neighbors = neighbors;

            entities = new List<IEntity>();
            movingEntities = new List<IEntity>();

            unitTargetPositionMarkers = new List<IMovementTargetPositionMarker>();

            obstacles = new List<ISearchObstacle>();

            cachedModels = new List<ICachedModel>();
        }
        #endregion

        #region Handling Events: IMovementComponent
        private void HandleMovementStart (IMovementComponent movementComponent, MovementEventArgs e)
        {
            IEntity entity = movementComponent.Entity;

            if (!movingEntities.Contains(entity)) 
                movingEntities.Add(entity);

            if(entityPositionCheckCoroutine == null) 
            {
                entityPositionCheckCoroutine = EntityPositionCheck();
                gridSearch.StartCoroutine(entityPositionCheckCoroutine);
            }
        }

        private void HandleMovementStop (IMovementComponent movementComponent, EventArgs e)
        {
            movingEntities.Remove(movementComponent.Entity);
        }
        #endregion

        #region Adding/Removing Entities
        public event CustomEventHandler<SearchCell, EventArgs> SearchCellUpdated;

        public void RaiseSearchCellUpdated ()
        {
            var handler = SearchCellUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public void Add(IEntity newEntity)
        {
            entities.Add(newEntity);

            if (newEntity.MovementComponent != null)
            {
                newEntity.MovementComponent.MovementStart += HandleMovementStart;
                newEntity.MovementComponent.MovementStop += HandleMovementStop;
                newEntity.EntityComponentUpgraded += HandleMovingEntityComponentUpgrade;

                if (newEntity.MovementComponent.HasTarget) 
                    HandleMovementStart(newEntity.MovementComponent, default);
            }

            RaiseSearchCellUpdated();
        }

        private void HandleMovingEntityComponentUpgrade(IEntity entity, EntityComponentUpgradeEventArgs args)
        {
            if(args.TargetComp is IMovementComponent)
            {
                IMovementComponent sourceMvtComp = args.SourceComp as IMovementComponent;
                if (sourceMvtComp.IsValid())
                {
                    sourceMvtComp.MovementStart -= HandleMovementStart;
                    sourceMvtComp.MovementStop -= HandleMovementStop;
                }

                IMovementComponent targetMvtComp = args.TargetComp as IMovementComponent;
                if (targetMvtComp.IsValid())
                {
                    targetMvtComp.MovementStart += HandleMovementStart;
                    targetMvtComp.MovementStop += HandleMovementStop;
                }
            }
        }

        public void Remove(IEntity entity)
        {
            entities.Remove(entity);

            if (entity.MovementComponent != null)
            {
                movingEntities.Remove(entity);

                entity.MovementComponent.MovementStart -= HandleMovementStart;
                entity.MovementComponent.MovementStop -= HandleMovementStop;
                entity.EntityComponentUpgraded -= HandleMovingEntityComponentUpgrade;

                if (entityPositionCheckCoroutine != null && movingEntities.Count == 0)
                {
                    //stop coroutine as there are no more entities moving inside this cell.
                    gridSearch.StopCoroutine(entityPositionCheckCoroutine);
                    entityPositionCheckCoroutine = null;
                }
            }

            RaiseSearchCellUpdated();
        }
        #endregion

        #region Tracking Moving Entities
        WaitForSeconds trackingUnitWaitFor = new WaitForSeconds(0.1f);
        /// <summary>
        /// Checks whether moving entities that belong to the search cell have left the cell or not.
        /// </summary>
        /// <param name="waitTime">How often to test whether moving entities are the in cell or not?</param>
        private IEnumerator EntityPositionCheck()
        {
            while (true)
            {
                yield return trackingUnitWaitFor;

                int i = 0;
                while (i < movingEntities.Count)
                {
                    if (!movingEntities[i].IsValid())
                    {
                        movingEntities.RemoveAt(i);
                        continue;
                    }

                    if (!IsIn(movingEntities[i].transform.position))
                    {
                        IEntity nextEntity = movingEntities[i];

                        // Find a new cell for the unit

                        if (gridSearch.TryGetSearchCell(nextEntity.transform.position, out SearchCell newCell) != ErrorMessage.none)
                        {
                            logger.LogError($"[SearchCell] Unable to find a new search cell for unit of code {nextEntity.Code} at position {nextEntity.transform.position}!");
                            continue;
                        }

                        newCell.Add(nextEntity);

                        RemoveCachedModel(nextEntity.EntityModel);
                        newCell.AddCachedModel(nextEntity.EntityModel);

                        Remove(nextEntity);

                        continue;
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// Check if a Vector3 position is inside the search cell's boundaries.
        /// </summary>
        /// <param name="testPosition">Vector3 position to test.</param>
        /// <returns>True if the input position is inside the search cell's boundaries, otherwise false.</returns>
        public bool IsIn (Vector3 testPosition)
        {
            return testPosition.x >= Position.x && testPosition.x < Position.x + gridSearch.CellSize
                && testPosition.z >= Position.y && testPosition.z < Position.y + gridSearch.CellSize;
        }
        #endregion

        #region Adding/Removing UnitTargetPositionMarker instances
        /// <summary>
        /// Adds a new UnitTargetPositionMarker instance to the tracked lists of unit target position markers inside this search cell.
        /// </summary>
        /// <param name="newMarker">The new UnitTargetPositionMarker instance to add.</param>
        public void Add(IMovementTargetPositionMarker newMarker)
        {
            if (!unitTargetPositionMarkers.Contains(newMarker)) //as long as the new marker hasn't been already added
                unitTargetPositionMarkers.Add(newMarker);
        }

        /// <summary>
        /// Removes a UnitTargetPositionMarker instance from the tracked list of markers inside this search cell.
        /// </summary>
        /// <param name="marker">The UnitTargetPositionMarker instance to remove.</param>
        public void Remove(IMovementTargetPositionMarker marker)
        {
            unitTargetPositionMarkers.Remove(marker);
        }
        #endregion

        #region Adding/Removing Search Obstacles
        public void AddObstacle(ISearchObstacle newObstacle)
        {
            if (obstacles.Contains(newObstacle))
                return;

            obstacles.Add(newObstacle);
            newObstacle.SearchObstacleDisabled += HandleSearchObstacleDisabled;
        }

        private void HandleSearchObstacleDisabled(ISearchObstacle obstacle, EventArgs args)
        {
            RemoveObstacle(obstacle);
        }

        private bool RemoveObstacle(ISearchObstacle obstacle)
        {
            obstacle.SearchObstacleDisabled -= HandleSearchObstacleDisabled;

            return obstacles.Remove(obstacle);
        }
        #endregion

        #region Cached Models 
        public bool IsRenderering { private set; get; }

        public void AddCachedModel(ICachedModel cachedModel)
        {
            if (!modelCacheMgr.IsActive)
                return;

            cachedModels.Add(cachedModel);

            cachedModel.CachedModelDisabled += HandleCachedModelDisabled;

            UpdateModelRendering(cachedModel);
        }

        private void HandleCachedModelDisabled(ICachedModel sender, EventArgs args)
        {
            RemoveCachedModel(sender);
        }

        private bool RemoveCachedModel(ICachedModel cachedModel)
        {
            cachedModel.CachedModelDisabled -= HandleCachedModelDisabled;

            return cachedModels.Remove(cachedModel);
        }

        public void OnUpdateRendering (bool isRenderering)
        {
            this.IsRenderering = isRenderering;

            if (!modelCacheMgr.IsActive)
                return;

            foreach(ICachedModel model in cachedModels)
                UpdateModelRendering(model);
        }

        private void UpdateModelRendering(ICachedModel model)
        {
            if (!model.IsValid()
                || model.IsRenderering == this.IsRenderering)
                return;

            if (this.IsRenderering)
                model.Show();
            else
                model.OnCached();
        }
        #endregion
    }
}
