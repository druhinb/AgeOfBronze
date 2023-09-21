﻿using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Effect;

namespace RTSEngine.Attack
{
    public interface IAttackManager : IPreRunGameService
    {
        IEffectObject TerrainAttackTargetEffect { get; }

        bool CanAttackMoveWithKey { get; }
        IEffectObject AttackMoveTargetEffect { get; }
        IReadOnlyDictionary<string, IEnumerable<IAttackObject>> ActiveAttackObjects { get; }

        bool CanLaunchTerrainAttack<T>(LaunchAttackData<T> data);

        ErrorMessage LaunchAttack(LaunchAttackData<IEntity> data);
        ErrorMessage LaunchAttackLocal(LaunchAttackData<IEntity> data);

        ErrorMessage LaunchAttack(LaunchAttackData<IReadOnlyList<IEntity>> data);
        ErrorMessage LaunchAttackLocal(LaunchAttackData<IReadOnlyList<IEntity>> data);

        bool TryGetAttackPosition(IEntity attacker, IFactionEntity target, Vector3 targetPosition, bool playerCommand, out Vector3 attackPosition);

        IAttackObject SpawnAttackObject(IAttackObject prefab, AttackObjectSpawnInput input);
        void Despawn(IAttackObject instance, bool destroyed = false);
        bool TryGetAttackObjectPrefab(string code, out IAttackObject prefab);
    }
}