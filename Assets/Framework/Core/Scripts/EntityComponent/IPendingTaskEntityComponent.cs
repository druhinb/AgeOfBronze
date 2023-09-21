using RTSEngine.Event;
using RTSEngine.Task;
using RTSEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IPendingTaskEntityComponent: IEntityComponent
    {
        IReadOnlyList<IEntityComponentTaskInput> Tasks { get; }

        ErrorMessage LaunchTaskAction(int taskID, bool playerCommand);

        void OnPendingTaskPreComplete(PendingTask pendingTask);
        void OnPendingTaskCompleted(PendingTask pendingTask);
        void OnPendingTaskCancelled(PendingTask pendingTask);

        event CustomEventHandler<IPendingTaskEntityComponent, PendingTaskEventArgs> PendingTaskAction;

    }
}
