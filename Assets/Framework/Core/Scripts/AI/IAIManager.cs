using System;
using System.Collections.Generic;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;

namespace RTSEngine.AI
{
    public interface IAIManager : IMonoBehaviour
    {
        AIType Type { get; }
        IFactionManager FactionMgr { get; }

        event CustomEventHandler<IAIManager, EventArgs> InitComplete;

        T GetAIComponent<T>() where T : IAIComponent;

        IEnumerable<T> GetAIComponentSet<T>() where T : IAIComponent;

        void Init(AIType npcType, IGameManager gameMgr, IFactionManager factionMgr);
    }
}
