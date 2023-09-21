using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Game;

namespace RTSEngine.Utilities
{
    public class ObjectPool<T, V> : MonoBehaviour, IMonoBehaviour where T : IPoolableObject where V : PoolableObjectSpawnInput
    {
        #region Attributes
        public IReadOnlyDictionary<string, T> ObjectPrefabs { private set; get; }

        private Dictionary<string, Queue<T>> inactiveDic = null;

        private Dictionary<string, List<T>> activeDic = null;
        public IReadOnlyDictionary<string, IEnumerable<T>> ActiveDic
            => activeDic
                .ToDictionary(elem => elem.Key, elem => elem.Value.AsEnumerable());

        // Other components
        protected IGameManager gameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;

            inactiveDic = new Dictionary<string, Queue<T>>();
            activeDic = new Dictionary<string, List<T>>();

            LoadPrefabs();

            OnObjectPoolInit();
        }

        protected virtual void OnObjectPoolInit() { }
        #endregion

        #region Handling Prefabs
        private void LoadPrefabs()
        {
            ObjectPrefabs = Resources
                .LoadAll("Prefabs", typeof(GameObject))
                .Cast<GameObject>()
                .Where(prefab => prefab.IsValid() && prefab.GetComponent<T>() != null)
                .Select(prefab => prefab.GetComponent<T>())
                .ToDictionary(prefab => prefab.Code, prefab => prefab);
        }
        #endregion

        #region Spawning/Despawning Effect Objects
        private T Get(T prefab)
        {
            if(!prefab.IsValid())
                return default(T);

            if (inactiveDic.TryGetValue(prefab.Code, out Queue<T> currentQueue) == false) //if the queue for this effect object type is not found
            {
                currentQueue = new Queue<T>();
                inactiveDic.Add(prefab.Code, currentQueue); //add it
            }

            if (currentQueue.Count == 0)
            {
                T newEffect = GameObject.Instantiate(prefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<T>();
                newEffect.Init(gameMgr);
                currentQueue.Enqueue(newEffect);
            }

            return currentQueue.Dequeue();
        }

        protected T Spawn(T prefab)
        {
            T nextInstance = Get(prefab);

            if(!nextInstance.IsValid())
                return default(T);

            if (!activeDic.TryGetValue(nextInstance.Code, out var currActiveList))
            {
                currActiveList = new List<T>();
                activeDic.Add(nextInstance.Code, currActiveList);
            }
            currActiveList.Add(nextInstance);

            nextInstance.gameObject.SetActive(true);

            return nextInstance;
        }

        public void Despawn(T instance, bool destroyed = false)
        {
            if (activeDic.TryGetValue(instance.Code, out var currActiveList))
                currActiveList.Remove(instance);

            if (destroyed)
                return;

            // Make sure it has no parent object anymore and it is inactive.
            instance.transform.SetParent(null, true);
            instance.gameObject.SetActive(false);

            if (!inactiveDic.TryGetValue(instance.Code, out var currInactiveQueue))
            {
                currInactiveQueue = new Queue<T>();
                inactiveDic.Add(instance.Code, currInactiveQueue);
            }
            currInactiveQueue.Enqueue(instance);
        }
        #endregion
    }
}
