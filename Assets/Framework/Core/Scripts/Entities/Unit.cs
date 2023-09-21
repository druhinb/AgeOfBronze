using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Movement;
using RTSEngine.Game;
using RTSEngine.Animation;
using RTSEngine.Health;

namespace RTSEngine.Entities
{
    public class Unit : FactionEntity, IUnit
    {
        #region Class Attributes
        public sealed override EntityType Type => EntityType.unit;

        //the component that is responsible for moving the unit when it is created.
        public IRallypoint SpawnRallypoint { private set; get; }

        // Component used to create the unit
        public IEntityComponent CreatorEntityComponent { private set; get; }

        [SerializeField, Tooltip("The Transform from which the look at position is set, when the unit spawns.")]
        private Transform spawnLookAt = null;

        public override bool CanMove(bool playerCommand)
        {
            return playerCommand && CarriableUnit.IsValid() && CarriableUnit.CurrCarrier.IsValid()
                ? CarriableUnit.AllowMovementToExitCarrier
                : base.CanMove(playerCommand);
        }

        public IDropOffSource DropOffSource { private set; get; }
        public IResourceCollector CollectorComponent { private set; get; }
        public IBuilder BuilderComponent { private set; get; }
        public ICarriableUnit CarriableUnit { private set; get; }
        public new IUnitHealth Health { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, InitUnitParameters initParams)
        {
            base.Init(gameMgr, initParams);

            //handling rigidbody:
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if(rigidbody)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            if (!IsFree && initParams.giveInitResources)
                resourceMgr.UpdateResource(FactionID, InitResources, add: true);

            SpawnRallypoint = initParams.rallypoint;
            CreatorEntityComponent = initParams.creatorEntityComponent;

            CompleteInit();
            globalEvent.RaiseUnitInitiatedGlobal(this);

            //handling spawn rotation:
            if (spawnLookAt) //if we have a set a position for the unit to look at when it is spawned.
                MovementComponent.UpdateRotationTarget(null, spawnLookAt.position);
            //if not, see if there is a creator for the unit and look in the opposite direction of it.
            else if (initParams.rallypoint.IsValid())
                MovementComponent.UpdateRotationTarget(null, initParams.rallypoint.Entity.transform.position, lookAway: true, setImmediately: true);
            else
                MovementComponent.UpdateRotationTarget(transform.rotation, setImmediately: true);

            // Allow CompleteInit() to initialize the movement component since all IEntityComponent components are initialized with that call.
            Radius = MovementComponent.Controller.Radius; //for units, their radius is overwritten by the movement component's controller radius
            SetInitialTargetPosition(initParams);
        }

        protected sealed override void FetchComponents()
        {
            DropOffSource = GetEntityComponent<IDropOffSource>();
            CollectorComponent = GetEntityComponent<IResourceCollector>();
            BuilderComponent = GetEntityComponent<IBuilder>();
            CarriableUnit = GetEntityComponent<ICarriableUnit>();

            Health = transform.GetComponentInChildren<IUnitHealth>();

            base.FetchComponents();

            // IEntity component is responsible for getting the movement component
            if (!logger.RequireValid(MovementComponent,
                $"[{GetType().Name} - {Code}] Units must have a component that implements #{typeof(IMovementComponent)}' that handles unit movement")
                || !logger.RequireValid(AnimatorController,
                $"[{GetType().Name} - {Code}] Units must have a component that implements #{typeof(IAnimatorController)}' that handles animation."))
                return;
        }

        // A method that is used to move the unit to its initial position after it spawns
        protected virtual void SetInitialTargetPosition (InitUnitParameters initParams)
        {
            if (!RTSHelper.IsMasterInstance()
                || !initParams.useGotoPosition)
                return;

            if (SpawnRallypoint != null)
                SpawnRallypoint.SendAction (this, playerCommand: false);
            else if (Vector3.Distance(initParams.gotoPosition, transform.position) > mvtMgr.StoppingDistance) //only if the goto position is not within the stopping distance of this unit
                mvtMgr.SetPathDestination(
                    this,
                    initParams.gotoPosition,
                    0.0f,
                    null,
                    new MovementSource { playerCommand = false });
        }

        protected sealed override void Disable(bool IsUpgrade, bool isFactionUpdate)
        {
            base.Disable(IsUpgrade, isFactionUpdate);

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Editor Only
#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
        }
#endif
        #endregion

    }
}
