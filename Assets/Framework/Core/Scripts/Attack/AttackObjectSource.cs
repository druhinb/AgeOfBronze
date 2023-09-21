using UnityEngine;

using RTSEngine.Effect;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using System;
using RTSEngine.Logging;
using RTSEngine.Utilities;
using RTSEngine.Model;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public struct AttackObjectSource
    {
        [EnforceType(typeof(IAttackObject)), Tooltip("The attack object to launch.")]
        public GameObjectToAttackObjectInput attackObject;

        [Tooltip("This is where the attack object will be launched from.")]
        public ModelCacheAwareTransformInput launchPosition; 
        [Tooltip("The initial rotation that the attack object will have as soon as it is spawned.")]
        public Vector3 launchRotationAngles;

        [Tooltip("The higher the absolute value in an axis, the less accurate the attack object movement is on that axis.")]
        public Vector3 accuracyModifier; 

        [Tooltip("Delay time before the attack object is created.")]
        public float preDelayTime; 
        [Tooltip("Delay time after the attack object is created.")]
        public float postDelayTime; 

        [Tooltip("Delay time that starts exactly when the attack object is created and is used to block the movement of the attack object.")]
        public float launchDelayTime; 
        [Tooltip("Deal damage with the attack object when it is in delay mode?")]
        public bool damageInDelay;
        [Tooltip("A parent object can be assigned to the attack object when it is in delay mode.")]
        public ModelCacheAwareTransformInput delayParentObject;

        // index of this source in the array in the AttackLauncher
        [HideInInspector]
        public int index;

        public void Init(IGameLoggingService logger, IAttackComponent sourceAttackComp, int index)
        {
            if(!logger.RequireValid(attackObject,
                $"[{GetType().Name} - {sourceAttackComp.Entity.Code}] The 'Sources' field includes element where the 'Attack Object' is unassigned or assigned to a prefab that does not include a component extending the '{typeof(IAttackObject).Name}' interface.")
                || !logger.RequireValid(launchPosition,
                $"[{GetType().Name} - {sourceAttackComp.Entity.Code}] The 'Sources' field includes one or more elements with the 'Launch Position' field unassigned."))
                return;

            this.index = index;
        }

        internal IAttackObject Launch(IAttackManager attackMgr, IAttackComponent sourceAttackComp)
        {
            Vector3 targetPosition = RTSHelper.GetAttackTargetPosition(sourceAttackComp.Target);

            IAttackObject nextAttackObj = attackMgr.SpawnAttackObject(
                attackObject.Output,
                new AttackObjectSpawnInput(
                    sourceAttackComp: sourceAttackComp,
                    sourceFactionID: sourceAttackComp.Entity.FactionID,
                    launcherSourceIndex: index,

                    spawnPosition: launchPosition.Position,
                    spawnRotation: Quaternion.Euler(launchRotationAngles),

                    target: sourceAttackComp.Target.instance,
                    targetPosition: targetPosition,

                    delayTime: launchDelayTime,
                    damageInDelay: damageInDelay,
                    delayParent: delayParentObject,

                    damageFriendly: sourceAttackComp.EngageOptions.engageFriendly
                    ));

            return nextAttackObj;
        }
    }
}

