using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Animation;
using RTSEngine.Determinism;
using RTSEngine.Audio;
using RTSEngine.Effect;
using RTSEngine.Model;
using RTSEngine.Utilities;

namespace RTSEngine.EntityComponent
{
    // In child components, make sure no other action uses the following value
    public enum ProgressActionType : byte { setNextProgressData = 254 }

    public abstract class FactionEntityTargetProgressComponent<T> : FactionEntityTargetComponent<T>, IEntityTargetProgressComponent where T : IEntity
    {
        #region Attributes
        public override bool IsIdle => !HasTarget;

        // Active Progress:
        [SerializeField, Tooltip("What audio clip to play when the component enters the progress state and starts affecting the target?")]
        protected AudioClipFetcher progressEnabledAudio = new AudioClipFetcher();

        [SerializeField, Tooltip("Allows to have a custom progress animaiton when the component enters progress state and starts affecting the target.")]
        protected AnimatorOverrideControllerFetcher progressOverrideController = new AnimatorOverrideControllerFetcher();

        [SerializeField, Tooltip("When having an active target, how long does it take for the component to progress and affect the target?")]
        protected float progressDuration = 1.0f;
        private TimeModifiedTimer progressTimer;

        [SerializeField, Tooltip("The maximum allowed distance between the faction entity and its target so that progress remains active."), Min(0.0f)]
        private float progressMaxDistance = 1.0f;
        public float ProgressMaxDistance => progressMaxDistance;

        /// <summary>
        /// Is the faction entity currently actively working the entity component?
        /// </summary>
        public bool InProgress { get; private set; } = false;
        // Assigned when OnStop() is called to hold the last target.
        public bool WasInProgress { get; private set; } = false;

        [SerializeField, Tooltip("Activated when the faction entity's component is in progress.")]
        protected ModelCacheAwareTransformInput inProgressObject;

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Triggered on the source faction entity when the component is in progress.")]
        protected GameObjectToEffectObjectInput sourceEffect = null;
        private IEffectObject currSourceEffect;

        [SerializeField, EnforceType(typeof(IEffectObject)), Tooltip("Triggered on the target when the component is in progress.")]
        protected GameObjectToEffectObjectInput targetEffect = null;
        private IEffectObject currTargetEffect;

        public EntityTargetComponentProgressData ProgressData => new EntityTargetComponentProgressData
        {
            progressTime = progressTimer.CurrValue
        };

        // When enabled, the progress data will be set for the next time OnProgressEnabled is called.
        private bool nextProgressDataEnabled = false;
        private EntityTargetComponentProgressData nextProgressData;
        // When enabled, the next progress data will only be set if the target matches the one in 'nextProgressDataTarget' field.
        private bool nextProgressDataTargetEnabled;
        private IEntity nextProgressDataTarget;
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnTargetInit()
        {
            progressTimer = new TimeModifiedTimer(progressDuration);

            OnProgressInit();
        }

        protected virtual void OnProgressInit() { }
        #endregion

        #region Updating Component State
        private void Update()
        {
            if (!IsInitialized
                || !IsActive
                || factionEntity.Health.IsDead) //if the faction entity is dead, do not proceed.
                return;


            OnUpdate();

            if (HasTarget) //unit has target -> active
                TargetUpdate(); //on active update
            else //no target? -> inactive
                NoTargetUpdate();
        }

        protected virtual void OnUpdate() { }

        protected abstract bool MustStopProgress();
        protected abstract bool CanEnableProgress();
        protected abstract bool CanProgress();
        protected abstract bool MustDisableProgress();

        private void TargetUpdate()
        {
            if ((Target.instance.IsValid() && Target.instance.IsInteractable == false))
            {
                Stop();
                return;
            }

            if (MustStopProgress())
            {
                Stop();
                return;
            }

            if (!InProgress && CanEnableProgress())
                EnableProgress();

            if (InProgress && CanProgress())
            {
                if (progressTimer.ModifiedDecrease())
                {
                    OnProgress();
                    progressTimer.Reload();
                }

                if (MustDisableProgress())
                    DisableProgress();
            }

            OnTargetUpdate();
        }

        protected virtual void OnTargetUpdate() { }

        private void NoTargetUpdate()
        {
            if (InProgress == true)
                Stop(); //cancel job

            OnNoTargetUpdate();
        }

        protected virtual void OnNoTargetUpdate() { }
        #endregion

        #region Stopping
        protected override bool CanStopOnNoTarget() => InProgress;

        protected sealed override void OnStop()
        {
            WasInProgress = InProgress;

            DisableProgress();

            if (factionEntity.AnimatorController.IsValid()
                && (!factionEntity.MovementComponent.IsValid() || !factionEntity.MovementComponent.HasTarget))
            {
                factionEntity.AnimatorController.ResetAnimatorOverrideControllerOnIdle();
                factionEntity.AnimatorController.SetState(AnimatorState.idle);
            }

            OnProgressStop();
        }

        protected virtual void OnProgressStop() { }
        #endregion

