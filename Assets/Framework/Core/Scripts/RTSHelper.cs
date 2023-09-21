using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using RTSEngine.Attack;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Movement;
using RTSEngine.UI;
using RTSEngine.Utilities;
using RTSEngine.Model;
using RTSEngine.Service;
using System.Text;
using RTSEngine.Task;

namespace RTSEngine
{
    public static class RTSHelper
    {
        #region Attributes/Initialization
        public static Color SemiTransparentWhite
        {
            get
            {
                Color color = Color.white;
                color.a = 0.5f;
                return color;
            }
        }

        public static char STR_SEPARATOR_L1 = '#';
        public static char STR_SEPARATOR_L2 = '*';

        private static IGameManager GameMgr;
        private static IInputManager InputMgr;
        private static IAttackManager AttackMgr;
        private static IMovementManager MvtMgr;
        private static ITaskManager TaskMgr;
        private static IModelCacheManager ModelCacheMgr;
        public static ILoggingService LoggingService { private set; get; }

        public static void Init(IGameManager gameMgr)
        {
            GameMgr = gameMgr;

            InputMgr = gameMgr.transform.GetComponentInChildren<IInputManager>();
            AttackMgr = gameMgr.transform.GetComponentInChildren<IAttackManager>();
            MvtMgr = gameMgr.transform.GetComponentInChildren<IMovementManager>();
            ModelCacheMgr = gameMgr.transform.GetComponentInChildren<IModelCacheManager>();
            TaskMgr = gameMgr.transform.GetComponentInChildren<ITaskManager>();

            LoggingService = gameMgr.transform.GetComponentInChildren<ILoggingService>();
        }

        public static void Init<T>(IServicePublisher<T> publisher)
        {
            LoggingService = publisher.transform.GetComponent<ILoggingService>();
        }
        #endregion

        #region General Helper Methods
        public static bool IsValidIndex<T>(this int index, T[] array) => index >= 0 && index < array.Length;
        public static bool IsValidIndex<T>(this int index, List<T> list) => index >= 0 && index < list.Count;

        public static int GetNextIndex<T>(this int index, T[] array) => index >= 0 && index < array.Length - 1 ? index + 1 : 0;
        public static int GetNextIndex<T>(this int index, List<T> list) => index >= 0 && index < list.Count - 1 ? index + 1 : 0;

        public static void ShuffleList<T>(List<T> inputList)
        {
            if(inputList.Count > 0)
            {
                for(int i = 0; i < inputList.Count; i++)
                {
                    int swapID = UnityEngine.Random.Range(0, inputList.Count);
                    if(swapID != i)
                    {
                        T tempElement = inputList[swapID];
                        inputList[swapID] = inputList[i];
                        inputList[i] = tempElement;
                    }
                }
            }
        }

        //Swap two items:
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }

        public static List<int> GenerateRandomIndexList (int length)
        {
            List<int> indexList = new List<int>();

            int i = 0;
            while (i < length) 
            {
                indexList.Add(i);
                i++;
            }

            ShuffleList(indexList);

            return indexList;
        }

        //Check if a layer is inside a layer mask:
        public static bool IsInLayerMask (LayerMask mask, int layer)
        {
            return ((mask & (1 << layer)) != 0);
        }

        public static int TryNameToLayer(string name)
        { 
            int layer = LayerMask.NameToLayer(name);
            if (layer == -1)
            {
                LoggingService.LogError($"[RTSHelper] Unable to find a layer for the name '{name}'. If you are running for the first time, then import the demo layers form 'RTS Engine -> Configure Demo Layers' in the top bar.");
            }
            return layer;
        }

        //a method to update the current rotation target
        public static Quaternion GetLookRotation(Transform transform, Vector3 targetPosition, bool reversed = false, bool fixYRotation = true)
        {
            if (reversed)
                targetPosition = transform.position - targetPosition;
            else
                targetPosition -= transform.position;

            if(fixYRotation == true)
                targetPosition.y = 0;
            if (targetPosition != Vector3.zero)
                return Quaternion.LookRotation(targetPosition);
            else
                return transform.rotation;
        }

