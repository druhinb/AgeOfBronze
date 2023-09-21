using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Upgrades;
using RTSEngine.UnitExtension;
using RTSEngine.Model;

namespace RTSEngine.EntityComponent
{
    public class UnitCreator : PendingTaskEntityComponentBase, IUnitCreator, IEntityPostInitializable
    {
        #region Class Attributes
        [SerializeField, Tooltip("List of unit creation tasks that can be launched through this component.")]
        private List<UnitCreationTask> creationTasks = new List<UnitCreationTask>();
        private List<UnitCreationTask> allCreationTasks;
        public override IReadOnlyList<IEntityComponentTaskInput> Tasks => allCreationTasks;

        [SerializeField, Tooltip("List of unit creation tasks that can be launched through this component after the unit upgrades are unlocked.")]
        private UnitCreationTask[] upgradeTargetCreationTasks = new UnitCreationTask[0];

        [SerializeField, Tooltip("The position at where the created units will spawn.")]
        private ModelCacheAwareTransformInput spawnTransform = null;
        public Vector3 SpawnPosition => spawnTransform.Position;

        // Game services
        protected IEntityUpgradeManager entityUpgradeMgr { private set; get; }
        protected IUnitManager unitMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnPendingInit()
        {
            if (!logger.RequireValid(spawnTransform,
                $"[{GetType().Name} - {Entity.Code}] Field 'Spawn Transform' must be assigned!", source: this)

                || !logger.RequireTrue(creationTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Creation Tasks' array have the 'Prefab Object' field unassigned!", source: this)

                || !logger.RequireTrue(upgradeTargetCreationTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Target Creation Tasks' array have the 'Prefab Object' field unassigned!", source: this))
                return;

            this.entityUpgradeMgr = gameMgr.GetService<IEntityUpgradeManager>();
            this.unitMgr = gameMgr.GetService<IUnitManager>();

            // Initialize creation tasks
            allCreationTasks = new List<UnitCreationTask>();
            int taskID = 0;
            for (taskID = 0; taskID < creationTasks.Count; taskID++)
            {
                creationTasks[taskID].Init(this, taskID, gameMgr);
                creationTasks[taskID].Enable();
            }
            allCreationTasks.AddRange(creationTasks);
            foreach(var nextTask in upgradeTargetCreationTasks)
            {
                nextTask.Init(this, taskID, gameMgr);
                taskID++;
            }
            allCreationTasks.AddRange(upgradeTargetCreationTasks);


            if (!Entity.IsFree
                && entityUpgradeMgr.TryGet (Entity.FactionID, out UpgradeElement<IEntity>[] upgradeElements))
            {
                foreach (var nextElement in upgradeElements)
                {
                    DisableTasksWithPrefabCode(nextElement.sourceCode);
                    EnableUpgradeTargetTasksWithPrefab(nextElement.target);
                }
            }

            globalEvent.UnitUpgradedGlobal += HandleUnitUpgradedGlobal;
        }

        protected override void OnPendingDisabled()
        {
            globalEvent.UnitUpgradedGlobal -= HandleUnitUpgradedGlobal;
        }

        private void DisableTasksWithPrefabCode (string prefabCode)
        {
            foreach (var task in creationTasks)
                if (task.Prefab.Code == prefabCode)
                {
                    task.Disable();
                    Entity.PendingTasksHandler.CancelBySourceID(this, task.ID);
                }
        }

        private void EnableUpgradeTargetTasksWithPrefab(IEntity prefab)
        {
            foreach (var upgradeTargetTask in upgradeTargetCreationTasks)
                if (upgradeTargetTask.Prefab.Code == prefab.Code)
                {
                    creationTasks.Add(upgradeTargetTask);
                    upgradeTargetTask.Enable();
                }
        }
        #endregion

        #region Handling Event: UnitUpgradedGlobal
        private void HandleUnitUpgradedGlobal(IUnit unit, UpgradeEventArgs<IEntity> args)
        {
            if (!Entity.IsSameFaction(args.FactionID))
                return;

            DisableTasksWithPrefabCode(args.UpgradeElement.sourceCode);
            EnableUpgradeTargetTasksWithPrefab(args.UpgradeElement.target);

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
            globalEvent.RaisePendingTaskEntityComponentUpdated(this);
        }
        #endregion

        #region Handling UnitCreation Actions
        protected override ErrorMessage CompleteTaskActionLocal(int creationTaskID, bool playerCommand)
        {
            unitMgr.CreateUnit(
                allCreationTasks[creationTaskID].Prefab,
                SpawnPosition,
                Quaternion.identity,
                new InitUnitParameters
                {
                    factionID = Entity.FactionID,
                    free = false,

                    setInitialHealth = false,

                    giveInitResources = true,

                    rallypoint = factionEntity.Rallypoint,
                    creatorEntityComponent = this,

                    useGotoPosition = true,
                    gotoPosition = SpawnPosition,
                    
                    playerCommand = playerCommand
                });

            return ErrorMessage.none;

        }
        #endregion

        #region Unit Creator Specific Methods
        // Find the task ID that allows to create the unit in the parameter
        public int FindTaskIndex(string unitCode)
        {
            return creationTasks.FindIndex(task => task.Prefab.Code == unitCode);
        }
        #endregion

        #region Task UI
        protected override string GetTooltipText(IEntityComponentTaskInput taskInput)
        {
            UnitCreationTask nextTask = taskInput as UnitCreationTask;

            textDisplayer.UnitCreationTaskToString(
                nextTask,
                out string tooltipText);

            return tooltipText;
        }
        #endregion
    }
}
        