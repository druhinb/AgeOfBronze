using RTSEngine.Game;

namespace RTSEngine.AI
{
    public interface IAIComponent : IMonoBehaviour
    {
        // When true, the component implementing the interface would only be allowed to have one instance per NPC faction
        bool IsSingleInstance { get; }

        bool IsActive { get; }

        void Init(IGameManager gameMgr, IAIManager npcMgr);
    }
}
