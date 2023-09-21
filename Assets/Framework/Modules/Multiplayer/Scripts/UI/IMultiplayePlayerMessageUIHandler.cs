using RTSEngine.Multiplayer.Service;
using RTSEngine.UI.Utilities;

namespace RTSEngine.Multiplayer.UI
{
    public interface IMultiplayePlayerMessageUIHandler : IMultiplayerService
    {
        ITextMessage Message { get; }
    }
}