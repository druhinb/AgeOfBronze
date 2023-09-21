using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.UI;

namespace RTSEngine.EntityComponent
{
    public abstract class EntityComponentBase : MonoBehaviour, IEntityComponent, IEntityPreInitializable
    {
        #region Attributes
        public bool IsInitialized { private set; get; } = false;
        public virtual bool AllowPreEntityInit => false;

        [SerializeField, Tooltip("Code that defines this component, uniquely within the entity.")]
        private string code = "comp_code";
        public string Code => code;

        public IEntity Entity { private set; get; }

        [SerializeField, Tooltip("Is the component enabled by default?")]
        private bool isActive = true;
        public bool IsActive => isActive;

        public EntityComponentData Data => new EntityComponentData
        {
            isActive = IsActive
        };

        protected IGameLoggingService logger { private set; get; }
        protected IGameManager gameMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            if (!AllowPreEntityInit || IsInitialized)
                return;

            Init(gameMgr, entity);
        }

        public void OnEntityPostInit (IGameManager gameMgr, IEntity entity)
        {
            if (IsInitialized)
                return;

            Init(gameMgr, entity);
        }

        private void Init(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.Entity = entity;

            if (IsInitialized)
            {
                logger.LogError($"[{GetType().Name} - {Entity.Code}] Component already initialized! It is not supposed to be initialized again! Please retrace and report!", source: this);
                return;
            }

            this.gameMgr = gameMgr;

            OnInit();

            IsInitialized = true;
        }

        protected virtual void OnInit() { }

        public void Disable()
        {
            if (!IsInitialized)
                return;

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Handling Component Upgrade
        public virtual void HandleComponentUpgrade (IEntityComponent sourceEntityComponent) { }
        #endregion

        #region Activating/Deactivating Component
        public ErrorMessage SetActive(bool active, bool playerCommand) => RTSHelper.SetEntityComponentActive(this, active, playerCommand);

        public ErrorMessage SetActiveLocal(bool active, bool playerCommand)
        {
            isActive = active;

            OnActiveStatusUpdated();

            return ErrorMessage.none;
        }

        protected virtual void OnActiveStatusUpdated() { }
        #endregion

        #region Handling Actions
        public ErrorMessage LaunchAction(byte actionID, SetTargetInputData input)
            => RTSHelper.LaunchEntityComponentAction(this, actionID, input);

        public virtual ErrorMessage LaunchActionLocal(byte actionID, SetTargetInputData input) => ErrorMessage.undefined;
        #endregion

        #region Task UI
        public virtual bool OnTaskUIRequest(
            out IEnumerable<EntityComponentTaskUIAttributes> taskUIAttributes,
            out IEnumerable<string> disabledTaskCodes)
        {
            taskUIAttributes = Enumerable.Empty<EntityComponentTaskUIAttributes>();
            disabledTaskCodes = Enumerable.Empty<string>();
            return false; 
        }

        public virtual bool OnTaskUIClick(EntityComponentTaskUIAttributes taskAttributes) 
        {
            return false;
        }

        public virtual bool OnAwaitingTaskTargetSet(EntityComponentTaskUIAttributes taskAttributes, TargetData<IEntity> target)
        {
            return false;
        }
        #endregion
    }
}
