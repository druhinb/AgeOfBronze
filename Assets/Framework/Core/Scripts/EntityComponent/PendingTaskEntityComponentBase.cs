using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Task;
using RTSEngine.UI;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Utilities;

namespace RTSEngine.EntityComponent
{
    public abstract class PendingTaskEntityComponentBase : EntityComponentBase, IPendingTaskEntityComponent
    {
        #region Class Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D {x = 0, y = 0};

        /*
         * Action types and their parameters:
         * launch: target.position.x => task ID/index of the task to launch.
         * complete: target.position.x => task ID/index of the task to complete.
         * */
        public enum ActionType : byte { launch, complete }

        protected IFactionEntity factionEntity { private set; get; }

        // Used by the child class to specify the tasks array (since the type of the task might be different depending on the child class):
        public abstract IReadOnlyList<IEntityComponentTaskInput> Tasks { get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameUITextDisplayManager textDisplayer { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IPendingTaskEntityComponent, PendingTaskEventArgs> PendingTaskAction;
        private void RaisePendingTaskAction(PendingTaskEventArgs args)
        {
            var handler = PendingTaskAction;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.textDisplayer = gameMgr.GetService<IGameUITextDisplayManager>(); 

            this.factionEntity = Entity as IFactionEntity;

            if (!Entity.PendingTasksHandler.IsValid())
            {
                logger.LogError($"[{GetType().Name} - {Entity.Code}] This component requires a component that implements '{typeof(IPendingTasksHandler).Name}' interface to be attached to the source entity!", source: this);
                return;
            }

            OnPendingInit();

            globalEvent.RaisePendingTaskEntityComponentAdded(this);
        }

        protected virtual void OnPendingInit() { }

        protected override void OnDisabled()
        {
            OnPendingDisabled();

            globalEvent.RaisePendingTaskEntityComponentRemoved(this);
        }

        protected virtual void OnPendingDisabled() { }
        #endregion

        #region Activating/Deactivating Component
        protected override void OnActiveStatusUpdated()
        {
            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            globalEvent.RaiseEntityComponentPendingTaskUIReloadRequestGlobal(Entity);
        }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input)
        {
            if (!Entity.CanLaunchTask)
                return ErrorMessage.taskSourceCanNotLaunch;

            switch ((ActionType)actionID)
            {
                case ActionType.launch:
                    return LaunchTaskActionLocal((int)input.target.position.x, input.playerCommand);
                case ActionType.complete:
                    return CompleteTaskActionLocal((int)input.target.position.x, input.playerCommand);
                default:
                    return ErrorMessage.undefined;
            }
        }
        #endregion

        #region Handling Launch Task Action
        public ErrorMessage LaunchTaskAction (int taskID, bool playerCommand)
        {
            if (!RTSHelper.HasAuthority(Entity))
                return ErrorMessage.noAuthority;
            else if (taskID < 0 || taskID >= Tasks.Count)
                return ErrorMessage.invalid;

            ErrorMessage errorMessage = Tasks[taskID].CanStart();
            return errorMessage != ErrorMessage.none
                ? errorMessage
                : LaunchAction(
                    (byte)ActionType.launch,
                    new SetTargetInputData
                    {
                        target = new TargetData<IEntity>
                        {
                            position = new Vector3(taskID, 0.0f, 0.0f)
                        },
                        playerCommand = playerCommand
                    });
        }

        private ErrorMessage LaunchTaskActionLocal(int taskID, bool playerCommand)
        {
            var task = Tasks[taskID];
            PendingTask newPendingTask = new PendingTask
            {
                sourceComponent = this,

                playerCommand = playerCommand,

                sourceTaskInput = task
            };
            Entity.PendingTasksHandler.Add(newPendingTask);

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            RaisePendingTaskAction(new PendingTaskEventArgs(data: newPendingTask, state: PendingTaskState.added));

            return ErrorMessage.none;
        }
        #endregion

        #region Handling Complete Action
        private ErrorMessage CompleteTaskAction(int taskID, bool playerCommand)
        {
            if (!RTSHelper.HasAuthority(Entity))
                return ErrorMessage.noAuthority;
            else if (taskID < 0 || taskID >= Tasks.Count)
                return ErrorMessage.invalid;

            return LaunchAction(
                (byte)ActionType.complete,
                new SetTargetInputData
                {
                    target = new TargetData<IEntity>
                    {
                        position = new Vector3(taskID, 0.0f, 0.0f)
                    },
                    playerCommand = playerCommand
                });
        }

        protected abstract ErrorMessage CompleteTaskActionLocal(int taskID, bool playerCommand);
        #endregion

        #region Task UI
        public override bool OnTaskUIRequest(out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes, out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            disabledTaskCodes = Enumerable.Empty<string>();

            if (!Entity.CanLaunchTask
                || !IsActive
                || !RTSHelper.IsLocalPlayerFaction(Entity))
                return false;

            //for upgrade tasks, we show tasks that do not have required conditions to launch but make them locked.
            taskUIAttributes = Tasks
                .Where(task => task.IsEnabled)
                .Select(task => new EntityComponentTaskUIAttributes
                {
                    data = task.Data,

                    locked = task.CanStart() != ErrorMessage.none,
                    lockedData = task.MissingRequirementData,

                    tooltipText = GetTooltipText(task)
                });

            return true;
        }

        protected abstract string GetTooltipText(IEntityComponentTaskInput taskInput);

        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            return LaunchTaskAction(
                RTSHelper.FindIndex(Tasks, nextTask => nextTask.Data.code == taskAttributes.data.code),
                true) == ErrorMessage.none;
        }
        #endregion

        #region Handling Pending Task
        public void OnPendingTaskPreComplete(PendingTask pendingTask)
        {
            RaisePendingTaskAction(new PendingTaskEventArgs(data: pendingTask, state: PendingTaskState.preCompleted));
        }

        public void OnPendingTaskCompleted(PendingTask pendingTask)
        {
            CompleteTaskAction(pendingTask.sourceTaskInput.ID, pendingTask.playerCommand);

            RaisePendingTaskAction(new PendingTaskEventArgs(data: pendingTask, state: PendingTaskState.completed));
        }

        public void OnPendingTaskCancelled(PendingTask pendingTask)
        {
            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(this);
            RaisePendingTaskAction(new PendingTaskEventArgs(data: pendingTask, state: PendingTaskState.cancelled));
        }
        #endregion
    }
}
