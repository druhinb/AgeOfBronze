using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Cameras;

namespace RTSEngine.Model
{
    public class ModelCacheManager : MonoBehaviour, IModelCacheManager
    {
        #region Attributes
        public int Priority => 500;

        [SerializeField, Tooltip("Enable to allow for entity and non entity models to be cached when they are not visible by the main camera.")]
        private bool isActive = false;
        public bool IsActive => isActive;

        [SerializeField, Tooltip("Enable to utilize the grid search cells to handling showing/hiding entity and non entity models. In case of a high amount of entities and a large map, this option allows to gain performance.")]
        private bool useGridSearch = true;
        public bool UseGridSearch => useGridSearch;

        // Holds the entity model references of the entity prefabs that can be created in the active game.
        private List<EntityModelConnections> entityModelReferences = new List<EntityModelConnections>();

        // Includes both EntityModel and NonEntityModel instances
        private List<ICachedModel> cachedModels = new List<ICachedModel>();
        
        private struct CachedEntityModelItem
        {
            public string name;
            public int copiesCount;
            public EntityModelConnections reference;
            public Vector3 defaultLocalPosition;
            public Quaternion defaultLocalRotation;

            public Stack<EntityModelConnections> cached;
        }
        private IDictionary<string, CachedEntityModelItem> cachedEntityModels;

        private struct CachedNonEntityModelItem
        {
            public Stack<GameObject> cached;
        }
        private Dictionary<string, CachedNonEntityModelItem> cachedNonEntityModels;
        private TerrainPositionsFromCameraBoundaries visibleTerrainPositions;

        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>(); 
            this.mainCameraController = gameMgr.GetService<IMainCameraController>();

            cachedEntityModels = new Dictionary<string, CachedEntityModelItem>();
            cachedNonEntityModels = new Dictionary<string, CachedNonEntityModelItem>();

            cachedModels = new List<ICachedModel>();

            globalEvent.CachedModelEnabledGlobal += HandleCachedModelEnabledGlobal;
            globalEvent.CachedModelDisabledGlobal += HandleCachedModelDisabledGlobal;

            visibleTerrainPositions = terrainMgr.BaseTerrainCameraBounds.Get();

            if (!isActive)
            {
                useGridSearch = false;
                return;
            }

            if(!UseGridSearch)
                mainCameraController.CameraPositionUpdated += HandleCameraPositionUpdated;
        }

        private void OnDestroy()
        {
            globalEvent.CachedModelEnabledGlobal -= HandleCachedModelEnabledGlobal;
            globalEvent.CachedModelDisabledGlobal -= HandleCachedModelDisabledGlobal;

            mainCameraController.CameraPositionUpdated -= HandleCameraPositionUpdated;
        }
        #endregion

        #region Handling Event: Cached Model Enabled/Disabled Global
        private void HandleCachedModelDisabledGlobal(ICachedModel sender, EventArgs args)
        {
            cachedModels.Remove(sender);
        }

        private void HandleCachedModelEnabledGlobal(ICachedModel sender, EventArgs args)
        {
            cachedModels.Add(sender);
            UpdateModelRenderering(sender);
        }
        #endregion

        #region Handling Entity Model References
        public EntityModelConnections GetCachedEntityModelReference(IEntity entity)
        {
            EntityModelConnections reference = cachedEntityModels.TryGetValue(entity.Code, out CachedEntityModelItem value) ? value.reference : null;

            if(!reference.IsValid())
            {
                logger.LogError($"[ModelCacheManager] Unable to find the model reference object for entity of code '{entity.Code}'. Are you sure that the prefab of this entity is loaded by the Input Manager?");
                return null;
            }

            reference.transform.SetParent(entity.transform);
            reference.transform.localPosition = value.defaultLocalPosition;
            reference.transform.localRotation = value.defaultLocalRotation;

            return reference;
        }

        public void HideEntityModelReference (IEntity entity)
        {
            EntityModelConnections reference = cachedEntityModels.TryGetValue(entity.Code, out CachedEntityModelItem value) ? value.reference : null;

            reference.gameObject.SetActive(false);
            reference.transform.SetParent(null);
            reference.transform.position = Vector3.zero;
        }
        #endregion

