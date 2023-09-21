using RTSEngine.Model;
using RTSEngine.Utilities;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace RTSEngine.Logging
{
    public class LoggerBase : MonoBehaviour
    {
        [SerializeField]
        private bool showErrors = true;
        [SerializeField]
        private bool showWarnings = true;
        [SerializeField]
        private bool showInfo = true;

        public void LogError(string message, IMonoBehaviour source = null) => Log(message, source, LoggingType.error);

        public void LogWarning(string message, IMonoBehaviour source = null) => Log(message, source, LoggingType.warning);

        public void Log(string message, IMonoBehaviour source = null, LoggingType type = LoggingType.info)
        {
            message = source.IsValid()
                ? $"*RTS ENGINE - SOURCE: {source.GetType().Name}* {message}"
                : $"*RTS ENGINE* {message}";

            switch (type)
            {
                case LoggingType.info:
                    if(showInfo)
                        Debug.Log(message, source as Object);
                    break;

                case LoggingType.warning:
                    if(showWarnings)
                        Debug.LogWarning(message, source as Object);
                    break;

                case LoggingType.error:
                    if(showErrors)
                        Debug.LogError(message, source as Object);
                    break;
            }
        }

        public bool RequireValid(object target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(Object target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(IMonoBehaviour target, string message, IMonoBehaviour source, LoggingType type = LoggingType.error)
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(IEnumerable<IMonoBehaviour> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.All(instance => instance.IsValid()))
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid(IEnumerable<Object> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (!target.All(instance => instance.IsValid()))
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid<T>(ModelCacheAwareInput<T> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error) where T : Component
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireValid<T>(GameObjectToComponentInput<T> target, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error) where T : IMonoBehaviour 
        {
            if (!target.IsValid())
            {
                Log(message, source, type);
                return false;
            }

            return true;
        }

        public bool RequireTrue(bool condition, string message, IMonoBehaviour source = null, LoggingType type = LoggingType.error)
        {
            if (condition)
                return true;

            Log(message, source, type);
            return false;
        }
    }
}