        #region Handling Progress
        private void EnableProgress()
        {
            audioMgr.PlaySFX(factionEntity.AudioSourceComponent, progressEnabledAudio.Fetch(), loop: true);

            if (nextProgressDataEnabled 
                    && (!nextProgressDataTargetEnabled || Target.instance.Equals(nextProgressDataTarget)))
            {
                // next data custom progress timer
                progressTimer.Reload(nextProgressData.progressTime);
            }
            else
            {
                progressTimer.Reload(); //start timer with default value 
            }

            // Disable next progress data for next EnableProgress call.
            nextProgressDataEnabled = false;

            InProgress = true; //the unit's job is now in progress

            OnInProgressEnabledEffects();

            if (factionEntity.AnimatorController.IsValid())
                factionEntity.AnimatorController.LockState = true;

            if (factionEntity.CanMove())
                factionEntity.MovementComponent.Stop();

            if (factionEntity.AnimatorController.IsValid())
            {
                factionEntity.AnimatorController.LockState = false;

                factionEntity.AnimatorController.SetOverrideController(progressOverrideController.Fetch());

                factionEntity.AnimatorController.SetState(AnimatorState.inProgress);
            }

            OnInProgressEnabled();
        }

        protected virtual void OnInProgressEnabledEffects ()
        {
            if(inProgressObject.IsValid())
                inProgressObject.IsActive = true;

            ToggleSourceTargetEffect(true); //enable the source and target effect objects
        }

        protected virtual void OnInProgressEnabled () { }

        protected virtual void OnProgress() { }

        protected void DisableProgress()
        {
            InProgress = false;

            OnInProgressDisabledEffects();

            OnProgressDisabled();
        }

        protected virtual void OnInProgressDisabledEffects()
        {
            if(inProgressObject.IsValid())
                inProgressObject.IsActive = false;

            ToggleSourceTargetEffect(false);
        }

        protected virtual void OnProgressDisabled() { }
        #endregion

        #region Handling Actions
        public override ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input)
        {
            switch ((ProgressActionType)actionID)
            {
                case ProgressActionType.setNextProgressData:

                    return SetNextProgressDataLocal(
                        new EntityTargetComponentProgressData
                        {
                            progressTime = input.target.position.x
                        },
                        input.target.instance,
                        input.playerCommand);

                default:
                    return base.LaunchActionLocal(actionID, input);
            }
        }
        #endregion

        #region Handling Progress Data
        public ErrorMessage SetNextProgressData (EntityTargetComponentProgressData nextData, IEntity target, bool playerCommand)
        {
            LaunchAction((byte)ProgressActionType.setNextProgressData,
                new SetTargetInputData
                {
                    target = new TargetData<IEntity>
                    {
                        instance = target,
                        position = new Vector3(nextData.progressTime, 0.0f, 0.0f)
                    },
                    playerCommand = playerCommand
                });

            return ErrorMessage.none;
        }

        public ErrorMessage SetNextProgressDataLocal (EntityTargetComponentProgressData nextData, IEntity target, bool playerCommand)
        {
            nextProgressDataEnabled = true;
            nextProgressData = nextData;

            nextProgressDataTarget = target;
            nextProgressDataTargetEnabled = nextProgressDataTarget.IsValid();

            return ErrorMessage.none;
        }
        #endregion

        #region Progress Effects
        protected void ToggleSourceTargetEffect (bool enable)
        {
            if (!enable)
            {
                if (currSourceEffect.IsValid()) //if the source unit effect was assigned and it's still valid
                {
                    currSourceEffect.Deactivate(); //stop it
                    currSourceEffect = null;
                }

                if (currTargetEffect.IsValid()) //if a target effect was assigned and it's still valid
                {
                    currTargetEffect.Deactivate(); //stop it
                    currTargetEffect = null;
                }

                return;
            }

            var sourceEffectSpawnData = new EffectObjectSpawnInput(
                    parent: Entity.transform,

                    useLocalTransform: false,
                    spawnPosition: Entity.transform.position,
                    spawnRotation: new RotationData(sourceEffect.Output),

                    enableLifeTime: false);

            if (currSourceEffect.IsValid())
                currSourceEffect.OnSpawn(sourceEffectSpawnData);
            else
                currSourceEffect = effectObjPool.Spawn(sourceEffect.Output, sourceEffectSpawnData);

            if (!Target.instance.IsValid())
                return;

            var targetEffectSpawnData = 
                new EffectObjectSpawnInput(
                    parent: Target.instance.transform,

                    useLocalTransform: false,
                    spawnPosition: Target.instance.transform.position,
                    spawnRotation: new RotationData(targetEffect.Output),

                    enableLifeTime: false);
            if (currTargetEffect.IsValid())
                currTargetEffect.OnSpawn(targetEffectSpawnData);
            else
                currTargetEffect = effectObjPool.Spawn(targetEffect.Output, targetEffectSpawnData);
        }
        #endregion

        #region Searching/Updating Target
        public virtual float GetProgressRange()
            => progressMaxDistance;
        public virtual Vector3 GetProgressCenter() => factionEntity.transform.position;

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            return Vector3.Distance(sourcePosition, target.instance.transform.position) <= progressMaxDistance + target.instance.Radius;
        }

        protected sealed override void OnTargetPreLocked(bool playerCommand, TargetData<IEntity> newTarget, bool sameTarget) 
        {
            DisableProgress();
        }
        #endregion

        #region 
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!HasTarget)
                return;

            // Progress Gizmos:
            Gizmos.color = InProgress ? Color.green : Color.red;
            Gizmos.DrawWireSphere(GetProgressCenter(), GetProgressRange());

            // Target Gizmos
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(Target.instance.transform.position, Target.instance.Radius);
        }
#endif
        #endregion
    }
}
