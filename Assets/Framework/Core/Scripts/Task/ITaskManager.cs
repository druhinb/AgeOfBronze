using RTSEngine.EntityComponent;
using RTSEngine.Game;
using System.Collections.Generic;

namespace RTSEngine.Task
{
    public interface ITaskManager : IPreRunGameService
    {
        EntityComponentAwaitingTask AwaitingTask { get; }
        IReadOnlyDictionary<int, Dictionary<string, Dictionary<int, EntityComponentTaskInputData>>> EntityComponentTaskInputInitialData { get; }
        bool IsTaskQueueEnabled { get; }

        IReadOnlyDictionary<IEntityComponent, Dictionary<int, EntityComponentTaskInputData>> EntityComponentTaskInputTrackerToData();
        void ResetEntityComponentTaskInputInitialData(IReadOnlyDictionary<int, Dictionary<string, Dictionary<int, EntityComponentTaskInputData>>> newInitialData);
        bool TryGetEntityComponentTaskInputInitialData(IEntityComponent sourceComponent, int taskID, out EntityComponentTaskInputData data);
    }
}