        public static Quaternion GetLookRotation(ModelCacheAwareTransformInput transform, Vector3 targetPosition, bool reversed = false, bool fixYRotation = true)
        {
            if (reversed)
                targetPosition = transform.Position - targetPosition;
            else
                targetPosition -= transform.Position;

            if(fixYRotation == true)
                targetPosition.y = 0;
            if (targetPosition != Vector3.zero)
                return Quaternion.LookRotation(targetPosition);
            else
                return transform.Rotation;
        }


        /// <summary>
        /// Sets the rotation of a Transform instance to the direction opposite from a Vector3 position.
        /// </summary>
        /// <param name="transform">Transform instance to set rotation for.</param>
        /// <param name="awayFrom">Vector3 position whose opposite direction the transform will look at.</param>
        public static void LookAwayFrom (Transform transform, Vector3 awayFrom, bool fixYRotation = false)
        {
            if (fixYRotation)
                awayFrom.y = transform.position.y;

            transform.LookAt(2 * transform.position - awayFrom);
        }

        private static StringBuilder timeStringBuilder = new StringBuilder();
        //a method that converts time in seconds to a string MM:SS
        public static string TimeToString (float time)
        {
            if (time <= 0.0f)
                return "00:00";

            timeStringBuilder.Clear();

            int seconds = Mathf.RoundToInt (time);
            int minutes = Mathf.FloorToInt (seconds / 60.0f);

            seconds -= minutes * 60;

            if (minutes < 10)
                timeStringBuilder.Append("0");
            timeStringBuilder.Append(minutes);
            timeStringBuilder.Append(":");
            if (seconds < 10)
                timeStringBuilder.Append("0");
            timeStringBuilder.Append(seconds);

            return timeStringBuilder.ToString();
        }

        //finds the index of an element inside a IReadOnlyList that satisfies a certain 'match' condition
        public static int FindIndex<T>(IReadOnlyList<T> list, Predicate<T> match)
        {
            int i = 0;
            foreach(T element in list)
            {
                if(match(element))
                    return i;
                i++;
            }
            return -1;
        }

        //finds the index of an element inside a IReadOnlyList
        public static int IndexOf<T>(IReadOnlyList<T> list, T elementToFind)
        {
            int i = 0;
            foreach(T element in list)
            {
                if(Equals(element, elementToFind))
                    return i;
                i++;
            }
            return -1;
        }

        public static void UpdateDropdownValue(ref Dropdown dropdownMenu, string lastOption, List<string> newOptions)
        {
            dropdownMenu.ClearOptions();
            dropdownMenu.AddOptions(newOptions);

            for (int i = 0; i < newOptions.Count; i++)
                if (newOptions[i] == lastOption)
                {
                    dropdownMenu.value = i;
                    return;
                }

            dropdownMenu.value = 0;
        }

        public static bool IsPrefab(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            return
                !gameObject.scene.IsValid() &&
                !gameObject.scene.isLoaded &&
                gameObject.GetInstanceID() >= 0 &&
                // I noticed that ones with IDs under 0 were objects I didn't recognize
                !gameObject.hideFlags.HasFlag(HideFlags.HideInHierarchy);
                // I don't care about GameObjects *inside* prefabs, just the overall prefab.
        }

        public static bool IsValid<T>(this ModelCacheAwareInput<T> input) where T : Component
        {
            return input != null 
                && input.IsStatusValid();
        }
        public static bool IsValid<T>(this GameObjectToComponentInput<T> input) where T : IMonoBehaviour 
        {
            return input != null
                && input.Output.IsValid();
        }

        public static bool DecreaseCountOnElementInCountDictionary(string[] elements, IReadOnlyDictionary<string, int> countDict, ref int count)
        {
            foreach(var code in elements)
            {
                if (countDict.TryGetValue(code, out int storedCount))
                    count -= storedCount;

                if (count <= 0)
                    return true;
            }

            return false;
        }
        #endregion

        #region RTS Engine General Helper Methods
        /// <summary>
        /// Find the first interface that implements a service interface in an implementation or returns the implementing class if it directly implements a service.
        /// </summary>
        /// <param name="implementation"></param>
        /// <returns></returns>
        public static Type GetSuperInterfaceType <T>(this Type implementation)
        {
            // Making sure that the implementation type is not an interface.
            if (!LoggingService.RequireTrue(!implementation.IsInterface,
                    $"[RTSHelper] You are not allowed to use the 'GetSuperInterfaceType' helper method with an interface type."))
                return null;

            // Get all interfaces of the implemetation type, except the IGameService one
            // Because if a higher level interface implements the IGameService one then both will appear from using the GetInterfaces() method.
            Type interfaceType = implementation
                .GetInterfaces()
                .Where(nextInterface => !nextInterface.Equals(typeof(T)))
                .FirstOrDefault(nextInterface => nextInterface.GetInterfaces().ToList().Contains(typeof(T)));

            // Goal here is to find the first interface from the above collection that implements IGameService at any levels of its interface implementation levels.
            return interfaceType.IsValid() ? interfaceType : implementation;
        }

