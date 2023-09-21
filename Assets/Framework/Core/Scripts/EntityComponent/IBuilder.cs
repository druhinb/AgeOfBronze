using RTSEngine.Entities;
using System.Collections.Generic;

namespace RTSEngine.EntityComponent
{
    public interface IBuilder : IEntityTargetComponent
    {
        TargetData<IBuilding> Target { get; }

        bool InProgress { get; }

        IReadOnlyList<BuildingCreationTask> CreationTasks { get; }
        IReadOnlyList<BuildingCreationTask> UpgradeTargetCreationTasks { get; }
    }
}
