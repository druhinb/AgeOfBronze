using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Determinism;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Audio;
using RTSEngine.ResourceExtension;
using RTSEngine.Selection;
using RTSEngine.Utilities;
using System.Collections;

namespace RTSEngine.Health
{
    public abstract class EntityHealth : MonoBehaviour, IEntityHealth, IEntityPreInitializable
    {
        #region Class Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        public bool IsInitialized { private set; get; } = false;

        public IEntity Entity { private set; get; }

        // IMPORTANT: must have ONE flag. It can not be a health component for both IBuilding and IResource, must have sepearate health components for that purpose.
        public abstract EntityType EntityType { get; }

        [SerializeField, Tooltip("Maximum health points that the entity can have."), Min(1)]
        private int maxHealth = 100;
        public int MaxHealth => maxHealth;

        public int CurrHealth { private set; get; } = 0;
        [SerializeField, Tooltip("Initial health points that the entity starts with."), Min(1)]
        private int initialHealth = 1;

        //This is not accounted for when testing whehter health value can be added or not using the CanAdd method.
        //This simply allows to not update the actual CurrHealth value while at the same time trigger the primitive methods in AddLocal using the input "args.Value"
        //In order to lock health and not allow the whole AddLocal method not to go through, CanDecrease and CanIncrease can be set to false.
        public bool LockHealth { protected set; get; } = false;

        public bool HasMaxHealth => CurrHealth >= MaxHealth;

        public float HealthRatio => (CurrHealth / (float)MaxHealth);

        [SerializeField, Tooltip("Can the health be increased?")]
        private bool canIncrease = true;
        public bool CanIncrease
        {
            get => canIncrease;
            set
            {
                canIncrease = value;
            }
        }

        [SerializeField, Tooltip("Can the health be decreased?")]
        private bool canDecrease = true;
        public bool CanDecrease
        {
            get => canDecrease;
            set
            {
                canDecrease = value;
            }
        }

        [SerializeField, Tooltip("The height (position on the Y axis) of the hover health bar UI element when it is enabled.")]
        private float hoverHealthBarY = 4.0f;
        public float HoverHealthBarY => hoverHealthBarY;

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Triggered on the entity when it loses health."), Space()]
        private GameObjectToEffectObjectInput hitEffect = null;
        private EffectObjectSpawnInput hitEffectSpawnInput;
        [SerializeField, Tooltip("Played when the entity loses health.")]
        private AudioClipFetcher hitAudio = null;

        public bool IsDead { private set; get; } = false;

        public IEntity TerminatedBy { private set; get; }
        [SerializeField, Tooltip("Destroy the entity object when health reaches zero?")]
        private bool destroyObject = true;
        [SerializeField, Tooltip("If the object is to be destroyed on zero health, this presents how long it takes before the object is destroyed.")]
        private float destroyObjectDelay = 0.0f;
        public virtual float DestroyObjectDelay => new TimeModifiedFloat(destroyObjectDelay).Value;
        [SerializeField, Tooltip("Resources to be awarded to the faction whose entity deals the damage that destroys this.")]
        private ResourceInput[] destroyAward = new ResourceInput[0];

        [SerializeField, Tooltip("What audio clip to play when the entity is destroyed?")]
        private AudioClipFetcher destructionAudio = new AudioClipFetcher();
        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Effect object to spawn when the entity is destroyed.")]
        private GameObjectToEffectObjectInput destructionEffect = null;

        protected EntityHealthStateHandler stateHandler;
        [SerializeField, Tooltip("Possible health states that the entity can have.")]
        private List<EntityHealthState> states = new List<EntityHealthState>();
        protected IReadOnlyList<EntityHealthState> States => states;
        [SerializeField, Tooltip("Health state activated when the building is destroyed.")]
        private EntityHealthState destroyState = new EntityHealthState();

        // Game services
        protected IInputManager inputMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IResourceManager resourceMgr { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, HealthUpdateArgs> EntityHealthUpdated;
        public event CustomEventHandler<IEntity, DeadEventArgs> EntityDead;

        public void RaiseEntityHealthUpdated(HealthUpdateArgs args)
        {
            var handler = EntityHealthUpdated;
            handler?.Invoke(Entity, args);

            globalEvent.RaiseEntityHealthUpdatedGlobal(Entity, args);
        }
        public void RaiseEntityDead(DeadEventArgs args)
        {
            var handler = EntityDead;
            handler?.Invoke(Entity, args);

            globalEvent.RaiseEntityDeadGlobal(Entity, args);
        }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.Entity = entity;

            // In case of entity conversion, there is an attempt to re-initialize this component but we do not allow it.
            if (!logger.RequireTrue(!IsInitialized,
              $"[{GetType().Name} - {Entity.Code}] Component already initialized! It is not supposed to be initialized again! Please retrace and report!"))
                return;

            this.inputMgr = gameMgr.GetService<IInputManager>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>();

            if (!logger.RequireTrue(maxHealth > 0,
                $"[{GetType().Name} - {entity.Code}] 'Max Health' field value must be > 0!"))
                return;

            // Each health component must serve one entity type
            // In case of a building resource for example, two health components for each entity type must be present
            // Check whether the health component is currently serving one entity type only
            if (!((int)EntityType == 1 << 0 || (int)EntityType == 1 << 1 || (int)EntityType == 1 << 2))
            {
                logger.LogError("[EntityHealth] The health component must serve only entity type and not multiple ones!", source: this);
                return;
            }

            hitEffectSpawnInput = new EffectObjectSpawnInput(
                parent: Entity.transform,

                useLocalTransform: true,
                spawnPosition: Vector3.zero,
                spawnRotation: Quaternion.identity);

            IsDead = false;

            stateHandler = new EntityHealthStateHandler();
            stateHandler.Init(gameMgr, this, states.Count);

            CurrHealth = 0;

            OnEntityHealthInit();

            IsInitialized = true;

            //must bypass the "CanAdd" conditions since the initial health value is enforced.
            //This is also called for all clients in a multiplayer game.
            AddLocal(new HealthUpdateArgs(Mathf.Clamp(initialHealth, 1, MaxHealth), source: null), force: true);
        }

