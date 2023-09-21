using UnityEngine;

using RTSEngine.Cameras;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Search;
using System;

namespace RTSEngine.Model
{
    public class NonEntityModel : MonoBehaviour, ICachedModel, IMonoBehaviour
    {
        #region Attributes
        public IMonoBehaviour Source { private set; get; }

        [SerializeField, Tooltip("Code that is unique to the mesh (or multiple meshes) rendererd by the model object assigned in the field below.")]
        private string code = "unique_non_entity_model";
        public string Code => code;

        [SerializeField, Tooltip("Game object that holds the mesh renderer(s) used to display the non entity model. This is the object that will be cached.")]
        private GameObject modelObject = null;

        [SerializeField, Tooltip("Offsets the position of the non entity model that will be used to determine which grid search cell the non entity model belongs to.")]
        private Vector3 offset = Vector3.zero;

        public Vector2 Position2D => new Vector2(transform.position.x, transform.position.z);

        public Vector3 Center => transform.position + offset;

        private SearchCell sourceCell;

        private ModelChildTransformHandler modelTransformHandler = null;

        public bool IsRenderering { private set; get; }

        protected IModelCacheManager modelCacheMgr { private set; get; } 
        protected IMainCameraController mainCam { private set; get; } 
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<ICachedModel, EventArgs> CachedModelDisabled;
        private void RaiseCachedModelDisabled()
        {
            var handler = CachedModelDisabled;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        private void Start()
        {
            RTSHelper.TryGameInitPostStart(Init);
        }

        private void Init(IGameManager gameMgr)
        {
            this.modelCacheMgr = gameMgr.GetService<IModelCacheManager>();
            this.mainCam = gameMgr.GetService<IMainCameraController>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            Source = this;

            modelTransformHandler = new ModelChildTransformHandler(this.transform, modelObject.transform, -1);

            IsRenderering = true;

            if(modelCacheMgr.IsActive)
                OnCached();

            globalEvent.RaiseCachedModelEnabledGlobal(this);
        }

        private void OnDestroy()
        {
            RaiseCachedModelDisabled();
            globalEvent.RaiseCachedModelDisabledGlobal(this);
        }
        #endregion

        #region Handling Caching/Showing Model
        public void OnCached()
        {
            if (!IsRenderering)
                return;

            modelCacheMgr.CacheModel(Code, modelObject);
            modelObject = null;
            IsRenderering = false;

            modelTransformHandler.Cache();
        }

        public bool Show()
        {
            if (IsRenderering
                || !(modelObject = modelCacheMgr.Get(this)).IsValid())
                return false;

            modelTransformHandler.Show(modelObject.transform);

            IsRenderering = true;

           return true;
        }
        #endregion
    }
}
