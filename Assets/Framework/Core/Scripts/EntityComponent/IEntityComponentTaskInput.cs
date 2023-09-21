﻿using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.UI;

namespace RTSEngine.EntityComponent
{
    public interface IEntityComponentTaskInput
    {
        IEntity Entity { get; }
        IEntityComponent SourceComponent { get; }
        int ID { get; }

        GameObject PrefabObject { get; }

        bool IsInitialized { get; }
        bool IsEnabled { get; }

        EntityComponentTaskUIData Data { get; }
        EntityComponentLockedTaskUIData MissingRequirementData { get; }
        IEnumerable<ResourceInput> RequiredResources { get; }
        IEnumerable<FactionEntityRequirement> FactionEntityRequirements { get; }

        int LaunchTimes { get; }
        int PendingAmount { get; }

        void Init(IEntityComponent entityComponent, int taskID, IGameManager gameMgr);
        void Disable();

        ErrorMessage CanStart();
        void OnStart();

        void OnCancel();

        ErrorMessage CanComplete();
        void OnComplete();
    }
}
