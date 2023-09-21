using RTSEngine.Entities;
using System;
using UnityEngine;

namespace RTSEngine.Faction
{
    [System.Serializable]
    public struct FreeFactionBehaviour
    {
        [Tooltip("Allow interaction with other free faction entities?")]
        public bool allowFreeFaction;
        [Tooltip("Allow interaction with the local player faction?")]
        public bool allowLocalPlayer;
        [Tooltip("Allow interaction with faction entities that are neither the local player nor free faction entities?")]
        public bool allowRest;

        [Space(), Tooltip("When the free carrier is carrying units from a faction, should it have its faction updated to that of the occupying units?")]
        public bool updateFactionOnOccupy;
        [Tooltip("When enabled and the carrier is not ocucpied by a unit from a valid faction anymore, it will return to a free faction state.")]
        public bool freeOnEjection;

        [Tooltip("Allow the carrier to be attacked while it does not belong to any faction?")]
        public bool canBeAttackedOnFreeFaction;
        [Tooltip("Allow the carrier to be attacked when its faction is updated to a valid one after being occupied by a non-faction-free unit?")]
        public bool canBeAttackedOnValidFaction;

        public bool IsEntityAllowed(IUnit unit)
        {
            if (unit.IsFree)
                return allowFreeFaction;
            else if (unit.IsLocalPlayerFaction())
                return allowLocalPlayer;

            return allowRest;
        }
    }
}