        protected virtual void OnEntityHealthInit() { }

        public void Disable() { }
        #endregion

        #region Updating MaxHealth
        public ErrorMessage SetMax(HealthUpdateArgs args)
        {
            return inputMgr.SendInput(
                new CommandInput
                {
                    sourceMode = (byte)InputMode.health,
                    targetMode = (byte)InputMode.healthSetMax,

                    intValues = inputMgr.ToIntValues((int)EntityType, args.Value)
                },
                Entity,
                args.Source);
        }

        public ErrorMessage SetMaxLocal(HealthUpdateArgs args)
        {
            maxHealth = Mathf.Max(1, args.Value);

            OnHealthUpdated(new HealthUpdateArgs(0, args.Source));

            RaiseEntityHealthUpdated(args);

            return ErrorMessage.none;
        }
        #endregion

        #region Updating Health
        public abstract ErrorMessage CanAdd(HealthUpdateArgs args);

        public ErrorMessage Add(HealthUpdateArgs args)
        {
            return inputMgr.SendInput(
                new CommandInput
                {
                    sourceMode = (byte)InputMode.health,
                    targetMode = (byte)InputMode.healthAddCurr,

                    intValues = inputMgr.ToIntValues((int)EntityType, args.Value)
                },
                source: Entity,
                target: args.Source);
        }

        public ErrorMessage AddLocal(HealthUpdateArgs args, bool force = false)
        {
            if (!force)
            {
                ErrorMessage errorMessage;
                if ((errorMessage = CanAdd(args)) != ErrorMessage.none)
                    return errorMessage;
            }

            if (force || !LockHealth)
            {
                CurrHealth += args.Value;
                CurrHealth = Mathf.Clamp(CurrHealth, 0, MaxHealth);
            }

            stateHandler.Update(args.Value > 0, CurrHealth);

            OnHealthUpdated(args);

            RaiseEntityHealthUpdated(args);

            if (CurrHealth >= MaxHealth)
                OnMaxHealthReached(args);
            else if (CurrHealth <= 0)
            {
                OnZeroHealthReached(args);

                Destroy(false, args.Source);
            }

            if(args.Value < 0)
            {
                // Hit effect and audio
                effectObjPool.Spawn(hitEffect.Output, hitEffectSpawnInput); 
                audioMgr.PlaySFX(Entity.AudioSourceComponent, hitAudio.Fetch(), loop:false);
            }

            return ErrorMessage.none;
        }

        protected virtual void OnMaxHealthReached (HealthUpdateArgs args) { }
        protected virtual void OnZeroHealthReached (HealthUpdateArgs args) { }
        protected virtual void OnHealthUpdated (HealthUpdateArgs args) { }
        #endregion

        #region Destroying Entity
        public struct DestroyProperties
        {
            public bool upgrade;

            public IEntity source;
        }

        public virtual ErrorMessage CanDestroy(bool upgrade, IEntity source) => IsDead ? ErrorMessage.dead : ErrorMessage.none;

        public ErrorMessage Destroy(bool upgrade, IEntity source)
        {
            return inputMgr.SendInput(
                    new CommandInput
                    {
                        sourceMode = (byte)InputMode.health,
                        targetMode = (byte)InputMode.healthDestroy,

                        intValues = inputMgr.ToIntValues((int)EntityType, upgrade ? 1 : 0)
                    },
                    Entity,
                    source);
        }

        public ErrorMessage DestroyLocal(bool upgrade, IEntity source)
        {
            ErrorMessage errorMessage;
            if ((errorMessage = CanDestroy(upgrade, source)) != ErrorMessage.none)
                return errorMessage;

            selectionMgr.Remove(Entity);

            IsDead = true;

            TerminatedBy = source;

            CurrHealth = 0;

            float nextDestroyDelay = upgrade ? 0.0f : DestroyObjectDelay;

            if (destroyObject || upgrade)
                //Destroy the faction entity's object:
                StartCoroutine(DestroyCoroutine(nextDestroyDelay));

            if (Entity.IsInitialized)
            {
                //If this is no upgrade
                if (!upgrade)
                {
                    IEffectObject nextEffect = effectObjPool.Spawn(destructionEffect.Output, transform.position);

                    audioMgr.PlaySFX(nextEffect.IsValid() && nextEffect.AudioSourceComponent.IsValid() 
                        ? nextEffect.AudioSourceComponent 
                        : Entity.AudioSourceComponent,
                        destructionAudio.Fetch(), false);

                    if (source?.IsFree == false)
                        resourceMgr.UpdateResource(source.FactionID, destroyAward, add: true);

                    stateHandler.Activate(destroyState);
                }
            }

            if(Entity.MovementComponent.IsValid())
                Entity.MovementComponent.TargetPositionMarker.Toggle(false);

            OnDestroyed(upgrade, source);

            RaiseEntityDead(new DeadEventArgs(upgrade, source, nextDestroyDelay));

            return ErrorMessage.none;
        }

        private IEnumerator DestroyCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            Destroy(gameObject);
        }

        protected virtual void OnDestroyed(bool upgrade, IEntity source) { }
        #endregion
    }
}
