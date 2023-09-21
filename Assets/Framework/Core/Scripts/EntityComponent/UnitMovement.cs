﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Movement;
using RTSEngine.Animation;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Determinism;
using RTSEngine.Audio;
using RTSEngine.Terrain;
using RTSEngine.UnitExtension;
using RTSEngine.Game;

namespace RTSEngine.EntityComponent
{
    public class UnitMovement : FactionEntityTargetComponent<IEntity>, IMovementComponent 
    {
        #region Class Attributes
        protected IUnit unit { private set; get; }

        [SerializeField, Tooltip("Pick the terrain are types that the unit is able to move within. Leave empty to allow all terrain area types registered in the Terrain Manager.")]
        private TerrainAreaType[] movableTerrainAreas = new TerrainAreaType[0];
        public IReadOnlyList<TerrainAreaType> TerrainAreas => movableTerrainAreas;
        public TerrainAreaMask AreasMask { private set; get; }

        [SerializeField, Tooltip("Movement formation for this unit type.")]
        private MovementFormationSelector formation = new MovementFormationSelector { };
        /// <summary>
        /// Gets the MovementFormation struct that defines the movement formation for the unit.
        /// </summary>
        public MovementFormationSelector Formation => formation;

        [SerializeField, Tooltip("The lower the Movement Priority value is, the more units of this type will be prioritized when generating the path destinations.")]
        private int movementPriority = 0;
        /// <summary>
        /// The lower the Movement Priority value is, the more units of this type will be prioritized when generating the path destinations.
        /// This can be used to have units pick path destinations in front of other units with higher Movement Priority values in the case of row-based formation movement.
        /// </summary>
        public int MovementPriority => movementPriority;

        private bool isMoving;
        public override bool HasTarget => isMoving;
        public override bool IsIdle => !isMoving;

        /// <summary>
        /// An instance that extends the IMovementController interface which is responsible for computing the navigation path and moving the unit.
        /// </summary>
        public IMovementController Controller { private set; get; }

        /// <summary>
        /// The current corner that the unit is moving towards in its current path.
        /// </summary>
        private Vector3 NextCorner;

        /// <summary>
        /// Has the unit reached its current's path destination?
        /// </summary>
        public bool DestinationReached { private set; get; }
        //public Vector3 Destination => Target.position;
        public Vector3 Destination => Controller.Destination;

        [SerializeField, Tooltip("Default movement speed.")]
        private TimeModifiedFloat speed = new TimeModifiedFloat(10.0f);

        [SerializeField, Tooltip("How fast will the unit reach its movement speed?")]
        private TimeModifiedFloat acceleration = new TimeModifiedFloat(10.0f);

        [SerializeField, Tooltip("How fast does the unit rotate while moving?")]
        private TimeModifiedFloat mvtAngularSpeed = new TimeModifiedFloat(250.0f);

        [SerializeField, Tooltip("When disabled, the unit will have to rotate to face the next corner of the path before moving to it.")]
        private bool canMoveRotate = true; //can the unit rotate and move at the same time? 

        [SerializeField, Tooltip("If 'Can Move Rotate' is disabled, this value represents the angle that the unit must face in each corner of the path before moving towards it.")]
        private float minMoveAngle = 40.0f; //the close this value to 0.0f, the closer must the unit face its next destination in its path to move.
        private bool facingNextCorner = false; //is the unit facing the next corner on the path regarding the min move angle value?.

        [SerializeField, Tooltip("Can the unit rotate while not moving?")]
        private bool canIdleRotate = true; //can the unit rotate when not moving?
        [SerializeField, Tooltip("Is the idle rotation smooth or instant?")]
        private bool smoothIdleRotation = true;
        [SerializeField, Tooltip("How fast does the unit rotate while attempting to face its next corner in the path or while idle? Only if the idle rotation is smooth.")]
        private TimeModifiedFloat idleAngularSpeed = new TimeModifiedFloat(2.0f);

        //rotation helper fields.
        public Quaternion NextRotationTarget { private set; get; }

        /// <summary>
        /// The IMovementTargetPositionMarker instance assigned to the unit movement that marks the position that the unit is moving towards.
        /// </summary>
        public IMovementTargetPositionMarker TargetPositionMarker { get; private set; }

        [SerializeField, Tooltip("What audio clip to loop when the unit is moving?")]
        private AudioClipFetcher mvtAudio = new AudioClipFetcher(); //Audio clip played when the unit is moving.
        [SerializeField, Tooltip("What audio clip to play when is unable to move?")]
        private AudioClipFetcher invalidMvtPathAudio = new AudioClipFetcher(); //When the movement path is invalid, this audio is played.

