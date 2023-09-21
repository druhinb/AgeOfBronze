using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Model;
using RTSEngine.Utilities;
using RTSEngine.Effect;

namespace RTSEngine.Demo
{
    public class OpenTreasureHandler : MonoBehaviour, IEntityPostInitializable
    {
        [SerializeField]
        private ModelCacheAwareAnimatorInput animator = null;
        [SerializeField]
        private string openChestStateParam = "IsOpen";

        [SerializeField, EnforceType(typeof(IEffectObject))]
        private GameObjectToEffectObjectInput openEffect = null;

        protected IEffectObjectPool effectObjPool { get; private set; }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>(); 
            if (!gameMgr.GetService<IGameLoggingService>().RequireValid(animator,
              $"[{GetType().Name}] The 'Animator' field must be assigned!"))
                return; 

            entity.Health.EntityDead += HandleEntityDead;
        }

        public void Disable()
        {
        }

        private void HandleEntityDead(IEntity entity, DeadEventArgs args)
        {
            animator.SetBool(openChestStateParam, true);

            effectObjPool.Spawn(openEffect.Output,
                new EffectObjectSpawnInput(
                    entity.transform,
                    useLocalTransform: true,
                    Vector3.zero,
                    new RotationData(openEffect.Output)));
            
            entity.Health.EntityDead -= HandleEntityDead;
        }
    }
}
