using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Terrain;
using System.Collections.Generic;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingPlacement : IPreRunGameService
    {
        bool IsPlacingBuilding { get; }

        float BuildingPositionYOffset { get; }
        float TerrainMaxDistance { get; }
        IEnumerable<TerrainAreaType> IgnoreTerrainAreas { get; }

        bool StartPlacement(BuildingCreationTask creationTask, BuildingPlacementOptions options = default);
        bool Stop();
    }
}