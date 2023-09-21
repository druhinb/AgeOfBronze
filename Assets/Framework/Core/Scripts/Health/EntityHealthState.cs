using UnityEngine;
using UnityEngine.Events;

using RTSEngine.Model;
using RTSEngine.Entities;

namespace RTSEngine.Health
{
    [System.Serializable]
    public class EntityHealthState
    {
        [SerializeField, Tooltip("The entity is considered in this state only if its health is inside this range.")]
        private IntRange healthRange = new IntRange(0, 100);

        public int UpperLimit => healthRange.max;
        public int LowerLimit => healthRange.min;

        // When 'upperBoundState' is set to true, it means that there is no other health state which has a higher health interval
        // In this case, we do not consider the upper bound of the interval
        public bool IsInRange(int value, bool upperBoundState = false) => value >= healthRange.min && (upperBoundState || value < healthRange.max);

        [SerializeField, Tooltip("Gameobjects to show when the entity is in this health state.")]
        private ModelCacheAwareTransformInput[] showChildObjects = new ModelCacheAwareTransformInput[0];

        [SerializeField, Tooltip("Gameobjects to hide when the entity is in this health state.")]
        private ModelCacheAwareTransformInput[] hideChildObjects = new ModelCacheAwareTransformInput[0];  

        [SerializeField, Tooltip("Event(s) triggered when the entity enters this health state.")]
        private UnityEvent triggerEvent = new UnityEvent();

        public void Init(IEntity entity)
        {
            foreach (ModelCacheAwareTransformInput obj in showChildObjects)
                if (!obj.IsValid())
                    RTSHelper.LoggingService.LogError($"[EntityHealthState - {entity.Code}] One of the entity health states assigned elements is either unassigned or assigned to an invalid child transform object!", source: entity);

            foreach (ModelCacheAwareTransformInput obj in hideChildObjects)
                if (!obj.IsValid())
                    RTSHelper.LoggingService.LogError($"[EntityHealthState - {entity.Code}] One of the entity health states assigned elements is either unassigned or assigned to an invalid child transform object!", source: entity);
        }

        public bool Toggle(bool enable)
        {
            foreach (ModelCacheAwareTransformInput obj in showChildObjects)
                if (obj.IsValid())
                    obj.IsActive = enable;

            foreach (ModelCacheAwareTransformInput obj in hideChildObjects)
                if (obj.IsValid())
                    obj.IsActive = !enable;

            if (enable)
                triggerEvent.Invoke();

            return true;
        }
    }
}
