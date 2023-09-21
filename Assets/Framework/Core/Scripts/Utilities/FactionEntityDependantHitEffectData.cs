using RTSEngine.Audio;
using RTSEngine.Effect;
using RTSEngine.Entities;
using UnityEngine;

namespace RTSEngine.Utilities
{
    [System.Serializable]
        public struct FactionEntityDependantHitEffectData
        {
            [SerializeField, Tooltip("Define the unit and building entities for which the hit effect object below will be used.")]
            public FactionEntityTargetPicker picker;
            [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Triggered on the attack object when it hits a target of type that belongs to the types defined above."),]
            public GameObjectToEffectObjectInput effect;
            [SerializeField, Tooltip("If there is a hit effect assigned, rotate it to face the opposite of the target entity.")]
            public bool faceSource;
            [SerializeField, Tooltip("Played when the attack objects hits a target.")]
            public AudioClipFetcher audio;
        }
}
