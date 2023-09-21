using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Model;
using RTSEngine.Attack;

namespace RTSEngine.Health
{
    public abstract partial class FactionEntityHealth : EntityHealth, IFactionEntityHealth
    {
        #region Attributes
        [SerializeField, Tooltip("When disabled, no entity with an Attack component can choose this entity as its target.")]
        private bool canBeAttacked = true;
        public bool CanBeAttacked { get => canBeAttacked; set { canBeAttacked = value; } }

        [SerializeField, Tooltip("Drag and drop a child Transform whose position will be used as the target position of attack objects directed at this entity. If left empty, the target position will be the position of the game object that holds the selection component of the entity.")]
        private ModelCacheAwareTransformInput attackTargetPosition = null;
        public IAttackTargetPositionGetter attackTargetPositionGetter { private set; get; }
        public Vector3 AttackTargetPosition => 
            attackTargetPositionGetter.IsValid() ? attackTargetPositionGetter.TargetPosition
            : (attackTargetPosition.IsValid() ? attackTargetPosition.Position : Entity.Selection.transform.position);

        private List<DamageOverTimeHandler> dotHandlers;
        public IEnumerable<DamageOverTimeHandler> DOTHandlers => dotHandlers;

        public IFactionEntity FactionEntity { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnEntityHealthInit()
        {
            FactionEntity = Entity as IFactionEntity;
            attackTargetPositionGetter = FactionEntity.GetComponentInChildren<IAttackTargetPositionGetter>();

            dotHandlers = new List<DamageOverTimeHandler>();

            OnFactionEntityHealthInit();
        }

        protected virtual void OnFactionEntityHealthInit() { }
        #endregion

        #region Updating Health
        public override ErrorMessage CanAdd (HealthUpdateArgs args)
        {
            if (IsDead)
                return ErrorMessage.dead;
            if (args.Value > 0 && !CanIncrease)
                return ErrorMessage.healthNoIncrease;
            else if (args.Value < 0 && !CanDecrease)
                return ErrorMessage.healthNoDecrease;

            return ErrorMessage.none;
        }
        protected override void OnHealthUpdated(HealthUpdateArgs args)
        {
            globalEvent.RaiseFactionEntityHealthUpdatedGlobal(FactionEntity, args);
        }
        #endregion

        #region Destroying Faction Entity
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseFactionEntityDeadGlobal(FactionEntity, new DeadEventArgs(upgrade, source, DestroyObjectDelay));
        }

#endregion
#region Handling Damage Over Time

        private void Update()
        {
            if (dotHandlers.Count == 0)
                return;

            int i = 0;
            while(i < dotHandlers.Count)
            {
                if(!dotHandlers[i].Update())
                {
                    dotHandlers.RemoveAt(i);
                    continue;
                }    

                i++;
            }
        }

        public void AddDamageOverTime (DamageOverTimeData nextDOTData, int damage, IEntity source, float initialCycleDuration = 0.0f)
        {
            dotHandlers.Add(new DamageOverTimeHandler(this, nextDOTData, damage, source, initialCycleDuration));
        }
        #endregion
    }
}
