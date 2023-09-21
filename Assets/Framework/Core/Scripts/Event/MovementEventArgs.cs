using RTSEngine.Movement;
using System;

namespace RTSEngine.Event
{
    public struct MovementEventArgs
    {
        public MovementSource Source { get; }

        public MovementEventArgs(MovementSource source)
        {
            this.Source = source;
        }
    }
}
