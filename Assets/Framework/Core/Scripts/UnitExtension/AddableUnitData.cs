using System.Collections.Generic;
using System.Linq;

using RTSEngine.EntityComponent;

namespace RTSEngine.UnitExtension
{
    [System.Serializable]
    public struct AddableUnitData
    {
        public bool allowDifferentFaction;
        public bool isMoveAttackRequest;
        public IEntityTargetComponent sourceTargetComponent;

        // When enabled, this tells the IAddableUnit target to ignore whether the unit can move or not
        // And when all other conditions pass, the unit will be directly teleported to the addable unit target.
        public bool ignoreMvt;

        // When enabled, this tells the IAddableUnit target to force the unit to be added on the slot with the index assigned to forcedSlotID   
        public bool forceSlot;
        public int forcedSlotID;

        public bool playerCommand;

        public AddableUnitData (
            IEntityTargetComponent sourceTargetComponent,
            bool playerCommand,
            bool allowDifferentFaction = false,
            bool isMoveAttackRequest = false)
        { 
            this.allowDifferentFaction = allowDifferentFaction;
            this.isMoveAttackRequest = isMoveAttackRequest;
            this.playerCommand = playerCommand;

            this.sourceTargetComponent = sourceTargetComponent;

            this.ignoreMvt = false;
            this.forceSlot = false;
            this.forcedSlotID = -1;
        }

        public AddableUnitData (
            IEntityTargetComponent sourceTargetComponent,
            SetTargetInputData input,
            bool allowDifferentFaction = false)
        { 
            this.allowDifferentFaction = allowDifferentFaction;
            this.isMoveAttackRequest = input.isMoveAttackRequest;
            this.playerCommand = input.playerCommand;

            this.sourceTargetComponent = sourceTargetComponent;

            this.ignoreMvt = false;
            this.forceSlot = false;
            this.forcedSlotID = -1;
        }

        public AddableUnitData (
            bool playerCommand,
            bool allowDifferentFaction = false,
            bool isMoveAttackRequest = false)
        {
            this.allowDifferentFaction = allowDifferentFaction;
            this.isMoveAttackRequest = isMoveAttackRequest;
            this.playerCommand = playerCommand;

            this.sourceTargetComponent = null;

            this.ignoreMvt = false;
            this.forceSlot = false;
            this.forcedSlotID = -1;
        }
    }
}
