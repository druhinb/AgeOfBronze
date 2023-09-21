using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.UnitExtension;
using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RTSEngine.Movement
{
    public struct MovementSource
    {
        public bool playerCommand;
        public IEntityTargetComponent sourceTargetComponent;

        //IAddableUnit component that initiated movement and the position it wants to add the unit at.
        //if assigned, the entity's movement goal will be to get added to this component.
        public IAddableUnit targetAddableUnit;
        public Vector3 targetAddableUnitPosition;

        /// <summary>
        /// True when the movement is requesting to start a new move-attack chain. 
        /// </summary>
        public bool isMoveAttackRequest;
        /// <summary>
        /// True when the movement is part of an attack-move command chain that was initiated by the player.
        /// </summary>
        public bool inMoveAttackChain;
        /// <summary>
        /// True when the unit attack component moves the unit after it finishes attacking its current target when attack-move was enabled by the player for the unit in a previous movement command.
        /// </summary>
        public bool isMoveAttackSource;
        /// <summary>
        /// True when the movement command is launched from a tasks queue
        /// </summary>
        public bool fromTasksQueue;

        public bool IsTargetDestinationValid(TargetData<IEntity> target)
        {
            return sourceTargetComponent.IsValid()
                ? sourceTargetComponent.IsTargetInRange(target.position, target)
                : true;
        }

        public MovementSourceBooleans BooleansToMask()
        {
            MovementSourceBooleans nextMask = MovementSourceBooleans.none; 
            if (isMoveAttackRequest)
                nextMask |= MovementSourceBooleans.isMoveAttackRequest;
            if (inMoveAttackChain)
                nextMask |= MovementSourceBooleans.inMoveAttackChain;
            if (isMoveAttackSource)
                nextMask |= MovementSourceBooleans.isMoveAttackSource;
            if (fromTasksQueue)
                nextMask |= MovementSourceBooleans.fromTasksQueue;

            return nextMask;
        }
    }

    public enum MovementSourceBooleans 
    {
        none = 0,
        inMoveAttackChain = 1 << 0,
        isMoveAttackRequest = 1 << 1,
        isMoveAttackSource = 1 << 2,
        fromTasksQueue = 1 << 3,
        all = ~0
    };

}
