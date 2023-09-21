
using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Model;
using RTSEngine.Logging;

namespace RTSEngine.Attack
{
    public class FactionEntityAttackTargetGetter : MonoBehaviour, IAttackTargetPositionGetter
    {
        private IFactionEntity factionEntity;

        [SerializeField, Tooltip("")]
        private ModelCacheAwareTransformInput attackTargetPosition = null;

        [SerializeField, Tooltip("")]
        private Vector3 offset = Vector3.zero;

        public Vector3 TargetPosition => attackTargetPosition.Position + offset;

        protected IGameLoggingService logger { private set; get; } 

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            if(!entity.IsFactionEntity())
            {
                logger.LogError($"[{GetType().Name}] This component must be attached to parent or a child object of of an entity of type '{typeof(IFactionEntity).Name}'");
                return;
            }
            else if(!attackTargetPosition.IsValid())
            {
                logger.LogError($"[{GetType().Name}] The 'Attack Target Position' must be assigned!");
                return;
            }

            factionEntity = entity as IFactionEntity;
        }

        public void Disable()
        {
        }
    }
}
