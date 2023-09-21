using UnityEngine;

using RTSEngine.Utilities;

namespace RTSEngine.Effect
{
    public class EffectObjectPool : ObjectPool<IEffectObject, EffectObjectSpawnInput>, IEffectObjectPool
    {
        #region Initializing/Terminating
        protected sealed override void OnObjectPoolInit() 
        { 
        }
        #endregion

        #region Spawning Effect Objects
        public IEffectObject Spawn(IEffectObject prefab, EffectObjectSpawnInput input)
        {
            IEffectObject nextEffect = base.Spawn(prefab);
            if (!nextEffect.IsValid())
                return null;

            nextEffect.OnSpawn(input);

            return nextEffect;
        }

        public IEffectObject Spawn(IEffectObject prefab, Vector3 spawnPosition)
        {
            return Spawn(prefab, new EffectObjectSpawnInput(
                parent: null,

                useLocalTransform: false,
                spawnPosition: spawnPosition,
                spawnRotation: prefab.IsValid() ? prefab.transform.rotation : Quaternion.identity));
        }

        public IEffectObject Spawn(IEffectObject prefab, Transform parent)
        {
            return Spawn(prefab, new EffectObjectSpawnInput(
                parent: parent,

                useLocalTransform: true,
                spawnPosition: Vector3.zero,
                spawnRotation: Quaternion.identity));
        }
        #endregion
    }
}