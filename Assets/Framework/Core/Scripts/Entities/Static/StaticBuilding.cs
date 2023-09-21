using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Animation;
using RTSEngine.BuildingExtension;
using RTSEngine.EntityComponent;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.ResourceExtension;
using RTSEngine.Selection;
using RTSEngine.Task;
using RTSEngine.UnitExtension;
using RTSEngine.Upgrades;
using RTSEngine.Model;
using RTSEngine.Logging;

namespace RTSEngine.Entities.Static
{
    public class StaticBuilding : MonoBehaviour, IBuilding
    {
        #region Attributes
        public  EntityType Type => EntityType.building;

        // Static building properties
        public bool IsBuilt => true;
        public bool IsPlacementInstance => false;

        // Static building components
        public IBorder CurrentCenter { private set; get; }
        public IBorder BorderComponent => null;
        public IBuildingPlacer PlacerComponent => null;
        public IBuildingHealth Health => null;
        public IBuildingWorkerManager WorkerMgr => null;

        // Static faction entity properties
        public IFactionManager FactionMgr { private set; get; }
        public bool IsMainEntity => false;
        public bool IsFactionLocked => true;
        public IEnumerable<ResourceInput> InitResources => Enumerable.Empty<ResourceInput>();
        public IEnumerable<ResourceInput> DisableResources => Enumerable.Empty<ResourceInput>();

        // Static faction entity components
        IFactionEntityHealth IFactionEntity.Health => Health;
        public IRallypoint Rallypoint => null;
        public IDropOffTarget DropOffTarget => null;
        public IUnitCarrier UnitCarrier => null;

        // Static entity properties
        public bool IsInitialized { private set; get; }

        public bool CanAttack => false;

        public bool CanMove() => false;
        public bool CanMove(bool playerCommand) => false;

        [SerializeField, Tooltip("Unique code for each entity to be used to identify the entity type in the RTS Engine.")]
        private string code = "entity_code"; 
        public string Code => code;
        public int Key => -1;
       
        [SerializeField, EntityCategoryInput(isDefiner: true), Tooltip("A category that is used to define a group of entities.")]
        private string category = "entity_category";
        public IEnumerable<string> Category => category.Split(',');

        [SerializeField, Tooltip("When enabled, an error will be printed out when the static building is initialized and it is not part of any of its faction border territories.")]
        private bool enforceInBorder = true; 

        public float Radius => 0.0f;

        public string Name => code;
        public string Description => code;
        public Sprite Icon => null;

        public bool IsFree { protected set; get; }
        public int FactionID { protected set; get; }
        public IFactionSlot Slot { private set; get; }

        public IEntityModel EntityModel => null; 
        public bool IsInteractable => false;
        public bool IsSearchable => true;

        public Color SelectionColor { private set; get; }

        // Static entity components
        public IReadOnlyDictionary<string, IEntityComponent> EntityComponents => new Dictionary<string, IEntityComponent>();
        public IPendingTasksHandler PendingTasksHandler => null;

        public IReadOnlyDictionary<string, IEntityTargetComponent> EntityTargetComponents => new Dictionary<string, IEntityTargetComponent>();

        public IEnumerable<IAttackComponent> AttackComponents => Enumerable.Empty<IAttackComponent>();
        public IAttackComponent AttackComponent => null;

        public IReadOnlyDictionary<string, IAddableUnit> AddableUnitComponents => new Dictionary<string, IAddableUnit>();

        public IMovementComponent MovementComponent => null;

        public IAnimatorController AnimatorController => null;

        public IEntitySelection Selection => null;
        public IEntitySelectionMarker SelectionMarker => null;

        public AudioSource AudioSourceComponent => null;

        IEntityHealth IEntity.Health => Health;

        IEntityWorkerManager IEntity.WorkerMgr => null;

        public bool CanLaunchTask => false;

        public bool IsIdle => false;

        public bool IsDummy => true;

        public ModelCacheAwareTransformInput TransformInput => null;

        public IReadOnlyDictionary<string, IEntityTargetProgressComponent> EntityTargetProgressComponents => throw new NotImplementedException();

        public IEntityTasksQueueHandler TasksQueue => null;
        #endregion

        #region Raising Events
        public event CustomEventHandler<IBuilding, EventArgs> BuildingBuilt;
        private void RaiseBuildingBuilt()
        {
            var handler = BuildingBuilt;
            handler?.Invoke(this, System.EventArgs.Empty);
        }

        public event CustomEventHandler<IEntity, EventArgs> EntityInitiated;

        private void RaiseEntityInitiated()
        {
            var handler = EntityInitiated;
            handler?.Invoke(this, System.EventArgs.Empty);
        }

#pragma warning disable CS0067 // The event is never used 
        public event CustomEventHandler<IEntity, FactionUpdateArgs> FactionUpdateComplete;
        public event CustomEventHandler<IEntity, EntityComponentUpgradeEventArgs> EntityComponentUpgraded;
#pragma warning restore CS0067 // The event is never used 
        #endregion

        #region Initializing/Terminating
        public void InitPrefab(IGameManager gameMgr) { }
        public void Init(IGameManager gameMgr, InitBuildingParameters initParams)
        {
            IsInitialized = false;

            this.FactionID = initParams.factionID;
            this.IsFree = initParams.free;
            this.Slot = gameMgr.GetFactionSlot(initParams.factionID);
            this.FactionMgr = Slot.FactionMgr;
            this.SelectionColor = Slot.Data.color;

            this.CurrentCenter = initParams.buildingCenter;
            if(enforceInBorder && !gameMgr.GetService<IGameLoggingService>().RequireValid(this.CurrentCenter,
                  $"[{GetType().Name}] Static building does not have a valid building center (building with a {typeof(IBorder).Name} component attached to it) assigned! Make sure it is initialized within the boundaries of its faction's territories.", source: this))
                    return; 

            IsInitialized = true;

            if (IsFree) //free builidng? job is done here
                return;

            RaiseEntityInitiated();
            gameMgr.GetService<IGlobalEventPublisher>().RaiseEntityInitiatedGlobal(this);

            RaiseBuildingBuilt();
        }

        // Static buildings do not have a placement instance but we need to define this to satisfy the interface
        public void InitPlacementInstance(IGameManager gameMgr, InitBuildingParameters initParams) { }
        #endregion

        #region Disabled (Undefined) Methods
        public ErrorMessage SetTargetFirst(SetTargetInputData input) => ErrorMessage.undefined;
        public ErrorMessage SetTargetFirstLocal(SetTargetInputData input) => ErrorMessage.undefined;

        public void SetIdle(bool includeMovement = true) { }
        public void SetIdle(IEntityTargetComponent exception = null, bool includeMovement = true) { }
        public void SetIdle(IEnumerable<IEntityTargetComponent> exceptions = null, bool includeMovement = true) { }

        public ErrorMessage SetFaction(IEntity source, int targetFactionID) => ErrorMessage.undefined;
        public ErrorMessage SetFactionLocal(IEntity source, int targetFactionID) => ErrorMessage.undefined;

        public void UpgradeComponent(UpgradeElement<IEntityComponent> upgradeElement) { }

        public bool Equals(IEntity other)
        {
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
            return other == this;
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
        }
        #endregion
    }
}
