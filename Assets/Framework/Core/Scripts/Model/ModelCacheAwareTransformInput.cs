using RTSEngine.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace RTSEngine.Model
{
    public enum ObjectToModelStatus { unassigned = 0, child = 1, notChild = 2 }

    public abstract class ModelCacheAwareInput<T> where T : Component
    {
        [SerializeField, ReadOnly]
        protected ObjectToModelStatus status = ObjectToModelStatus.unassigned;
        public ObjectToModelStatus Status => status;

        [SerializeField]
        protected T obj = null;

        [SerializeField, ReadOnly]
        protected int indexKey = -1;

        [SerializeField, ReadOnly, FormerlySerializedAs("EntityModel")]
        private Entity entity = null;
        public IEntityModel EntityModel
        {
            get
            {
                if (!entity.IsValid() && status == ObjectToModelStatus.child)
                    RTSHelper.LoggingService.LogError($"[{GetType().Name}] Unable to find {typeof(IEntityModel).Name} in a parent object of the object attached to this component. Please follow error trace to find the field and re-assign it to the component in prefab mode to retrieve the entity model component.");

                return entity.EntityModel;
            }
        }

        public bool IsStatusValid()
        {
            switch (Status)
            {
                case ObjectToModelStatus.notChild:
                    return obj.IsValid();
                case ObjectToModelStatus.child:
                    return IsChildStatusValid();
                default:
                    return false;
            }
        }

        protected abstract bool IsChildStatusValid();

        public ModelCacheAwareInput()
        {
            status = ObjectToModelStatus.unassigned;
            indexKey = -1;
            entity = null;
            obj = null;
        }

        protected void OnInvalidSetOrGetOperation()
        {
            RTSHelper.LoggingService.LogError("[ModelCacheAwareTransformInput] Attempting to set/get a property of a un-assigned field! Please follow error trace to see where this is coming from.");
        }
    }

    [System.Serializable]
    public class ModelCacheAwareTransformInput : ModelCacheAwareInput<Transform>
    {
        #region Constructor
        // When this constructor is used, the transform is assumed to be not a child of the model or the model itself but rather a non-cachable transform
        public ModelCacheAwareTransformInput(Transform t)
        {
            obj = t;
            status = ObjectToModelStatus.notChild;
        }

        protected override bool IsChildStatusValid()
            => EntityModel.IsValid() && EntityModel.IsTransformChildValid(indexKey);
        #endregion

        #region IsActive 
        // Caching the status of the activity of a transform is now only available for child objects
        public void SetActiveCached(bool isActiveCached, bool statePreCache)
        {
            switch (status)
            {
                case ObjectToModelStatus.child:
                    EntityModel.GetTransformChild(indexKey).SetActiveCached(isActiveCached, statePreCache);
                    break;

                default:
                    OnInvalidSetOrGetOperation();
                    break;
            }
        }
        public bool IsActiveCached
        {
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).IsActiveCached;

                    default:
                        OnInvalidSetOrGetOperation();
                        return false;
                }
            }
        }

        public bool IsActive {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.gameObject.SetActive(value);
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetTransformChild(indexKey).IsActive = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj != null ? obj.gameObject.activeInHierarchy : false;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).IsActive;

                    default:
                        OnInvalidSetOrGetOperation();
                        return false;
                }
            }
        }
        #endregion

        #region Position / LocalPosition
        public Vector3 Position
        {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.position = value;
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetTransformChild(indexKey).Position = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.position;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).Position;

                    default:
                        OnInvalidSetOrGetOperation();
                        return Vector3.zero;
                }
            }
        }

        public Vector3 LocalPosition {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.localPosition = value;
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetTransformChild(indexKey).LocalPosition = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.localPosition;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).LocalPosition;

                    default:
                        OnInvalidSetOrGetOperation();
                        return Vector3.zero;
                }
            }
        }
        #endregion

        #region Rotation / LocalRotation
        public Quaternion Rotation {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.rotation = value;
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetTransformChild(indexKey).Rotation = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.rotation;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).Rotation;

                    default:
                        OnInvalidSetOrGetOperation();
                        return Quaternion.identity;
                }
            }
        }

        public Quaternion LocalRotation {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.localRotation = value;
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetTransformChild(indexKey).LocalRotation = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.localRotation;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).LocalRotation;

                    default:
                        OnInvalidSetOrGetOperation();
                        return Quaternion.identity;
                }
            }
        }
        #endregion

        #region LocalScale 
        public Vector3 LocalScale {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.localScale = value;
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetTransformChild(indexKey).LocalScale = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.localScale;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetTransformChild(indexKey).LocalScale;

                    default:
                        OnInvalidSetOrGetOperation();
                        return Vector3.one;
                }
            }
        }
        #endregion
    }

    [System.Serializable]
    public class ModelCacheAwareRendererInput : ModelCacheAwareInput<Renderer>
    {
        protected override bool IsChildStatusValid()
            => EntityModel.IsValid() && EntityModel.IsRendererChildValid(indexKey);

        #region Color
        // False in case of invalid materialID

        public bool SetColor(int materialID, Color color)
        {
            switch(status)
            {
                case ObjectToModelStatus.notChild:
                    if (!materialID.IsValidIndex(obj.materials))
                        return false;

                    obj.materials[materialID].color = color;
                    return true;
                case ObjectToModelStatus.child:
                    return EntityModel.GetRendererChild(indexKey).SetColor(materialID, color);

                default:
                    OnInvalidSetOrGetOperation();
                    return false;
            }
        }

        public bool SetColor(int materialID, string propertyName, Color color)
        {
            switch(status)
            {
                case ObjectToModelStatus.notChild:
                    if (!materialID.IsValidIndex(obj.materials))
                        return false;

                    obj.materials[materialID].SetColor(propertyName, color);
                    return true;
                case ObjectToModelStatus.child:
                    return EntityModel.GetRendererChild(indexKey).SetColor(materialID, propertyName, color);

                default:
                    OnInvalidSetOrGetOperation();
                    return false;
            }
        }
        #endregion
    }

    [System.Serializable]
    public class ModelCacheAwareAnimatorInput : ModelCacheAwareInput<Animator>
    {
        protected override bool IsChildStatusValid()
            => EntityModel.IsValid() && EntityModel.IsAnimatorChildValid(indexKey);

        #region Bool Parameter
        public bool GetBool(string name)
        {
            switch(status)
            {
                case ObjectToModelStatus.notChild:
                    return obj.GetBool(name);
                case ObjectToModelStatus.child:
                    return EntityModel.GetAnimatorChild(indexKey).GetBool(name);

                default:
                    OnInvalidSetOrGetOperation();
                    return false;
            }
        }

        public void SetBool(string name, bool value)
        {
            switch(status)
            {
                case ObjectToModelStatus.notChild:
                    obj.SetBool(name, value);
                    break;

                case ObjectToModelStatus.child:
                    EntityModel.GetAnimatorChild(indexKey).SetBool(name, value);
                    break;

                default:
                    OnInvalidSetOrGetOperation();
                    break;
            }
        }
        #endregion

        #region Speed 
        public float Speed
        {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.speed = value;
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetAnimatorChild(indexKey).Speed = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.speed;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetAnimatorChild(indexKey).Speed;

                    default:
                        OnInvalidSetOrGetOperation();
                        return 1.0f;
                }
            }
        }
        #endregion

        #region Controller 
        public RuntimeAnimatorController Controller
        {
            set
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        obj.runtimeAnimatorController = value;
                        obj.Play(obj.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
                        break;

                    case ObjectToModelStatus.child:
                        EntityModel.GetAnimatorChild(indexKey).Controller = value;
                        break;

                    default:
                        OnInvalidSetOrGetOperation();
                        break;
                }
            }
            get
            {
                switch(status)
                {
                    case ObjectToModelStatus.notChild:
                        return obj.runtimeAnimatorController;

                    case ObjectToModelStatus.child:
                        return EntityModel.GetAnimatorChild(indexKey).Controller;

                    default:
                        OnInvalidSetOrGetOperation();
                        return null;
                }
            }
        }
        #endregion
    }
}
