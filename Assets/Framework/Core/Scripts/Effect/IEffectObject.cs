using UnityEngine;

using RTSEngine.Utilities;

namespace RTSEngine.Effect
{

    public interface IEffectObject : IPoolableObject 
    {
        EffectObjectState State { get; }

        AudioSource AudioSourceComponent { get; }
        FollowTransform FollowTransform { get; }
        float CurrLifeTime { get; }

        void OnSpawn(EffectObjectSpawnInput input);
        void Deactivate(bool useDisableTime = true);
    }
}
