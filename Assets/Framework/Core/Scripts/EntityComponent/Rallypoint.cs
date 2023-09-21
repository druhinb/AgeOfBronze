using System;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.UI;
using RTSEngine.Movement;
using RTSEngine.Terrain;
using RTSEngine.Model;
using System.Collections;
using System.Collections.Generic;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.EntityComponent
{
    [RequireComponent(typeof(IFactionEntity))]
    public class Rallypoint : FactionEntityTargetComponent<IEntity>, IRallypoint
    {
        #region Attributes
        /*
         * Action types and their parameters:
         * send: Target.instance is the target unit to send to the rallypoint.
         * */
        public enum ActionType : byte { send }
        public override bool IsIdle => true;
        public override bool AllowPreEntityInit => true;

        [SerializeField, Tooltip("Initial rallypoint transform. Determines where created units will move to after they spawn.")]
        private ModelCacheAwareTransformInput gotoTransform = null;
        public Vector3 GotoPosition => gotoTransform.Position;

        [SerializeField, Tooltip("If populated then this defines the types of terrain areas where the rallypoint can be placed at.")]
        private TerrainAreaType[] forcedTerrainAreas = new TerrainAreaType[0];
        public IReadOnlyList<TerrainAreaType> ForcedTerrainAreas => forcedTerrainAreas;
        [SerializeField, Tooltip("If populated, this defines the types of terrain areas that the rallypoint can not be placed at.")]
        private TerrainAreaType[] forbiddenTerrainAreas = new TerrainAreaType[0];

        [SerializeField, Tooltip("Enable to define constraints on the range of the rallypoint from the source faction entity.")]
        private bool maxDistanceEnabled = false;
        [SerializeField, Tooltip("The maximum allowed distance between the faction entity and the rallypoint."), Min(0.0f)]
        private float maxDistance = 50.0f;
        // Enabled when the goto position target is an entity with a valid movement component
        // When SendAction is called and this field is enabled, this tells us to redo the IsTargetInRange calculations
        private bool trackGotoTargetPosition;

        [SerializeField, Tooltip("When enabled, this component will attempt to reposition the rallypoint object on building placement around the entity's radius to attempt and find a position that satisfies the rallypoint's forced terrain areas.")]
        private bool repositionToValidTerrainArea = true;
        [SerializeField, Tooltip("The offset size when repositioning the rallypoint around this faction entity's radius. The smaller, the more potential positions to be sampled.")]
        private float repositionSize = 1.0f;

        // Rallypoint component does not require the faction entity it is attached on to be idle when picking a target
        public override bool RequireIdleEntity => false;
        // Rallypoint always has a target, which is the GotoPosition
        public override bool HasTarget => true;

        private float initialAngle;
        private Vector3 initialDirection;

        // Game services
        protected ITerrainManager terrainMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnTargetInit()
        {
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();

            if (!logger.RequireValid(gotoTransform,
                  $"[{GetType().Name} - {Entity.Code}] The 'Goto Transform' field must be assigned!")

                || !logger.RequireTrue(forcedTerrainAreas.Length == 0 || forcedTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Entity.Code}] The 'Forced Terrain Areas' field must be either empty or populated with valid elements!")

                || !logger.RequireTrue(forbiddenTerrainAreas.Length == 0 || forbiddenTerrainAreas.All(terrainArea => terrainArea.IsValid()),
                  $"[{GetType().Name} - {Entity.Code}] The 'Forbidden Terrain Areas' field must be either empty or populated with valid elements!"))
                return;

            initialAngle = Vector3.Angle(factionEntity.transform.position + Vector3.right * factionEntity.Radius, GotoPosition - factionEntity.transform.position);
            initialDirection = GotoPosition - factionEntity.transform.position;

            SetGotoTransformActive(factionEntity.IsDummy);

            // Set the initial goto position for buildings when they are completely built for the first time (else task will not go through due to building being unable to launch any task).
            if (factionEntity.IsBuilding())
            {
                IBuilding building = factionEntity as IBuilding;
                if (building.IsPlacementInstance)
                    building.PlacerComponent.BuildingPlacementPositionUpdated += HandleBuildingPlacementPositionUpdated;
                else
                    building.BuildingBuilt += HandleBuildingBuilt;
            }
            else
                SetTarget(GotoPosition, false);

            // Rallypoint component requires no auto-target search
            TargetFinder.Enabled = false;

            this.factionEntity.Selection.Selected += HandleFactionEntitySelectionUpdated;
            this.factionEntity.Selection.Deselected += HandleFactionEntitySelectionUpdated;
        }

        private void HandleBuildingPlacementPositionUpdated(IBuilding sender, EventArgs args)
        {
            TryFindValidInitialGotoPosition();
        }

        private void HandleBuildingBuilt(IBuilding building, EventArgs args)
        {
            if(!TryFindValidInitialGotoPosition())
                  logger.LogError($"[{GetType().Name} - {Entity.Code}] Unable to update the goto transform position as it is initial position does not comply with the forced and forbidden terrain areas!");

            building.BuildingBuilt -= HandleBuildingBuilt;
        }

        protected override void OnTargetDisabled()
        {
            if (factionEntity.IsValid())
            {
                factionEntity.Selection.Selected -= HandleFactionEntitySelectionUpdated;
                factionEntity.Selection.Deselected -= HandleFactionEntitySelectionUpdated;
            }
        }
        #endregion

        #region Handling Component Upgrade
        protected override void OnComponentUpgraded(FactionEntityTargetComponent<IEntity> sourceFactionEntityTargetComponent)
        {
            gotoTransform.Position = sourceFactionEntityTargetComponent.Target.position;
        }
        #endregion

        #region Handling Events: Entity Selection
        private void HandleFactionEntitySelectionUpdated(IEntity entity, EventArgs args)
        {
            if (!entity.IsLocalPlayerFaction()
                || !entity.CanLaunchTask)
                return;

            SetGotoTransformActive(factionEntity.Selection.IsSelected);
        }
        #endregion

        #region Searching/Updating Target
        public override bool CanSearch => false;

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
            => !maxDistanceEnabled || Vector3.Distance(sourcePosition, target.position) <= maxDistance;

        public override ErrorMessage IsTargetValid(TargetData<IEntity> potentialTarget, bool playerCommand)
        {
            if (potentialTarget.instance.IsValid())
            {
                if (!potentialTarget.instance.IsInteractable)
                    return ErrorMessage.uninteractable;
                else if (potentialTarget.instance == Entity)
                    return ErrorMessage.invalid;
            }

            if (!IsTargetInRange(factionEntity.transform.position, potentialTarget.position))
                return ErrorMessage.rallypointTargetNotInRange;
            else if (!terrainMgr.GetTerrainAreaPosition(potentialTarget.position, forcedTerrainAreas, out _)
                || (forbiddenTerrainAreas.Length > 0 && terrainMgr.GetTerrainAreaPosition(potentialTarget.position, forbiddenTerrainAreas, out _)))
                return ErrorMessage.rallypointTerrainAreaMismatch;

            return ErrorMessage.none;
        }

        protected override void OnTargetPostLocked(SetTargetInputData input, bool sameTarget)
        {
            // In the case where the rallypoint sends
            if (!Target.instance.IsValid())
                gotoTransform.Position = Target.position;

            if (input.playerCommand && factionEntity.IsLocalPlayerFaction())
            {
                SetGotoTransformActive(!Target.instance.IsValid() && factionEntity.Selection.IsSelected);

                if (Target.instance.IsValid())
                {
                    mouseSelector.FlashSelection(Target.instance, factionEntity.IsFriendlyFaction(Target.instance));
                    trackGotoTargetPosition = Target.instance.MovementComponent.IsValid();
                }
            }
        }

        public void SetGotoTransformActive(bool active)
        {
            gotoTransform.IsActive = active;

            OnGotoTransformActiveUpdated(active);
        }

        protected virtual void OnGotoTransformActiveUpdated(bool active) { }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input)
        {
            switch ((ActionType)actionID)
            {
                case ActionType.send:
                    return SendActionLocal(input.target.instance as IUnit, input.playerCommand);

                default:
                    return base.LaunchActionLocal(actionID, input);
            }
        }
        #endregion

        #region Handling Rallypoint
        public ErrorMessage SendAction(IUnit unit, bool playerCommand)
        {
            return LaunchAction(
                (byte)ActionType.send,
                new SetTargetInputData
                {
                    target = new TargetData<IEntity> { instance = unit, position = GotoPosition },
                    playerCommand = playerCommand
                });
        }

        public ErrorMessage SendActionLocal(IUnit unit, bool playerCommand)
        {
            if (Target.instance.IsValid() && (!trackGotoTargetPosition || IsTargetInRange(Entity.transform.position, Target)))
                return unit.SetTargetFirstLocal(new SetTargetInputData { target = Target, playerCommand = false });

            return mvtMgr.SetPathDestination(
                    unit,
                    GotoPosition,
                    0.0f,
                    null,
                    new MovementSource { playerCommand = false });
        }
        #endregion

        private bool TryFindValidInitialGotoPosition()
        {
            Vector3 originalPosition = factionEntity.transform.position + initialDirection;
            if (IsTargetValid(originalPosition, playerCommand: false) == ErrorMessage.none)
            {
                if (factionEntity.IsDummy)
                    gotoTransform.Position = originalPosition;
                else
                    SetTarget(originalPosition, false);

                return true;
            }

            if (repositionToValidTerrainArea)
            {
                int expectedPositionCount = Mathf.FloorToInt(2.0f * Mathf.PI * factionEntity.Radius / (repositionSize * 2.0f));

                // If no expected positions are to be found and the radius offset is zero then set the expected position count to 1 to test the actual target position if it is valid
                if (expectedPositionCount == 0 && factionEntity.Radius == 0.0f)
                    expectedPositionCount = 1;

                // Represents increment value of the angle inside the current circle with the above perimeter
                float angleIncValue = 360f / expectedPositionCount;
                float currentAngle = initialAngle;

                Vector3 nextDestination = factionEntity.transform.position + factionEntity.Radius * new Vector3(Mathf.Cos(Mathf.Deg2Rad * currentAngle), 0.0f, Mathf.Sin(Mathf.Deg2Rad * currentAngle));

                int counter = 0;

                Vector3 closestPosition = Vector3.zero;
                float closestDistance = Mathf.Infinity;
                bool foundPosition = false;

                // As long as we haven't inspected all the expected free positions inside this cirlce
                while (counter < expectedPositionCount)
                {
                    if (IsTargetValid(nextDestination, false) == ErrorMessage.none
                        && Vector3.Distance(nextDestination, originalPosition) < closestDistance)
                    {
                        foundPosition = true;
                        closestDistance = Vector3.Distance(nextDestination, originalPosition);
                        closestPosition = nextDestination;
                    }

                    // Increment the angle value to find the next position on the circle
                    currentAngle += angleIncValue;

                    // Rotate the nextDestination vector around the y axis by the current angle value
                    nextDestination = factionEntity.transform.position + factionEntity.Radius * new Vector3(Mathf.Cos(Mathf.Deg2Rad * currentAngle), 0.0f, Mathf.Sin(Mathf.Deg2Rad * currentAngle));

                    counter++;
                }

                if (foundPosition)
                {
                    if (factionEntity.IsDummy)
                        gotoTransform.Position = closestPosition;
                    else
                        SetTarget(closestPosition, false);

                    return true;
                }
            }

            return false;
        }
    }
}