        // Game services
        protected ITimeModifier timeModifier { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IMovementComponent, MovementEventArgs> MovementStart;
        public event CustomEventHandler<IMovementComponent, EventArgs> MovementStop;

        private void RaiseMovementStart (IMovementComponent sender, MovementEventArgs args)
        {
            var handler = MovementStart;
            handler?.Invoke(sender, args);
        }
        private void RaiseMovementStop (IMovementComponent sender)
        {
            var handler = MovementStop;
            handler?.Invoke(sender, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnTargetInit()
        {
            this.unit = Entity as IUnit;

            this.timeModifier = gameMgr.GetService<ITimeModifier>();

            this.timeModifier.ModifierUpdated += HandleTimeModifierUpdated;

            Controller = unit.gameObject.GetComponentInChildren<IMovementController>();

            if (!logger.RequireValid(this.unit,
              $"[{GetType().Name}] This component must be initialized with a valid instane of {typeof(IUnit).Name}!")

                || !logger.RequireValid(Controller,
                $"[{GetType().Name} - '{unit.Code}'] A component that implements the '{typeof(IMovementController).Name}' interface must be attached to the object.")

                || !logger.RequireValid(formation.type,
                $"[{GetType().Name} - '{unit.Code}'] The movement formation type must be assigned!")

                || !logger.RequireTrue(movableTerrainAreas.Length == 0 || movableTerrainAreas.All(area => area.IsValid()),
                $"[{GetType().Name} - '{unit.Code}'] The field 'Movable Terrain Areas' must be either empty or have valid elements assigned!"))
                return;

            formation.Init();

            AreasMask = gameMgr.GetService<ITerrainManager>().TerrainAreasToMask(TerrainAreas);

            Controller.Init(gameMgr, this, TimeModifiedControllerData);
            TargetPositionMarker = new UnitTargetPositionMarker(gameMgr, this);

            // Movement component requires no auto-target search
            TargetFinder.Enabled = false;

            isMoving = false;

            UpdateRotationTarget(Entity.transform.rotation);
        }

        protected override void OnTargetDisabled() 
        {
            TargetPositionMarker.Toggle(enable: false);
            this.timeModifier.ModifierUpdated -= HandleTimeModifierUpdated;
        }
        #endregion

        #region Handling Component Upgrade
        protected override void OnComponentUpgraded(FactionEntityTargetComponent<IEntity> sourceFactionEntityTargetComponent) 
        {
            // Reset animator state (to the same previous state) post upgrade
            unit.AnimatorController.SetState(unit.AnimatorController.CurrState);

            UnitMovement sourceMovementComp = sourceFactionEntityTargetComponent as UnitMovement;

            // Reset the rotation target as well
            UpdateRotationTarget(sourceMovementComp.NextRotationTarget);
        }
        #endregion

        #region Handling Event: Time Modifier Update
        public MovementControllerData TimeModifiedControllerData => new MovementControllerData
        {
            speed = speed.Value,
            acceleration = acceleration.Value,

            angularSpeed = mvtAngularSpeed.Value,
            stoppingDistance = mvtMgr.StoppingDistance
        };

        private void HandleTimeModifierUpdated(ITimeModifier sender, EventArgs args)
        {
            // Update the movement time modified values to keep up with the time modifier
            Controller.Data = TimeModifiedControllerData;
        }
        #endregion

        #region Updating Unit Movement State
        /// <summary>
        /// Handles updating the unit state whether it is in its idle or movement state.
        /// </summary>
        void FixedUpdate()
        {
            if (unit.Health.IsDead) //if the unit is already dead
                return; //do not update movement

            if (isMoving == false)
            {
                UpdateIdleRotation();
                return;
            }

            //to sync the unit's movement with its animation state, only handle movement if the unit is in its mvt animator state.
            if (!unit.AnimatorController.IsInMvtState == true)
                return;

            UpdateMovementRotation();

            if (Controller.LastSource.targetAddableUnit.IsValid() //we have an addable target
                //and it moved further away from the fetched addable position when the path was calculated and movement started.
                && Vector3.Distance(Controller.LastSource.targetAddableUnitPosition, Controller.LastSource.targetAddableUnit.GetAddablePosition(unit)) > mvtMgr.StoppingDistance)
            {
                OnHandleAddableUnitStop();
                return;
            }

            if (DestinationReached == false) //check if the unit has reached its target position or not
                if ((DestinationReached = IsPositionReached(Destination)))
                    OnHandleAddableUnitStop();
        }

        public bool IsPositionReached(Vector3 position)
            => Vector3.Distance(unit.transform.position, position) <= mvtMgr.StoppingDistance;

        public void OnHandleAddableUnitStop()
        {
            MovementSource lastSource = Controller.LastSource;
            Stop(); //stop the unit mvt

            if (lastSource.targetAddableUnit.IsValid()) //unit is supposed to be added to this instance.
            {
                //so that the unit does not look at the IAddableUnit entity after it is added.
                Target = new TargetData<IEntity> { opPosition = Target.opPosition };
                lastSource.targetAddableUnit.Add(
                    unit,
                    lastSource.sourceTargetComponent.IsValid() && lastSource.sourceTargetComponent != unit.CarriableUnit
                        ? new AddableUnitData(lastSource.sourceTargetComponent, playerCommand: false)
                        : unit.CarriableUnit.GetAddableData(playerCommand: false));
            }
        }

        /// <summary>
        /// Handles updating the unit's rotation while in idle state.
        /// </summary>
        private void UpdateIdleRotation ()
        {
            if (!canIdleRotate || NextRotationTarget == Quaternion.identity) //can the unit rotate when idle + there's a valid rotation target
                return;

            if (Target.instance.IsValid()) //if there's a target object to look at.
                NextRotationTarget = RTSHelper.GetLookRotation(unit.transform, Target.instance.transform.position); //keep updating the rotation target as the target object might keep changing position

            if (smoothIdleRotation)
                unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, NextRotationTarget, Time.deltaTime * idleAngularSpeed.Value);
            else
                unit.transform.rotation = NextRotationTarget;
        }

        /// <summary>
        /// Deactivates the movement controller and sets the unit's rotation target to the next corner in the path.
        /// </summary>
        private void EnableMovementRotation ()
        {
            facingNextCorner = false; //to trigger checking for correct rotation properties
            Controller.IsActive = false; //stop handling rotation using the movement controller

            NextCorner = Controller.NextPathTarget; //assign new corner in path
            //set the rotation target to the next corner.
            NextRotationTarget = RTSHelper.GetLookRotation(unit.transform, NextCorner);
        }

        /// <summary>
        /// Handles updating the unit's rotation while it is in its movement state.
        /// This mainly handles blocking the movement controller and rotating the unit if it is required to rotate toward its target before moving.
        /// </summary>
        private void UpdateMovementRotation()
        {
            if (canMoveRotate) //can move and rotate? do not proceed.
                return;

            if (NextCorner != Controller.NextPathTarget) //if the next corner/destination on path has been updated
                EnableMovementRotation();

            if (facingNextCorner) //facing next corner? we good
                return;

            if (Controller.IsActive) //stop movement it if it's not already stopped
                Controller.IsActive = false;

            //keep checking if the angle between the unit and its next destination
            Vector3 IdleLookAt = NextCorner - unit.transform.position;
            IdleLookAt.y = 0.0f;

            //as long as the angle is still over the min allowed movement angle, then do not proceed to keep moving
            //allow the controller to retake control of the movement if we're correctly facing the next path corner.
            if(facingNextCorner = Vector3.Angle(unit.transform.forward, IdleLookAt) <= minMoveAngle)
            { 
                Controller.IsActive = true;
                return;
            }

            //update the rotation as long as the unit is attempting to look at the next target in the path before it the Controller takes over movement (and rotation)
            unit.transform.rotation = Quaternion.Slerp(
                unit.transform.rotation,
                NextRotationTarget,
                Time.deltaTime * idleAngularSpeed.Value);
        }
        #endregion

        #region Updating Movement Target
        public override ErrorMessage SetTarget (SetTargetInputData input)
        {
            if(Entity.TasksQueue.IsValid() && Entity.TasksQueue.CanAdd(input))
            {
                return Entity.TasksQueue.Add(new SetTargetInputData 
                {
                    componentCode = Code,

                    target = input.target,
                    playerCommand = input.playerCommand,
                });
            }

            return mvtMgr.SetPathDestination(
                unit,
                input.target.position,
                0.0f,
                input.target.instance,
                new MovementSource { 
                    playerCommand = input.playerCommand,
                    isMoveAttackRequest = input.isMoveAttackRequest,
                    fromTasksQueue = input.fromTasksQueue });
        }

        public override ErrorMessage SetTargetLocal(SetTargetInputData input)
        {
            return mvtMgr.SetPathDestinationLocal(
                unit,
                input.target.position,
                0.0f,
                input.target.instance,
                new MovementSource { 
                    playerCommand = input.playerCommand,
                    isMoveAttackRequest = input.isMoveAttackRequest,
                    fromTasksQueue = input.fromTasksQueue });
        }

        public ErrorMessage SetTarget(TargetData<IEntity> newTarget, float stoppingDistance, MovementSource source)
        {
            return mvtMgr.SetPathDestination(
                unit, 
                newTarget.position, 
                stoppingDistance, 
                newTarget.instance,
                source);
        }

        public ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, float stoppingDistance, MovementSource source)
        {
            return mvtMgr.SetPathDestinationLocal(
                unit, 
                newTarget.position, 
                stoppingDistance, 
                newTarget.instance,
                source);
        }

        public ErrorMessage OnPathDestination(TargetData<IEntity> newTarget, MovementSource source)
        {
            Target = newTarget;
            TargetFromQueue = source.fromTasksQueue;

            Controller.Prepare(newTarget.position, source);

            isMoving = true; //player is now marked as moving

            //enable the target position marker and set the unit's current target destination to reserve it
            TargetPositionMarker.Toggle(true, Target.position);

            return ErrorMessage.none;
        }

        public void OnPathFailure()
        {
            unit.SetIdle(); //stop all unit activities in case path was supposed to be for a certain activity

            if (Controller.LastSource.playerCommand && RTSHelper.IsLocalPlayerFaction(unit)) //if the local player owns this unit and the player called this
                audioMgr.PlaySFX(invalidMvtPathAudio.Fetch());
        }

        public void OnPathPrepared(MovementSource source)
        {
            if (unit.AnimatorController?.CurrState == AnimatorState.moving) //if the unit was already moving, then lock changing the animator state briefly
                unit.AnimatorController.LockState = true;

            globalEvent.RaiseMovementStartGlobal(this);
            RaiseMovementStart(this, new MovementEventArgs(source));

            unit.SetIdle(source.sourceTargetComponent, false);

            DestinationReached = false; //destination is not reached by default

            if (unit.AnimatorController.IsValid())
            {
                unit.AnimatorController.LockState = false; //unlock animation state and play the movement anim
                unit.AnimatorController.SetState(AnimatorState.moving);
            }

            Controller.Launch();
            NextCorner = Controller.NextPathTarget; //set the current target destination corner

            if (!canMoveRotate) //can not move before facing the next corner in the path by a certain angle?
                EnableMovementRotation();

            if (Controller.LastSource.playerCommand && RTSHelper.IsLocalPlayerFaction(unit))
            {
                audioMgr.PlaySFX(unit.AudioSourceComponent, mvtAudio.Fetch(), true);
            }
        }

        protected override bool CanStopOnNoTarget() => false;
        /// <summary>
        /// Stops the current unit's movement.
        /// </summary>
        /// <param name="prepareNextMovement">When true, not all movement settings will be reset since a new movement command will be followed.</param>
        protected override void OnStop()
        {
            audioMgr.StopSFX(unit.AudioSourceComponent); //stop the movement audio from playing

            isMoving = false; //marked as not moving

            globalEvent.RaiseMovementStopGlobal(this);
            RaiseMovementStop(this);

            Controller.IsActive = false; 

            //update the next rotation target using the registered IdleLookAt position for the idle rotation.
            //only do this once the unit stops moving in case there's no IdleLookAt object.
            UpdateRotationTarget(
                LastTarget.instance,
                LastTarget.instance.IsValid() ? LastTarget.instance.transform.position : LastTarget.opPosition
            );

            TargetPositionMarker.Toggle(true, unit.transform.position);

            if (!unit.Health.IsDead) //if the unit is not dead
            {
                unit.AnimatorController?.SetState(AnimatorState.idle); //get into idle state

                if (!Controller.LastSource.sourceTargetComponent.IsValid())
                    unit.SetIdle(exception: this, includeMovement: false);
            }
        }

        public override ErrorMessage IsTargetValid(TargetData<IEntity> potentialTarget, bool playerCommand) => ErrorMessage.none;
        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target) => true;
        public override bool CanSearch => false;

        public void UpdateRotationTarget(IEntity rotationTarget, Vector3 rotationPosition, bool lookAway = false, bool setImmediately = false)
        {
            Target = new TargetData<IEntity>
            {
                position = Target.position,

                instance = rotationTarget,
                opPosition = rotationPosition
            };

            UpdateRotationTarget(
                RTSHelper.GetLookRotation(unit.transform, Target.opPosition, reversed: lookAway, fixYRotation: true),
                setImmediately);
        }

        // Updating idle rotation
        public void UpdateRotationTarget (Quaternion targetRotation, bool setImmediately = false)
        {
            NextRotationTarget = targetRotation;

            if (setImmediately)
                unit.transform.rotation = NextRotationTarget;
        }
        #endregion
    }
}