        /// <summary>
        /// Determines whether an entity instance belongs to the local player or not.
        /// </summary>
        /// <param name="entity">IEntity instance to test.</param>
        /// <returns>True if the entity belongs to the local player, otherwise false.</returns>
        public static bool HasAuthority(this IEntity entity)
        {
            return entity.IsValid()
                && (IsMasterInstance() || IsLocalPlayerFaction(entity));
        }

        public static bool HasAuthority(this IEnumerable<IEntity> entities)
        {
            return entities.All(instance => instance.IsValid())
                && (IsMasterInstance() || IsLocalPlayerFaction(entities));
        }

        public static bool IsMasterInstance()
        {
            return GameMgr.IsValid()
                && (!GameMgr.CurrBuilder.IsValid() || GameMgr.CurrBuilder.IsMaster);
        }

        public static bool IsLocalPlayerFaction(this IEntity entity) => 
            entity.IsValid() && !entity.IsFree && entity.Slot.Data.isLocalPlayer;

        public static bool IsLocalPlayerFaction(this IEnumerable<IEntity> entities) => 
            entities
            .All(instance => instance.IsValid()
                && !instance.IsFree 
                && instance.Slot.Data.isLocalPlayer);

        public static bool IsLocalPlayerFaction(this IFactionSlot factionSlot)
            => factionSlot.IsValid() && factionSlot.Data.isLocalPlayer;

        public static bool IsLocalPlayerFaction(this int factionID) => 
            GameMgr.GetFactionSlot(factionID).Data.isLocalPlayer;

        public static bool IsFactionEntity(this IEntity entity, int factionID) 
            => entity.IsValid() && entity.FactionID == factionID;

        public static bool IsSameFaction(this IEntity entity1, IEntity entity2) 
            => entity1.IsValid() && entity2.IsValid() && entity1.FactionID == entity2.FactionID;
        public static bool IsSameFaction(this IEntity entity1, int factionID) 
            => entity1.IsValid()  && entity1.FactionID == factionID;
        public static bool IsSameFaction(this IFactionManager factionMgr, IEntity entity) 
            => factionMgr.IsValid() && entity.IsValid() && entity.FactionID == factionMgr.FactionID;
        public static bool IsSameFaction(this IFactionManager factionMgr, int factionID) 
            => factionMgr.IsValid() && factionID == factionMgr.FactionID;
        public static bool IsSameFaction(this IFactionSlot factionSlot, IEntity entity)
            => factionSlot.IsValid() && entity.IsValid() && entity.FactionID == factionSlot.ID;
        public static bool IsSameFaction(this int factionID1, int factionID2) => factionID1 == factionID2;

        public static bool IsFriendlyFaction(this IEntity source, IEntity target)
            => source.IsValid() && target.IsValid()
            && (target.IsResourceOnly()
                || source.FactionID == target.FactionID);

        public static bool IsFriendlyFaction(this IEntity source, int factionID)
            => source.IsValid()
            && (source.IsResourceOnly()
                || source.FactionID == factionID);

        public static bool IsFriendlyFaction(this IEntity source, IFactionSlot slot)
            => source.IsValid() && slot.IsValid()
            && (source.IsResourceOnly()
                || source.FactionID == slot.ID);

        public static bool IsFriendlyFaction(this int sourceFactionID, int targetFactionID) => sourceFactionID == targetFactionID;

        public static int FREE_FACTION_ID = -1;

        public static bool IsValidFaction(this int factionID) => factionID >= 0 && factionID < GameMgr.FactionCount;

        public static bool IsActiveFaction(this IFactionSlot factionSlot) 
            => factionSlot.IsValid() && factionSlot.State == FactionSlotState.active;

        public static bool IsNPCFaction(this IEntity entity)
            => entity.IsValid() && !entity.IsFree && entity.Slot.Data.role == FactionSlotRole.npc;

