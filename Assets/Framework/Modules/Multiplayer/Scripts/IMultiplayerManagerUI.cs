using RTSEngine.Multiplayer.Service;

namespace RTSEngine.Multiplayer
{
    public interface IMultiplayerManagerUI : IMonoBehaviour, IMultiplayerService
    {
        void UpdateServerAccessDataUI();
    }
}
