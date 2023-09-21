using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;

namespace RTSEngine.Lobby
{
    public interface ILobbyFactionSlot : IMonoBehaviour
    {
        bool IsInitialized { get; }

        FactionSlotRole Role { get; }
        FactionSlotData Data { get; }
        IFactionSlot GameFactionSlot { get; }

        event CustomEventHandler<ILobbyFactionSlot, EventArgs> RoleUpdated;

        void Init(ILobbyManager manager, bool isPlayerControlled);
        void SetInteractable(bool interactable);

        void OnFactionSlotValidated(ILobbyFactionSlot newFactionSlot);

        void KickAttempt(int factionSlotID);

        void UpdateLobbyGameDataAttempt(LobbyGameData newLobbyGameData);

        void UpdateRoleRequest(FactionSlotRole newRole);

        void OnStartLobbyRequest();
        void OnStartLobbyInterrupted();

        void OnGameBuilt(IFactionSlot gameFactionSlot);
    }
}
