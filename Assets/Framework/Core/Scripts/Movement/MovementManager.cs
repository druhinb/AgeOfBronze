using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Terrain;
using RTSEngine.Search;
using RTSEngine.Attack;
using RTSEngine.Audio;
using RTSEngine.Utilities;

namespace RTSEngine.Movement
{
    public class MovementManager : MonoBehaviour, IMovementManager
    {
        #region Attributes
        [SerializeField, Tooltip("Determines the distance at which a unit stops before it reaches its movement target position.")]
        private float stoppingDistance = 0.3f;
        public float StoppingDistance => stoppingDistance;

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Visible to the local player when they command unit(s) to move to a location.")]
        private GameObject movementTargetEffectPrefab = null;
        public IEffectObject MovementTargetEffect { get; private set; }

        // Collection used to store and pass path destination between methods in this script
        // Having it as a member variable makes more sense to avoid the garbage produced by creating and destroying this list every time we are generating path destinations
        private List<Vector3> pathDestinations;
        private const int PATH_DESTINATIONS_DEFAULT_CAPACITY = 50;

        // When generating path destinations, this hashset holds the formation types that this manager used to generate path destinations
        // In case two formation types are the fallback formation types of each other, without knowing that one was already tried before the other one...
        // ...it would generate an endless loop if both formation types are unable to generate enough valid path destinations
        private HashSet<MovementFormationType> attemptedFormationTypes;

        /// <summary>
        /// Handles connecting the pathfinding system and the RTS Engine movement system.
        /// </summary>
        public IMovementSystem MvtSystem { private set; get; }

        private IReadOnlyDictionary<MovementFormationType, IMovementFormationHandler> formationHandlers = null;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGridSearchHandler gridSearch { private set; get; }
        protected IAttackManager attackMgr { private set; get; } 
        protected IGameAudioManager audioMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.gridSearch = gameMgr.GetService<IGridSearchHandler>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>(); 

            MvtSystem = gameObject.GetComponent<IMovementSystem>();

            if (!MvtSystem.IsValid())
            {
                logger.LogError("[MovementManager] A component that implements the 'IMovementSystem' interface interface must be attached to the object.");
                return;
            }

            pathDestinations = new List<Vector3>(PATH_DESTINATIONS_DEFAULT_CAPACITY);
            attemptedFormationTypes = new HashSet<MovementFormationType>();

            if (movementTargetEffectPrefab.IsValid())
                this.MovementTargetEffect = movementTargetEffectPrefab.GetComponent<IEffectObject>();

            formationHandlers = gameObject
                .GetComponents<IMovementFormationHandler>()
                .ToDictionary(handler =>
                {
                    handler.Init(gameMgr);

                    return handler.FormationType;
                });
        }
        #endregion

        #region Setting Path Destination Helper Methods
        private void OnPathDestinationCalculationStart (IEntity entity)
        {
            // Disable the target position marker so it won't intefer in determining the target positions
            entity.MovementComponent.TargetPositionMarker.Toggle(false);
        }
        private void OnPathDestinationCalculationStart (IReadOnlyList<IEntity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
                // Disable the target position marker so it won't intefer in determining the target positions
                entities[i].MovementComponent.TargetPositionMarker.Toggle(false);
        }

        private void OnPathDestinationCalculationInterrupted (IEntity entity)
        {
            entity.MovementComponent.TargetPositionMarker.Toggle(true);
        }
        private void OnPathDestinationCalculationInterrupted (IReadOnlyList<IEntity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
                entities[i].MovementComponent.TargetPositionMarker.Toggle(true);
        }
        #endregion

        #region Setting Path Destination: Single Entity
        public ErrorMessage SetPathDestination(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source)
        {
            return inputMgr.SendInput(
                new CommandInput()
                {
                    sourceMode = (byte)InputMode.entity,
                    targetMode = (byte)InputMode.movement,

                    sourcePosition = entity.transform.position,
                    targetPosition = destination,

                    floatValue = offsetRadius,

                    // MovementSource:
                    code = $"{source.sourceTargetComponent?.Code}" +
                    $"{RTSHelper.STR_SEPARATOR_L1}{source.targetAddableUnit?.Code}",
                    opPosition = source.targetAddableUnitPosition,
                    playerCommand = source.playerCommand,
                    intValues = inputMgr.ToIntValues((int)source.BooleansToMask())
                    //intValues = inputMgr.ToIntValues(source.isAttackMove ? 1 : 0, source.isOriginalAttackMove ? 1 : 0)
                },
                source: entity,
                target: target);
        }

