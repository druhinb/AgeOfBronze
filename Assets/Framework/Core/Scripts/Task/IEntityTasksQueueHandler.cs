using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System.Collections.Generic;

namespace RTSEngine.Task
{
    public interface IEntityTasksQueueHandler : IEntityComponent
    {
        int QueueCount { get; }
        IEnumerable<SetTargetInputData> Queue { get; }
        bool IsRunningQueueTask { get; }
        string RunningQueueTaskCompCode { get; }

        ErrorMessage Add(SetTargetInputData newTask);
        bool CanAdd(SetTargetInputData input);
        void Clear();
    }
}