        public static bool IsNPCFaction(this IFactionSlot factionSlot)
            => factionSlot.IsValid() && factionSlot.Data.role == FactionSlotRole.npc;

        public static bool IsNPCFaction(this IEnumerable<IEntity> entities) => 
            entities
            .All(instance => instance.IsValid()
                && !instance.IsFree 
                && instance.Slot.Data.role == FactionSlotRole.npc);

        // Disabled since it conflicts with the IsValid method that takes System.Object as parameter
        /*public static bool IsValid(this IMonoBehaviour monoBehaviour)
            => monoBehaviour != null && !monoBehaviour.Equals(null);*/

        public static bool IsValid(this UnityEngine.Object obj)
            => obj != null && !obj.Equals(null);

        public static bool IsValid(this System.Object obj)
            => obj != null && !obj.Equals(null);
        public static bool IsValid(this IEnumerable<IMonoBehaviour> collection)
            => collection.All(obj => obj.IsValid());
        public static bool IsValid(this IReadOnlyList<IMonoBehaviour> collection)
        {
            for (int i = 0; i < collection.Count; i++)
                if (!collection[i].IsValid())
                    return false;

            return true;
        }

        public static int GetKey(this IEntity entity)
            => entity.IsValid() ? entity.Key : InputManager.INVALID_ENTITY_KEY;

        public static string GetKey(this RTSEngineScriptableObject so)
            => so.IsValid() ? so.Key : "Unassigned";

        public static IEnumerable<T> FromGameObject<T>(this IEnumerable<GameObject> gameObjects) where T : IMonoBehaviour
            => gameObjects.Select(obj => obj.IsValid() ? obj.GetComponent<T>() : default);

        public static bool IsEntityTypeMatch(this IEntity entity, EntityType testType)
            => testType.HasFlag(entity.Type);

