using System;

using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Model;
using RTSEngine.Determinism;

namespace RTSEngine.Demo
{
    [System.Serializable]
    public struct ModelPositionModifierData
    {
        [SerializeField, Tooltip("The height (local position on the y axis) that the model starts with.")]
        public float initialHeight;
        [SerializeField, Tooltip("The height (local position on the y axis) that the model attempts to reach when it is in this state.")]
        public float targetHeight;

        [SerializeField, Tooltip("How fast will the height of the model is updated.")]
        public TimeModifiedFloat speed;
    }

    public class ModelHeightModifierBase : MonoBehaviour, IEntityPreInitializable, IMonoBehaviour
    {
        #region Attributes
        public bool IsInitialized { private set; get; } = false;
        protected bool isActive { private set; get; } = false;

        protected IEntity entity { private set; get; }

        [SerializeField, Tooltip("Model object of the building.")]
        private ModelCacheAwareTransformInput model = null;
        protected ModelCacheAwareTransformInput Model => model;

        protected ModelPositionModifierData currModifier { private set; get; }
        private float currVelocity;
        protected Func<float> targetHeightUpdateFunction { private set; get; }
        // The position on the y axis that the construction model attempts to reach.
        protected float currTargetHeight { private set; get; }

        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Initializing/Terminating
        private void Start()
        {
            // If the Start() Unity message is called while this entity service was not initialized (since it iniitalizes post entity init and that is only after building placement for buildings).
            // We keep this component inactive so the FixedUpdate() method is not called
            if (!IsInitialized)
            {
                isActive = false;
                enabled = true;
            }
        }

        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();

            if (!logger.RequireValid(model,
              $"[{GetType().Name} - {entity.Code}] The 'Model' field must be assigned!", source: this))
                return; 

            this.entity = entity;

            OnInit();

            IsInitialized = true;
        }

        protected virtual void OnInit() { }

        public void Disable()
        {
            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Updating Height
        private void FixedUpdate()
        {
            if (!isActive)
                return;

            Vector3 nextPosition = model.LocalPosition;
            nextPosition.y = Mathf.SmoothDamp(nextPosition.y, targetHeightUpdateFunction(), ref currVelocity, 1 / currModifier.speed.Value);

            model.LocalPosition = nextPosition;
        }

        protected void Deactivate(float resetHeight)
        {
            isActive = false;
            targetHeightUpdateFunction = null;

            model.LocalPosition = new Vector3(
                model.LocalPosition.x,
                resetHeight,
                model.LocalPosition.z);
        }

        protected void Activate(ModelPositionModifierData nextModifier, Func<float> targetHeightUpdateFunction)
        {
            currModifier = nextModifier;
            currTargetHeight = currModifier.targetHeight - currModifier.initialHeight;
            this.targetHeightUpdateFunction = targetHeightUpdateFunction;

            currVelocity = 0.0f;

            // Enabling building construction elevator effect
            model.LocalPosition = new Vector3(
                model.LocalPosition.x, 
                currModifier.initialHeight, 
                model.LocalPosition.z);

            isActive = true;
        }
        #endregion
    }
}
