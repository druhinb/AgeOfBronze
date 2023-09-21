using RTSEngine.Attack;
using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.Logging;
using RTSEngine.Model;
using RTSEngine.Movement;
using RTSEngine.ResourceExtension;
using RTSEngine.UnitExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTSEngine.Determinism
{
    public enum FetchSpawnablePrefabsType { auto = 0, manual = 1, codeCategoryPicker = 2 };

    public class InputManager : MonoBehaviour, IInputManager
    {
        public const int INVALID_ENTITY_KEY = -1;

        #region Attributes
        private List<IEntity> entityPrefabs = new List<IEntity>();
        // key: entity code, value: index of the entity prefab in the entityPrefabs list.
        public IReadOnlyDictionary<string, int> entityCodeToPrefabIndex = null;

        [SerializeField, Tooltip("Pick how entity prefabs can be fetched to be spawnable in this map scene. Auto: Fetches all entity prefabs in the project. Manual: Drag and drop spawnable prefabs in the inspector. Code Category Picker: Fetches entity prefabs from your project that match code and category requirements in the inspector.")]
        private FetchSpawnablePrefabsType fetchSpawnablePrefabsType = FetchSpawnablePrefabsType.auto;

        [SerializeField, EnforceType(typeof(IEntity)), Tooltip("Drag and drop entity prefabs that can be used in this map scene in this field. Spawnable prefabs are prefabs with the IEntity component placed in a path that ends with: '../Resources/Prefabs'")]
        private GameObject[] manualSpawnablePrefabs = new GameObject[0];

        [SerializeField, Tooltip("Pick the codes and categories of entities whose prefabs are spawnable in this map scene. Spawnable prefabs are prefabs with the IEntity component placed in a path that ends with: '../Resources/Prefabs'")]
        private EntityTargetPicker spawnablePrefabsTargetPicker = new EntityTargetPicker();

        // The entities spawned through this component can be referenced using each entity's unique key.
        private Dictionary<int, IEntity> spawnedEntities = null;
        private HashSet<int> deadEntityKeys;
        // Allows to determine the next key to use for the next entity that reigsters itself in this component.
        private int nextKey = 0;

        // Stores inputs received before the IInputAdder of the current game is initialized, in case there is supposed to be one.
        private List<CommandInput> awaitingInputAdderInputs;

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IUnitManager unitMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        protected IAttackManager attackMgr { private set; get; }
        protected ITimeModifier timeModifier { private set; get; }
        protected IModelCacheManager modelCacheMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.timeModifier = gameMgr.GetService<ITimeModifier>();
            this.modelCacheMgr = gameMgr.GetService<IModelCacheManager>();

            spawnedEntities = new Dictionary<int, IEntity>();
            deadEntityKeys = new HashSet<int>();
            nextKey = -1;

            awaitingInputAdderInputs = new List<CommandInput>();
            // If the input adder is not ready yet, we subscribe to the event that will be triggered when it is enabled
            if (gameMgr.CurrBuilder.IsValid() && !gameMgr.CurrBuilder.IsInputAdderReady)
                gameMgr.CurrBuilder.InputAdderReady += HandleInputAdderReady;

            gameMgr.GameStartRunning += HandleGameStartRunning;
        }

        private void OnDestroy()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }

        private void HandleGameStartRunning(IGameManager gameMgr, EventArgs args)
        {
            GameObject prefabsParent = new GameObject("PrefabsParent");
            prefabsParent.transform.SetParent(gameMgr.transform);
            prefabsParent.transform.localPosition = Vector3.zero;

            entityPrefabs = (fetchSpawnablePrefabsType == FetchSpawnablePrefabsType.manual
                ? manualSpawnablePrefabs
                : Resources
                .LoadAll("Prefabs", typeof(GameObject))
                .Cast<GameObject>())
                .Where(prefabObj =>
                {
                    IEntity nextPrefab = prefabObj.IsValid() ? prefabObj.GetComponent<IEntity>() : null;
                    return nextPrefab.IsValid()
                        && (fetchSpawnablePrefabsType != FetchSpawnablePrefabsType.codeCategoryPicker || spawnablePrefabsTargetPicker.IsValidTarget(prefabObj.GetComponent<IEntity>()));
                })
                .Select(prefabObj =>
                {
                    IEntity prefabInstance = GameObject.Instantiate(prefabObj, Vector3.zero, prefabObj.transform.rotation).GetComponent<IEntity>();
                    return prefabInstance;
                })
                .ToList();

            int index = -1;
            entityCodeToPrefabIndex = entityPrefabs
                .ToDictionary(entity => entity.Code, entity => { index++; return index; });

            // Initialize prefabs as some entity components (i.e. IEntityModel) must perform some actions on the prefab level.
            foreach (IEntity entity in entityPrefabs)
            {
                entity.InitPrefab(gameMgr);
                entity.transform.SetParent(prefabsParent.transform);
                entity.transform.localPosition = Vector3.zero;
                entity.gameObject.SetActive(false);
            }

            // Entity prefabs are actually just inactive instances of the original prefabs that have been initialized with InitPrefab

            foreach (IFactionSlot faction in gameMgr.FactionSlots)
                faction.InitDefaultFactionEntities();
        }

        private void HandleInputAdderReady(IGameBuilder sender, EventArgs args)
        {
            if(awaitingInputAdderInputs.Count > 0)
            {
                logger.Log("[InputManager] '{typeof(IInputAdder).Name}' instance is now ready to relay inputs. Relayed {awaitingInputAdderInputs.Count} late cached inputs...");
                gameMgr.CurrBuilder.InputAdder.AddInput(awaitingInputAdderInputs);
            }

            awaitingInputAdderInputs.Clear();

            gameMgr.CurrBuilder.InputAdderReady -= HandleInputAdderReady;
        }
        #endregion

        #region Registering Entities
        public int RegisterEntity(IEntity newEntity, InitEntityParameters initParams)
        {
            if (!logger.RequireValid(newEntity,
                $"[InputManager] Register an invalid entity is not allowed!"))
                return INVALID_ENTITY_KEY;

            int newEntityKey = INVALID_ENTITY_KEY;
            if (initParams.enforceKey)
            {

                if (spawnedEntities.ContainsKey(initParams.key))
                {
                    logger.LogError($"[InputManager] Enforced input key {initParams.key} has been already registered for another entity {spawnedEntities[initParams.key].gameObject.name}!");
                    return INVALID_ENTITY_KEY;
                }
                newEntityKey = initParams.key;
            }
            else
            {
                while(nextKey < 0 || spawnedEntities.ContainsKey(nextKey))
                    nextKey++;

                newEntityKey = nextKey;
            }

            spawnedEntities.Add(newEntityKey, newEntity);
            newEntity.Health.EntityDead += HandleRegisteredEntityDead;
            return newEntityKey;
        }

        private void HandleRegisteredEntityDead(IEntity registeredEntity, DeadEventArgs args)
        {
            //spawnedEntities.Remove(registeredEntity.Key);
            deadEntityKeys.Add(registeredEntity.Key);
            registeredEntity.Health.EntityDead -= HandleRegisteredEntityDead;
        }
        #endregion


        #region Sending Input
        public ErrorMessage SendInput(CommandInput newInput, IEntity source, IEntity target)
        {
            if (!logger.RequireValid(source,
                "[InputManager] Can not process input without a valid source!"))
                return ErrorMessage.invalid;

            if (newInput.playerCommand)
            {
                if  (!RTSHelper.IsLocalPlayerFaction(source) && !(RTSHelper.IsNPCFaction(source) && RTSHelper.IsMasterInstance()))
                    return ErrorMessage.noAuthority;
            }
            else if (!RTSHelper.IsMasterInstance())
                return ErrorMessage.noAuthority;

            // The logic below does not allow playerCommand flagged inputs to come from NPC factions.
            /*if (!RTSHelper.IsMasterInstance() && !newInput.playerCommand 
                || (newInput.playerCommand && !RTSHelper.IsLocalPlayerFaction(source)))
                return ErrorMessage.noAuthority;*/

            // New input can now be processed after checking for the game's permissions over the input's source.

            // If we're creating an object, then look in the spawnable prefabs list
            // Otherwise, use the unique key assigned to each IEntity as the sourceID
            if (newInput.isSourcePrefab)
            {
                if (!entityCodeToPrefabIndex.TryGetValue(source.Code, out newInput.sourceID))
                {
                    logger.LogError("[InputManager] Attempting to launch command with entity prefab of code '{source.Code}' as source while the entity prefab is not within a path that ends with '**/Resources/Prefabs' or not added manually to the inspector of this component!");
                    return ErrorMessage.invalid;
                }
            }
            else
                newInput.sourceID = source.Key;

            newInput.targetID = target.IsValid() ? target.Key : INVALID_ENTITY_KEY;

            return SendInputFinal(newInput);
        }

        public ErrorMessage SendInput(CommandInput newInput, IEnumerable<IEntity> source, IEntity target)
        {
            if (!logger.RequireValid(source,
                "[InputManager] Can not process input without a valid source!"))
                return ErrorMessage.invalid;

            if (newInput.playerCommand)
            {
                if (!RTSHelper.IsLocalPlayerFaction(source) && !(RTSHelper.IsNPCFaction(source) && RTSHelper.IsMasterInstance()))
                    return ErrorMessage.noAuthority;
            }
            else if (!RTSHelper.IsMasterInstance())
                return ErrorMessage.noAuthority;

            newInput.code = EntitiesToKeyString(source);

            newInput.targetID = target.IsValid() ? target.Key : INVALID_ENTITY_KEY;

            return SendInputFinal(newInput);
        }

        public ErrorMessage SendInput(CommandInput newInput)
        {
            if (!RTSHelper.IsMasterInstance())
                return ErrorMessage.noAuthority;

            return SendInputFinal(newInput);
        }

        private ErrorMessage SendInputFinal(CommandInput newInput)
        {
            if (gameMgr.State == GameStateType.frozen)
                return ErrorMessage.gameFrozen;
            else if (gameMgr.CurrBuilder.IsValid())
            {
                // If the input adder instance is not ready yet then cache the received inputs to be relayed when the input adder is ready
                if (!gameMgr.CurrBuilder.IsInputAdderReady)
                    awaitingInputAdderInputs.Add(newInput);
                else
                    gameMgr.CurrBuilder.InputAdder.AddInput(newInput);
            }
            else
                LaunchInput(newInput);

            return ErrorMessage.none;
        }
        #endregion

        #region Launching Input
        public void LaunchInput(IEnumerable<CommandInput> inputs)
        {
            foreach (CommandInput input in inputs)
                LaunchInput(input);
        }

        public void LaunchInput(CommandInput input)
        {
            switch ((InputMode)input.sourceMode)
            {
                case InputMode.master:
                    OnMasterInput(input);
                    break;

                case InputMode.create:
                    OnCreateInput(input);
                    break;

                case InputMode.faction:
                    OnFactionInput(input);
                    break;

                case InputMode.entity:
                    OnEntityInput(input);
                    break;
                case InputMode.entityGroup:
                    OnEntityGroupInput(input);
                    break;

                case InputMode.health:
                    OnHealthInput(input);
                    break;

                case InputMode.custom:
                    OnCustomInput(input);
                    break;

                default:
                    logger.LogError("[InputManager] Undefined input source type of ID: {input.sourceID}!");
                    break;
            }
        }

        private void OnMasterInput(CommandInput input)
        {
            switch((InputMode)input.targetMode)
            {
                case InputMode.setTimeModifier:

                    timeModifier.SetModifierLocal(input.floatValue, input.playerCommand);

                    break;

                default:
                    logger.LogError("[InputManager] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.master}!");
                    break;
            }
        }

        public bool TryGetEntityInstanceWithKey (int key, out IEntity entity)
        {
            return spawnedEntities.TryGetValue(key, out entity);
        }
        public bool TryGetEntityPrefabWithCode (string key, out IEntity entity)
        {
            if(entityCodeToPrefabIndex.TryGetValue(key, out int prefabIndex))
            {
                entity = entityPrefabs[prefabIndex];
                return true;
            }
            entity = null;
            return false;
        }

        private bool GetInputSourceEntity (CommandInput input, out IEntity entity)
        {
            try
            {
                entity = input.isSourcePrefab
                    ? entityPrefabs[input.sourceID]
                    : spawnedEntities[input.sourceID];

                return true;
            }
            catch
            {
                entity = null;
                logger.LogError($"[InputManager] Unable to get prefab ({input.isSourcePrefab}) or instance ({!input.isSourcePrefab}) of ID: {input.sourceID}");

                return false;
            }
        }

        protected virtual void OnCustomInput(CommandInput input) { }

        private void OnCreateInput(CommandInput input)
        {
            if (!GetInputSourceEntity(input, out IEntity prefab))
                return;

            switch ((InputMode)input.targetMode)
            {
                case InputMode.unit:

                    InitUnitParametersInput unitParamsInput = JsonUtility.FromJson<InitUnitParametersInput>(input.code);

                    InitUnitParameters unitParams = unitParamsInput.ToParams(this);

                    unitMgr.CreateUnitLocal(
                        prefab as IUnit,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        unitParams);


                    break;
                case InputMode.building:

                    InitBuildingParametersInput buildingParamsInput = JsonUtility.FromJson<InitBuildingParametersInput>(input.code);
                    InitBuildingParameters buildingParams = buildingParamsInput.ToParams(this);

                    buildingMgr.CreatePlacedBuildingLocal(
                        prefab as IBuilding,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        buildingParams);

                    break;

                case InputMode.resource:

                    InitResourceParametersInput resourceParamsInput = JsonUtility.FromJson<InitResourceParametersInput>(input.code);
                    InitResourceParameters resourceParams = new InitResourceParameters
                    {
                        enforceKey = resourceParamsInput.enforceKey,
                        key = resourceParamsInput.key,

                        factionID = resourceParamsInput.factionID,
                        free = resourceParamsInput.free,

                        setInitialHealth = resourceParamsInput.setInitialHealth,
                        initialHealth = resourceParamsInput.initialHealth,

                        playerCommand = resourceParamsInput.playerCommand
                    };

                    resourceMgr.CreateResourceLocal(
                        prefab as IResource,
                        input.sourcePosition,
                        Quaternion.Euler(input.opPosition),
                        resourceParams);

                    break;
                default:
                    logger.LogError("[InputManager] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.create}!");
                    break;
            }
        }

        private void OnFactionInput(CommandInput input)
        {
            switch ((InputMode)input.targetMode)
            {
                case InputMode.factionDestroy:

                    gameMgr.OnFactionDefeatedLocal(input.intValues.Item1); //the input.value holds the faction ID of the faction to destroy
                    break;

                default:
                    logger.LogError("[InputManager] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.faction}!");
                    break;
            }
        }

        private void OnEntityInput(CommandInput input)
        {
            if (!GetInputSourceEntity(input, out IEntity sourceEntity))
                return;

            spawnedEntities.TryGetValue(input.targetID, out IEntity target);
            TargetData<IEntity> targetData = new TargetData<IEntity> { instance = target, position = input.targetPosition, opPosition = input.opPosition };

            switch ((InputMode)input.targetMode)
            {
                case InputMode.setFaction:

                    sourceEntity.SetFactionLocal(target, input.intValues.Item1);
                    break;

                case InputMode.setComponentActive:

                    sourceEntity.EntityComponents[input.code].SetActiveLocal(input.intValues.Item1 == 1 ? true : false, input.playerCommand);
                    break;

                case InputMode.setComponentTargetFirst:

                    SetTargetInputDataBooleans setTargetFirstBooleans = (SetTargetInputDataBooleans)input.intValues.Item1;

                    sourceEntity.SetTargetFirstLocal(
                        new SetTargetInputData
                        {
                            target = targetData,
                            playerCommand = input.playerCommand,

                            includeMovement = setTargetFirstBooleans.HasFlag(SetTargetInputDataBooleans.includeMovement),
                            isMoveAttackRequest = setTargetFirstBooleans.HasFlag(SetTargetInputDataBooleans.isMoveAttackRequest),
                            fromTasksQueue = setTargetFirstBooleans.HasFlag(SetTargetInputDataBooleans.fromTasksQueue),
                        });

                    break;

                case InputMode.setComponentTarget:

                    SetTargetInputDataBooleans setTargetBooleans = (SetTargetInputDataBooleans)input.intValues.Item1;

                    sourceEntity.EntityTargetComponents[input.code].SetTargetLocal(
                        new SetTargetInputData
                        {
                            target = targetData,
                            playerCommand = input.playerCommand,

                            includeMovement = setTargetBooleans.HasFlag(SetTargetInputDataBooleans.includeMovement),
                            isMoveAttackRequest = setTargetBooleans.HasFlag(SetTargetInputDataBooleans.isMoveAttackRequest),
                            fromTasksQueue = setTargetBooleans.HasFlag(SetTargetInputDataBooleans.fromTasksQueue),
                        });
                    break;

                case InputMode.launchComponentAction:

                    SetTargetInputDataBooleans launchActionBooleans = (SetTargetInputDataBooleans)input.intValues.Item2;

                    sourceEntity.EntityComponents[input.code].LaunchActionLocal(
                        (byte)input.intValues.Item1,
                        new SetTargetInputData
                        {
                            target = targetData,
                            componentCode = input.opCode,
                            playerCommand = input.playerCommand,

                            includeMovement = launchActionBooleans.HasFlag(SetTargetInputDataBooleans.includeMovement),
                            isMoveAttackRequest = launchActionBooleans.HasFlag(SetTargetInputDataBooleans.isMoveAttackRequest),
                            fromTasksQueue = launchActionBooleans.HasFlag(SetTargetInputDataBooleans.fromTasksQueue),
                        });
                    break;

                case InputMode.attack:

                    LaunchAttackBooleans launchAttackBooleans = (LaunchAttackBooleans)input.intValues.Item1;

                    attackMgr.LaunchAttackLocal(new LaunchAttackData<IEntity>
                    {
                        source = sourceEntity,
                        targetEntity = target as IFactionEntity,
                        targetPosition = input.targetPosition,
                        playerCommand = input.playerCommand,

                        isMoveAttackRequest = launchAttackBooleans.HasFlag(LaunchAttackBooleans.isMoveAttackRequest),
                        allowTerrainAttack = launchAttackBooleans.HasFlag(LaunchAttackBooleans.allowTerrainAttack),
                    });
                    break;

                case InputMode.movement:

                    string[] mvtParams = input.code.Split(RTSHelper.STR_SEPARATOR_L1);
                    IEntityTargetComponent sourceComponent = null;
                    IAddableUnit targetAddableUnit = null;

                    if(sourceEntity.IsValid())
                        sourceEntity.EntityTargetComponents.TryGetValue(mvtParams[0], out sourceComponent);
                    if(target.IsValid())
                        target.AddableUnitComponents.TryGetValue(mvtParams[1], out targetAddableUnit);

                    MovementSourceBooleans sourceBooleansMask = (MovementSourceBooleans)input.intValues.Item1;

                    mvtMgr.SetPathDestinationLocal(
                        sourceEntity,
                        input.targetPosition,
                        input.floatValue,
                        target,
                        new MovementSource
                        {
                            playerCommand = input.playerCommand,
                            sourceTargetComponent = sourceComponent,
                            targetAddableUnit = targetAddableUnit,
                            targetAddableUnitPosition = input.opPosition,
                            isMoveAttackRequest = sourceBooleansMask.HasFlag(MovementSourceBooleans.isMoveAttackRequest),
                            inMoveAttackChain = sourceBooleansMask.HasFlag(MovementSourceBooleans.inMoveAttackChain),
                            isMoveAttackSource = sourceBooleansMask.HasFlag(MovementSourceBooleans.isMoveAttackSource),
                            fromTasksQueue = sourceBooleansMask.HasFlag(MovementSourceBooleans.fromTasksQueue),
                        });
                    break;

                default:
                    logger.LogError("[InputManager] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.entity}!");
                    break;
            }
        }

        private void OnEntityGroupInput(CommandInput input)
        {
            List<IEntity> sourceEntities = KeyStringToEntities<IEntity>(input.code); //get the units list
            spawnedEntities.TryGetValue(input.targetID, out IEntity target); //attempt to get the target Entity instance for this unit input

            if (sourceEntities.Count > 0) //if there's actual units in the list
            {
                switch ((InputMode)input.targetMode)
                {
                    case InputMode.attack:

                        LaunchAttackBooleans launchAttackBooleans = (LaunchAttackBooleans)input.intValues.Item1;

                        //if the target mode is attack -> make the unit group launch an attack on the target.
                        attackMgr.LaunchAttackLocal(new LaunchAttackData<IReadOnlyList<IEntity>>
                        {
                            source = sourceEntities,
                            targetEntity = target as IFactionEntity,
                            targetPosition = input.targetPosition,
                            playerCommand = input.playerCommand,

                            isMoveAttackRequest = launchAttackBooleans.HasFlag(LaunchAttackBooleans.isMoveAttackRequest),
                            allowTerrainAttack = launchAttackBooleans.HasFlag(LaunchAttackBooleans.allowTerrainAttack),
                        });
                        break;

                    case InputMode.movement:

                        MovementSourceBooleans sourceBooleansMask = (MovementSourceBooleans)input.intValues.Item1;

                        mvtMgr.SetPathDestinationLocal(
                            sourceEntities,
                            input.targetPosition,
                            input.floatValue,
                            target as IEntity,
                            new MovementSource
                            {
                                playerCommand = input.playerCommand,
                                sourceTargetComponent = null,
                                targetAddableUnit = null,

                                isMoveAttackRequest = sourceBooleansMask.HasFlag(MovementSourceBooleans.isMoveAttackRequest),
                                inMoveAttackChain = sourceBooleansMask.HasFlag(MovementSourceBooleans.inMoveAttackChain),
                                isMoveAttackSource = sourceBooleansMask.HasFlag(MovementSourceBooleans.isMoveAttackSource),
                                fromTasksQueue = sourceBooleansMask.HasFlag(MovementSourceBooleans.fromTasksQueue),
                            });
                        break;

                    default:
                        logger.LogError("[InputManager] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.entityGroup}!");
                        break;
                }
            }
        }

        private void OnHealthInput(CommandInput input)
        {
            if (!GetInputSourceEntity(input, out IEntity sourceEntity))
                return;

            spawnedEntities.TryGetValue(input.targetID, out IEntity target);

            IEntityHealth sourceHealth;

            switch((EntityType)input.intValues.Item1)
            {
                case EntityType.unit:
                    sourceHealth = (sourceEntity as IUnit).Health;
                    break;

                case EntityType.building:
                    sourceHealth = (sourceEntity as IBuilding).Health;
                    break;

                case EntityType.resource:
                    sourceHealth = (sourceEntity as IResource).Health;
                    break;

                default:
                    logger.LogError($"[InputManager] Invalid source entity type of ID: {input.intValues.Item1}!");
                    return;
            }

            switch((InputMode)input.targetMode)
            {
                case InputMode.healthSetMax:

                    sourceHealth.SetMaxLocal(new HealthUpdateArgs(input.intValues.Item2, target));
                    break;

                case InputMode.healthAddCurr:

                    sourceHealth.AddLocal(new HealthUpdateArgs(input.intValues.Item2, target));
                    break;

                case InputMode.healthDestroy:

                    sourceHealth.DestroyLocal(input.intValues.Item2 == 1 ? true : false, target); 
                    break;

                default:
                    logger.LogError($"[InputManager] Invalid input target mode of ID: {input.targetMode} for input source mode: {InputMode.health}!");
                    break;
            }
        }
        #endregion

        #region Helper Methods
        private string EntitiesToKeyString(IEnumerable<IEntity> entities)
        {
            return String.Join(".", entities.Select(entity => $"{entity.Key}"));
        }

        private List<T> KeyStringToEntities<T>(string inputString) where T : IEntity
        {
            List<T> nextEntities = new List<T>();

            if (String.IsNullOrEmpty(inputString))
                return nextEntities;

            string[] entityKeys = inputString.Split('.');
            for (int i = 0; i < entityKeys.Length; i++)
            {
                int key = Int32.Parse(entityKeys[i]);
                spawnedEntities.TryGetValue(key, out IEntity entity);
                if (!entity.IsValid() && !deadEntityKeys.Contains(key))
                {
                    logger.LogError($"[InputManager] Attempting to break down entity key string into entities and unable to find entity with key: {key}!", source: this);
                }
                else
                    nextEntities.Add((T)entity);
            }

            return nextEntities;
        }

        // Temporary handling of int values in an input command.
        public IntValues ToIntValues(int int1) => new IntValues { Item1 = int1 };
        public IntValues ToIntValues(int int1, int int2) => new IntValues { Item1 = int1, Item2 = int2 };
        #endregion
    }
}