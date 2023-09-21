using RTSEngine.Task;
using System;

namespace RTSEngine.Event
{
    public struct PendingTaskEventArgs
    {
        public PendingTask Data { get; }
        public int pendingQueueID { get; }
        public PendingTaskState State { get; }

        public PendingTaskEventArgs(PendingTask data, PendingTaskState state, int pendingQueueID = -1)
        {
            this.Data = data;
            this.pendingQueueID = pendingQueueID;
            this.State = state;
        }
    }
}
