using RTSEngine.Entities;
using RTSEngine.Model;
using System;

namespace RTSEngine.Event
{
    public class UnitCarrierEventArgs : EventArgs
    {
        public IUnit Unit { private set; get; }
        public ModelCacheAwareTransformInput Slot { private set; get; }
        public int SlotID { private set; get; }

        public UnitCarrierEventArgs(IUnit unit, ModelCacheAwareTransformInput slot, int slotID)
        {
            this.Unit = unit;
            this.Slot = slot;
            this.SlotID = slotID;
        }
    }
}
