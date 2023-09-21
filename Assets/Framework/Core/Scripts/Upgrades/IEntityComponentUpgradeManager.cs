using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;

namespace RTSEngine.Upgrades
{
    public interface IEntityComponentUpgradeManager : IPreRunGameService
    {
        IEnumerable<IReadOnlyDictionary<string, IEnumerable<UpgradeElement<IEntityComponent>>>> Elements { get; }
        IReadOnlyDictionary<IEntity, IEnumerable<UpgradeElement<IEntityComponent>>> SourceOnlyElements { get; }

        bool TryGet(IEntity entity, int factionID, out List<UpgradeElement<IEntityComponent>> componentUpgrades);

        bool IsLaunched(EntityComponentUpgrade upgrade, EntityComponentUpgradeElementSource upgradeSource, int factionID);

        ErrorMessage LaunchLocal(EntityComponentUpgrade upgrade, EntityComponentUpgradeElementSource upgradeSource, int factionID);
        void ResetUpgrades(IEnumerable<IReadOnlyDictionary<string, IEnumerable<UpgradeElement<IEntityComponent>>>> newElements, IReadOnlyDictionary<IEntity, IEnumerable<UpgradeElement<IEntityComponent>>> newSourceOnlyElements);
    }
}