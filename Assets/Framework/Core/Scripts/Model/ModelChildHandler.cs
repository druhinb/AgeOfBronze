using UnityEngine;

using System.Collections.Generic;
using System.Linq;

namespace RTSEngine.Model
{
    public abstract class ModelChildHandler<T> : IModelChild where T : Component 
    {
        public int IndexKey { private set; get; }
        protected T current { private set; get; }
        public bool IsRenderering => current.IsValid();

        public ModelChildHandler(T initial, int indexKey)
        {
            this.IndexKey = indexKey;
            this.current = initial;
        }

        public void Show(T next)
        {
            this.current = next;
            OnShow();
        }

        protected virtual void OnShow() { }

        public void Cache()
        {
            this.current = null;
            OnCache();
        }

        protected virtual void OnCache() { }
    }

    public class ModelChildTransformHandler : ModelChildHandler<Transform>, IModelChildTransform
    {
        #region Attributes
        public Transform MainTransform { private set; get; }
        #endregion

        #region IsActive
        // When enabled, the activity of the object will be stored in memory but not reflected in the actual active status of the gameObject itself
        // The activity is cached in the "lastCachedIsActive" boolean field
        private bool lastCachedIsActive = false;

        public bool IsActiveCached { private set; get; } 

        // statePreCache: What IsActive to leave the transform at before it goes cached?
        public void SetActiveCached(bool isActiveCached, bool statePreCache)
        {
            if (isActiveCached)
            {
                bool preCacheIsActive = IsActive;

                IsActive = statePreCache;
                lastCachedIsActive = preCacheIsActive;

                this.IsActiveCached = true;
            }
            else
            {
                bool wasActiveCached = this.IsActiveCached;
                this.IsActiveCached = false;
                if(wasActiveCached)
                    IsActive = lastCachedIsActive;
            }
        }

        private bool isActive;
        public bool IsActive
        {
            set
            {
                if(IsActiveCached)
                {
                    lastCachedIsActive = value;
                    return;
                }

                isActive = value;

                if (!current.IsValid())
                    return;

                current.gameObject.SetActive(isActive);
            }
            get
            {
                if(IsActiveCached)
                    return lastCachedIsActive;

                return isActive;
            }
        }
        #endregion

        #region Position / LocalPosition
        private Vector3 localPosition;
        public Vector3 LocalPosition
        {
            set
            {
                localPosition = value;

                if (!current.IsValid())
                    return;

                current.localPosition = localPosition;
                positionToMain = MainTransform.InverseTransformPoint(current.position);
            }
            get
            {
                return localPosition;
            }
        }

        private Vector3 positionToMain;
        public Vector3 Position
        {
            set
            {
                positionToMain = MainTransform.InverseTransformPoint(value);

                if (current.IsValid())
                {
                    current.position = value;
                    localPosition = current.localPosition;
                }
            }
            get
            {
                if (current.IsValid())
                    return current.position;

                return MainTransform.TransformPoint(positionToMain);
            }
        }
        #endregion

        #region Rotation / LocalRotation
        private Quaternion localRotation;
        public Quaternion LocalRotation
        {
            set
            {
                localRotation = value;

                if (!current.IsValid())
                    return;

                current.localRotation = localRotation;
                rotationToMain = Quaternion.Inverse(MainTransform.transform.rotation) * current.rotation;
            }
            get
            {
                return localRotation;
            }
        }

        private Quaternion rotationToMain;
        public Quaternion Rotation 
        {
            set
            { 
                rotationToMain = Quaternion.Inverse(MainTransform.transform.rotation) * value;

                if (current.IsValid())
                {
                    current.rotation = value;
                    localRotation = current.localRotation;
                }
            }
            get 
            {
                if (current.IsValid())
                    return current.rotation;

                return MainTransform.transform.rotation * rotationToMain;
            }

        }
        #endregion

        #region LocalScale
        private Vector3 localScale;
        public Vector3 LocalScale
        {
            set
            {
                localScale = value;

                if (!current.IsValid())
                    return;

                current.transform.localScale = localScale;
            }
            get
            {
                return localScale;
            }
        }
        #endregion

        #region Constructor
        public ModelChildTransformHandler(Transform mainTransform, Transform initialTransform, int indexKey)
            : base(initialTransform, indexKey)
        {
            this.MainTransform = mainTransform;

            this.positionToMain = MainTransform.transform.InverseTransformPoint(this.current.position);
            this.rotationToMain = Quaternion.Inverse(MainTransform.transform.rotation) * this.current.rotation;

            this.isActive = initialTransform.gameObject.activeSelf;
            this.localPosition = initialTransform.localPosition;
            this.localRotation = initialTransform.localRotation;
            this.localScale = initialTransform.localScale;
        }
        #endregion