        /// <summary>
        /// Searches for a building center that allows the given building type to be built inside its territory.
        /// </summary>
        /// <param name="building">Code of the building type to place/build.</param>
        /// <returns></returns>
        public static bool GetEntityFirst<T>(this IEnumerable<T> set, out T entity, Func<T, bool> condition) where T : IEntity
        {
            //if(center.BorderComponent.AllowBuildingInBorder(code))
            entity = default;

            foreach(T instance in set)
                if(condition(instance))
                {
                    entity = instance;
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Sorts a set of instances that extend the IEntity interface into a ChainedSortedList based on the entities code.
        /// </summary>
        /// <typeparam name="T">A type that extends IEntity.</typeparam>
        /// <param name="allComponents">An IEnumerable of instances that extend the IEntity interface.</param>
        /// <param name="filter">Determines what entities are eligible to be added to the chained sorted list and which are not.</param>
        /// <returns>ChainedSortedList instance of the sorted entities based on their code.</returns>
        public static ChainedSortedList<string, T> SortEntitiesByCode <T> (IReadOnlyList<T> allComponents, System.Func<T, bool> filter) where T : IEntity
        {
            //this will hold the resulting chained sorted list.
            ChainedSortedList<string, T> sortedEntities = new ChainedSortedList<string, T>();

            //go through the input entities
            for (int i = 0; i < allComponents.Count; i++)
            {
                T entity = allComponents[i];
                if (filter(entity)) //only if the entity returns true according to the assigned filter
                    sortedEntities.Add(entity.Code, entity); //and add them based on their code
            }

            return sortedEntities;
        }

        /// <summary>
        /// Gets the direction of a list of entities in regards to a target position.
        /// </summary>
        /// <param name="entities">List of IEntity instances.</param>
        /// <param name="targetPosition">Vector3 that represents the position the entities will get their direction to.</param>
        /// <returns>Vector3 that represents the direction of the entities towards the target position.</returns>
        public static Vector3 GetEntitiesDirection (IReadOnlyList<IEntity> entities, Vector3 targetPosition)
        {
            Vector3 direction = Vector3.zero;
            int count = 0;

            for (int i = 0; i < entities.Count; i++) //make a sum of each unit's direction towards the target position
            {
                direction += (targetPosition - entities[i].transform.position).normalized;
                count++;
            }

            return direction / count;
        }

        public static TargetData<T> ToTargetData<T>(this T entity) where T : IEntity
        {
            return entity.IsValid()
                ? new TargetData<T> { instance = entity, position = entity.transform.position }
                : Vector3.zero;
        }

        //Tests whether a set of faction entities are spawned with a certain amount for a particular faction.
        public static bool TestFactionEntityRequirements (this IEnumerable<FactionEntityRequirement> requirements, IFactionManager factionMgr)
            => requirements.All(req => req.TestFactionEntityRequirement(factionMgr));

        public static bool TestFactionEntityRequirement (this FactionEntityRequirement req, IFactionManager factionMgr)
        {
            int requiredAmount = req.amount;

            return (DecreaseCountOnElementInCountDictionary(req.codes.codes, factionMgr.FactionEntityToAmount, ref requiredAmount)
                || DecreaseCountOnElementInCountDictionary(req.codes.categories, factionMgr.FactionEntityCategoryToAmount, ref requiredAmount));
        }

        public static T GetClosestEntity<T> (Vector3 searchPosition, IEnumerable<T> entities) where T : IEntity
        {
            return entities
                .OrderBy(entity => (entity.transform.position - searchPosition).sqrMagnitude)
                .FirstOrDefault();
        }

        public static T GetClosestEntity<T> (Vector3 searchPosition, IEnumerable<T> entities, System.Func<T, bool> condition) where T : IEntity
        {
            return entities
                .Where(entity => condition(entity))
                .OrderBy(entity => (entity.transform.position - searchPosition).sqrMagnitude)
                .FirstOrDefault();
        }

        public static IEnumerable<T> FilterEntities<T>(IEnumerable<T> entities, System.Func<T, bool> condition) where T : IEntity
        {
            return entities
                .Where(entity => condition(entity));
        }

        public static bool IsFactionEntity (this IEntity source)
        {
            return IsUnit(source) || IsBuilding(source);
        }
        public static bool IsUnit(this IEntity source) => source.Type.HasFlag(EntityType.unit);
        public static bool IsBuilding(this IEntity source) => source.Type.HasFlag(EntityType.building);

        public static bool IsResource(this IEntity source) => source.Type.HasFlag(EntityType.resource);
        public static bool IsResourceOnly(this IEntity source) => source.Type == EntityType.resource;

        public static IFactionSlot ToFactionSlot(this int factionID)
            => GameMgr.GetFactionSlot(factionID);

        public static bool TryGameInitPostStart(this Action<IGameManager> sourceInitMethod)
        {
            if (!GameMgr.IsValid())
            {
                LoggingService.LogError("[RTSHelper] Unable to initialize without a valid 'IGameManager' instance! This can only be called when a game is active.");
                return false;
            }

            sourceInitMethod(GameMgr);

            return true;
        }
        #endregion

        #region RTS Engine Entity Component Helper Methods
        public static bool TryGetEntityComponentWithCode (IEntity entity, string code, out IEntityComponent component)
        {
            component = null;

            if (!entity.IsValid())
                return false;

            component = entity.transform
                .GetComponentsInChildren<IEntityComponent>()
                .Where(c => c.Code == code)
                .FirstOrDefault();

            return component.IsValid();
        }

        public static bool OnSingleTaskUIRequest(
            IEntityComponent entityComponent, out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes, EntityComponentTaskUIAsset taskUIAsset, bool requireActiveComponent = true,
            bool extraCondition = true)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            disabledTaskCodes = Enumerable.Empty<string>();

            if (!entityComponent.Entity.CanLaunchTask
                || (!entityComponent.IsActive && requireActiveComponent)
                || !entityComponent.Entity.IsLocalPlayerFaction())
                return false;

            if (taskUIAsset.IsValid())
            {
                if (extraCondition)
                    taskUIAttributes = taskUIAttributes.Append(
                        new EntityComponentTaskUIAttributes
                        {
                            data = taskUIAsset.Data,

                            locked = false
                        });
                else
                    disabledTaskCodes = Enumerable.Repeat(taskUIAsset.Key, 1);
            }

            return true;
        }
        public static ErrorMessage SetEntityComponentActive(IEntityComponent entityComponent, bool active, bool playerCommand)
        {
            CommandInput newInput = new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.setComponentActive,

                playerCommand = playerCommand,

                code = entityComponent.Code,

                intValues = InputMgr.ToIntValues(active ? 1 : 0)
            };

            return InputMgr.SendInput(newInput, entityComponent.Entity, null);
        }

