using RTSEngine.Entities;
using RTSEngine.Game;
using System.Collections.Generic;

namespace RTSEngine.Upgrades
{
    public interface IEntityUpgradeManager : IPreRunGameService
    {
        IEnumerable<IEnumerable<UpgradeElement<IEntity>>> Elements { get; }

        bool TryGet(int factionID, out UpgradeElement<IEntity>[] upgradeElements);

        bool IsLaunched(EntityUpgrade upgrade, EntityUpgradeElementSource upgradeSource, int factionID);

        ErrorMessage LaunchLocal(EntityUpgrade upgrade, EntityUpgradeElementSource upgradeSource, int factionID);
        void ResetUpgrades(IEnumerable<IEnumerable<UpgradeElement<IEntity>>> newElements);
    }
}