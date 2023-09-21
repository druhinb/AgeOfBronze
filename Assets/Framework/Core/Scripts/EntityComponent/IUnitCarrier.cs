using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.UnitExtension;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IUnitCarrier : IEntityComponent, IAddableUnit, IEntityPostInitializable
    {
        int CurrAmount { get; }
        bool HasMaxAmount { get; }
        int MaxAmount { get; }

        IEnumerable<IUnit> StoredUnits { get; }

        event CustomEventHandler<IUnitCarrier, UnitCarrierEventArgs> UnitAdded;
        event CustomEventHandler<IUnitCarrier, UnitCarrierEventArgs> UnitRemoved;
        event CustomEventHandler<IUnitCarrier, UnitCarrierEventArgs> UnitCalled;

        ErrorMessage CanCallUnit(TargetData<IEntity> testTarget, bool playerCommand);
        ErrorMessage CallUnitsAction(bool playerCommand);

        ErrorMessage EjectAction(IUnit unit, bool destroyed, bool playerCommand);
        ErrorMessage EjectAllAction(bool destroyed, bool playerCommand);
        Vector3 GetEjectablePosition(IUnit unit);

        bool IsUnitStored(IUnit unit);

        //ErrorMessage Add(IUnit unit, bool playerCommand);
    }
}