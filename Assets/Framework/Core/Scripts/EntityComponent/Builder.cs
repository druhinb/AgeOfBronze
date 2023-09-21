﻿using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UI;
using RTSEngine.Upgrades;
using RTSEngine.BuildingExtension;
using RTSEngine.Audio;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
	public class Builder : FactionEntityTargetProgressComponent<IBuilding>, IBuilder
    {
        #region Attributes 
        private IUnit unit;

        [SerializeField, Tooltip("Health amount to add to the constructed building every progress round."), Min(0)]
        private int healthPerProgress = 5;

        [SerializeField, Tooltip("Define the buildings that can be constructed by this builder.")]
        private FactionEntityTargetPicker targetPicker = new FactionEntityTargetPicker();
        [SerializeField, Tooltip("When enabled, the restrictions defined in the target picker will only apply to placing the buildings but the builder will still be able to construct buildings that are already placed.")]
        private bool restrictBuildingPlacementOnly = true;

        [SerializeField, Tooltip("List of building creation tasks that can be launched through this component.")]
        private List<BuildingCreationTask> creationTasks = new List<BuildingCreationTask>();
        public IReadOnlyList<BuildingCreationTask> CreationTasks => creationTasks;

        [SerializeField, Tooltip("List of building creation tasks that can be launched through this component after the building upgrades are unlocked.")]
        private BuildingCreationTask[] upgradeTargetCreationTasks = new BuildingCreationTask[0];
        public IReadOnlyList<BuildingCreationTask> UpgradeTargetCreationTasks => upgradeTargetCreationTasks.ToList();

        [SerializeField, Tooltip("What audio clip to play when constructing a building?")]
		private AudioClipFetcher constructionAudio = new AudioClipFetcher();

        // Game services
        protected IEntityUpgradeManager entityUpgradeMgr { private set; get; }
        protected IBuildingPlacement placementMgr { private set; get; } 
        protected IGameUITextDisplayManager textDisplayer { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected override void OnProgressInit()
        {
            unit = Entity as IUnit;

            if (!logger.RequireTrue(healthPerProgress >= 0,
                $"[{GetType().Name} - {unit.Code}] The 'Health Per Progress' must have a positive value.")

                || !logger.RequireTrue(creationTasks.All(task => task.PrefabObject != null),
                $"[{GetType().Name} - {unit.Code}] Some elements in the 'Creation Tasks' array have the 'Prefab Object' field unassigned!")

                || !logger.RequireTrue(upgradeTargetCreationTasks.All(task => task.PrefabObject != null),
                $"[{GetType().Name} - {Entity.Code}] Some elements in the 'Upgrade Target Creation Tasks' array have the 'Prefab Object' field unassigned!"))
                return;

            this.entityUpgradeMgr = gameMgr.GetService<IEntityUpgradeManager>();
            this.placementMgr = gameMgr.GetService<IBuildingPlacement>(); 
            this.textDisplayer = gameMgr.GetService<IGameUITextDisplayManager>();

            // Initialize creation tasks
            int taskID = 0;
            for (taskID = 0; taskID < creationTasks.Count; taskID++)
            {
                creationTasks[taskID].Init(this, taskID, gameMgr);
                creationTasks[taskID].Enable();
            }
            foreach(var nextTask in upgradeTargetCreationTasks)
            {
                nextTask.Init(this, taskID, gameMgr);
                taskID++;
            }

            // Check for building upgrades:
            if (!Entity.IsFree
                && entityUpgradeMgr.TryGet (Entity.FactionID, out UpgradeElement<IEntity>[] upgradeElements))
            {
                foreach(var nextElement in upgradeElements)
                {
                    DisableTasksWithPrefabCode(nextElement.sourceCode);
                    EnableUpgradeTargetTasksWithPrefab(nextElement.target);
                }
            }

            globalEvent.BuildingUpgradedGlobal += HandleBuildingUpgradedGlobal;
        }

        protected override void OnTargetDisabled()
        {
            globalEvent.BuildingUpgradedGlobal -= HandleBuildingUpgradedGlobal;
        }

        private void DisableTasksWithPrefabCode (string prefabCode)
        {
            foreach (var task in creationTasks)
                if (task.Prefab.Code == prefabCode)
                    task.Disable();
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

        #region Handling Events: Building Upgrade
        private void HandleBuildingUpgradedGlobal(IBuilding building, UpgradeEventArgs<IEntity> args)
        {
            if (!Entity.IsSameFaction(args.FactionID))
                return;

            // Disable the upgraded tasks
            DisableTasksWithPrefabCode(args.UpgradeElement.sourceCode);
            EnableUpgradeTargetTasksWithPrefab(args.UpgradeElement.target);

            // Remove the building creation task of the unit that has been upgraded

            globalEvent.RaiseEntityComponentTaskUIReloadRequestGlobal(
                this,
                new TaskUIReloadEventArgs(reloadAll: true));
        }
        #endregion

        #region Updating Component State
        protected override bool MustStopProgress()
        {
            return Target.instance.Health.IsDead
                || Target.instance.Health.CurrHealth >= Target.instance.Health.MaxHealth
                || Target.instance.FactionID != factionEntity.FactionID
                || (InProgress && !IsTargetInRange(transform.position, Target));
        }

        protected override bool CanEnableProgress()
        {
            return IsTargetInRange(transform.position, Target);
        }

        protected override bool CanProgress() => true;

        protected override bool MustDisableProgress() => false;

        protected override void OnProgressStop()
        {
            if (LastTarget.instance.IsValid())
                LastTarget.instance.WorkerMgr.Remove(unit);
        }
        #endregion

        #region Handling Progress
        protected override void OnInProgressEnabled()
        {
            audioMgr.PlaySFX(unit.AudioSourceComponent, constructionAudio.Fetch(), true);

            globalEvent.RaiseEntityComponentTargetStartGlobal(this, new TargetDataEventArgs(Target));

            unit.MovementComponent.UpdateRotationTarget(Target.instance, Target.instance.transform.position);
        }

        protected override void OnProgress()
        {
            Target.instance.Health.Add(new HealthUpdateArgs(healthPerProgress, unit));
        }
        #endregion

        #region Searching/Updating Target
        public override bool CanSearch => true;

        public override float GetProgressRange()
            => Target.instance.WorkerMgr.GetOccupiedPosition(unit, out _)
            ? mvtMgr.StoppingDistance
            : base.GetProgressRange();
        public override Vector3 GetProgressCenter() 
            => Target.instance.WorkerMgr.GetOccupiedPosition(unit, out Vector3 workerPosition)
            ? workerPosition
            : base.GetProgressCenter();

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            return (target.instance as IBuilding).WorkerMgr.GetOccupiedPosition(unit, out Vector3 workerPosition)
                ? Vector3.Distance(sourcePosition, workerPosition) <= mvtMgr.StoppingDistance
                : base.IsTargetInRange(sourcePosition, target);
        }

        public override ErrorMessage IsTargetValid (TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IBuilding> potentialTarget = testTarget;

            if (!IsActive || !potentialTarget.instance.IsValid())
                return ErrorMessage.invalid;
            else if (!potentialTarget.instance.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (!RTSHelper.IsSameFaction(potentialTarget.instance, factionEntity))
                return ErrorMessage.factionMismatch;
            else if (!restrictBuildingPlacementOnly && !targetPicker.IsValidTarget(potentialTarget.instance))
                return ErrorMessage.entityCompTargetPickerUndefined;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.dead;
            else if (potentialTarget.instance.Health.HasMaxHealth)
                return ErrorMessage.healthtMaxReached;
            else if (!factionEntity.CanMove() && !IsTargetInRange(transform.position, potentialTarget))
                return ErrorMessage.entityCompTargetOutOfRange;
            else if (Target.instance != potentialTarget.instance && potentialTarget.instance.WorkerMgr.HasMaxAmount)
                return ErrorMessage.workersMaxAmountReached;

            return potentialTarget.instance.WorkerMgr.CanMove(unit);
        }

        protected override void OnTargetPostLocked(SetTargetInputData input, bool sameTarget)
        {
            // For the worker component manager, make sure that enough worker positions is available even in the local method.
            // Since they are only updated in the local method, meaning that the SetTarget method would always relay the input in case a lot of consecuive calls are made...
            //... on the same resource from multiple collectors.
            if(Target.instance.WorkerMgr.Move(
                unit,
                new AddableUnitData(sourceTargetComponent: this, input)) != ErrorMessage.none)
            {
                Stop();
                return;
            }

            globalEvent.RaiseEntityComponentTargetLockedGlobal(this, new TargetDataEventArgs(Target));
        }
        #endregion

        #region Task UI
        public override bool OnTaskUIRequest(
            out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes)
        {
            if (base.OnTaskUIRequest(out taskUIAttributes, out disabledTaskCodes) == false)
                return false;

            // For building creation tasks, we show building creation tasks that do not have required conditions to launch but make them locked.
            taskUIAttributes = taskUIAttributes
                .Concat(creationTasks
                    .Select(task => new EntityComponentTaskUIAttributes
                    {
                        data = task.Data,

                        // Wa want the building placement process to start once, this avoids having each builder component instance launch a placement
                        launchOnce = true,

                        locked = task.CanStart() != ErrorMessage.none,
                        lockedData = task.MissingRequirementData,

                        tooltipText = GetTooltipText(task)
                    }));

            return true;
        }

        public override bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes)
        {
            if (base.OnTaskUIClick(taskAttributes))
                return true;

            // If it's not the launch task (task that makes the builder construct a building) then it is a building placement task.
            foreach (BuildingCreationTask creationTask in creationTasks)
                if (creationTask.Data.code == taskAttributes.data.code)
                {
                    placementMgr.StartPlacement(creationTask);
                    return true;
                }

            return false;
        }

        private string GetTooltipText(BuildingCreationTask nextTask)
        {
            textDisplayer.BuildingCreationTaskToString(
                nextTask,
                out string tooltipText);

            return tooltipText;
        }
        #endregion
    }
}