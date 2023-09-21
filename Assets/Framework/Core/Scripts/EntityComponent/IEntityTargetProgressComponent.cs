using System;

using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
    [Serializable]
    public struct EntityTargetComponentProgressData
    {
        public float progressTime;
    }

    public interface IEntityTargetProgressComponent : IEntityTargetComponent
    {
        bool InProgress { get; }
        EntityTargetComponentProgressData ProgressData { get; }
    }
}
