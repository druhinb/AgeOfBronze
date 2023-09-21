using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Selection;
using RTSEngine.Health;
using RTSEngine.Event;
using RTSEngine.Animation;
using RTSEngine.Upgrades;
using RTSEngine.Task;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Faction;
using RTSEngine.Determinism;
using RTSEngine.UnitExtension;
using RTSEngine.Utilities;
using RTSEngine.Model;
using RTSEngine.Attack;
using RTSEngine.Movement;

namespace RTSEngine.Entities
{
    public abstract class Entity : MonoBehaviour, IEntity
    {
        #region Class Attributes
        [HideInInspector]
        public Int2D tabID = new Int2D { x = 0, y = 0 };

        public abstract EntityType Type { get; }

        public bool IsInitialized { private set; get; }

        //multiplayer related:
        public int Key { private set; get; }

        [SerializeField, Tooltip("Name of the entity that will be displayed in UI elements.")]
        private string _name = "entity_name";
        public string Name => _name;

        [SerializeField, EntityCodeInput(isDefiner: true), Tooltip("Unique code for each entity to be used to identify the entity type in the RTS Engine.")]
        private string code = "entity_code";
        public string Code => code;

        [SerializeField, EntityCategoryInput(isDefiner: true), Tooltip("A category that is used to define a group of entities. You can input multiple categories separated by a ','.")]
        private string category = "entity_category";
        public IEnumerable<string> Category => category.Split(',');

        [SerializeField, TextArea(minLines: 5, maxLines: 5), Tooltip("Description of the entity to be displayed in UI elements.")]
        private string description = "entity_description";
        public string Description => description;

        [SerializeField, IconDrawer, Tooltip("Icon of the entity to be displayed in UI elements.")]
        private Sprite icon = null;
        public Sprite Icon => icon;

        [SerializeField, Tooltip("Defines the range that the entity is supposed to occupy on the map. This is represented by the blue sphere gizmo.")]
        private float radius = 2.0f;
        public float Radius { get { return radius; } protected set { radius = value; } }

        [SerializeField, Tooltip("Drag and drop the model object of the entity into this field. Make sure the model object has the 'EntityModelConnections' component attached to it.")]
        private EntityModelConnections model = null;
        public IEntityModel EntityModel { private set; get; }

        public ModelCacheAwareTransformInput TransformInput { private set; get; }

        //double clicking on the unit allows to select all entities of the same type within a certain range
        private float doubleClickTimer;

        public bool IsFree { protected set; get; }
        public int FactionID { protected set; get; }
        public IFactionSlot Slot => gameMgr.GetFactionSlot(FactionID);

        public Color SelectionColor { protected set; get; }

        public AudioSource AudioSourceComponent { private set; get; }

        public IAnimatorController AnimatorController { private set; get; }
        public IEntitySelection Selection { private set; get; }
        public IEntitySelectionMarker SelectionMarker { private set; get; }
        public IEntityHealth Health { protected set; get; }
        public IEntityWorkerManager WorkerMgr { private set; get; }

        public virtual bool CanLaunchTask => IsInitialized && !Health.IsDead;

        private bool interactable;

        public virtual bool IsDummy => false;

        public bool IsInteractable {
            protected set => interactable = value;
            get => gameObject.activeInHierarchy && interactable && IsInitialized;
        }
        public bool IsSearchable => IsInteractable;

        public bool IsIdle
        {
            get
            {
                if (TasksQueue.IsValid() && TasksQueue.QueueCount > 0)
                    return false;

                for (int i = 0; i < entityTargetComponents.Length; i++)
                    if (!entityTargetComponents[i].IsIdle)
                        return false;

                return true;
            }
        }

        //entity components:
        public IReadOnlyDictionary<string, IEntityComponent> EntityComponents { private set; get; }

        public IPendingTasksHandler PendingTasksHandler { private set; get; }
        public IEntityTasksQueueHandler TasksQueue { private set; get; }

