using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Movement;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Terrain;

namespace RTSEngine.EntityComponent
{
    public interface IMovementComponent : IEntityTargetComponent
    {
        bool DestinationReached { get; }
        Vector3 Destination { get; }
        TargetData<IEntity> Target { get; }

        IReadOnlyList<TerrainAreaType> TerrainAreas { get; }
        TerrainAreaMask AreasMask { get; }

        MovementFormationSelector Formation { get; }

        IMovementController Controller { get; }

        IMovementTargetPositionMarker TargetPositionMarker { get; }
        int MovementPriority { get; }

        event CustomEventHandler<IMovementComponent, MovementEventArgs> MovementStart;
        event CustomEventHandler<IMovementComponent, EventArgs> MovementStop;

        ErrorMessage SetTarget(TargetData<IEntity> newTarget, float stoppingDistance, MovementSource source);
        ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, float stoppingDistance, MovementSource source);

        void OnPathFailure();
        void OnPathPrepared(MovementSource source);

        ErrorMessage OnPathDestination(TargetData<IEntity> newTarget, MovementSource source);

        bool IsPositionReached(Vector3 position);

        void UpdateRotationTarget(IEntity rotationTarget, Vector3 rotationPosition, bool lookAway = false, bool setImmediately = false);
        void UpdateRotationTarget(Quaternion targetRotation, bool setImmediately = false);
    }
}