        #region Show/Hide
        protected override void OnShow()
        {
            IsActive = this.isActive;
            LocalPosition = this.localPosition;
            LocalRotation = this.localRotation;
            LocalScale = this.localScale;
        }

        protected override void OnCache() { }
        #endregion
    }

    public class ModelChildRendererHandler : ModelChildHandler<Renderer>, IModelChildRenderer
    {
        #region Attributes
        private IDictionary<int, Color> materialsColorDic = null;

        private IDictionary<int, Dictionary<string, Color>> materialsPropertyColorDic = null;
        #endregion

        #region Constructor
        public ModelChildRendererHandler(Renderer initialRenderer, int indexKey)
            : base(initialRenderer, indexKey)
        {
            int index = -1;
            materialsColorDic = this.current
                .materials
                .ToDictionary(material => { index++; return index; }, material => material.color);

            materialsPropertyColorDic = this.current
                .materials
                .ToDictionary(material => { index++; return index; }, material => new Dictionary<string, Color>());
        }
        #endregion

        #region Color
        public bool SetColor(int materialID, string propertyName, Color color)
        {
            if (!materialsPropertyColorDic.ContainsKey(materialID))
                return false;

            if (!materialsPropertyColorDic[materialID].ContainsKey(propertyName))
                materialsPropertyColorDic[materialID].Add(propertyName, color);
            else
                materialsPropertyColorDic[materialID][propertyName] = color;

            if (!current.IsValid())
                return true;

            current.materials[materialID].SetColor(propertyName, color);
            return true;
        }

        public bool SetColor(int materialID, Color color)
        {
            if (!materialsColorDic.ContainsKey(materialID))
                return false;

            materialsColorDic[materialID] = color;

            if (!current.IsValid())
                return true;

            current.materials[materialID].color = color;
            return true;
        }
        #endregion

        #region Show/Hide
        protected override void OnShow()
        {
            foreach (KeyValuePair<int, Color> kvp in materialsColorDic)
                current.materials[kvp.Key].color = kvp.Value;

            foreach (KeyValuePair<int, Dictionary<string, Color>> kvp1 in materialsPropertyColorDic)
                foreach (var kvp2 in materialsPropertyColorDic[kvp1.Key])
                    current.materials[kvp1.Key].SetColor(kvp2.Key, kvp2.Value);
        }

        protected override void OnCache() { }
        #endregion
    }

    public class ModelChildAnimatorHandler : ModelChildHandler<Animator>, IModelChildAnimator
    {
        #region Attributes
        private IDictionary<string, bool> paramsDic = null;
        #endregion

        #region Constructor
        public ModelChildAnimatorHandler(Animator initialAnimator, int indexKey)
            : base(initialAnimator, indexKey)
        {
            paramsDic = initialAnimator.parameters.ToDictionary(elem => elem.name, elem => elem.defaultBool);
            this.speed = initialAnimator.speed;
            this.controller = initialAnimator.runtimeAnimatorController;
        }
        #endregion

        #region Bool Parameter
        public bool GetBool(string name)
        {
            return paramsDic[name];
        }

        public void SetBool(string name, bool value)
        {
            paramsDic[name] = value;

            if (!current.IsValid())
                return;

            current.SetBool(name, value);
        }
        #endregion

        #region Speed
        private float speed;
        public float Speed
        {
            set
            {
                speed = value;

                if (!current.IsValid())
                    return;

                current.speed = speed;
            }
            get
            {
                return speed;
            }
        }
        #endregion

        #region Current Controller
        private RuntimeAnimatorController controller;
        public RuntimeAnimatorController Controller
        {
            set
            {
                controller = value;

                if (!current.IsValid())
                    return;

                current.runtimeAnimatorController = controller;
                current.Play(current.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
            }
            get
            {
                return controller;
            }
        }
        #endregion

        #region Show/Hide
        protected override void OnShow()
        {
            Speed = this.speed;
            Controller = this.controller;

            foreach (KeyValuePair<string, bool> kvp in paramsDic)
                current.SetBool(kvp.Key, kvp.Value);
        }

        protected override void OnCache() { }
        #endregion
    }
}
