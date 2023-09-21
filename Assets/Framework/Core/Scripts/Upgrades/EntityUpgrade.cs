using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
    [System.Serializable]
    public struct EntityUpgradeElementSource
    {
        [Space(), SerializeField, EnforceType(typeof(IEntity)), Tooltip("The upgrade entity target.")]
        private GameObject upgradeTarget;
        public IEntity UpgradeTarget => upgradeTarget.IsValid() ? upgradeTarget.GetComponent<IEntity>() : null;

        [SerializeField, Tooltip("Pick the entity components of the upgrade source that match the upgrade target.")]
        private EntityUpgradeComponentMatcherElement[] entityComponentMatcher;
        public IEnumerable<EntityUpgradeComponentMatcherElement> EntityComponentMatcher => entityComponentMatcher;

        [Space(), SerializeField, Tooltip("Upgrades to trigger/launch when this upgrade is completed.")]
        private TriggerUpgrade[] triggerUpgrades;
        public IEnumerable<TriggerUpgrade> TriggerUpgrades => triggerUpgrades;
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

    [RequireComponent(typeof(IEntity))]
    public class EntityUpgrade : Upgrade
    {
        public string SourceCode => SourceEntity.IsValid() ? SourceEntity.Code : "";

        [SerializeField, Tooltip("Define the possible upgrades that can be launched from this component.")]
        private EntityUpgradeElementSource[] upgrades = new EntityUpgradeElementSource[0];
        public EntityUpgradeElementSource GetUpgrade(int index)
        {
            if (index.IsValidIndex(upgrades))
                return upgrades[index];

            string errorMsg = $"[EntityUprade - {SourceEntity?.Code}] Unable to fetch upgrade of invalid index {index}";
            if (RTSHelper.LoggingService.IsValid())
                RTSHelper.LoggingService.LogError(errorMsg, source: this);
            else
                Debug.LogError($"[RTSEditorHelper] {errorMsg}");
            return default;
        }

        public override void LaunchLocal(IGameManager gameMgr, int upgradeIndex, int factionID)
        {
            gameMgr.GetService<IEntityUpgradeManager>().LaunchLocal(this, GetUpgrade(upgradeIndex), factionID);
        }
    }
}
