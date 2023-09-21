using System;
using RTSEngine.ResourceExtension;

namespace RTSEngine.Event
{
    public class ResourceAmountEventArgs : EventArgs
    {
        public ResourceInput ResourceInput { get; }

        public ResourceAmountEventArgs(ResourceInput resourceInput)
        {
            ResourceInput = resourceInput;
        }
    }
}
