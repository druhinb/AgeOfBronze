using RTSEngine.Audio;
using RTSEngine.Effect;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public abstract class AttackSubComponent
    {
        protected IAttackComponent SourceAttackComp { private set; get; }

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IAttackManager attackMgr { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; } 

        public void Init (IGameManager gameMgr, IAttackComponent sourceAttackComp)
        {
            this.gameMgr = gameMgr;
            this.SourceAttackComp = sourceAttackComp;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>(); 

            OnInit();
        }

        protected virtual void OnInit() { }
    }
}
