using RTSEngine.Cameras;
using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.ResourceExtension;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine.UI
{
    public class ResourceNotificationSpawnInput : EffectObjectSpawnInput 
    {
        public IEntity entity { get; }
        public ResourceInput resourceInput { get; }

        public ResourceNotificationSpawnInput(IEntity entity,
                                              ResourceInput resourceInput,
                                              Vector3 spawnPosition)
            : base(null, false, spawnPosition, Quaternion.identity, true, true, 0.0f)
        {
            this.entity = entity;
            this.resourceInput = resourceInput;
        }
    }

    public class ResourceNotification : EffectObject 
    {
        [SerializeField, Tooltip("UI Image used to display the resource icon.")]
        private Image image = null;
        private Color lastColor;

        [SerializeField, Tooltip("How fast will the resource notification will move up while losing its transparency?")]
        private TimeModifiedFloat speed = new TimeModifiedFloat(3.0f);

        private Canvas canvas;

        private Transform mainCamTransform = null;

        protected ResourceNotificationUIHandler handler { private set; get; } 

        #region Initializing/Terminating
        protected sealed override void OnEffectObjectInit()
        {
            this.handler = gameMgr.GetService<ResourceNotificationUIHandler>();
            mainCamTransform = gameMgr.GetService<IMainCameraController>().MainCamera.transform;

            if (!logger.RequireValid(image,
              $"[{GetType().Name}] The 'Image' field must be assigned!"))
                return; 

            canvas = GetComponent<Canvas>();

            if (!logger.RequireValid(canvas,
              $"[{GetType().Name}] This component can only be attached to a game object with a '{typeof(Canvas).Name}' component attached to it!"))
                return;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = gameMgr.GetService<IMainCameraController>().MainCamera;
        }

        protected sealed override void OnEffectObjectDestroy()
        {
        }
        #endregion

        public void OnSpawn(ResourceNotificationSpawnInput input)
        {
            base.OnSpawn(input);

            image.sprite = input.resourceInput.type.Icon;

            lastColor = image.color;
            lastColor.a = 1.0f;
            image.color = lastColor;

            image.transform.localPosition = Vector3.zero;
        }

        protected override void OnActiveUpdate()
        {
            //move the canvas in order to face the camera and look at it
            transform.LookAt(transform.position + mainCamTransform.rotation * Vector3.forward,
                mainCamTransform.rotation * Vector3.up);

            lastColor.a = timer.CurrValue / lastLifeTime;
            image.color = lastColor;

            image.transform.localPosition = new Vector3(
                image.transform.localPosition.x,
                image.transform.localPosition.y + speed.Value * Time.deltaTime,
                image.transform.localPosition.z);
        }

        #region Updating State (Enabling/Disabling)
        protected override void OnDeactivated() 
        {
            handler.Despawn(this);
        }
        #endregion

    }
}