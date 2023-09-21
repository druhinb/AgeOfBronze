using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Audio;
using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.Controls;
using RTSEngine.Utilities;
using UnityEngine.Serialization;

namespace RTSEngine.Attack
{
    public class AttackManager : ObjectPool<IAttackObject, AttackObjectSpawnInput>, IAttackManager
    {
        #region Attributes
        // EDITOR ONLY
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        [SerializeField, Tooltip("Allow faction entities that do not require a target to launch attacks on the terrain?")]
        private bool terrainAttackEnabled = true;
        [SerializeField, Tooltip("If terrain attack is enabled, this represents the key that the player can use while selecting an attack faction entity to directly launch a terrain attack.")]
        private ControlType terrainAttackKey = null;
        //private KeyCode terrainAttackKey = KeyCode.T;
        public bool IsTerrainAttackKeyDown => controls.Get(terrainAttackKey);

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Visible to the local player when they command unit(s) to perform a terrain attack on a location.")]
        private GameObjectToEffectObjectInput terrainAttackTargetEffectPrefab = null;
        public IEffectObject TerrainAttackTargetEffect => terrainAttackTargetEffectPrefab.Output;

        [SerializeField, Tooltip("Allow movable units with an Attack component to search for attack targets while moving towards their destinations when a movement command is set by the player while holding down the below key?"), FormerlySerializedAs("moveAttackEnabled")]
        private bool attackMoveWithKeyEnabled = true;
        [SerializeField, Tooltip("Key that must be held down by the local player while launching a movement command into a movable unit to allow it to search for attack targets while moving."), FormerlySerializedAs("moveAttackKey")]
        private ControlType attackMoveKey = null;
        public bool CanAttackMoveWithKey => controls.Get(attackMoveKey);

        //private KeyCode moveAttackKey = KeyCode.M;
        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Visible to the local player when they command unit(s) to perform a move-attack."), FormerlySerializedAs("moveAttackTargetEffectPrefab")]
        private GameObject attackMoveTargetEffectPrefab = null;
        public IEffectObject AttackMoveTargetEffect { get; private set; }

        // Collection used to store and pass path destination for attack positions between methods in this script and the IMovementManager
        // Having it as a member variable makes more sense to avoid the garbage produced by creating and destroying this list every time we are generating path destinations
        private List<Vector3> pathDestinations;
        private int PATH_DESTINATIONS_DEFAULT_CAPACITY = 50;

        // List used to cache the non movable attackers when generating attack path destinations since non movable attackers are handled differently
        private List<IEntity> nonMovableAttackers;
        private int NON_MOVABLE_ATTACKERS_DEFAULT_CAPACITY = 50;