        public static ErrorMessage LaunchEntityComponentAction (IEntityComponent entityComponent, byte actionID, SetTargetInputData input)
        {
            return InputMgr.SendInput(new CommandInput()
            {
                sourceMode = (byte)InputMode.entity,
                targetMode = (byte)InputMode.launchComponentAction,

                sourcePosition = entityComponent.Entity.transform.position,
                targetPosition = input.target.position,
                opPosition = input.target.opPosition,

                intValues = InputMgr.ToIntValues(actionID, (int)input.BooleansToMask()),
                opCode = input.componentCode,

                code = entityComponent.Code,
                playerCommand = input.playerCommand
            },
            source: entityComponent.Entity,
            target: input.target.instance);
        }

        private enum SetTargetFirstManyType { toQueue, attack, mvt, targetFirst }

        public static void SetTargetFirstMany(this IEnumerable<IEntity> entities, SetTargetInputData input)
        {
            if (!input.isMoveAttackRequest && entities.IsLocalPlayerFaction())
                input.isMoveAttackRequest = (AttackMgr.CanAttackMoveWithKey && input.playerCommand);

            var entityGroups = entities
                .GroupBy(entity =>
                {
                    if (entity.TasksQueue.IsValid() && entity.TasksQueue.CanAdd(input))
                        return SetTargetFirstManyType.toQueue;
                    else if (entity.CanAttack && entity.AttackComponent.IsTargetValid(input.target, input.playerCommand) == ErrorMessage.none)
                        return SetTargetFirstManyType.attack;
                    else if (input.includeMovement && entity.CanMove(input.playerCommand))
                        return SetTargetFirstManyType.mvt;
                    else
                        return SetTargetFirstManyType.targetFirst;
                });

            foreach(var group in entityGroups)
            {
                switch(group.Key)
                {
                    case SetTargetFirstManyType.toQueue:
                        foreach (IEntity entity in group)
                            entity.SetTargetFirst(input);

                        break;  
                    case SetTargetFirstManyType.attack:

                        AttackMgr.LaunchAttack(
                            new LaunchAttackData<IReadOnlyList<IEntity>>
                            {
                                source = group.ToList(),

                                targetEntity = input.target.instance as IFactionEntity,
                                targetPosition = input.target.instance.IsValid() ? input.target.instance.transform.position : input.target.position,

                                playerCommand = input.playerCommand,
                                isMoveAttackRequest = input.isMoveAttackRequest
                            });

                        break;

                    case SetTargetFirstManyType.mvt:

                        MvtMgr.SetPathDestination(
                            group.ToList(),
                            input.target.position,
                            0.0f,
                            input.target.instance,
                            new MovementSource { 
                                playerCommand = input.playerCommand,
                                isMoveAttackRequest = input.isMoveAttackRequest
                            });

                        break;

                    default:

                        foreach (IEntity entity in group)
                            InputMgr.SendInput(new CommandInput
                            {
                                sourceMode = (byte)InputMode.entity,
                                targetMode = (byte)InputMode.setComponentTargetFirst,
                                targetPosition = input.target.position,
                                opPosition = input.target.opPosition,

                                playerCommand = input.playerCommand,

                                intValues = InputMgr.ToIntValues((int)input.BooleansToMask())
                            },
                            source: entity,
                            target: input.target.instance);

                        break;
                }
            }
        }

        public delegate ErrorMessage IsTargetValidDelegate(TargetData<IEntity> target, bool playerCommand);
        #endregion

        #region RTS Engine Attack Helper Methods
        public static Vector3 GetAttackTargetPosition(TargetData<IFactionEntity> target)
            => target.instance.IsValid() ? target.instance.Health.AttackTargetPosition : target.opPosition;

        public static ErrorMessage IsAttackLOSBlocked (PathDestinationInputData pathDestinationInput, Vector3 testPosition)
        {
            IEntity entity = pathDestinationInput.refMvtComp?.Entity;
            if (entity == null
                || !entity.CanAttack)
                return ErrorMessage.undefined;

            return entity.AttackComponent.LineOfSight.IsObstacleBlocked(testPosition, pathDestinationInput.target.position)
                ? ErrorMessage.LOSObstacleBlocked
                : ErrorMessage.none;
        }
        #endregion

        #region RTS Engine Determinism Helper Methods

        #endregion
    }
}

