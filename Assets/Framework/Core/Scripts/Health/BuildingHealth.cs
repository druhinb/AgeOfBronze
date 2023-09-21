using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using System;
using RTSEngine.Game;

namespace RTSEngine.Health
{
    public class BuildingHealth : FactionEntityHealth, IBuildingHealth, IEntityPostInitializable
    {
        #region Attributes
        public IBuilding Building { private set; get; }
        public override EntityType EntityType => EntityType.building;

        public override float DestroyObjectDelay => Building.IsPlacementInstance ? 0.0f : base.DestroyObjectDelay;


        [SerializeField, Tooltip("Possible health states that the building can have while it is being constructed.")]
        private List<EntityHealthState> constructionStates = new List<EntityHealthState>();  

        [SerializeField, Tooltip("State to activate when the building completes construction, a transition state from construction states to regular building states.")]
        private EntityHealthState constructionCompleteState = new EntityHealthState();
        #endregion

        #region Initializing/Terminating
        protected override void OnFactionEntityHealthInit()
        {
            Building = Entity as IBuilding;
        }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            // Show the construction state only if this is not the placement instance
            // We also check for whether the building has been built or not because in case of a faction conversion, components are re-initiated and this would cause the construction states to appear.
            if(!Building.IsPlacementInstance && !Building.IsBuilt) 
                stateHandler.Reset(constructionStates, CurrHealth);
        }
        #endregion

        #region Updating Health
        protected override void OnHealthUpdated(HealthUpdateArgs args)
        {
            base.OnHealthUpdated(args);

            globalEvent.RaiseBuildingHealthUpdatedGlobal(Building, args);
        }

        protected override void OnMaxHealthReached(HealthUpdateArgs args)
        {
            if(Building.IsBuilt)
            {
                stateHandler.Activate(constructionCompleteState);

                stateHandler.Reset(States, CurrHealth);
            }

            base.OnMaxHealthReached(args);
        }
        #endregion

        #region Destroying Building
        protected override void OnDestroyed(bool upgrade, IEntity source)
        {
            base.OnDestroyed(upgrade, source);

            globalEvent.RaiseBuildingDeadGlobal(Building, new DeadEventArgs(upgrade, source, DestroyObjectDelay));
        }
        #endregion
    }
}
