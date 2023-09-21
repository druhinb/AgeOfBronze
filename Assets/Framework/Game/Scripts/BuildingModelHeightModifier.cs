using System;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;

namespace RTSEngine.Demo
{
    public class BuildingModelHeightModifier : ModelHeightModifierBase
    {
        #region Attributes
        protected IBuilding building { private set; get; }

        [SerializeField]
        private ModelPositionModifierData constructionModifier = new ModelPositionModifierData { speed = new TimeModifiedFloat(0.5f) };

        [SerializeField]
        private ModelPositionModifierData destructionModifier = new ModelPositionModifierData { speed = new TimeModifiedFloat(10.0f) };
        private float destroyDelay;
        private float deathTimer;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            this.building = entity as IBuilding;

            Deactivate(constructionModifier.targetHeight);

            building.Health.EntityDead += HandleEntityDead;

            if (building.IsPlacementInstance || building.IsBuilt)
                return;

            building.EntityInitiated += HandleEntityInitiated;
            building.BuildingBuilt += HandleBuildingBuilt;
        }

        protected override void OnDisabled()
        {
            building.EntityInitiated -= HandleEntityInitiated;
            building.BuildingBuilt -= HandleBuildingBuilt;
        }
        #endregion

        #region Handling Event: Building Placed
        private void HandleEntityInitiated(IEntity entity, EventArgs args)
        {
            // Building already consturcted, do not enable this effect
            if (building.IsBuilt)
                return;

            // Start this component when the building is placed.
            Activate(constructionModifier, UpdateTargetConstructionHeight);
            //building.Health.EntityHealthUpdated += HandleEntityHealthUpdated;
        }
        #endregion

        #region Handling Event: Building Built
        private void HandleBuildingBuilt(IBuilding sender, EventArgs args)
        {
            // Stop this elevator effect as soon as the building is completely built.
            Deactivate(constructionModifier.targetHeight);
            //building.Health.EntityHealthUpdated -= HandleEntityHealthUpdated;
        }
        #endregion

        #region Handling Event: Entity Dead
        private void HandleEntityDead(IEntity sender, DeadEventArgs args)
        {
            destroyDelay = args.DestroyObjectDelay;
            if (destroyDelay <= 0.0f)
                return;

            deathTimer = destroyDelay;
            destructionModifier.initialHeight = Model.LocalPosition.y;
            Activate(destructionModifier, UpdateTargetDestructionHeight);

            building.Health.EntityDead -= HandleEntityDead;
        }
        #endregion

        #region Updating Building Height
        private void HandleEntityHealthUpdated(IEntity entity, HealthUpdateArgs e) => UpdateTargetConstructionHeight();

        private float UpdateTargetConstructionHeight()
        {
            return building.Health.HealthRatio * currTargetHeight + currModifier.initialHeight;
        }

        private float UpdateTargetDestructionHeight()
        {
            deathTimer -= Time.deltaTime;
            return ((destroyDelay - deathTimer) / destroyDelay) * currTargetHeight + currModifier.initialHeight;
        }
        #endregion
    }
}
