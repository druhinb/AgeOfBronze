using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System;

namespace RTSEngine.Event
{
    public struct TargetDataEventArgs
    {
        public TargetData<IEntity> Data { get; }

        public TargetDataEventArgs (TargetData<IEntity> data)
        {
            this.Data = data;
        }
    }
}