        // Game services
        protected IMovementManager mvtMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        protected IGameControlsManager controls { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnObjectPoolInit() 
        { 
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();
            this.controls = gameMgr.GetService<IGameControlsManager>();

            if (attackMoveTargetEffectPrefab.IsValid())
                this.AttackMoveTargetEffect = attackMoveTargetEffectPrefab.GetComponent<IEffectObject>();

            pathDestinations = new List<Vector3>(PATH_DESTINATIONS_DEFAULT_CAPACITY);
            nonMovableAttackers = new List<IEntity>(NON_MOVABLE_ATTACKERS_DEFAULT_CAPACITY);

            // Move attack initial state
            if (!attackMoveWithKeyEnabled)
                enabled = false;

            OnInit();
        }

        protected virtual void OnInit() { }
        #endregion

        #region Handling Terrain Attack
        public bool CanLaunchTerrainAttack<T>(LaunchAttackData<T> data)
        {
            return terrainAttackEnabled && (IsTerrainAttackKeyDown || data.allowTerrainAttack);
        }
        #endregion

        #region Handling Attack-Move
        #endregion

        #region Launching Attack: Multiple Attackers
        public ErrorMessage LaunchAttack(LaunchAttackData<IReadOnlyList<IEntity>> data)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entityGroup,
                targetMode = (byte)InputMode.attack,

                targetPosition = data.targetPosition,

                playerCommand = data.playerCommand,

                intValues = inputMgr.ToIntValues((int)data.BooleansToMask())
            },
            source: data.source,
            target: data.targetEntity);
        }

        public ErrorMessage LaunchAttackLocal(LaunchAttackData<IReadOnlyList<IEntity>> data)
        {
            if (!data.source.IsValid())
            {
                logger.LogError("[AttackManager] Some or all entities that are attempting to attack are invalid!");
                return ErrorMessage.invalid;
            }
            else if (data.source.Count < 2)
                return LaunchAttackLocal(
                    new LaunchAttackData<IEntity>
                    {
                        source = data.source[0],
                        targetEntity = data.targetEntity,
                        targetPosition = data.targetPosition,
                        playerCommand = data.playerCommand
                    });
            else if (!data.targetEntity.IsValid() && !CanLaunchTerrainAttack(data))
                return ErrorMessage.attackTerrainDisabled;

            // Take out the attack entities which do not use a movement component, for those, a direct target set is done where the attack position is the current entity position.
            nonMovableAttackers.Clear();

            // We first start by handling the movable attackers

            // Sort the attack entities based on their codes, we assume that units that share the same code (which is the defining property of an entity in the RTS Engine) are identical.
            // And filter out any units that do not have an attack component.
            ChainedSortedList<string, IEntity> sortedAttackers = RTSHelper.SortEntitiesByCode(data.source, x => x.CanAttack);

            // At least one attacker to get the attack order audio from.
            IEntity refAttacker = null; 

            foreach (IReadOnlyList<IEntity> attackerSet in sortedAttackers.Values)
            {
                // If the current unit type is unable to have the entity as the target, move to the next unit type list
                if (attackerSet[0].AttackComponent.IsTargetValid(RTSHelper.ToTargetData(data.targetEntity), data.playerCommand) != ErrorMessage.none)
                    continue;

                // Generate movement path destinations for the current list of identical unit types:
                mvtMgr.GeneratePathDestination(
                    attackerSet,
                    data.targetPosition,
                    attackerSet[0].AttackComponent.Formation.MovementFormation,
                    attackerSet[0].AttackComponent.Formation.GetStoppingDistance(data.targetEntity, min: true),
                    new MovementSource { playerCommand = data.playerCommand, isMoveAttackRequest = data.isMoveAttackRequest},
                    ref pathDestinations,
                    condition: RTSHelper.IsAttackLOSBlocked);

                // No valid path destinations generated? do not continue as there is nowhere to move to
                if (pathDestinations.Count == 0)
                    continue;

                // Index counter for the generated path destinations.
                int destinationID = 0;

                foreach (IEntity attacker in attackerSet)
                {
                    if (!attacker.CanMove())
                    {
                        nonMovableAttackers.Add(attacker);
                        continue;
                    }

                    // Create the next target struct without specifying the attack position
                    TargetData<IFactionEntity> nextTarget = new TargetData<IFactionEntity>
                    {
                        instance = data.targetEntity,
                        opPosition = data.targetPosition,
                    };

                    pathDestinations.Sort((pos1, pos2) => (pos1 - attacker.transform.position).sqrMagnitude.CompareTo((pos2 - attacker.transform.position).sqrMagnitude));
                    destinationID = pathDestinations.FindIndex(pos => attacker.AttackComponent.IsTargetInRange(pos, nextTarget));
                    if (destinationID == -1)
                    {
                        // Only disallow to set the attack target if this is not a playerCommand
                        // If it is a player command, pick the closest path destination to the source attacker...
                        // ...so that it will be able to move towards that position and not attack
                        if (!data.playerCommand)
                            continue;

                        destinationID = 0;
                    }

                    nextTarget.position = pathDestinations[destinationID];

                    // If current unit is able to engage with its target using the computed path then move to the next path, if not, test the path on the next unit.
                    // The last argument of the SetTarget method is set to the playerCommand because we still want to move the units to computed attack position...
                    // ...even if it is out of the attack range because the player issued the attack/movement command.
                    ErrorMessage setTargetErorr = attacker.AttackComponent.SetTargetLocal(
                        new SetTargetInputData
                        {
                            target = nextTarget,
                            playerCommand = data.playerCommand,
                            isMoveAttackRequest = data.isMoveAttackRequest
                        });
                    if (setTargetErorr == ErrorMessage.none || setTargetErorr == ErrorMessage.attackMoveToTargetOnly)
                    {
                        // Assign the reference unit from which the attack order will be played.
                        if (!refAttacker.IsValid())
                            refAttacker = attacker;

                        pathDestinations.RemoveAt(destinationID);

                        // No more paths to test, stop moving units to attack.
                        if (destinationID >= pathDestinations.Count)
                            break;
                    }
                }
            }

            // Finally handle setting targets for the non movable attackers
            foreach (IEntity attacker in nonMovableAttackers)
            {
                // Assign the reference attacker from which the attack order will be played, if none has been assigned yet.
                if (!refAttacker.IsValid())
                    refAttacker = attacker;

                attacker.AttackComponent?.SetTargetLocal(
                    new SetTargetInputData
                    {
                        target = new TargetData<IFactionEntity>
                        {
                            instance = data.targetEntity,
                            opPosition = data.targetEntity.IsValid() ? data.targetEntity.transform.position : data.targetPosition,

                            position = attacker.transform.position
                        },
                        isMoveAttackRequest = data.isMoveAttackRequest,
                        playerCommand = data.playerCommand
                    });
            }

            if (data.playerCommand && refAttacker.IsValid() && refAttacker.IsLocalPlayerFaction())
            {
                if (!data.targetEntity.IsValid())
                    effectObjPool.Spawn(TerrainAttackTargetEffect, data.targetPosition);

                audioMgr.PlaySFX(refAttacker.AttackComponent.OrderAudio, false);
            }

            return ErrorMessage.none;
        }
        #endregion

        #region Launching Attack: Single Attacker
        public ErrorMessage LaunchAttack(LaunchAttackData<IEntity> data)
        {
            return inputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.attack,

                sourcePosition = data.source.transform.position,
                targetPosition = data.targetPosition,

                playerCommand = data.playerCommand,

                intValues = inputMgr.ToIntValues((int)data.BooleansToMask())
            },
            source: data.source,
            target: data.targetEntity);
        }

        public ErrorMessage LaunchAttackLocal(LaunchAttackData<IEntity> data)
        {
            if (!data.source.IsValid())
            {
                logger.LogError("[AttackManager] Can not attack with an invalid entity!");
                return ErrorMessage.invalid;
            }
            else if (!data.source.CanAttack)
                return ErrorMessage.attackDisabled;
            else if (!data.targetEntity.IsValid() && !CanLaunchTerrainAttack(data))
                return ErrorMessage.attackTerrainDisabled;

            ErrorMessage errorMsg;
            //check whether the new target is valid for this attack type.
            if ((errorMsg = data.source.AttackComponent.IsTargetValid(RTSHelper.ToTargetData(data.targetEntity), data.playerCommand)) != ErrorMessage.none)
            {
                if (data.playerCommand && data.source.IsLocalPlayerFaction())
                    playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                    {
                        message = errorMsg,

                        source = data.source,
                        target = data.targetEntity
                    });
                return errorMsg;
            }

            // If the attack order was issued by the local player and this is the local player's instance.
            if (data.playerCommand && data.source.IsLocalPlayerFaction())
            {
                if (!data.targetEntity.IsValid())
                    effectObjPool.Spawn(TerrainAttackTargetEffect, data.targetPosition);

                audioMgr.PlaySFX(data.source.AttackComponent.OrderAudio, false);
            }

            // Calculate a target attack position and attempt to set a new attack target for the source unit.
            return data.source.AttackComponent.SetTargetLocal(
                new SetTargetInputData
                {
                    target = new TargetData<IFactionEntity>
                    {
                        instance = data.targetEntity,
                        opPosition = data.targetPosition,

                        position = data.source.CanMove() && TryGetAttackPosition(data.source, data.targetEntity, data.targetPosition, data.playerCommand, out Vector3 attackPosition)
                            ? attackPosition
                            : data.source.transform.position
                    },

                    isMoveAttackRequest = data.isMoveAttackRequest,

                    playerCommand = data.playerCommand,
                });
        }
        #endregion

        #region Generating Attack Position
        public bool TryGetAttackPosition(IEntity attacker, IFactionEntity target, Vector3 targetPosition, bool playerCommand, out Vector3 attackPosition)
        {
            attackPosition = Vector3.positiveInfinity;


            if (!attacker.IsValid() || !attacker.CanAttack)
            {
                logger.LogError($"[AttackManager - {attacker.Code}] Can not calculate an attack position with an invalid entity instance or a non attack entity!");
                return false;
            }

            // Generate movement attack path destination for the new target
            mvtMgr.GeneratePathDestination(
                attacker,
                targetPosition,
                attacker.MovementComponent.Formation,
                attacker.AttackComponent.Formation.GetStoppingDistance(target, min: true),
                new MovementSource { playerCommand = playerCommand },
                ref pathDestinations,
                condition: RTSHelper.IsAttackLOSBlocked);

            // If there's a valid attack movement destination produced, get the closest target position
            if (pathDestinations.Count == 0)
                return false;

            attackPosition = pathDestinations.OrderBy(pos => (pos - attacker.transform.position).sqrMagnitude).First();
            return true;
        }
        #endregion

        #region IAttackObject Pooling
        public IReadOnlyDictionary<string, IEnumerable<IAttackObject>> ActiveAttackObjects => ActiveDic;

        public bool TryGetAttackObjectPrefab(string code, out IAttackObject prefab)
            => ObjectPrefabs.TryGetValue(code, out prefab);
        
        public IAttackObject SpawnAttackObject(IAttackObject prefab, AttackObjectSpawnInput input)
        {
            IAttackObject nextAttackObj = base.Spawn(prefab);
            if (!nextAttackObj.IsValid())
                return null;

            nextAttackObj.OnSpawn(input);

            return nextAttackObj;
        }
        #endregion
    }
}