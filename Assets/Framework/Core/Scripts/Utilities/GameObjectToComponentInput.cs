using RTSEngine.Attack;
using RTSEngine.BuildingExtension;
using RTSEngine.Effect;
using UnityEngine;

namespace RTSEngine.Utilities
{
    public class GameObjectToComponentInput<T> where T : IMonoBehaviour
    {
        public GameObject input;
        private T output = default;
        public T Output
        {
            get
            {
                if (output.IsValid())
                    return output;
                else if (input.IsValid())
                {
                    output = input.GetComponent<T>();
                    return output;
                }
                else
                    return default;
            }
        }
    }

    [System.Serializable]
    public class GameObjectToEffectObjectInput : GameObjectToComponentInput<IEffectObject> 
    {
        //[SerializeField, HideInInspector]
        //private ModelCacheAwareTransformInput parent;

        //public ModelCacheAwareTransformInput GetParent(ModelCacheAwareTransformInput defaultParent)
            //=> parent.IsValid() ? parent : defaultParent;
    }

    [System.Serializable]
    public class GameObjectToAttackObjectInput : GameObjectToComponentInput<IAttackObject> { }

    [System.Serializable]
    public class GameObjectToBorderObjectInput : GameObjectToComponentInput<IBorderObject> { }
}
