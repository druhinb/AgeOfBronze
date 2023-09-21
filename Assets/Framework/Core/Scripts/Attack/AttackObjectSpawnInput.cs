using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Entities;
using RTSEngine.Effect;
using RTSEngine.Model;

namespace RTSEngine.Attack
{
    public class AttackObjectSpawnInput : EffectObjectSpawnInput
    {
        public IAttackComponent source;
        public int sourceFactionID;

        public int launcherSourceIndex;

        public IFactionEntity target;
        public Vector3 targetPosition;

        public float delayTime;
        public bool damageInDelay;
        public ModelCacheAwareTransformInput delayParent;

        public bool damageFriendly;

        public AttackObjectSpawnInput(IAttackComponent sourceAttackComp,
                                      int sourceFactionID,
                                      int launcherSourceIndex,

                                      Vector3 spawnPosition,
                                      Quaternion spawnRotation,

                                      IFactionEntity target,
                                      Vector3 targetPosition,

                                      float delayTime,
                                      bool damageInDelay,
                                      ModelCacheAwareTransformInput delayParent,

                                      bool damageFriendly,

                                      bool enableLifeTime = true,
                                      bool useDefaultLifeTime = true,
                                      float customLifeTime = 0.0f
                                      )
            : base(null,

                  false,
                  spawnPosition,
                  spawnRotation,
                  enableLifeTime,
                  useDefaultLifeTime,
                  customLifeTime
                  )
        {
            this.source = sourceAttackComp;
            this.sourceFactionID = sourceFactionID;
            this.launcherSourceIndex = launcherSourceIndex;

            this.target = target;
            this.targetPosition = targetPosition;

            this.delayTime = delayTime;
            this.damageInDelay = damageInDelay;
            this.delayParent = delayParent;

            this.damageFriendly = damageFriendly;
        }
    }
}
