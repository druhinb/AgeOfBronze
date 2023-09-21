
using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Demo
{
    public class VehicleUnitDestroyHeightModifier : ModelHeightModifierBase
    {
        #region Attributes
        protected IUnit unit { private set; get; }

        [SerializeField]
        private ModelPositionModifierData destructionModifier = new ModelPositionModifierData { speed = new TimeModifiedFloat(0.5f) };
        private float destroyDelay;
        private float deathTimer;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.unit = entity as IUnit;

            Deactivate(destructionModifier.initialHeight);

            unit.Health.EntityDead += HandleEntityDead;
        }

        protected override void OnDisabled()
        {
        }
        #endregion

        #region Handling Event: Entity Dead
        private void HandleEntityDead(IEntity sender, DeadEventArgs args)
        {
            destroyDelay = args.DestroyObjectDelay;
            if (destroyDelay <= 0.0f)
                return;

            deathTimer = destroyDelay;
            Activate(destructionModifier, UpdateTargetDestructionHeight);

            unit.Health.EntityDead -= HandleEntityDead;
        }
        #endregion

        #region Updating Unit Height
        private float UpdateTargetDestructionHeight()
        {
            deathTimer -= Time.deltaTime;
            return ((destroyDelay - deathTimer) / destroyDelay) * currModifier.targetHeight - currModifier.initialHeight;
        }
        #endregion
    }
}