        public ErrorMessage SetPathDestinationLocal(IEntity entity, Vector3 destination, float offsetRadius, IEntity target, MovementSource source)
        {
            if (!entity.IsValid())
            {
                logger.LogError("[MovementManager] Can not move an invalid entity!");
                return ErrorMessage.invalid;
            }
            else if (!entity.CanMove(source.playerCommand))
                return ErrorMessage.mvtDisabled;

            OnPathDestinationCalculationStart(entity);

            // Used for the movement target effect and rotation look at of the unit
            Vector3 originalDestination = destination;
            TargetData <IEntity> targetData = new TargetData<IEntity>
            {
                instance = target,
                position = destination,
                opPosition = originalDestination
            };

            // First check if the actual destination is a valid target position, if it can't be then search for a valid one depending on the movement formation
            // If the offset radius is not zero, then the unit will be moving towards a target entity and a calculation for a path destination around that target is required
            if (offsetRadius > 0.0f
                || IsPositionClear(ref destination, entity.MovementComponent, source.playerCommand) != ErrorMessage.none)
            {
                targetData.position = destination;

                GeneratePathDestination(
                    entity,
                    targetData,
                    entity.MovementComponent.Formation,
                    offsetRadius,
                    source,
                    ref pathDestinations);

                if (pathDestinations.Count == 0)
                {
                    OnPathDestinationCalculationInterrupted(entity);
                    //logger.LogError($"[Movement Manager] Unable to determine path destination! Please follow error trace to find the movement's source!");
                    return ErrorMessage.mvtTargetPositionNotFound;
                }

                // Get the closest target position
                //destination = pathDestinations.OrderBy(pos => (pos - entity.transform.position).sqrMagnitude).First();
                pathDestinations.Sort((pos1, pos2) => (pos1 - entity.transform.position).sqrMagnitude.CompareTo((pos2 - entity.transform.position).sqrMagnitude));
                destination = pathDestinations[0];
            }

            if (source.playerCommand && !target.IsValid() && RTSHelper.IsLocalPlayerFaction(entity))
            {
                SpawnMovementTargetEffect(entity, originalDestination, source);

                audioMgr.PlaySFX(entity.MovementComponent.OrderAudio, false); //play the movement audio.
            }

            return entity.MovementComponent.OnPathDestination(
                new TargetData<IEntity>
                {
                    instance = target,
                    position = destination,
                    opPosition = originalDestination
                },
                source);
        }
        #endregion

        #region Setting Path Destination: Multiple Entities
        public ErrorMessage SetPathDestination(IReadOnlyList<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, MovementSource source)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entityGroup,
                targetMode = (byte)InputMode.movement,

                targetPosition = destination,
                floatValue = offsetRadius,

