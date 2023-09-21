using RTSEngine.UI;
using System;

namespace RTSEngine.Event
{
    public struct MessageEventArgs
    {
        public MessageType Type { get; }
        public string Message { get; }

        public bool CustomDurationEnabled { get; }
        public float CustomDuration { get; }

        public MessageEventArgs(MessageType type,
                                      string message,
                                      bool customDurationEnabled = false,
                                      float customDuration = 0.0f)
        {
            this.Type = type;
            this.Message = message;

            this.CustomDurationEnabled = customDurationEnabled;
            this.CustomDuration = customDuration;
        }
    }
}