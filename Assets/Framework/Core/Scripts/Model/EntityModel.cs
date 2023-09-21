using System;
using System.Linq;
using System.Collections;

using UnityEngine;

using RTSEngine.Cameras;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Model
{
    public class EntityModel : MonoBehaviour, IEntityModel 
    {
        #region Attributes
        public byte PreInitPriority => 0;

        public IEntity Entity { private set; get;}
        public IMonoBehaviour Source => Entity;

        private EntityModelConnections modelObject = null;
        public IEntityModelConnections ModelConnections => modelObject;

        private ModelChildTransformHandler[] transformHandlers = new ModelChildTransformHandler[0]; 
        private ModelChildAnimatorHandler[] animatorHandlers = new ModelChildAnimatorHandler[0];
        private ModelChildRendererHandler[] rendererHandlers = new ModelChildRendererHandler[0];

        public bool IsRenderering { private set; get; }

        public IEntityModel Parent;

        public Vector2 Position2D => new Vector2(Entity.transform.position.x, Entity.transform.position.z);

        public Vector3 Center => Entity.transform.position;

        protected IModelCacheManager modelCacheMgr { private set; get; } 
        protected IMainCameraController mainCam { private set; get; } 
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntityModel, EventArgs> ModelCached;
        public event CustomEventHandler<IEntityModel, EventArgs> ModelShown;
        private void RaiseModelCached()
        {
            var handler = ModelCached;
            handler?.Invoke(this, EventArgs.Empty);
        }
        private void RaiseModelShown()
        {
            var handler = ModelShown;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public event CustomEventHandler<ICachedModel, EventArgs> CachedModelDisabled;
        private void RaiseCachedModelDisabled()
        {
            var handler = CachedModelDisabled;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void OnPrefabInit(IEntity entity, IGameManager gameMgr)
        {
            this.modelObject = null;
            IsRenderering = false;
        }

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.modelCacheMgr = gameMgr.GetService<IModelCacheManager>();
            this.mainCam = gameMgr.GetService<IMainCameraController>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            this.Entity = entity;

            OnClonedEntityPreInit();

            // In case this is a dummy entity, it means that it is not trakced by any other component and is being used locally for a certain purpose..
            // ... such as building placement as the building placement instance is a dummy entity
            // in this case, we want to only show the entity's model for the local player that owns it.
            if (Entity.IsDummy)
            {
                if(Entity.IsLocalPlayerFaction())
                {
                    modelObject = modelCacheMgr.Get(Entity);

                    IsRenderering = true;

                    ShowChildHandlers();
                }
            }
        }

        private void OnClonedEntityPreInit()
        {
            modelObject = modelCacheMgr.GetCachedEntityModelReference(Entity);
            modelObject.gameObject.SetActive(true);

            GenerateChildrenHandlers();

            modelObject = null;
            IsRenderering = false;

            CacheChildHandlers();

            modelCacheMgr.HideEntityModelReference(Entity);

            if (!modelCacheMgr.IsActive)
                Show();
        }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            globalEvent.RaiseCachedModelEnabledGlobal(this);
        }

        public void Disable()
        {
            StartCoroutine(DisableCoroutine());
        }

        public IEnumerator DisableCoroutine()
        {
            yield return new WaitForSeconds(Entity.Health.DestroyObjectDelay);

            if(modelObject.IsValid() && Entity.Health.CanDestroy(false, null) == ErrorMessage.none)
            {
                modelCacheMgr.CacheModel(Entity.Code, modelObject);
            }

            RaiseCachedModelDisabled();
            globalEvent.RaiseCachedModelDisabledGlobal(this);
        }
        #endregion

        #region Children Handlers
        private void CacheChildHandlers()
        {
            foreach(var handler in transformHandlers)
                if(handler != null)
                    handler.Cache();
            foreach(var handler in animatorHandlers)
                if(handler != null)
                    handler.Cache();
            foreach(var handler in rendererHandlers)
                if(handler != null)
                    handler.Cache();
        }

        private void ShowChildHandlers()
        {
            foreach(var handler in transformHandlers)
                if(handler != null)
                    handler.Show(modelObject.TransformConnections.Get(handler.IndexKey));
            foreach(var handler in animatorHandlers)
                if(handler != null)
                    handler.Show(modelObject.AnimatorConnections.Get(handler.IndexKey));
            foreach(var handler in rendererHandlers)
                if(handler != null)
                    handler.Show(modelObject.RendererConnections.Get(handler.IndexKey));
        }

        private void GenerateChildrenHandlers ()
        {
            int queryIndex = -1;
            transformHandlers = modelObject.TransformConnections
                .ConnectedChildren
                .Select(child =>
                {
                    queryIndex++;
                    if(!modelObject.TransformConnections.IsChildValid(child, modelObject))
                        return null;

                    ModelChildTransformHandler newHandler = new ModelChildTransformHandler(
                        Entity.transform,
                        child,
                        queryIndex);

                    return newHandler;
                })
                .ToArray();

            queryIndex = -1;
            animatorHandlers = modelObject.AnimatorConnections
                .ConnectedChildren
                .Select(child =>
                {
                    queryIndex++;
                    if(!modelObject.AnimatorConnections.IsChildValid(child, modelObject))
                        return null;

                    ModelChildAnimatorHandler newHandler = new ModelChildAnimatorHandler(
                        child,
                        queryIndex);

                    return newHandler;
                })
                .ToArray();

            queryIndex = -1;
            rendererHandlers = modelObject.RendererConnections
                .ConnectedChildren
                .Select(child =>
                {
                    queryIndex++;
                    if(!modelObject.RendererConnections.IsChildValid(child, modelObject))
                        return null;

                    ModelChildRendererHandler newHandler = new ModelChildRendererHandler(
                        child,
                        queryIndex);

                    return newHandler;
                })
                .ToArray();
        }
        #endregion

        #region Handling Hiding/Showing Entity Model
        public bool Show()
        {
            if (Parent.IsValid())
                return false;

            return ShowFinal();
        }

        private bool ShowFinal()
        {
            if (Entity.IsDummy
                || IsRenderering)
                return false;

            modelObject = modelCacheMgr.Get(Entity);

            IsRenderering = true;

            ShowChildHandlers();

            RaiseModelShown();

           return true;
        }
        public void OnCached()
        {
            if (Parent.IsValid())
                return;

            OnCachedFinal();
        }

        public void OnCachedFinal()
        {
            if (Entity.IsDummy
                || !IsRenderering)
                return;

            modelCacheMgr.CacheModel(Entity.Code, modelObject);
            modelObject = null;
            IsRenderering = false;

            CacheChildHandlers();

            RaiseModelCached();
        }
        #endregion

        #region Retrieving  Transform, Animator and Renderer Children
        public bool IsTransformChildValid(int indexKey)
            => indexKey.IsValidIndex(transformHandlers) && transformHandlers[indexKey] != null;

        public IModelChildTransform GetTransformChild(int indexKey)
        {
            if(!IsTransformChildValid(indexKey))
            {
                logger.LogError($"[{GetType().Name} - {Entity.Code}] Trying to access unassigned model child transform object, please follow error trace to see where the request is coming from!", source: Source);
                return null;
            }

            return transformHandlers[indexKey];
        }

        public bool IsAnimatorChildValid(int indexKey)
            => indexKey.IsValidIndex(animatorHandlers) && animatorHandlers[indexKey] != null;
        public IModelChildAnimator GetAnimatorChild(int indexKey)
        {
            if(!IsAnimatorChildValid(indexKey))
            {
                logger.LogError($"[{GetType().Name} - {Entity.Code}] Trying to access unassigned model child animator object, please follow error trace to see where the request is coming from!", source: Source);
                return null;
            }

            return animatorHandlers[indexKey];
        }

        public bool IsRendererChildValid(int indexKey)
            => indexKey.IsValidIndex(rendererHandlers) && rendererHandlers[indexKey] != null;
        public IModelChildRenderer GetRendererChild(int indexKey)
        {
            if(!IsRendererChildValid(indexKey))
            {
                logger.LogError($"[{GetType().Name} - {Entity.Code} - {indexKey}] Trying to access unassigned model child renderer object, please follow error trace to see where the request is coming from!", source: Source);
                return null;
            }

            return rendererHandlers[indexKey];
        }
        #endregion

        #region Handling Parent
        public void SetParent(IEntityModel parent)
        {
            // Had another parent before.
            if(this.Parent.IsValid())
            {
                this.Parent.ModelShown -= HandleParentModelShown;
                this.Parent.ModelCached -= HandleParentModelCached;
            }    

            this.Parent = parent;

            if (Parent.IsValid())
            {
                this.Parent.ModelShown += HandleParentModelShown;
                this.Parent.ModelCached += HandleParentModelCached;

                if (Parent.IsRenderering)
                    ShowFinal();
                else
                    OnCachedFinal();
            }
            else
            {
                modelCacheMgr.UpdateModelRenderering(this);
            }
        }

        private void HandleParentModelShown(IEntityModel sender, EventArgs args)
        {
            ShowFinal();
        }

        private void HandleParentModelCached(IEntityModel sender, EventArgs args)
        {
            OnCachedFinal();
        }
        #endregion
    }
}