using RTSEngine.Audio;
using RTSEngine.Multiplayer.Logging;

namespace RTSEngine.Multiplayer.Audio
{
    public class MultiplayerAudioManager : AudioManagerBase, IMultiplayerAudioManager
    {
        #region Initializing/Terminating
        public void Init(IMultiplayerManager multiplayerMgr)
        {
            InitBase(multiplayerMgr.GetService<IMultiplayerLoggingService>());
        }
        #endregion
    }
}
