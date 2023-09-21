using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Controls;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.Selection
{
    public enum SelectionGroupAction { select, assign, append, substract }

    [System.Serializable]
    public class SelectionGroup
    {
        #region Attributes
        [SerializeField, Tooltip("Control type that defines the key used to use this selection group.")]
        private ControlType key = null;

        [SerializeField, Tooltip("Define what entity types can be added to this selection group.")]
        private EntityType allowedEntityTypes = EntityType.unit & EntityType.building;
        
        [SerializeField, Tooltip("Enable an upper boundary on the amount of entities allowed in this selection group?")]
        private bool maxAmountEnabled = false;
        [SerializeField, Tooltip("The maximum amount of entities allowed in the selection group at the same time if the above field is enabled."), Min(1)]
        private int maxAmount = 20;

        private List<IEntity> current;

        protected ISelectionManager selectionMgr { private set; get; }
        protected IGameControlsManager controls { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public bool Init(int index, IGameManager gameMgr)
        {
            if(!key.IsValid())
            {
                gameMgr.GetService<IGameLoggingService>().LogError($"[{GetType().Name} - index: {index}] 'Key' field must be assigned!");
                return false;
            }

            this.selectionMgr = gameMgr.GetService<ISelectionManager>();
            this.controls = gameMgr.GetService<IGameControlsManager>(); 

            current = new List<IEntity>();

            return true;
        }
        #endregion

        #region Adding/Removing
        public bool Update(SelectionGroupAction action)
        {
            if (!controls.Get(key))
                return false;

            switch(action)
            {
                case SelectionGroupAction.assign:

                    current.Clear();

                    Add(selectionMgr.GetEntitiesList(allowedEntityTypes, exclusiveType: false, localPlayerFaction: true));
                    break;

                case SelectionGroupAction.append:

                    Add(selectionMgr.GetEntitiesList(allowedEntityTypes, exclusiveType: false, localPlayerFaction: true));
                    break;

                case SelectionGroupAction.substract:

                    Remove(selectionMgr.GetEntitiesList(allowedEntityTypes, exclusiveType: false, localPlayerFaction: true));
                    break;

                default:

                    selectionMgr.Add(current);
                    break;
            }

            return true;
        }

        public bool Add(IEnumerable<IEntity> entities)
        {
            foreach (IEntity entity in entities)
            {
                if (maxAmountEnabled && current.Count == maxAmount)
                    return false;

                current.Add(entity);
                entity.Health.EntityDead += HandleEntityDead;
                entity.FactionUpdateComplete += HandleFactionUpdateComplete;
            }

            return true;
        }

        public void Remove(IEnumerable<IEntity> entities)
        {
            foreach (IEntity entity in entities)
                Remove(entity);
        }

        public void Remove(IEntity entity)
        {
            current.Remove(entity);
            entity.Health.EntityDead += HandleEntityDead;
            entity.FactionUpdateComplete += HandleFactionUpdateComplete;
        }
        #endregion

        #region Tracking Entities: FactionUpdateComplete, EntityDead
        private void HandleFactionUpdateComplete(IEntity entity, FactionUpdateArgs args)
        {
            if (!entity.IsLocalPlayerFaction())
                Remove(entity);
        }

        private void HandleEntityDead(IEntity entity, DeadEventArgs args)
        {
            Remove(entity);
        }
        #endregion
    }

    public class SelectionGroupHandler : MonoBehaviour, IPostRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Enable assigning, appending and substracting entity selection groups?")]
        private bool isActive = true;
        public bool IsActive => isActive && assignKey.IsValid();

        [SerializeField, Tooltip("Required control type that defines the key used to assign selected entities into a selection group.")]
        private ControlType assignKey = null;
        [SerializeField, Tooltip("Optional control type that defines the key used to append the currently selected entities into a selection group.")]
        private ControlType appendKey = null;
        [SerializeField, Tooltip("Optional control type that defines the key used to subtract the currently selected entities from a selection group.")]
        private ControlType substractKey = null;

        [SerializeField, Tooltip("For each selection group slot, define an element in this array field.")]
        private SelectionGroup[] groups = new SelectionGroup[0];

        protected IGameLoggingService logger { private set; get; }
        protected IGameControlsManager controls { private set; get; } 
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 
            this.controls = gameMgr.GetService<IGameControlsManager>(); 

            if (!IsActive)
                return;

            if(!assignKey.IsValid())
            {
                logger.LogError($"[{GetType().Name}] The 'Assign Group Key' field must be assigned!");
                return;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                if (!groups[i].Init(i, gameMgr))
                    return;
            }
        }
        #endregion

        #region Handling Selection Groups
        private void Update()
        {
            if (!IsActive)
                return;

            SelectionGroupAction nextAction = SelectionGroupAction.select;
            if(controls.Get(assignKey))
                nextAction = SelectionGroupAction.assign;
            else if (appendKey.IsValid() && controls.Get(appendKey))
                nextAction = SelectionGroupAction.append;
            else if (substractKey.IsValid() && controls.Get(substractKey))
                nextAction = SelectionGroupAction.substract;

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].Update(nextAction))
                    return;
            }
        }
        #endregion
    }
}
