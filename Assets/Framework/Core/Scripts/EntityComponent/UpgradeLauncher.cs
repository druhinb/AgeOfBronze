using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Upgrades;
using RTSEngine.Event;
using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(IEntity))]
    public class UpgradeLauncher : PendingTaskEntityComponentBase, IUpgradeLauncher, IEntityPostInitializable
    {
        #region Class Attributes
        [SerializeField, Tooltip("Task input for EntityUpgrade or EntityComponentUpgrade upgrades that can be launched using this component.")]
        private List<UpgradeTask> upgradeTasks = new List<UpgradeTask>();
        private List<UpgradeTask> allUpgradeTasks;
        public override IReadOnlyList<IEntityComponentTaskInput> Tasks => allUpgradeTasks;

        [SerializeField, Tooltip("List of entity upgrade tasks that can be launched through this component after their entity upgrades are unlocked.")]
        private UpgradeTask[] entityTargetUpgradeTasks = new UpgradeTask[0];

        [SerializeField, Tooltip("List of entity component upgrade tasks that can be launched through this component after their entity component upgrades are unlocked.")]
        private UpgradeTask[] entityComponentTargetUpgradeTasks = new UpgradeTask[0];

        // Game services
        protected IEntityUpgradeManager entityUpgradeMgr { private set; get; }
        protected IEntityComponentUpgradeManager entityCompUpgradeMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnPendingInit()
        {
            if (!logger.RequireTrue(upgradeTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Tasks' array have the 'Prefab Object' field unassigned!")

                || !logger.RequireTrue(entityTargetUpgradeTasks.All(task => task.PrefabObject.IsValid()),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Target Upgrade Tasks' array have the 'Prefab Object' field unassigned!"))
                return;

            this.entityUpgradeMgr = gameMgr.GetService<IEntityUpgradeManager>();
            this.entityCompUpgradeMgr = gameMgr.GetService<IEntityComponentUpgradeManager>();

            // Initialize upgrade tasks
            int taskID = 0;
            allUpgradeTasks = new List<UpgradeTask>();
            for (taskID = 0; taskID < upgradeTasks.Count; taskID++)
            {
                upgradeTasks[taskID].Init(this, taskID, gameMgr);
                upgradeTasks[taskID].Enable();
            }
            allUpgradeTasks.AddRange(upgradeTasks);

            foreach(var nextTask in entityTargetUpgradeTasks)
            {
                nextTask.Init(this, taskID, gameMgr);
                taskID++;
            }
            allUpgradeTasks.AddRange(entityTargetUpgradeTasks);

            foreach(var nextTask in entityComponentTargetUpgradeTasks)
            {
                nextTask.Init(this, taskID, gameMgr);
                taskID++;
            }
            allUpgradeTasks.AddRange(entityComponentTargetUpgradeTasks);

            if (!Entity.IsFree)
            {
                // Divide upgrade tasks into entity upgrades (group with key = true) and entity component upgrades (group with key = false)
                var upgradeTaskGroups = upgradeTasks
                    .GroupBy(task => task.Prefab is EntityUpgrade);

                var entityUpgradeTasks = upgradeTaskGroups
                    .Where(group => group.Key == true)
                    .SelectMany(group => group);

                // Check for already launched entity upgrades:
                if (entityUpgradeMgr.TryGet(Entity.FactionID, out UpgradeElement<IEntity>[] upgradedEntityElements))
                {
                    foreach(var nextElement in upgradedEntityElements)
                    {
                        DisableTasksWithPrefabCode(nextElement.sourceCode, isEntityUpgrade: true);
                        EnableUpgradeTargetTasksWithPrefab(nextElement.target.Code, isEntityUpgrade: true);
                    }
                }

                // Get the EntityComponentUpgrade tasks
                var entityComponentUpgradeTasks = upgradeTaskGroups
                    .Where(group => group.Key == false)
                    .SelectMany(group => group)
                    .ToList();

                // Check for already launched entity component upgrades
                // Go through each EntityComponentUpgrade in the above group
                foreach (EntityComponentUpgrade componentUpgrade in entityComponentUpgradeTasks
                    .Where(task => (task.Prefab is EntityComponentUpgrade))
                    .Select(task => task.Prefab as EntityComponentUpgrade)
                    .ToArray())
                {
                    // For each instance, check if the upgrades have been already launched
                    if (entityCompUpgradeMgr.TryGet(componentUpgrade.SourceEntity, Entity.FactionID, out List<UpgradeElement<IEntityComponent>> upgradedComponentElements))
                    {
                        foreach(var nextElement in upgradedComponentElements)
                        {
                            DisableTasksWithPrefabCode(nextElement.sourceCode, isEntityUpgrade: false);
                            EnableUpgradeTargetTasksWithPrefab(nextElement.target.Code, isEntityUpgrade: false);
                        }
                    }
                }
            }

            globalEvent.EntityUpgradedGlobal += HandleEntityUpgradedGlobal;
            globalEvent.EntityComponentUpgradedGlobal += HandleEntityComponentUpgradedGlobal;
        }

        protected override void OnPendingDisabled()
        {
            globalEvent.EntityUpgradedGlobal -= HandleEntityUpgradedGlobal;
            globalEvent.EntityComponentUpgradedGlobal -= HandleEntityComponentUpgradedGlobal;
        }

        private void DisableTasksWithPrefabCode(string prefabCode, bool isEntityUpgrade)
        {
            // Disable upgraded tasks
            foreach (var task in upgradeTasks)
            {
                bool condition;
                if (isEntityUpgrade)
                {
                    EntityUpgrade entityUpgrade = (task.Prefab as EntityUpgrade);
                    condition = entityUpgrade.IsValid() && entityUpgrade.SourceCode == prefabCode;
                }
                else
                {
                    EntityComponentUpgrade entityComponentUpgrade = (task.Prefab as EntityComponentUpgrade);
                    condition = entityComponentUpgrade.IsValid() && entityComponentUpgrade.GetUpgrade(task.UpgradeIndex).GetSourceCode(task.Prefab.SourceEntity) == prefabCode;
                }

                if(condition)
                {
                    task.Disable();
                    // If there are pending tasks that use the upgraded entity then cancel them.
                    Entity.PendingTasksHandler.CancelBySourceID(this, task.ID);
                }
            }
        }

        private void EnableUpgradeTargetTasksWithPrefab(string targetCode, bool isEntityUpgrade)
        {
            foreach (var upgradeTargetTask in isEntityUpgrade ? entityTargetUpgradeTasks : entityComponentTargetUpgradeTasks)
            {
                string nextCode;
                if (isEntityUpgrade)
                {
                    nextCode = (upgradeTargetTask.Prefab as EntityUpgrade).SourceCode; 
                }
                else
                {
                    nextCode = (upgradeTargetTask.Prefab as EntityComponentUpgrade)
                           .GetUpgrade(upgradeTargetTask.UpgradeIndex)
                           .GetSourceCode(upgradeTargetTask.Prefab.SourceEntity);
                }

                if (nextCode == targetCode)
                {
                    upgradeTasks.Add(upgradeTargetTask);
                    upgradeTargetTask.Enable();
                }
            }
        }
        #endregion

        #region Handling Event: EntityComponentUpgradedGlobal
        private void HandleEntityComponentUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntityComponent> args)
        {
            if (args.FactionID != Entity.FactionID)
                return;

            DisableTasksWithPrefabCode(args.UpgradeElement.sourceCode, isEntityUpgrade: false);
            EnableUpgradeTargetTasksWithPrefab(args.UpgradeElement.target.Code, isEntityUpgrade: false);

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
            globalEvent.RaisePendingTaskEntityComponentUpdated(this);
        }
        #endregion

        #region Handling Event: EntityUpgradedGlobal
        private void HandleEntityUpgradedGlobal(IEntity sender, UpgradeEventArgs<IEntity> args)
        {
            if (args.FactionID != Entity.FactionID)
                return;

            DisableTasksWithPrefabCode(args.UpgradeElement.sourceCode, isEntityUpgrade: true);
            EnableUpgradeTargetTasksWithPrefab(args.UpgradeElement.target.Code, isEntityUpgrade: true);

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
            globalEvent.RaisePendingTaskEntityComponentUpdated(this);
        }
        #endregion

        #region Handling Upgrade Action
        protected override ErrorMessage CompleteTaskActionLocal(int upgradeTaskID, bool playerCommand)
        {
            allUpgradeTasks[upgradeTaskID].Prefab.LaunchLocal(
                gameMgr,
                allUpgradeTasks[upgradeTaskID].UpgradeIndex,
                factionEntity.FactionID);

            return ErrorMessage.none;
        }
        #endregion

        #region Task UI
        protected override string GetTooltipText(IEntityComponentTaskInput taskInput)
        {
            UpgradeTask nextTask = taskInput as UpgradeTask;

            textDisplayer.UpgradeTaskToString(
                nextTask,
                out string tooltipText);

            return tooltipText;
        }
        #endregion
    }
}