                playerCommand = source.playerCommand,
                intValues = inputMgr.ToIntValues((int)source.BooleansToMask())
            },
            source: entities,
            target: target);
        }

        public ErrorMessage SetPathDestinationLocal(IReadOnlyList<IEntity> entities, Vector3 destination, float offsetRadius, IEntity target, MovementSource source)
        {
            if (!entities.IsValid())
            {
                logger.LogError("[MovementManager] Some or all entities that are attempting to move are invalid!!");
                return ErrorMessage.invalid;
            }
            // Only one entity to move? use the dedicated method instead!
            else if (entities.Count < 2) 
                return SetPathDestinationLocal(entities[0], destination, offsetRadius, target, source);

            // Sort the attack units based on their codes, we assume that units that share the same code (which is the defining property of an entity in the RTS Engine) are identical.
            // Additionally, filter out any units that are not movable.

            // CHANGE ME: In case the below OrderBy call is creating too much garbage, it needs to change
            // Maybe using a pre existing list that gets cleared everytime but holds the same capacity would help? But how to handle the simple sorting?
            var sortedMvtSources = RTSHelper.SortEntitiesByCode(
                entities,
                entity => entity.CanMove(source.playerCommand))
                .Values
                .OrderBy(mvtSourceSet => mvtSourceSet[0].MovementComponent.MovementPriority)
                .ToList();


            TargetData <IEntity> targetData = new TargetData<IEntity>
            {
                instance = target,
                position = destination,
            };

            for (int i = 0; i < sortedMvtSources.Count; i++) 
            {
                List<IEntity> mvtSourceSet = sortedMvtSources[i];

                OnPathDestinationCalculationStart(mvtSourceSet);

                GeneratePathDestination(
                    mvtSourceSet,
                    targetData,
                    mvtSourceSet[0].MovementComponent.Formation,
                    offsetRadius,
                    source,
                    ref pathDestinations);

                if (pathDestinations.Count == 0)
                {
                    OnPathDestinationCalculationInterrupted(mvtSourceSet);
                    //logger.LogError($"[Movement Manager] Unable to determine path destination! Please follow error trace to find the movement's source!");
                    return ErrorMessage.mvtTargetPositionNotFound;
                }

                // Compute the directions of the units we have so we know the direction they will face in regards to the target.
                Vector3 unitsDirection = RTSHelper.GetEntitiesDirection(entities, destination);
                unitsDirection.y = 0;

                // Index counter for the generated path destinations.
                int destinationID = 0;
                // Index for the entities in the current set
                int j = 0;

                for (j = 0; j < mvtSourceSet.Count; j++) 
                {
                    IEntity mvtSource = mvtSourceSet[j];

                    // If this movement is towards a target, pick the closest position to the target for each unit
                    if (target.IsValid())
                    {
                        //pathDestinations.OrderBy(pos => (pos - mvtSource.transform.position).sqrMagnitude).ToList();
                        pathDestinations.Sort((pos1, pos2) => (pos1 - mvtSource.transform.position).sqrMagnitude.CompareTo((pos2 - mvtSource.transform.position).sqrMagnitude));
                    }

                    if (mvtSource.MovementComponent.OnPathDestination(
                        new TargetData<IEntity>
                        {
                            instance = target,
                            position = pathDestinations[destinationID],

                            opPosition = pathDestinations[destinationID] + unitsDirection // Rotation look at position
                        },
                        source) != ErrorMessage.none)
                    {
                        OnPathDestinationCalculationInterrupted(mvtSource);
                        continue;
                    }

                    // Only move to the next path destination if we're moving towards a non target, if not keep removing the first element of the list which was the closest to the last unit
                    if (target.IsValid())
                        pathDestinations.RemoveAt(0);
                    else
                        destinationID++;

                    if (destinationID >= pathDestinations.Count) // No more paths to test, stop moving units.
                        break;
                }

                // If no path destinations could be assigned to the rest of the units, interrupt their path calculation state
                if(j < mvtSourceSet.Count)
                    OnPathDestinationCalculationInterrupted(mvtSourceSet.GetRange(j + 1, mvtSourceSet.Count - (j + 1)));
            }


            if (source.playerCommand && !target.IsValid() && RTSHelper.IsLocalPlayerFaction(entities.FirstOrDefault()))
            {
                IEntity refEntity = entities.First();
                SpawnMovementTargetEffect(refEntity, destination, source);

                audioMgr.PlaySFX(refEntity.MovementComponent.OrderAudio, false); //play the movement audio.
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Generating Path Destinations
        public ErrorMessage GeneratePathDestination(IEntity entity, TargetData<IEntity> target, MovementFormationSelector formationSelector, float offset, MovementSource source, ref List<Vector3> pathDestinations, System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null)
            => GeneratePathDestination(
                entity,
                1,
                (target.position - entity.transform.position).normalized,
                target,
                formationSelector,
                offset,
                source,
                ref pathDestinations,
                condition
        );

        public ErrorMessage GeneratePathDestination(IReadOnlyList<IEntity> entities, TargetData<IEntity> target, MovementFormationSelector formationSelector, float offset, MovementSource source, ref List<Vector3> pathDestinations, System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null)
            => GeneratePathDestination(
                entities[0],
                entities.Count(),
                RTSHelper.GetEntitiesDirection(entities, target.position),
                target,
                formationSelector,
                offset,
                source,
                ref pathDestinations,
                condition
        );


        // refMvtSource: The unit that will be used as a reference to the rest of the units of the same type.
        // amount: The amount of path destinations that we want to produce.
        // direction: the direction the units will face in regards to the target.
        public ErrorMessage GeneratePathDestination(IEntity refMvtSource, int amount, Vector3 direction, TargetData<IEntity> target, MovementFormationSelector formationSelector, float offset, MovementSource source, ref List<Vector3> pathDestinations, System.Func<PathDestinationInputData, Vector3, ErrorMessage> condition = null)
        {
            // ASSUMPTIONS: All entities are of the same type.

            // Ref list must be already initialized.
            pathDestinations.Clear();
            attemptedFormationTypes.Clear();

            if (!formationSelector.type.IsValid())
            {
                logger.LogError($"[MovementManager] Requesting path destinations for entity of code '{refMvtSource.Code}' with invalid formation type!");
                return ErrorMessage.invalid;
            }
            else if (!formationHandlers.ContainsKey(formationSelector.type))
            {
                logger.LogError($"[MovementManager] Requesting path destinations for formation of type: '{formationSelector.type.Key}' but no suitable component that implements '{typeof(IMovementFormationHandler).Name}' is found!");
                return ErrorMessage.invalid;
            }

            // Depending on the ref entity's movable terrain areas, adjust the target position
            terrainMgr.GetTerrainAreaPosition(target.position, refMvtSource.MovementComponent.TerrainAreas, out target.position);

            ErrorMessage errorMessage;

            // We want to handle setting the height by sampling the terrain to get the correct height since there's no way to know it directly.
            // There we keep this direction position value on the y axis to 0
            direction.y = 0;

            // Holds the amount of attempts made to generate path destinations but resulted in no generated positions.
            int emptyAttemptsCount = 0;
            // In case the attack formation is switched due to max empty attempts or an error then we want to reset the offset.
            float originalOffset = offset;

            while (amount > 0)
            {
                // In case the path destination generation methods result into a failure, return with the failure's error code.
                if ((errorMessage = formationHandlers[formationSelector.type].GeneratePathDestinations(
                    new PathDestinationInputData
                    {
                        refMvtComp = refMvtSource.MovementComponent,

                        target = target,
                        direction = direction,

                        source = source,
                        formationSelector = formationSelector,

                        condition = condition,
                        
                        playerCommand = source.playerCommand
                    },
                    ref amount,
                    ref offset,
                    ref pathDestinations,
                    out int generatedAmount)) != ErrorMessage.none || emptyAttemptsCount >= formationHandlers[formationSelector.type].MaxEmptyAttempts)
                {
                    attemptedFormationTypes.Add(formationSelector.type);

                    // Reset empty attemps count and offset for next fallback formation type
                    emptyAttemptsCount = 0;
                    offset = originalOffset;

                    // Current formation type could not compute all path destinations then generate path destinations with the fall back formation if there's one
                    if (formationHandlers[formationSelector.type].FallbackFormationType.IsValid()
                        // And make sure we have not attempted to generate path destinations with the fallback formation in this call
                        && !attemptedFormationTypes.Contains(formationHandlers[formationSelector.type].FallbackFormationType))
                    {
                        formationSelector = new MovementFormationSelector
                        {
                            type = formationHandlers[formationSelector.type].FallbackFormationType,
                            properties = formationSelector.properties
                        };

                        continue;
                    }

                    // No fallback formation? exit!
                    return errorMessage;
                }

                // Only if the last attempt resulted in no generated path destinations.
                if (generatedAmount == 0)
                    emptyAttemptsCount++;
            }

            // We have computed at least one path destination, the count of the list is either smaller or equal to the initial value of the "amount" argument.
            return ErrorMessage.none; 
        }
        #endregion

        #region Generating Path Destinations Helper Methods
        public ErrorMessage IsPositionClear(ref Vector3 targetPosition, IMovementComponent refMvtComp, bool playerCommand)
            => IsPositionClear(ref targetPosition, refMvtComp.Controller.Radius, refMvtComp.Controller.NavigationAreaMask, refMvtComp.AreasMask, playerCommand);

        public ErrorMessage IsPositionClear(ref Vector3 targetPosition, float agentRadius, LayerMask navAreaMask, TerrainAreaMask areasMask, bool playerCommand)
        {
            ErrorMessage errorMessage;
            if ((errorMessage = gridSearch.IsPositionReserved(targetPosition, agentRadius, areasMask, playerCommand)) != ErrorMessage.none)
                return errorMessage;

            else if (TryGetMovablePosition(targetPosition, agentRadius, navAreaMask, out targetPosition))
                return ErrorMessage.none;

            return ErrorMessage.mvtPositionNavigationOccupied;
        }

        public bool TryGetMovablePosition(Vector3 center, float radius, LayerMask areaMask, out Vector3 movablePosition)
            => MvtSystem.TryGetValidPosition(center, radius, areaMask, out movablePosition);

        public bool GetRandomMovablePosition(IEntity entity, Vector3 origin, float range, out Vector3 targetPosition, bool playerCommand)
        {
            targetPosition = entity.transform.position;
            if (!entity.IsValid() || !entity.CanMove())
                return false;

            // Pick a random direction to go to
            Vector3 randomDirection = Random.insideUnitSphere * range; 
            randomDirection += origin;
            randomDirection.y = terrainMgr.SampleHeight(randomDirection, entity.MovementComponent);

            // Get the closet movable point to the randomly chosen direction
            if (MvtSystem.TryGetValidPosition(randomDirection, range, entity.MovementComponent.Controller.NavigationAreaMask, out targetPosition)
                && IsPositionClear(ref targetPosition, entity.MovementComponent, playerCommand) == ErrorMessage.none)
                return true;

            return false;
        }
        #endregion

        #region Movement Helper Methods
        private void SpawnMovementTargetEffect(IEntity entity, Vector3 position, MovementSource source)
        {
            effectObjPool.Spawn(
                source.isMoveAttackRequest && entity.AttackComponent.IsValid() && entity.AttackComponent.IsAttackMoveEnabled
                ? attackMgr.AttackMoveTargetEffect
                : MovementTargetEffect,
                position);
        }
        #endregion
    }
}