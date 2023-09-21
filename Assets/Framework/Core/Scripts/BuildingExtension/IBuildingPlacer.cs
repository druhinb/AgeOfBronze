using System;
using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Terrain;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacer : IMonoBehaviour
    {
        IBuilding Building { get; }

        IReadOnlyList<TerrainAreaType> PlacableTerrainAreas { get; }

        bool CanPlace { get; }
        bool CanPlaceOutsideBorder { get; }
        bool Placed { get; }

        IBorder PlacementCenter { get; }

        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementStatusUpdated;
        event CustomEventHandler<IBuilding, EventArgs> BuildingPlacementPositionUpdated;

        void OnPlacementStart();
        void OnPositionUpdate();

        bool IsBuildingInBorder();
    }
}
