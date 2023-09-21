using RTSEngine.Multiplayer.Audio;
using RTSEngine.Multiplayer.Logging;
using RTSEngine.UI;

namespace RTSEngine.Multiplayer.UI
{
    public class MultiplayePlayerMessageUIHandler : PlayerMessageUIHandlerBase, IMultiplayePlayerMessageUIHandler
    {
        #region Initializing/Terminating
        public void Init(IMultiplayerManager multiplayerMgr)
        {
            InitBase(logger: multiplayerMgr.GetService<IMultiplayerLoggingService>(),
                audioMgr: multiplayerMgr.GetService<IMultiplayerAudioManager>());
        }
        #endregion
    }
}

