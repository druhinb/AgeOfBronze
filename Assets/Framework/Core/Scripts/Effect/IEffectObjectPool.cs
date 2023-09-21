using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Effect
{
    public interface IEffectObjectPool : IPreRunGameService
    {
        IReadOnlyDictionary<string, IEnumerable<IEffectObject>> ActiveDic { get; }

        IEffectObject Spawn(IEffectObject prefab, EffectObjectSpawnInput input);
        IEffectObject Spawn(IEffectObject prefab, Vector3 spawnPosition);

        void Despawn(IEffectObject instance, bool destroyed = false);
        IEffectObject Spawn(IEffectObject prefab, Transform parent);
    }
}