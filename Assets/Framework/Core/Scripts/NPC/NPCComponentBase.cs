using System;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Determinism;
using System.Collections.Generic;

namespace RTSEngine.NPC
{
    public abstract class NPCComponentBase : MonoBehaviour, INPCComponent
    {
        #region Event Logging
        // WIP
        [SerializeField, Tooltip("Enable logging to record logs of the events taken by this NPC component which will be logged in the 'Event Logs' field in the inspector.")]
        private bool logEvents = false;
        [SerializeField, ReadOnly]
        private List<string> eventLogs = new List<string>();
        public const int EVENT_LOGS_MAX_SIZE = 50;

        public void LogEvent(string newEvent)
        {
            if(logEvents)
                eventLogs.Add($"[{Time.time}] {newEvent}");
            if (eventLogs.Count > EVENT_LOGS_MAX_SIZE)
                eventLogs.RemoveAt(0);
        }
        #endregion

        #region Attributes 
        protected INPCManager npcMgr { private set; get; }
        protected IFactionManager factionMgr { private set; get; }
        protected IFactionSlot factionSlot { private set; get; }
        protected IGameManager gameMgr { private set; get; }

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IInputManager inputMgr { private set; get; } 

        [SerializeField, ReadOnly, Space(), Tooltip("Active status of the NPC component.")]
        private bool isActive = false;
        public bool IsActive { 
            protected set 
            {
                isActive = value;

                if (isActive)
                    OnActivtated();
                else
                    OnDeactivated();
            }
            get => isActive;
        }

#if UNITY_EDITOR
        [SerializeField, Tooltip("Enable to allow to update logs on the inspector of the NPC component. Functional only in the editor.")]
        private bool debugEnabled = false;
        protected bool DebugEnabled => debugEnabled;
#endif

        public virtual bool IsSingleInstance => true;
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, INPCManager npcMgr)
        {
            this.gameMgr = gameMgr;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.inputMgr = gameMgr.GetService<IInputManager>(); 

            this.npcMgr = npcMgr; 
            this.factionMgr = npcMgr.FactionMgr;
            this.factionSlot = factionMgr.Slot;

            OnPreInit();

            this.npcMgr.InitComplete += HandleNPCFactionInitComplete;
        }

        /// <summary>
        /// Called when the INPCManager instance first initializes the INPCComponent instance.
        /// </summary>
        protected virtual void OnPreInit() { }

        private void HandleNPCFactionInitComplete(INPCManager npcManager, EventArgs args)
        {
            OnPostInit();

            this.npcMgr.InitComplete -= HandleNPCFactionInitComplete;
        }

        /// <summary>
        /// Called after all INPCComponent instances have been cached and initialized by the INPCManager instance.
        /// </summary>
        protected virtual void OnPostInit() { }

        private void OnDestroy()
        {
            this.npcMgr.InitComplete -= HandleNPCFactionInitComplete;

            OnDestroyed();
        }

        protected virtual void OnDestroyed() { }
        #endregion

        #region Activating/Deactivating:
        protected virtual void OnActivtated() { }

        protected virtual void OnDeactivated() { }
        #endregion

        #region Updating Component
        private void Update()
        {
#if UNITY_EDITOR
            if(DebugEnabled)
                UpdateLogStats();
#endif

            if (!IsActive)
                return;

            OnActiveUpdate();
        }

#if UNITY_EDITOR
        protected virtual void UpdateLogStats()
        {
        }
#endif

        protected virtual void OnActiveUpdate () { }
        #endregion
    }
}
