using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.BuildingExtension;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.Logging;
using RTSEngine.UnitExtension;
using System;
using RTSEngine.Selection;

namespace RTSEngine.Upgrades
{ 
    public class EntityUpgradeManager : MonoBehaviour, IEntityUpgradeManager
    {
        #region Attributes
        // Holds the elements that define the entity upgrades for each faction slot
        private List<UpgradeElement<IEntity>>[] elements;
        public IEnumerable<IEnumerable<UpgradeElement<IEntity>>> Elements => elements;

        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IUnitManager unitMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; } 
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        protected ISelectionManager selectionMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.unitMgr = gameMgr.GetService<IUnitManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>(); 

            gameMgr.GameBuilt += HandleGameBuilt;
        }

        private void OnDestroy()
        {
            gameMgr.GameBuilt -= HandleGameBuilt;
        }

        private void HandleGameBuilt(IGameManager sender, EventArgs args)
        {
            // When the game is built, the factions slots are ready.
            elements = gameMgr.FactionSlots
                .Select(factionSlot => new List<UpgradeElement<IEntity>>())
                .ToArray();

            gameMgr.GameBuilt -= HandleGameBuilt;
        }
        #endregion

        #region Fetching Entity Upgrade Data
        public bool TryGet (int factionID, out UpgradeElement<IEntity>[] upgradeElements)
        {
            upgradeElements = null;

            if (!RTSHelper.IsValidFaction(factionID))
            {
                logger.LogError($"[EntityUpgradeManager] Attempting to get entity upgrade elements for faction ID: {factionID} is not allowed!",
                    source: this);
                return false;
            }

            upgradeElements = elements[factionID].ToArray();
            return true;
        }

        public bool IsLaunched(EntityUpgrade upgrade, EntityUpgradeElementSource upgradeSource, int factionID)
        {
            if (!RTSHelper.IsValidFaction(factionID))
            {
                logger.LogError($"[EntityUpgradeManager] Attempting to check entity upgrade for invalid faction ID: {factionID} is not allowed!",
                    source: this);
                return false;
            }

            return elements[factionID]
                .Where(element => element.sourceCode == upgrade.SourceCode
                    && element.target == upgradeSource.UpgradeTarget)
                .Any();
        }
        #endregion

        #region Launching Entity Upgrades
        public ErrorMessage LaunchLocal(EntityUpgrade upgrade, EntityUpgradeElementSource upgradeSource, int factionID)
        {
            if (!RTSHelper.IsValidFaction(factionID))
            {
                logger.LogError($"[EntityUpgradeManager - {upgrade}] Attempting to launch entity upgrade for invalid faction ID: {factionID} is not allowed!", source: this);
                return ErrorMessage.invalid;
            }
            else if (!upgradeSource.UpgradeTarget.IsValid())
            {
                logger.LogError($"[EntityUpgradeManager - {upgrade}] Attempting to launch upgrade for invalid target entity is not allowed!", source: this);
                return ErrorMessage.invalid;
            }
            else if (IsLaunched(upgrade, upgradeSource, factionID))
            {
                logger.LogError($"[EntityUpgradeManager] Attempting to launch entity upgrade for entity of code '{upgrade.SourceCode}' (Faction ID: {factionID}) that has been already launched!", source: this);
                return ErrorMessage.upgradeLaunched;
            }
            else if (upgrade.SourceEntity.IsValid() && upgrade.SourceEntity.Type != upgradeSource.UpgradeTarget.Type)
            {
                logger.LogError($"[EntityUpgradeManager] Upgrade source entity ('{upgrade.SourceEntity?.Type}') and target entity ('{(upgradeSource.UpgradeTarget).Type}') have different types!", source: this);
                return ErrorMessage.upgradeTypeMismatch;
            }

            UpgradeElement<IEntity> newElement = new UpgradeElement<IEntity>
            {
                sourceCode = upgrade.SourceCode,
                target = upgradeSource.UpgradeTarget
            };

            if (upgrade.SourceInstanceOnly)
            {
                if(upgrade.SourceEntity.IsValid())
                    UpgradeInstance(upgrade.SourceEntity, newElement, factionID, upgradeSource.EntityComponentMatcher, upgrade.UpgradeEffect);
            }
            else
            {
                elements[factionID].Add(newElement);

                // Upgrade all spawned instances of the same type of the upgrade source?
                if (upgrade.UpdateSpawnedInstances && upgrade.SourceEntity.IsValid())
                    // Why use ToArray() here? because we do not want to update the FactionManager's FactionEntities collection directly since we will be destroying/creating new entities
                    foreach (IFactionEntity factionEntity in gameMgr.GetFactionSlot(factionID).FactionMgr.FactionEntities.ToArray())
                        if (factionEntity.Code == upgrade.SourceCode)
                            UpgradeInstance(factionEntity, newElement, factionID, upgradeSource.EntityComponentMatcher, upgrade.UpgradeEffect);

                if(upgradeSource.UpgradeTarget.IsBuilding())
                    globalEvent.RaiseBuildingUpgradedGlobal(
                        upgrade.SourceEntity as IBuilding,
                        new UpgradeEventArgs<IEntity>(newElement, factionID, null));
                else if(upgradeSource.UpgradeTarget.IsUnit())
                    globalEvent.RaiseUnitUpgradedGlobal(
                        upgrade.SourceEntity as IUnit,
                        new UpgradeEventArgs<IEntity>(newElement, factionID, null));
                else 
                    logger.LogError($"[EntityUpgradeManager] Upgrading 'resource' instances is currently not allowed!");

                globalEvent.RaiseEntityUpgradedGlobal(
                    upgrade.SourceEntity,
                    new UpgradeEventArgs<IEntity>(newElement, factionID, null));
            }

            //if there are upgrades that get triggerd from this one, launch them
            foreach (TriggerUpgrade triggerUpgrade in upgradeSource.TriggerUpgrades)
                triggerUpgrade.upgradeComp.LaunchLocal(gameMgr, triggerUpgrade.upgradeIndex, factionID);

            return ErrorMessage.none;
        }

        // Upgrades a faction entity instance locally
        private void UpgradeInstance(IFactionEntity sourceInstance, UpgradeElement<IEntity> upgradeElement, int factionID, IEnumerable<EntityUpgradeComponentMatcherElement> entityComponentMatcher, IEffectObject upgradeEffect)
        {
            // Upgraded instances get the same curr health to max health ratio when they are created as the instnaces that they were upgraded from
            float healthRatio = sourceInstance.Health.CurrHealth / (float)sourceInstance.Health.MaxHealth;

            // We want to re-select the upgraded instance after creating if this is the case
            bool wasSelected = sourceInstance.Selection.IsSelected;

            IEntity upgradedInstance = null;

            if (sourceInstance.IsBuilding())
            {
                IBuilding currBuilding = sourceInstance as IBuilding;

                // Get the current builders of this building if there are any
                // And make them stop building the instance of the building since it will be destroyed.
                IEnumerable<IUnit> currBuilders = currBuilding.WorkerMgr.Workers.ToArray();
                foreach (IUnit unit in currBuilders)
                    unit.BuilderComponent.Stop();

                // Create upgraded instance of the building
                upgradedInstance = buildingMgr.CreatePlacedBuildingLocal(
                    upgradeElement.target as IBuilding,
                    sourceInstance.transform.position,
                    sourceInstance.transform.rotation,
                    new InitBuildingParameters
                    {
                        free = sourceInstance.IsFree,
                        factionID = factionID,

                        setInitialHealth = true,
                        initialHealth = (int)(healthRatio * upgradeElement.target.gameObject.GetComponent<IEntityHealth>().MaxHealth),

                        buildingCenter = currBuilding.CurrentCenter,
                    });

                foreach (IUnit unit in currBuilders)
                    unit.SetTargetFirst(new SetTargetInputData
                    {
                        target = RTSHelper.ToTargetData<IEntity>(upgradedInstance),
                        playerCommand =  false
                    });
            }
            else if (sourceInstance.IsUnit())
            {
                IUnit unitPrefab = upgradeElement.target as IUnit;

                // Create upgraded instance of the unit
                upgradedInstance = unitMgr.CreateUnitLocal(
                    unitPrefab,
                    sourceInstance.transform.position,
                    sourceInstance.transform.rotation,
                    new InitUnitParameters
                    {
                        free = sourceInstance.IsFree,
                        factionID = factionID,

                        setInitialHealth = true,
                        initialHealth = (int)(healthRatio * upgradeElement.target.gameObject.GetComponent<IEntityHealth>().MaxHealth),

                        rallypoint = unitPrefab.SpawnRallypoint,
                        gotoPosition = sourceInstance.transform.position,
                    });
            }
            else
            {
                logger.LogError($"[EntityUpgradeManager] Upgrading 'resource' instances is currently not allowed!");
            }

            foreach (EntityUpgradeComponentMatcherElement matcherElement in entityComponentMatcher)
                if (sourceInstance.EntityComponents.TryGetValue(matcherElement.sourceComponentCode, out IEntityComponent sourceComponent)
                    && upgradedInstance.EntityComponents.TryGetValue(matcherElement.targetComponentCode, out IEntityComponent targetComponent))
                    targetComponent.HandleComponentUpgrade(sourceComponent);

            globalEvent.RaiseEntityInstanceUpgradedGlobal(
                sourceInstance,
                new UpgradeEventArgs<IEntity>(upgradeElement, factionID, upgradedInstance));

            effectObjPool.Spawn(upgradeEffect, upgradedInstance.transform);

            // Destroy the upgraded instance
            sourceInstance.Health.DestroyLocal(true, null);

            if (wasSelected)
                selectionMgr.Add(upgradedInstance, SelectionType.multiple);
        }
        #endregion

        #region Reseting Upgrades
        public void ResetUpgrades(IEnumerable<IEnumerable<UpgradeElement<IEntity>>> newElements)
        {
            if (newElements.Count() != gameMgr.FactionCount)
            {
                logger.LogError("[EntityUpgradeManager] The upgrades elements count must match the faction slots count!", source: this);
                return;
            }

            elements = newElements
                .Select(factionSlotUpgrades => factionSlotUpgrades
                    .ToList())
                .ToArray();
        }
        #endregion
    }
}
