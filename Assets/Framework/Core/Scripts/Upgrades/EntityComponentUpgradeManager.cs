using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Effect;

namespace RTSEngine.Upgrades
{
    public class EntityComponentUpgradeManager : MonoBehaviour, IEntityComponentUpgradeManager
    {
        #region Attributes
        // Holds the elements that define the upgrade source and targets for IEntityComponent components.
        // Key: source entity code that defines the entity whose components will be upgraded
        // Value: list of IEntityComponent upgrades
        // Each faction slot gets its own element in the array
        private Dictionary<string, List<UpgradeElement<IEntityComponent>>>[] elements;
        public IEnumerable<IReadOnlyDictionary<string, IEnumerable<UpgradeElement<IEntityComponent>>>> Elements => elements
            .Select(nextDic =>
            {
                var outputDic = new Dictionary<string, IEnumerable<UpgradeElement<IEntityComponent>>>();
                foreach (var kvp in nextDic)
                    outputDic.Add(kvp.Key, kvp.Value);
                return outputDic;
            });


        // Key: Key of the entity that had the source only upgrade
        private Dictionary<IEntity, List<UpgradeElement<IEntityComponent>>> sourceOnlyElements;
        public IReadOnlyDictionary<IEntity, IEnumerable<UpgradeElement<IEntityComponent>>> SourceOnlyElements
        {
            get {
                var outputDic = new Dictionary<IEntity, IEnumerable<UpgradeElement<IEntityComponent>>>();
                foreach (var kvp in sourceOnlyElements)
                    outputDic.Add(kvp.Key, kvp.Value);
                return outputDic;
            }
        }

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IInputManager inputMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();

            gameMgr.GameBuilt += HandleGameBuilt;
        }

        private void OnDestroy()
        {
            foreach(IEntity entity in sourceOnlyElements.Keys)
                entity.Health.EntityDead -= HandleSourceOnlyUpgradeDead;

            gameMgr.GameBuilt -= HandleGameBuilt;
        }

        private void HandleGameBuilt(IGameManager sender, EventArgs args)
        {
            // When the game is built, the factions slots are ready.
            elements = gameMgr.FactionSlots
                .Select(factionSlot => new Dictionary<string, List<UpgradeElement<IEntityComponent>>>())
                .ToArray();

            sourceOnlyElements = new Dictionary<IEntity, List<UpgradeElement<IEntityComponent>>>();

            gameMgr.GameBuilt -= HandleGameBuilt;
        }
        #endregion

        #region Fetching Entity Component Upgrade Data
        public bool TryGet(IEntity entity, int factionID, out List<UpgradeElement<IEntityComponent>> componentUpgrades)
        {
            componentUpgrades = null;

            if (!logger.RequireTrue(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to get entity component upgrade elements for faction ID: {factionID} is not allowed!",
                    source: this))
                return false;

            return elements[factionID].TryGetValue(entity.Code, out componentUpgrades);
        }

