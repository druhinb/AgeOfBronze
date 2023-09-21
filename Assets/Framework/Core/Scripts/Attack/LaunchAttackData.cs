
using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.Attack
{
    public struct LaunchAttackData<T>
    {
        public T source;

        public IFactionEntity targetEntity;

        public Vector3 targetPosition;

        public bool playerCommand;

        public bool isMoveAttackRequest;

        public bool allowTerrainAttack;

        public LaunchAttackBooleans BooleansToMask()
        {
            LaunchAttackBooleans nextMask = LaunchAttackBooleans.none; 
            if (isMoveAttackRequest)
                nextMask |= LaunchAttackBooleans.isMoveAttackRequest;
            if (allowTerrainAttack)
                nextMask |= LaunchAttackBooleans.allowTerrainAttack;

            return nextMask;
        }
    }

    public enum LaunchAttackBooleans 
    {
        none = 0,
        isMoveAttackRequest = 1 << 0,
        allowTerrainAttack = 1 << 2,
        all = ~0
    };
}