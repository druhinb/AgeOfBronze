
using RTSEngine.Game;

namespace RTSEngine.Utilities
{
    public interface IPoolableObject : IMonoBehaviour
    {
        string Code { get; }

        void Init(IGameManager gameMgr);
    }
}
