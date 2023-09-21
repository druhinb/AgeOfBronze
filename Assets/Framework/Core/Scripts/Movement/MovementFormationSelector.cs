using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Movement
{
    [System.Serializable]
    public class MovementFormationSelector
    {

#if UNITY_EDITOR
        [HideInInspector]
        public MovementFormationType lastType;
#endif

        [Tooltip("What movement formation type to use?")]
        public MovementFormationType type;

        [Tooltip("Assign values for the formation properties")]
        public MovementFormationData properties;

        public IReadOnlyDictionary<string, float> DefaultFloatProperties { get; private set; }
        public IReadOnlyDictionary<string, float> CurrentFloatProperties { get; private set; }

        public IReadOnlyDictionary<string, int> DefaultIntProperties { get; private set; }
        public IReadOnlyDictionary<string, int> CurrentIntProperties { get; private set; }

        public void Init()
        {
            DefaultFloatProperties = type.DefaultFloatProperties
                .ToDictionary(prop => prop.name, prop => prop.value);
            CurrentFloatProperties = properties.floatProperties
                .ToDictionary(prop => prop.name, prop => prop.value);

            DefaultIntProperties = type.DefaultIntProperties
                .ToDictionary(prop => prop.name, prop => prop.value);
            CurrentIntProperties = properties.intProperties
                .ToDictionary(prop => prop.name, prop => prop.value);
        }

        public float GetFloatPropertyValue(string propName, string fallbackPropName = default)
        {
            if (!CurrentFloatProperties.IsValid()
                || !DefaultFloatProperties.IsValid())
                Init();

            return GetFormationPropertyValue(CurrentFloatProperties, DefaultFloatProperties, propName, fallbackPropName);
        }

        public int GetIntPropertyValue(string propName, string fallbackPropName = default)
        { 
            if (!CurrentIntProperties.IsValid()
                || !DefaultIntProperties.IsValid())
                Init();

            return GetFormationPropertyValue(CurrentIntProperties, DefaultIntProperties, propName, fallbackPropName);
        }

        private T GetFormationPropertyValue<T> (IReadOnlyDictionary<string, T> propDic, IReadOnlyDictionary<string, T> defaultPropDic, string propName, string fallbackPropName = default)
        {

            // Requested prop is found in the values propDic
            if (propDic.ContainsKey(propName))
                return propDic[propName];
            // Requested prop is not found but the fallback one is available
            else if (fallbackPropName != default && propDic.ContainsKey(fallbackPropName))
                return propDic[fallbackPropName];
            // The requested prop and fall back one are not found so we get the default value of the requested prop
            else if (RTSHelper.LoggingService.RequireTrue(defaultPropDic.ContainsKey(propName),
              $"[{GetType().Name}] Unable to find formation property '{propName}' in the default properties."))
                return defaultPropDic[propName];

            return default;
        }
    }
}
