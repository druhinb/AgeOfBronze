using UnityEngine;

namespace RTSEngine.Health
{
    [System.Serializable]
    public struct DamageOverTimeData
    {
        [Tooltip("Does the DoT keep going until the target is destroyed?")]
        public bool infinite;
        [Tooltip("How frequent will damage be dealt?")]
        public float cycleDuration;
        [Tooltip("If the DoT is not infinite, this is the amount of cycles it is going to deal damage to the target before it is disabled, with cycle last the duration assigned in the above field.")]
        public int cycles;
    }
}
