using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;

namespace RTSEngine.Movement
{
    public struct PathDestinationInputData
    {
        public IMovementComponent refMvtComp;

        public TargetData<IEntity> target;
        public Vector3 direction;

        // Maximum distance between the generated path destination and the target position
        public float maxDistance;

        public MovementSource source;
        public MovementFormationSelector formationSelector;

        public System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition;

        public bool playerCommand;
    }
}
