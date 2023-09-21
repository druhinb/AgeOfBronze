using RTSEngine.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Health
{
    public interface IFactionEntityHealth : IEntityHealth
    {
        bool CanBeAttacked { get; set; }

        IFactionEntity FactionEntity { get; }

        IEnumerable<DamageOverTimeHandler> DOTHandlers { get; }
        Vector3 AttackTargetPosition { get; }

        void AddDamageOverTime(DamageOverTimeData nextDOTData, int damage, IEntity source, float initialCycleDuration = 0.0f);
    }
}
