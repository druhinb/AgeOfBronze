﻿using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Utilities;
using RTSEngine.Faction;

namespace RTSEngine.DevTools.Faction
{
    public class FactionLimitSetter : DevToolComponentBase 
    {
        [SerializeField, Tooltip("The faction entity amount limits to assign to faction slots that pass the above filter.")]
        private List<FactionEntityAmountLimit> limits = new List<FactionEntityAmountLimit>();

        protected override void OnPostRunInit()
        {
            if(Label)
                Label.text = $"Reset Faction Limits";

            if (IsActive)
                Set();
        }

        public void Set()
            => Set(RoleFilter);

        public void Set(FactionSlotRoleFilter filter)
        {
            foreach(IFactionSlot slot in gameMgr.FactionSlots)
            {
                if (!filter.IsAllowed(slot))
                    continue;

                slot.FactionMgr.AssignLimits(limits);
            }
        }

        public override void OnUIInteraction() 
        {
            Set();
        }
    }
}
