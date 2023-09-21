using RTSEngine.BuildingExtension;
using RTSEngine.Determinism;
using RTSEngine.EntityComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Entities
{
    [Serializable]
    public abstract class InitEntityParameters
    {
        public bool enforceKey;
        public int key;

        public int factionID;
        public bool free;

        public bool setInitialHealth;
        public int initialHealth;

        public bool playerCommand;
    }

    [Serializable]
    public abstract class InitFactionEntityParameters : InitEntityParameters
    {
        public bool giveInitResources;
    }

    [Serializable]
    public class InitBuildingParameters : InitFactionEntityParameters
    {
        public IBorder buildingCenter;

        public bool isBuilt;

        public InitBuildingParametersInput ToInput()
        {
            return new InitBuildingParametersInput
            {
                enforceKey = enforceKey,
                key = key,

                factionID = factionID,
                free = free,

                setInitialHealth = setInitialHealth,
                initialHealth = initialHealth,

                giveInitResources = giveInitResources,

                buildingCenterKey = buildingCenter.IsValid() ? buildingCenter.Building.GetKey() : InputManager.INVALID_ENTITY_KEY,
                isBuilt = isBuilt,

                playerCommand = playerCommand
            };
        }
    }

    [Serializable]
    public class InitBuildingParametersInput : InitFactionEntityParameters 
    {
        public int buildingCenterKey;

        public bool isBuilt;

        public InitBuildingParameters ToParams(IInputManager inputMgr)
        {
            inputMgr.TryGetEntityInstanceWithKey(buildingCenterKey, out IEntity buildingCenter);
            return new InitBuildingParameters
            {
                enforceKey = enforceKey,
                key = key,

                factionID = factionID,
                free = free,

                setInitialHealth = setInitialHealth,
                initialHealth = initialHealth,

                giveInitResources = giveInitResources,

                buildingCenter = buildingCenter.IsValid() ? (buildingCenter as IBuilding).BorderComponent : null,
                isBuilt = isBuilt,

                playerCommand = playerCommand
            };
        }
    }

    [Serializable]
    public class InitUnitParameters : InitFactionEntityParameters
    {
        public IRallypoint rallypoint;
        public IEntityComponent creatorEntityComponent;

        public bool useGotoPosition;
        public Vector3 gotoPosition;

        public InitUnitParametersInput ToInput()
        {
            return new InitUnitParametersInput
            {
                enforceKey = enforceKey,
                key = key,

                factionID = factionID,
                free = free,

                setInitialHealth = setInitialHealth,
                initialHealth = initialHealth,

                giveInitResources = giveInitResources,

                rallypointEntityKey = rallypoint.IsValid() ? rallypoint.Entity.Key : InputManager.INVALID_ENTITY_KEY,
                creatorEntityKey = creatorEntityComponent.IsValid() ? creatorEntityComponent.Entity.Key : InputManager.INVALID_ENTITY_KEY,
                creatorEntityComponentCode = creatorEntityComponent?.Code,

                useGotoPosition = useGotoPosition,
                gotoPosition = gotoPosition,

                playerCommand = playerCommand,
            };
        }
    }

    [Serializable]
    public class InitUnitParametersInput : InitFactionEntityParameters 
    {
        public int rallypointEntityKey;

        public int creatorEntityKey;
        public string creatorEntityComponentCode;

        public bool useGotoPosition;
        public Vector3 gotoPosition;

        public InitUnitParameters ToParams(IInputManager inputMgr)
        {
            inputMgr.TryGetEntityInstanceWithKey(rallypointEntityKey, out IEntity rallypointEntity);
            inputMgr.TryGetEntityInstanceWithKey(creatorEntityKey, out IEntity creatorEntity);

            return new InitUnitParameters
            {
                enforceKey = enforceKey,
                key = key,

                factionID = factionID,
                free = free,

                setInitialHealth = setInitialHealth,
                initialHealth = initialHealth,

                giveInitResources = giveInitResources,

                rallypoint = rallypointEntity.IsValid() ? (rallypointEntity as IFactionEntity).Rallypoint : null,
                creatorEntityComponent = creatorEntity.IsValid() ? 
                    (creatorEntity.EntityComponents.ContainsKey(creatorEntityComponentCode) == true 
                        ? creatorEntity.EntityComponents[creatorEntityComponentCode] 
                        : null)
                    : null,

                useGotoPosition = useGotoPosition,
                gotoPosition = gotoPosition,

                playerCommand = playerCommand
            };
        }
    }

    [Serializable]
    public class InitResourceParameters : InitEntityParameters
    {
        public InitResourceParametersInput ToInput()
        {
            return new InitResourceParametersInput
            {
                enforceKey = enforceKey,
                key = key,

                factionID = factionID,
                free = free,

                setInitialHealth = setInitialHealth,
                initialHealth = initialHealth,

                playerCommand = playerCommand
            };
        }
    }

    [Serializable]
    public class InitResourceParametersInput : InitEntityParameters
    {
        public InitResourceParameters ToParams(IInputManager inputMgr)
        {
            return new InitResourceParameters
            {
                enforceKey = enforceKey,
                key = key,

                factionID = factionID,
                free = free,

                setInitialHealth = setInitialHealth,
                initialHealth = initialHealth,

                playerCommand = playerCommand
            };
        }
    }
}
