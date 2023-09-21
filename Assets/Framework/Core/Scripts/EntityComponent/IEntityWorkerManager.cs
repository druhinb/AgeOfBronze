using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public interface IEntityWorkerManager : IAddableUnit
    {
        IEntity Entity { get; }

        int Amount { get; }
        int MaxAmount { get; }
        bool HasMaxAmount { get; }

        IReadOnlyList<IUnit> Workers { get; }

        // True if the worker position is static
        bool GetOccupiedPosition(IUnit worker, out Vector3 position);
        void Remove(IUnit worker);

        event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerAdded;
        event CustomEventHandler<IEntity, EntityEventArgs<IUnit>> WorkerRemoved;
    }
}
