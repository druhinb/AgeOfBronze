using UnityEngine;
using UnityEngine.Events;

using RTSEngine.Audio;
using RTSEngine.Determinism;
using RTSEngine.Event;
using RTSEngine.Utilities;

namespace RTSEngine.Effect
{
    public class EffectObject : PoolableObject, IEffectObject
    {
        #region Attributes
        // EDITOR ONLY
        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        /// <summary>
        /// The current state of the effect object.
        /// </summary>
        public EffectObjectState State { private set; get; }

        [SerializeField, Tooltip("Enable to control the life time of the effect object.")]
        private bool enableLifeTime = true;

        [SerializeField, Tooltip("If the life time is enabled then this represents the time (in seconds) during which the effect object will be shown.")]
        private float defaultLifeTime = 3.0f;
        protected float lastLifeTime { private set; get; }

        // When > 0, the disable events will be invoked and then timer with this length will start and then the effect object will be hidden
        [SerializeField, Tooltip("When the effect object is disabled, this is how long (in seconds) it will take for the object to disappear.")]
        private float disableTime = 0.0f;

        // Handles life and disable timers
        protected TimeModifiedTimer timer { private set; get; }
        public float CurrLifeTime => timer.CurrValue;

        [SerializeField, Tooltip("Offsets the spawn position of the effect object when it is enabled.")]
        private Vector3 spawnPositionOffset = Vector3.zero;
        [SerializeField, Tooltip("Enable to apply the position offset on the local position instead of the global position?")]
        private bool offsetLocalPosition = false;

        [SerializeField, Tooltip("Invoked when the effect object is enabled.")]
        private UnityEvent enableEvent = null;
        [SerializeField, Tooltip("Invoked when the effect object is disabled.")]
        private UnityEvent disableEvent = null;

        // Used as a replacement for parenting the attack object to differnet objects over the course of its lifetime, which can wreck its scale.
        protected FollowTransform followTransform = null;
        public FollowTransform FollowTransform => followTransform;

        public AudioSource AudioSourceComponent { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }

        #endregion

        #region Initializing/Terminating
        protected sealed override void OnPoolableObjectInit()
        {
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();

            AudioSourceComponent = GetComponent<AudioSource>();

            globalEvent.RaiseEffectObjectCreatedGlobal(this);

            // Create the FollowTransform instance to be used to "parent" the attack object to delay parent or to the target it deals damage to.
            followTransform = new FollowTransform(source: transform, OnFollowTargetInvalid);

            OnEffectObjectInit();
        }

        protected virtual void OnEffectObjectInit() { }

        protected sealed override void OnPoolableObjectDestroy()
        {
            globalEvent.RaiseEffectObjectDestroyedGlobal(this);

            OnEffectObjectDestroy();
        }

        protected virtual void OnEffectObjectDestroy() { }
        #endregion

        #region Updating State (Enabling/Disabling)
        public void OnSpawn(EffectObjectSpawnInput input)
        {
            base.OnSpawn(input);

            if (State != EffectObjectState.inactive)
                return;

            State = EffectObjectState.running;

            this.enableLifeTime = input.enableLifeTime;
            if (this.enableLifeTime)
            {
                lastLifeTime = input.useDefaultLifeTime ? defaultLifeTime : input.customLifeTime;
                timer = new TimeModifiedTimer(lastLifeTime);
            }
            else
                lastLifeTime = 0.0f;

            if(offsetLocalPosition)
                transform.localPosition += spawnPositionOffset;
            else
                transform.position += spawnPositionOffset;

            gameObject.SetActive(true);

            enableEvent.Invoke();

            OnEffectObjectSpawn();
        }

        protected virtual void OnEffectObjectSpawn() { }

        private void Update()
        {
            if (State == EffectObjectState.inactive)
                return;

            OnStaticUpdate();

            if (!enableLifeTime)
                return;

            OnActiveUpdate();

            timer.ModifiedDecrease();
            if (timer.CurrValue > 0.0f)
                return;

            switch (State)
            {
                case EffectObjectState.running:
                    Deactivate();
                    break;

                case EffectObjectState.disabling:
                    DeactivateFinale();
                    break;
            }
        }

        protected virtual void OnStaticUpdate() { }

        protected virtual void OnActiveUpdate() {
            // Update the follow transform in case the attack object is "parented" into another game object that it needs to follow.
            followTransform.Update();
        }

        public void Deactivate(bool useDisableTime = true)
        {
            if (disableTime <= 0.0f || !useDisableTime)
            {
                DeactivateFinale();
                return;
            }

            // If the effect object is already being disabled with the timer then wait for the timer
            if (State != EffectObjectState.running)
                return;

            disableEvent.Invoke();

            State = EffectObjectState.disabling;
            timer = new TimeModifiedTimer(disableTime);
            // Enable life time to allow for the disable time to go through
            enableLifeTime = true;
        }

        protected virtual void OnDeactivated() 
        {
            effectObjPool.Despawn(this);
        }

        private void DeactivateFinale()
        {
            if (State == EffectObjectState.inactive)
                return;

            State = EffectObjectState.inactive;

            OnDeactivated();
        }
        #endregion

        #region Handling Follow Transform
        // Called when the supposed "parent" object of the attack object that it triggered damage is destroyed.
        private void OnFollowTargetInvalid()
        {
            Deactivate(useDisableTime: false);
        }
        #endregion
    }
}
