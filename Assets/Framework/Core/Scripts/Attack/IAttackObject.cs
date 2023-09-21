using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Attack
{
    public interface IAttackObject : IEffectObject
    {
        AttackObjectSpawnInput Data { get; }
        bool InDelay { get; }
        float DelayTime { get; }

        void OnSpawn(AttackObjectSpawnInput input);
    }
}
