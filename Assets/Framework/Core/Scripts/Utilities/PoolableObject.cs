using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Utilities
{

    public abstract class PoolableObject : MonoBehaviour, IPoolableObject
    {
        #region Attributes
        [SerializeField, Tooltip("Assign a unique code for each poolable object type.")]
        private string code = "unique_effect_object"; 
        public string Code => code;

        // Other components
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            OnPoolableObjectInit();
        }

        protected virtual void OnPoolableObjectInit() { }

        private void OnDestroy()
        {
            OnPoolableObjectDestroy();
        }

        protected virtual void OnPoolableObjectDestroy() { }
        #endregion

        #region Spawning/Despawning
        protected void OnSpawn(PoolableObjectSpawnInput input)
        {
            transform.SetParent(input.parent, true);

            if(input.useLocalTransform)
                transform.localPosition = input.spawnPosition;
            else
                transform.position = input.spawnPosition;

            input.spawnRotation.Apply(transform, input.useLocalTransform);
        }
        #endregion
    }
}
