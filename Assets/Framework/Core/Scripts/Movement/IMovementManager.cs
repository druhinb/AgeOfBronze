using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Movement
{
    public interface IMovementManager : IPreRunGameService
    {
        float StoppingDistance { get; }

        IEffectObject MovementTargetEffect { get; }

        IMovementSystem MvtSystem { get; }

        ErrorMessage SetPathDestination(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source);
        ErrorMessage SetPathDestinationLocal(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source);

        ErrorMessage SetPathDestination(IReadOnlyList<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, MovementSource source);
        ErrorMessage SetPathDestinationLocal(IReadOnlyList<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, MovementSource source);

        ErrorMessage GeneratePathDestination(IEntity entity, TargetData<IEntity> target, MovementFormationSelector formationSelector, float offset, MovementSource source, ref List<Vector3> pathDestinations, Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null);
        ErrorMessage GeneratePathDestination(IReadOnlyList<IEntity> entities, TargetData<IEntity> target, MovementFormationSelector formationSelector, float offset, MovementSource source, ref List<Vector3> pathDestinations, Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null);
        ErrorMessage GeneratePathDestination(IEntity refMvtSource, int amount, Vector3 direction, TargetData<IEntity> target, MovementFormationSelector formationSelector, float offset, MovementSource source, ref List<Vector3> pathDestinations, Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null);

        bool TryGetMovablePosition(Vector3 center, float radius, LayerMask areaMask, out Vector3 movablePosition);
        bool GetRandomMovablePosition(IEntity entity, Vector3 origin, float range, out Vector3 targetPosition, bool playerCommand);

        ErrorMessage IsPositionClear(ref Vector3 targetPosition, float agentRadius, LayerMask navAreaMask, TerrainAreaMask areasMask, bool playerCommand);
        ErrorMessage IsPositionClear(ref Vector3 targetPosition, IMovementComponent refMvtComp, bool playerCommand);
    }
}