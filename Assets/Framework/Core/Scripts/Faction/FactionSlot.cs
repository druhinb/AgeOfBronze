﻿using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.NPC;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;
using RTSEngine.Cameras;
using UnityEngine.Serialization;

namespace RTSEngine.Faction
{
    [System.Serializable]
    public class FactionSlot : IFactionSlot
    {
        #region Attributes

        public FactionSlotState State { private set; get; } = FactionSlotState.inactive;

        // Unique ID for each faction slot.
        public int ID { private set; get; }

        // The faction manager is a component that stores the faction entities data. Each faction is required to have one.
        public IFactionManager FactionMgr { private set; get; }

        [SerializeField, Tooltip("Default faction slot parameters.")]
        private FactionSlotData data = new FactionSlotData { name = "faction_name", color = Color.blue, npcType = null, type = null, role = FactionSlotRole.host };
        public FactionSlotData Data => data;

        [SerializeField, Tooltip("This is the position where the camera will look at when the game starts if this faction slot is the local player's.")]
        private Transform initialCamLookAtPosition = null;
        public Vector3 FactionSpawnPosition { private set; get; }

        [Space(), SerializeField, FormerlySerializedAs("defaultFactionEntities"), Tooltip("Units and buildings that are spawned initially for the faction, depending on the type it is assigned.")]
        private FactionTypeFilteredFactionEntities initialFactionEntities = new FactionTypeFilteredFactionEntities();
        // Keeps track of whether the above initial faction entities have been initialized or not.
        private bool initialFactionEntitiesInitialized = false;

