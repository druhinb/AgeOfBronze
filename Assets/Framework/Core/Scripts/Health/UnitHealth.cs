using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Health
{
    public class UnitHealth : FactionEntityHealth, IUnitHealth
    {
        #region Attributes
        public IUnit Unit { private set; get; }
        public override EntityType EntityType => EntityType.unit;

        [SerializeField, Tooltip("Stop the unit's movement when it receives damage?"), Header("Unit Health")]
        private bool stopMovingOnDamage = false;
        #endregion

        #region Initializing/Terminating
        protected override void OnFactionEntityHealthInit()
        {
            Unit = Entity as IUnit;

            stateHandler.Reset(States, CurrHealth);
        }
        #endregion

        #region Updating Health
        protected override void OnHealthUpdated(HealthUpdateArgs args)
        {
            base.OnHealthUpdated(args);

            if (args.Value < 0)
            {
                if (stopMovingOnDamage)
                    Unit.MovementComponent.Stop();
            }

            globalEvent.RaiseUnitHealthUpdatedGlobal(Unit, args);
        }
        #endregion

        #region Destroying Unit
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseUnitDeadGlobal(Unit, new DeadEventArgs(upgrade, source, DestroyObjectDelay));
        }
        #endregion
    }
}
