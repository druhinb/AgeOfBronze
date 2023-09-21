using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Movement
{
    [System.Serializable]
    public struct MovementFormationData
    {
        [SerializeField, Tooltip("Create properties of type 'float' for this formation. Make sure each property has a unique name!")]
        public MovementFormationPropertyFloat[] floatProperties;

        [SerializeField, Tooltip("Create properties of type 'int' for this formation. Make sure each property has a unique name!")]
        public MovementFormationPropertyInt[] intProperties;
    }
}