        public bool IsLaunched(EntityComponentUpgrade upgrade, EntityComponentUpgradeElementSource upgradeSource, int factionID)
        {
            if (!logger.RequireValid(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to get entity component upgrade elements for faction ID: {factionID} is not allowed!"))
                return false;

            if (elements[factionID].TryGetValue(upgradeSource.GetSourceCode(upgrade.SourceEntity), out List<UpgradeElement<IEntityComponent>> componentUpgrades))
                return componentUpgrades
                    .Where(element => element.sourceCode == upgradeSource.GetSourceCode(upgrade.SourceEntity) 
                        && element.target == upgradeSource.UpgradeTarget)
                    .Any();

            return false;
        }
        #endregion

        #region Launching Entity Component Upgrades
        public ErrorMessage LaunchLocal(EntityComponentUpgrade upgrade, EntityComponentUpgradeElementSource upgradeSource, int factionID)
        {
            if (!logger.RequireTrue(RTSHelper.IsValidFaction(factionID),
                    $"[{GetType().Name}] Attempting to launch upgrade for invalid faction ID: {factionID} is not allowed!")
                || !logger.RequireValid(upgrade.SourceEntity,
                    $"[{GetType().Name}] Attempting to launch upgrade for invalid source entity is not allowed!")
                || !logger.RequireValid(upgradeSource.UpgradeTarget,
                    $"[{GetType().Name}] Attempting to launch upgrade for invalid target entity component is not allowed!"))
                return ErrorMessage.invalid;

            else if (!logger.RequireTrue(!IsLaunched(upgrade, upgradeSource, factionID),
                $"[{GetType().Name}] Attempting to launch entity component upgrade for entity of code '{(upgrade.SourceEntity.IsValid() ? upgrade.SourceEntity.Code : "DESTROYED")}' that has been already launched!"))
                return ErrorMessage.upgradeLaunched;

            UpgradeElement<IEntityComponent> newElement = new UpgradeElement<IEntityComponent>
            {
                sourceCode = upgradeSource.GetSourceCode(upgrade.SourceEntity),
                target = upgradeSource.UpgradeTarget
            };

            if (upgrade.SourceInstanceOnly)
            {
                effectObjPool.Spawn(upgrade.UpgradeEffect, upgrade.SourceEntity.transform);
                upgrade.SourceEntity.UpgradeComponent(newElement);

                if (!sourceOnlyElements.ContainsKey(upgrade.SourceEntity))
                    sourceOnlyElements.Add(upgrade.SourceEntity, new List<UpgradeElement<IEntityComponent>>());
                sourceOnlyElements[upgrade.SourceEntity].Add(newElement);

                // When the entity is destroyed, we make sure to remove it from the source only upgrade elments list
                upgrade.SourceEntity.Health.EntityDead += HandleSourceOnlyUpgradeDead;
            }
            else
            {
                if (elements[factionID].TryGetValue(upgrade.SourceEntity.Code, out List<UpgradeElement<IEntityComponent>> componentUpgrades))
                    componentUpgrades.Add(newElement);
                else
                    elements[factionID].Add(upgrade.SourceEntity.Code, new List<UpgradeElement<IEntityComponent>> { newElement });

                // Upgrade all spawned elements of the same type of the upgrade source?
                if (upgrade.UpdateSpawnedInstances)
                    foreach (IFactionEntity factionEntity in gameMgr.GetFactionSlot(factionID).FactionMgr.FactionEntities)
                        if (factionEntity.Code == upgrade.SourceEntity.Code)
                        {
                            factionEntity.UpgradeComponent(newElement);
                            effectObjPool.Spawn(upgrade.UpgradeEffect, factionEntity.transform);
                        }

                globalEvent.RaiseEntityComponentUpgradedGlobal(
                    upgrade.SourceEntity,
                    new UpgradeEventArgs<IEntityComponent>(newElement, factionID, null));
            }

            // If there are upgrades that get triggerd from this one, launch them
            foreach (TriggerUpgrade triggerUpgrade in upgradeSource.TriggerUpgrades)
                triggerUpgrade.upgradeComp.LaunchLocal(gameMgr, triggerUpgrade.upgradeIndex, factionID);

            return ErrorMessage.none;
        }

        private void HandleSourceOnlyUpgradeDead(IEntity sender, DeadEventArgs args)
        {
            sourceOnlyElements.Remove(sender);
            sender.Health.EntityDead -= HandleSourceOnlyUpgradeDead;
        }
        #endregion

        #region Reseting Upgrades
        public void ResetUpgrades(IEnumerable<IReadOnlyDictionary<string, IEnumerable<UpgradeElement<IEntityComponent>>>> newElements,
            IReadOnlyDictionary<IEntity, IEnumerable<UpgradeElement<IEntityComponent>>> newSourceOnlyElements)
        {
            if (newElements.Count() != gameMgr.FactionCount)
            {
                logger.LogError("[EntityComponentUpgradeManager] The upgrades elements count must match the faction slots count!", source: this);
                return;
            }

            elements = newElements
                .Select(factionSlotUpgrades =>
                {
                    var nextDic = new Dictionary<string, List<UpgradeElement<IEntityComponent>>>();
                    foreach (var upgradeElement in factionSlotUpgrades)
                    {
                        nextDic.Add(upgradeElement.Key, new List<UpgradeElement<IEntityComponent>>());
                        nextDic[upgradeElement.Key].AddRange(upgradeElement.Value);
                    }
                    return nextDic;
                })
                .ToArray();

            sourceOnlyElements.Clear();
            foreach (var kvp in sourceOnlyElements)
            {
                sourceOnlyElements.Add(kvp.Key, kvp.Value.ToList());
                // When source only component upgrades are reset, we launch them again
                foreach(var nextUpgradeElement in kvp.Value)
                {
                    kvp.Key.UpgradeComponent(nextUpgradeElement);
                }
            }
        }
        #endregion
    }
}
