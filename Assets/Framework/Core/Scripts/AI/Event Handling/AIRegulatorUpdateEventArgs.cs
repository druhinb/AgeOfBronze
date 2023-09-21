using System;

namespace RTSEngine.AI.Event
{
    public class NPCRegulatorUpdateEventArgs : EventArgs
    {
        public int Count { private set; get; }
        public int PendingAmount { private set; get; }

        public void AIRegulatorUpdateEventArgs(int count, int pendingAmount)
        {
            this.Count = count;

            this.PendingAmount = pendingAmount;
        }
    }
}
