﻿using RTSEngine.Event;
using System;
using System.Collections.Generic;

using RTSEngine.Game;
using RTSEngine.Service;
using RTSEngine.Multiplayer.Event;
using RTSEngine.Multiplayer.Game;
using RTSEngine.Multiplayer.Lobby;
using RTSEngine.Multiplayer.Server;
using RTSEngine.Multiplayer.Service;
using RTSEngine.Multiplayer.Utilities;

namespace RTSEngine.Multiplayer
{
    public interface IMultiplayerManager : IServicePublisher<IMultiplayerService>
    {
        MultiplayerState State { get; }
        MultiplayerRole Role { get; }

        IMultiplayerLobbyManager CurrentLobby { get; }

        IGameManager CurrentGameMgr { get; }
        IEnumerable<IMultiplayerFactionManager> MultiplayerFactionMgrs { get; }
        IMultiplayerFactionManager LocalMultiplayerFactionMgr { get; }

        bool IsServerOnly { get; }
        IMultiplayerServerManager ServerMgr { get; }
        IMultiplayerServerGameManager ServerGameMgr { get; }
        ServerAccessData CurrServerAccessData { get; }

        event CustomEventHandler<IMultiplayerManager, MultiplayerStateEventArgs> MultiplayerStateUpdated;
        event CustomEventHandler<IMultiplayerFactionManager, MultiplayerFactionEventArgs> MultiplayerFactionManagerValidated;

        ServerAccessData UpdateServerAccessData(ServerAccessData accessData);

        void LaunchHost();
        void LaunchClient();
        void LaunchServer();

        void OnLobbyLoaded(IMultiplayerLobbyManager currentLobby);
        ErrorMessage CanStartLobby();
        ErrorMessage StartLobby();

        void OnGameLoaded(IGameManager gameMgr);
        void OnMultiplayerFactionManagerValidated(IMultiplayerFactionManager multiplayerFactionMgr, float initialRTT);

        void Stop(DisconnectionReason reason);
        bool InterruptStartLobby();
    }
}
