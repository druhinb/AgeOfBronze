using RTSEngine.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Faction
{
    [Serializable]
    public class FactionTargetPicker : TargetPicker<int, List<int>>
    {
        [SerializeField, Tooltip("Allow local player faction?")]
        public bool localPlayerFaciton = true;
        [SerializeField, Tooltip("Allow NPC factions?")]
        public bool npcFaction = false;
        [SerializeField, Tooltip("Allow free faction?")]
        public bool freeFaction = false;

        public override bool IsValidTarget(int targetFactionID)
        {
            if (!localPlayerFaciton && targetFactionID.IsLocalPlayerFaction())
                return false;
            else if (!npcFaction && !targetFactionID.IsLocalPlayerFaction())
                return false;
            else if (!freeFaction && !targetFactionID.IsValidFaction())
                return false;

            return base.IsValidTarget(targetFactionID);
        }

        protected override bool IsInList(int targetFactionID)
        {
            return options.Contains(targetFactionID);
        }
    }
}