        public INPCManager CurrentNPCMgr { private set; get; }

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; }
        protected IMainCameraController mainCameraController { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IFactionSlot, EventArgs> FactionSlotStateUpdated;
        private void RaiseFactionSlotStateUpdated()
        {
            var handler = FactionSlotStateUpdated;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(FactionSlotData data, int ID, IGameManager gameMgr)
        {
            this.gameMgr = gameMgr;
            this.logger = this.gameMgr.GetService<IGameLoggingService>();
            this.mainCameraController = gameMgr.GetService<IMainCameraController>(); 

            if (!logger.RequireTrue(State == FactionSlotState.inactive,
                $"[FactionSlot] Slot state must be set to inactive in order to initialize it, current state is '{State}'!",
                source: gameMgr))
                return;

            this.data = data;
            this.ID = ID;
            if(data.forceID && data.forcedID != this.ID)
            {
                logger.LogError($"[FactionSlot] faction slot was supposed to initialize with forced ID {data.forcedID} but was assigned ID {this.ID} instead!");
                return;
            }

            // Check for valid input
            if (logger.RequireValid(initialCamLookAtPosition,
                $"[FactionSlot - ID: {ID}] The 'Initial Camera Look At Position' field has not been assigned and therefore the initial camera position is set to (0,0,0)",
                source: gameMgr,
                type: LoggingType.warning))
                FactionSpawnPosition = initialCamLookAtPosition.position;
            else
                FactionSpawnPosition = Vector3.zero;

            // Check for validity of assigned initial faction entities
            foreach (IFactionEntity instance in initialFactionEntities.GetAll())
                if (!logger.RequireValid(instance,
                    $"[FactionSlot - ID: {ID}] 'Initial Faction Entities' has some invalid elements assigned!"))
                    return;
            initialFactionEntitiesInitialized = false;

            UpdateState(FactionSlotState.active);
        }

        private void InitNPC()
        {
            if (!logger.RequireValid(data.npcType,
                $"[FactionSlot - ID: {ID}] No NPC type has been assigned! Faction will be initiated as dummy faction.",
                type: LoggingType.warning))
                return;

            INPCManager npcMgrPrefab = data.npcType.GetNPCManagerPrefab(data.type);

            if (!npcMgrPrefab.IsValid())
            {
                logger.LogWarning($"[FactionSlot - ID: {ID}] No NPC manager defined for the faction slot's NPC type {data.npcType.Key} and faction type '{data.type.GetKey()}'. Faction will be initiated as dummy faction.", source: gameMgr);
                return;
            }

            CurrentNPCMgr = UnityEngine.Object.Instantiate(npcMgrPrefab.gameObject).GetComponent<INPCManager>();

            CurrentNPCMgr.Init(data.npcType, gameMgr, FactionMgr);
        }

        public void InitDefaultFactionEntities ()
        {
            if (!logger.RequireTrue(!initialFactionEntitiesInitialized,
                $"[FactionSlot - ID: {ID}] The initial faction entities have been already initialized. Unable to initialize them again!"))
                return;

            var buildingInitParams = new InitBuildingParameters
            {
                factionID = ID,
                free = false,

                setInitialHealth = false,

                giveInitResources = true
            };

            var unitInitParams = new InitUnitParameters
            {
                factionID = ID,
                free = false,

                setInitialHealth = false,

                rallypoint = null,

                giveInitResources = true
            };

            IEnumerable<IFactionEntity> usedEntities = initialFactionEntities.GetFiltered(data.type, out IEnumerable<IFactionEntity> unusedEntities);

            foreach (IFactionEntity instance in usedEntities)
            {
                if(gameMgr.ClearDefaultEntities)
                {
                    UnityEngine.Object.DestroyImmediate(instance.gameObject);
                    continue;
                }

                if (instance.IsBuilding())
                {
                    // It makes sense to have the building centers come up first in the initial faction entities array.
                    // So that their border components are initiated and other buildings can recongize them as building centers.
                    buildingInitParams.buildingCenter = RTSHelper.GetClosestEntity(
                        instance.transform.position,
                        FactionMgr.BuildingCenters,
                        center => center.BorderComponent.IsInBorder(instance.transform.position))?.BorderComponent;

                    (instance as IBuilding).Init(gameMgr, buildingInitParams);
                }
                else if (instance.IsUnit())
                {
                    unitInitParams.gotoPosition = instance.transform.position;
                    (instance as IUnit).Init(gameMgr, unitInitParams);
                }
            }

            // Destroy the unusued entities (ones that do not fit the current faction's type)
            foreach (IFactionEntity instance in unusedEntities)
                UnityEngine.Object.DestroyImmediate(instance.gameObject);

            if (Data.isLocalPlayer) 
                mainCameraController.LookAt(FactionSpawnPosition, smooth: false);
        }

        // Called to destroy the faction slot when intializing the game, usually due to the slot being an excess one.
        public void InitDestroy()
        {
            foreach (IFactionEntity instance in initialFactionEntities.GetAll())
                UnityEngine.Object.DestroyImmediate(instance.gameObject);
        }
        #endregion

        #region Handling Faction Slot State/Role
        public void UpdateState (FactionSlotState newState)
        {
            if (State == newState)
                return;

            switch(State)
            {
                case FactionSlotState.inactive:
                    if (!logger.RequireTrue(newState == FactionSlotState.active,
                        $"[FactionSlot - ID: {ID}] Unable to update the faction slot state from '{this.State}' to '{newState}'!."))
                        return;

                    this.FactionMgr = new FactionManager();
                    FactionMgr.Init(gameMgr, this);

                    if (data.role == FactionSlotRole.npc)
                        InitNPC();

                    break;

                case FactionSlotState.active:

                    if (!logger.RequireTrue(newState == FactionSlotState.eliminated,
                        $"[FactionSlot - ID: {ID}] Unable to update the faction slot state from '{this.State}' to '{newState}'!."))
                    return;

                    // Disable NPC
                    if (CurrentNPCMgr.IsValid())
                        GameObject.Destroy(CurrentNPCMgr.gameObject);

                    break;

                default:

                    logger.LogError($"[FactionSlot - ID: {ID}] Unable to update the faction slot state from '{this.State}' to '{newState}'!.");

                    return;
            }

            this.State = newState;
            RaiseFactionSlotStateUpdated();
        }

        public void UpdateRole(FactionSlotRole newRole)
        {
            data.role = newRole;
        }
        #endregion
    }
}