        public IReadOnlyDictionary<string, IAddableUnit> AddableUnitComponents { private set; get; }

        public IMovementComponent MovementComponent { private set; get; }

        public bool CanMove() => MovementComponent.IsValid() && MovementComponent.IsActive;
        public virtual bool CanMove(bool playerCommand) => CanMove();

        public IEntityTargetComponent[] entityTargetComponents;
        public IReadOnlyDictionary<string, IEntityTargetComponent> EntityTargetComponents { private set; get; }
        public IReadOnlyDictionary<string, IEntityTargetProgressComponent> EntityTargetProgressComponents { private set; get; }

        public IEnumerable<IAttackComponent> AttackComponents { private set; get; }
        public IAttackComponent AttackComponent => AttackComponents.Where(comp => comp.IsActive).FirstOrDefault();
        public bool CanAttack => AttackComponent != null && AttackComponent.IsActive;

        // Services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IMouseSelector mouseSelector { private set; get; }
        protected ISelectionManager selectionMgr { private set; get; } 
        protected IInputManager inputMgr { private set; get; }
        protected IEntityComponentUpgradeManager entityComponentUpgradeMgr { private set; get; } 
        protected ITaskManager taskMgr { private set; get; }
        protected IAttackManager attackMgr { private set; get; }
        protected IMovementManager mvtMgr { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IEntity, System.EventArgs> EntityInitiated;
        private void RaiseEntityInitiated()
        {
            var handler = EntityInitiated;
            handler?.Invoke(this, System.EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, EventArgs> EntityDoubleClicked;
        private void RaiseEntityDoubleClicked ()
        {
            var handler = EntityDoubleClicked;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, FactionUpdateArgs> FactionUpdateComplete;

        protected void RaiseFactionUpdateComplete(FactionUpdateArgs eventArgs)
        {
            var handler = FactionUpdateComplete;
            handler?.Invoke(this, eventArgs);
        }

        public event CustomEventHandler<IEntity, EntityComponentUpgradeEventArgs> EntityComponentUpgraded;
        private void RaiseEntityComponentUpgraded(EntityComponentUpgradeEventArgs args)
        {
            var handler = EntityComponentUpgraded;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public virtual void InitPrefab(IGameManager gameMgr)
        {
            if (!gameMgr.GetService<IGameLoggingService>().RequireValid(model,
              $"[{GetType().Name} - {Code}] The 'Model' field must be assigned!", source: this))
                return;

            if (!(EntityModel = GetComponent<IEntityModel>()).IsValid())
                EntityModel = gameObject.AddComponent<EntityModel>();

            // Immediately set parent to null since some model cache aware calculations require the entity to be parentless
            transform.SetParent(null, worldPositionStays: true);

            // Initialize the model connections
            model.Init(gameMgr, this);
            gameMgr.GetService<IModelCacheManager>().CacheModel(Code, model);

            // The above constructor allows to cache the model object in the prefab which is why we want to set the model to null
            // So that any entity created from this base prefab will not have a model by default.
            model = null;

            foreach (var component in GetComponents<IEntityPrefabInitializable>())
                component.OnPrefabInit(this, gameMgr);
        }

        public virtual void Init(IGameManager gameMgr, InitEntityParameters initParams)
        {
            this.gameMgr = gameMgr;
            this.logger = this.gameMgr.GetService<IGameLoggingService>();

            if(!logger.RequireTrue(!IsInitialized,
                $"[{GetType().Name} - {Code}] Entity has been already initiated!"))
                return;

            if (!initParams.free && !initParams.factionID.IsValidFaction())
            {
                logger.LogError($"[{GetType().Name} - {Code}] Initializing entity with invalid faction ID '{initParams.factionID}'!");
                return;
            }

            this.inputMgr = gameMgr.GetService<IInputManager>();

            // Immediately set parent to null since some model cache aware calculations require the entity to be parentless
            transform.SetParent(null, worldPositionStays: true);

            this.IsFree = initParams.free;
            this.FactionID = IsFree ? RTSHelper.FREE_FACTION_ID : initParams.factionID;

            // The reason behind not caching an already existing model and opting to destroy it...
            // ... instead is to make sure all pre-placed entities of the same type would use clones of the same model entity that comes from the prefab...
            // ... and this allows us to avoid a lot of issues
            if(model.IsValid())
                Destroy(model.gameObject);
            /*if(model.IsValid())
                gameMgr.GetService<IModelCacheManager>().CacheModel(Code, model);*/
            model = null;

            this.globalEvent = this.gameMgr.GetService<IGlobalEventPublisher>();
            this.mouseSelector = this.gameMgr.GetService<IMouseSelector>();
            this.selectionMgr = gameMgr.GetService<ISelectionManager>(); 
            this.entityComponentUpgradeMgr = gameMgr.GetService<IEntityComponentUpgradeManager>();
            this.taskMgr = gameMgr.GetService<ITaskManager>();
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.mvtMgr = gameMgr.GetService<IMovementManager>(); 

            TransformInput = new ModelCacheAwareTransformInput(transform);

            //get the components attached to the entity
            HandleComponentUpgrades();
            FetchEntityComponents();
            FetchComponents();

            // Dummy entities are entities that would not be registered for the faction but are used to fulfil a local service like placement buildings
            if (!IsDummy)
            {
                Key = inputMgr.RegisterEntity(this, initParams);
            }

            SubToEvents();

            //entity parent objects are set to ignore raycasts because selection relies on raycasting selection objects which are typically direct children of the entity objects.
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            InitPriorityComponents();

            UpdateColors();

            InitComponents(initPre: true);

            if(initParams.setInitialHealth)
                //must bypass the "CanAdd" conditions in IEntityHealth since the initial health value is enforced.
                //This is also called for all clients in a multiplayer game.
                Health.AddLocal(new HealthUpdateArgs(initParams.initialHealth - Health.CurrHealth, null));

            //initial settings for the double click
            doubleClickTimer = 0.0f;
        }

        protected void CompleteInit()
        {
            //by default, an entity is interactable.
            IsInteractable = true;
            IsInitialized = true;

            InitComponents(initPost: true);

            RaiseEntityInitiated();
            globalEvent.RaiseEntityInitiatedGlobal(this);

            OnInitComplete();
        }

        protected virtual void OnInitComplete() { }

        protected void InitPriorityComponents()
        {
            foreach (IEntityPriorityPreInitializable component in transform
                .GetComponentsInChildren<IEntityPriorityPreInitializable>()
                .OrderBy(component => component.PreInitPriority))
                component.OnEntityPreInit(gameMgr, this);
        }

        protected void InitComponents(bool initPre = false, bool initPost = false)
        {
            if (initPre)
                foreach (IEntityPreInitializable component in transform.GetComponentsInChildren<IEntityPreInitializable>())
                    component.OnEntityPreInit(gameMgr, this);

            if (initPost)
            {
                foreach (IEntityPostInitializable component in transform.GetComponentsInChildren<IEntityPostInitializable>())
                    component.OnEntityPostInit(gameMgr, this);
            }
        }

        protected void DisableComponents()
        {
            foreach (IEntityPreInitializable component in transform.GetComponentsInChildren<IEntityPreInitializable>())
                component.Disable();

            if (!IsInitialized)
                return;

            foreach (IEntityPostInitializable component in transform.GetComponentsInChildren<IEntityPostInitializable>())
                component.Disable();
        }

        private void HandleComponentUpgrades()
        {
            if (IsFree 
                || !entityComponentUpgradeMgr.TryGet(this, FactionID, out List<UpgradeElement<IEntityComponent>> componentUpgrades))
                return;

            foreach(UpgradeElement<IEntityComponent> element in componentUpgrades)
                UpgradeComponent(element);
        }

        //the assumption here is that the targetComponent is attached to an empty prefab game object that includes no additional components!
        //initTime is set to true when this method is called from the initializer method of the Entity, in that case, no need to init the new component/re-fetch components
        //if this method is called from outside the Init() method of this class, then the initTime must be set to false so that components can be refetched and the new component is initialized
        public void UpgradeComponent(UpgradeElement<IEntityComponent> upgradeElement)
        {
            //get the component to be upgraded, destroy it and replace it with the target upgrade component
            //both components must be valid
            if (!upgradeElement.target.IsValid())
                return;

            RTSHelper.TryGetEntityComponentWithCode(this, upgradeElement.sourceCode, out IEntityComponent sourceComponent);

            //since components with their field values can not be added directly, we create a child object of the entity with the upgraded component
            Transform newComponentTransform = Instantiate(upgradeElement.target.gameObject).transform;
            newComponentTransform.SetParent(transform, true);
            newComponentTransform.transform.localPosition = Vector3.zero;
            IEntityComponent newEntityComponent = newComponentTransform.GetComponent<IEntityComponent>();

            if (AnimatorController.IsValid())
                AnimatorController.LockState = true;

            RaiseEntityComponentUpgraded(new EntityComponentUpgradeEventArgs(sourceComponent, newEntityComponent));

            if (IsInitialized)
            {
                // Disable the old entity component flagging it to make it unfetchable // this can happen in the upgrade method itself
                // Fetch mvt, attack and special entity components through the fetched entity components that do not include disabled ones
                FetchEntityComponents(sourceComponent);
                FetchComponents();

                newEntityComponent.OnEntityPostInit(gameMgr, this);
                if (sourceComponent.IsValid())
                    newEntityComponent.HandleComponentUpgrade(sourceComponent);
            }

            //disable old component
            if (sourceComponent.IsValid())
            {
                if(IsInitialized) // Only disable the component if the entity has been initialized
                    sourceComponent.Disable();

                DestroyImmediate(sourceComponent as UnityEngine.Object);
            }

            if (AnimatorController.IsValid())
                AnimatorController.LockState = false;
        }

        protected virtual void SubToEvents()
        {
            //subscribe to events:
            Health.EntityDead += HandleEntityDead;
            Selection.Selected += HandleEntitySelected;
        }

        private void FetchEntityComponents(IEntityComponent exception)
            => FetchEntityComponents(Enumerable.Repeat(exception, 1));
        private void FetchEntityComponents ()
        {
            //finding and initializing entity components.
            IEntityComponent[] entityComponents = transform
                .GetComponentsInChildren<IEntityComponent>();

            FetchEntityComponentsInternal(entityComponents);
        }
        private void FetchEntityComponents (IEnumerable<IEntityComponent> exceptions)
        {
            //finding and initializing entity components.
            IEntityComponent[] entityComponents = transform
                .GetComponentsInChildren<IEntityComponent>()
                .Except(exceptions)
                .ToArray();

            FetchEntityComponentsInternal(entityComponents);
        }
        private void FetchEntityComponentsInternal(IEntityComponent[] entityComponents)
        {
            if (!logger.RequireTrue(entityComponents.Select(comp => comp.Code).Distinct().Count() == entityComponents.Length,
                $"[{GetType().Name} - {Code}] All entity components attached to the entity must each have a distinct code to identify it within the entity!"))
                return;

            EntityComponents = entityComponents.ToDictionary(comp => comp.Code);
        }

        protected T GetEntityComponent<T>() where T : IEntityComponent
        {
            return (T)EntityComponents.Values.FirstOrDefault(comp => comp is T);
        }    
        protected IEnumerable<T> GetEntityComponents<T>() where T : IEntityComponent
        {
            return EntityComponents.Values.Where(comp => comp is T).Cast<T>();
        }    

        protected virtual void FetchComponents()
        {
            if (!(EntityModel = GetComponent<IEntityModel>()).IsValid())
                EntityModel = gameObject.AddComponent<EntityModel>();

            AnimatorController = transform.GetComponentInChildren<IAnimatorController>();

            Selection = transform.GetComponentInChildren<IEntitySelection>();
            if (!logger.RequireValid(Selection,
                $"[{GetType().Name} - {Code}] A selection component that extends {typeof(IEntitySelection).Name} must be assigned to the 'Selection' field!"))
                return;

            SelectionMarker = GetComponentInChildren<IEntitySelectionMarker>();

            // The Health component is assigned in the childrten of this class before this is called.
            if (!logger.RequireValid(Health,
                $"[{GetType().Name} - {Code}] An entity health component that extends {typeof(IEntityHealth).Name} must be assigned attache to the entity!"))
                return;

            WorkerMgr = transform.GetComponentInChildren<IEntityWorkerManager>();

            //get the audio source component attached to the entity main object:
            AudioSourceComponent = transform.GetComponentInChildren<AudioSource>();

            PendingTasksHandler = transform.GetComponentInChildren<IPendingTasksHandler>();
            TasksQueue = transform.GetComponentInChildren<IEntityTasksQueueHandler>();

            AddableUnitComponents = transform.GetComponentsInChildren<IAddableUnit>().ToDictionary(comp => comp.Code);

            // Entity Components: Fetch from EntityComponents instead of GetComponentInChildren!!
            MovementComponent = GetEntityComponent<IMovementComponent>();

            if (!logger.RequireTrue(GetEntityComponents<IMovementComponent>().Count() < 2,
                $"[{GetType().Name} - {Code}] Having more than one components that extend {typeof(IMovementComponent).Name} interface attached to the same entity is not allowed!"))
                return;

            entityTargetComponents = GetEntityComponents<IEntityTargetComponent>().OrderBy(comp => comp.Priority).ToArray();
            EntityTargetComponents = entityTargetComponents.ToDictionary(comp => comp.Code);

            EntityTargetProgressComponents = GetEntityComponents<IEntityTargetProgressComponent>().ToDictionary(comp => comp.Code);

            AttackComponents = GetEntityComponents<IAttackComponent>().OrderBy(comp => comp.Priority);

            if (AttackComponents.Where(attackComponent => attackComponent.IsActive).Count() > 1)
            {
                logger.LogError($"[Entity - {Code}] More than one attack components are marked as active on the same entity, this is not allowed. Make sure at most one is active when the entitiy is initialized!", source: this);
                return;
            }
        }

        protected virtual void Disable (bool isUpgrade, bool isFactionUpdate)
        {
            SetIdle();

            // Only completely disable components in case of a complete destruction of the entity
            // Because disabling a component = no going back from there, it is not functional anymore
            if(!isFactionUpdate)
                DisableComponents();
        }

        private void OnDestroy()
        {
            if(Health.IsValid())
                Health.EntityDead -= HandleEntityDead;
            if (Selection.IsValid())
                Selection.Selected -= HandleEntitySelected;
        }
        #endregion

        #region Handling Events
        private void HandleEntityDead(IEntity sender, DeadEventArgs e)
        {
            Disable(e.IsUpgrade, false);
        }

        private void HandleEntitySelected(IEntity sender, EntitySelectionEventArgs args)
        {
            SelectionMarker.StopFlash(); //in case the selection texture of the entity was flashing

            if (args.Type != SelectionType.single)
                return;

            if(doubleClickTimer > 0.0f)
            {
                doubleClickTimer = 0.0f;

                //if this is the second click (double click), select all entities of the same type within a certain range
                mouseSelector.SelectEntitisInRange(this, playerCommand: true);

                RaiseEntityDoubleClicked();

                return;
            }

            //if the player doesn't have the multiple selection key down (not looking to select multiple entities one by one)
            if (mouseSelector.MultipleSelectionKeyDown == false)
                doubleClickTimer = 0.5f;
        }
        #endregion

        #region Handling Double Clicks
        protected virtual void Update()
        {
            if (doubleClickTimer > 0)
                doubleClickTimer -= Time.deltaTime;

        }

        public void OnPlayerClick()
        {
        }
        #endregion

        #region Updating IEntityTargetComponent Components State (Except Movement and Attack).
        public ErrorMessage SetTargetFirst (SetTargetInputData input)
        {
            if (!input.isMoveAttackRequest && this.IsLocalPlayerFaction())
                input.isMoveAttackRequest = (attackMgr.CanAttackMoveWithKey && input.playerCommand);

            if (CanAttack && AttackComponent.IsTargetValid(input.target, input.playerCommand) == ErrorMessage.none)
            {
                if(TasksQueue.IsValid() && TasksQueue.CanAdd(input))
                {
                    return TasksQueue.Add(new SetTargetInputData 
                    {
                        componentCode = AttackComponent.Code,

                        target = input.target,
                        playerCommand = input.playerCommand,

                        includeMovement = input.includeMovement
                    });
                }

                return attackMgr.LaunchAttack(
                    new LaunchAttackData<IEntity>
                    {
                        source = this,
                        targetEntity = input.target.instance as IFactionEntity,
                        targetPosition = input.target.instance.IsValid() ? input.target.instance.transform.position : input.target.position,
                        playerCommand = input.playerCommand,
                        isMoveAttackRequest = input.isMoveAttackRequest
                    });
            }
            else if (input.includeMovement && CanMove(input.playerCommand))
            {
                if(TasksQueue.IsValid() && TasksQueue.CanAdd(input))
                {
                    return TasksQueue.Add(new SetTargetInputData 
                    {
                        componentCode = MovementComponent.Code,

                        target = input.target,
                        playerCommand = input.playerCommand,

                        includeMovement = input.includeMovement
                    });
                }

                return mvtMgr.SetPathDestination(
                    this,
                    input.target.position,
                    input.target.instance.IsValid() ? input.target.instance.Radius : 0.0f,
                    input.target.instance,
                    new MovementSource { 
                        playerCommand = input.playerCommand,
                        isMoveAttackRequest = input.isMoveAttackRequest
                    });
            }
            else
            {
                foreach (IEntityTargetComponent comp in entityTargetComponents)
                {
                    if (comp.IsActive
                        && comp != MovementComponent
                        && comp != AttackComponent
                        && comp.IsTargetValid(input.target, input.playerCommand) == ErrorMessage.none)
                    {
                        return comp.SetTarget(input);
                    }
                }
            }

            return ErrorMessage.failed;
        }

        public ErrorMessage SetTargetFirstLocal (SetTargetInputData input)
        {
            foreach (IEntityTargetComponent comp in entityTargetComponents)
            {
                if (comp.IsActive
                    && comp != MovementComponent
                    && comp != AttackComponent
                    && comp.SetTargetLocal(input) == ErrorMessage.none)
                    return ErrorMessage.none;
            }

            return ErrorMessage.failed;
        }

        public void SetIdle(bool includeMovement = true)
            => SetIdle(source: null, includeMovement);
        public void SetIdle(IEntityTargetComponent source, bool includeMovement = true)
        {
            for (int i = 0; i < entityTargetComponents.Length; i++)
            {
                IEntityTargetComponent nextComp = entityTargetComponents[i];
                if (source != nextComp
                    && (!source.IsValid() || nextComp.CanStopOnSetIdleSource(source))
                    && !nextComp.IsIdle
                    && (includeMovement || nextComp != MovementComponent))
                {
                    nextComp.Stop();
                }
            }
        }
        #endregion

        #region Updating Faction
        public abstract ErrorMessage SetFaction(IEntity source, int targetFactionID);

        public abstract ErrorMessage SetFactionLocal(IEntity source, int targetFactionID);
        #endregion

        #region Updating Entity Colors
        protected abstract void UpdateColors();
        #endregion

        #region IEquatable Implementation
        public bool Equals(IEntity other)
        {
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
            return other == this;
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
        }
        #endregion

        #region Editor Only
#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            //Draw the entity's radius in blue
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
        #endregion
    }
}
