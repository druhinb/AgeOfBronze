using RTSEngine.Entities;
using RTSEngine.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Health
{
    public interface IEntityHealth : IMonoBehaviour
    {
        bool IsInitialized { get; }

        IEntity Entity { get; }
        EntityType EntityType { get; }

        int MaxHealth { get; }
        int CurrHealth { get; }
        bool HasMaxHealth { get; }
        float HealthRatio { get; }
        float HoverHealthBarY { get; }

        // WARNING: Make sure that changing these values is locally synced.
        bool CanIncrease { get; set; }
        bool CanDecrease { get; set; }

        bool IsDead { get; }
        IEntity TerminatedBy { get; }
        float DestroyObjectDelay { get; }

        ErrorMessage CanAdd(HealthUpdateArgs updateArgs);
        ErrorMessage Add(HealthUpdateArgs updateArgs);
        ErrorMessage AddLocal(HealthUpdateArgs updateArgs, bool force = false);

        ErrorMessage CanDestroy(bool upgrade, IEntity source);
        ErrorMessage Destroy(bool upgrade, IEntity source);
        ErrorMessage DestroyLocal(bool upgrade, IEntity source);
        ErrorMessage SetMax(HealthUpdateArgs updateArgs);
        ErrorMessage SetMaxLocal(HealthUpdateArgs updateArgs);

        event CustomEventHandler<IEntity, HealthUpdateArgs> EntityHealthUpdated;
        event CustomEventHandler<IEntity, DeadEventArgs> EntityDead;
    }
}