        #region Handling Caching/Getting Entity Models
        public void CacheModel(string code, EntityModelConnections modelObject)
        {
            if (!logger.RequireValid(modelObject,
              $"[{GetType().Name}] The provided model object for entity of code '{code}' has not been assigned!"))
                return; 

            modelObject.gameObject.SetActive(false);
            Vector3 localPosition = modelObject.transform.localPosition;
            Quaternion localRotation = modelObject.transform.localRotation;
            modelObject.transform.SetParent(null);
            modelObject.transform.position = Vector3.zero;

            if (!cachedEntityModels.ContainsKey(code))
            {
                string name = modelObject.name;

                EntityModelConnections firstCopy = GameObject.Instantiate(modelObject, Vector3.zero, modelObject.transform.rotation);
                modelObject.name = $"{name}_ref";
                firstCopy.name = $"{name}_0";

                entityModelReferences.Add(modelObject);

                cachedEntityModels.Add(
                    code,
                    new CachedEntityModelItem
                    {
                        name = name,
                        reference = modelObject,
                        defaultLocalPosition = localPosition,
                        defaultLocalRotation = localRotation,
                        cached = new Stack<EntityModelConnections>(new EntityModelConnections[] { firstCopy }),
                        copiesCount = 1,
                    });

                return;
            }
            else if (cachedEntityModels[code].reference == modelObject)
                logger.LogError($"[{GetType().Name}] Can not cache reference model object! Use HideModelReference() instead!");

            cachedEntityModels[code].cached.Push(modelObject);
        }

        public EntityModelConnections Get(IEntity source)
        {
            EntityModelConnections nextModel;

            if(cachedEntityModels.TryGetValue(source.Code, out CachedEntityModelItem item) && item.cached.Count > 0)
            { 
                nextModel = item.cached.Pop();
            }
            else
            {
                nextModel = GameObject.Instantiate(item.reference);
                nextModel.name = $"{item.name}_{item.copiesCount}";
                item.copiesCount += 1;
            }

            nextModel.transform.SetParent(source.transform);
            nextModel.transform.localPosition = item.defaultLocalPosition;
            nextModel.transform.localRotation = item.defaultLocalRotation;
            nextModel.gameObject.SetActive(true);

            return nextModel;
        }
        #endregion

        #region Handling Caching/Getting Non Entity Models
        public void CacheModel(string code, GameObject modelObject)
        {
            modelObject.gameObject.SetActive(false);
            modelObject.transform.SetParent(null);
            modelObject.transform.position = Vector3.zero;

            if (!cachedNonEntityModels.ContainsKey(code))
            {
                cachedNonEntityModels.Add(
                    code,
                    new CachedNonEntityModelItem 
                    {
                        cached = new Stack<GameObject> ()
                    });
            }

            cachedNonEntityModels[code].cached.Push(modelObject);
        }

        public GameObject Get(NonEntityModel source)
        {
            GameObject nextModel;

            if(cachedNonEntityModels.TryGetValue(source.Code, out CachedNonEntityModelItem item) && item.cached.Count > 0)
            { 
                nextModel = item.cached.Pop();

                nextModel.transform.SetParent(source.transform);
                nextModel.gameObject.SetActive(true);

                return nextModel;
            }
            else
            {
                logger.LogError($"[{GetType().Name}] Unable to find a cached model for the non entity model of code '{source.Code}'", source: source.Source);
                return null;
            }
        }
        #endregion

        #region Handling Active (non grid-search based) Caching
        private void HandleCameraPositionUpdated(IMainCameraController sender, EventArgs args)
        {
            visibleTerrainPositions = terrainMgr.BaseTerrainCameraBounds.Get();
            
            foreach (ICachedModel nextModel in cachedModels)
                UpdateModelRenderering(nextModel);
        }

        public void UpdateModelRenderering(ICachedModel nextModel)
        {
            if (!IsActive)
                return;

            if (visibleTerrainPositions.IsInsidePolygon(nextModel.Position2D))
            {
                if(!nextModel.IsRenderering)
                    nextModel.Show();
            }
            else if(nextModel.IsRenderering)
                nextModel.OnCached();
        }

        #endregion
    }
}

