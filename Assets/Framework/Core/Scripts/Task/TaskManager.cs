using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Event;
using RTSEngine.EntityComponent;
using System;
using System.Collections.Generic;
using RTSEngine.Entities;
using System.Linq;
using RTSEngine.Controls;

namespace RTSEngine.Task
{
    [Serializable]
    public struct EntityComponentTaskInputData
    {
        //public bool isEnabled;
        public int launchTimes;
    }

    public class TaskManager : MonoBehaviour, ITaskManager
    {
        #region Attributes
        [SerializeField, Tooltip("Cursor and awaiting task settings")]
        private EntityComponentAwaitingTask awaitingTask = new EntityComponentAwaitingTask();
        public EntityComponentAwaitingTask AwaitingTask => awaitingTask;

        // Key1: Entity key that has the component where the task inputs are
        // Key2: Code of the component where the task inputs are
        // Key3: TaskID of the task input
        // Value: Actual task input data
        public IReadOnlyDictionary<int, Dictionary<string, Dictionary<int, EntityComponentTaskInputData>>> EntityComponentTaskInputInitialData { private set; get; }
        public Dictionary<IEntity, Dictionary<IEntityComponent, List<IEntityComponentTaskInput>>> entityComponentTaskInputTracker;

        [Space, SerializeField, Tooltip("When held down, this key allows to add the command to the entity's task queue instead of launching it immediately.")]
        private ControlType tasksQueueKey = null;
        public bool IsTaskQueueEnabled => tasksQueueKey.IsValid() && Input.GetKey(tasksQueueKey.DefaultKeyCode);

        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>(); 

            awaitingTask.Init(gameMgr);

            entityComponentTaskInputTracker = new Dictionary<IEntity, Dictionary<IEntityComponent, List<IEntityComponentTaskInput>>>();

            globalEvent.EntityComponentTaskInputInitializedGlobal += HandleEntityComponentTaskInputInitializedGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.EntityComponentTaskInputInitializedGlobal -= HandleEntityComponentTaskInputInitializedGlobal;
        }
        #endregion

        #region Handling Events: IEntityComponentTaskInput
        private void HandleEntityComponentTaskInputInitializedGlobal(IEntityComponentTaskInput taskInput, EventArgs args)
        {
            if (!entityComponentTaskInputTracker.ContainsKey(taskInput.Entity))
            {
                entityComponentTaskInputTracker.Add(taskInput.Entity, new Dictionary<IEntityComponent, List<IEntityComponentTaskInput>>());
                taskInput.Entity.Health.EntityDead += HandleEntityComponentTaskInputSourceDead;
            }

            if (!entityComponentTaskInputTracker[taskInput.Entity].ContainsKey(taskInput.SourceComponent))
                entityComponentTaskInputTracker[taskInput.Entity].Add(taskInput.SourceComponent, new List<IEntityComponentTaskInput>());

            entityComponentTaskInputTracker[taskInput.Entity][taskInput.SourceComponent].Add(taskInput);
        }

        private void HandleEntityComponentTaskInputSourceDead(IEntity deadEntity, DeadEventArgs args)
        {
            entityComponentTaskInputTracker.Remove(deadEntity);
        }

        public void ResetEntityComponentTaskInputInitialData(IReadOnlyDictionary<int, Dictionary<string, Dictionary<int, EntityComponentTaskInputData>>> newInitialData)
        {
            EntityComponentTaskInputInitialData = newInitialData;
        }

        public IReadOnlyDictionary<IEntityComponent, Dictionary<int, EntityComponentTaskInputData>> EntityComponentTaskInputTrackerToData()
        {
            return entityComponentTaskInputTracker
                .Values
                .SelectMany(value => value.Keys)
                .ToDictionary(
                    component => component,
                    component => entityComponentTaskInputTracker[component.Entity][component]
                        .ToDictionary(
                            taskInput => taskInput.ID,
                            taskInput => new EntityComponentTaskInputData
                            {
                                //isEnabled = taskInput.IsEnabled,
                                launchTimes = taskInput.LaunchTimes
                            }));
        }

        public bool TryGetEntityComponentTaskInputInitialData(IEntityComponent sourceComponent, int taskID, out EntityComponentTaskInputData data)
        {
            if(EntityComponentTaskInputInitialData.IsValid()
                && EntityComponentTaskInputInitialData.ContainsKey(sourceComponent.Entity.Key))
                if (EntityComponentTaskInputInitialData[sourceComponent.Entity.Key].ContainsKey(sourceComponent.Code))
                    if (EntityComponentTaskInputInitialData[sourceComponent.Entity.Key][sourceComponent.Code].ContainsKey(taskID))
                    {
                        data = EntityComponentTaskInputInitialData[sourceComponent.Entity.Key][sourceComponent.Code][taskID];
                        return true;
                    }

            data = default;
            return false;
        }
        #endregion
    }
}