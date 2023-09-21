using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.BuildingExtension
{
    public interface IBuildingManager : IPreRunGameService
    {
        IEnumerable<IBorder> AllBorders { get; }
        int LastBorderSortingOrder { get; }

        Color FreeBuildingColor { get; }
        IEnumerable<IBuilding> FreeBuildings { get; }

        ErrorMessage CreatePlacedBuilding(IBuilding buildingPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitBuildingParameters initParams);
        IBuilding CreatePlacedBuildingLocal(IBuilding buildingPrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitBuildingParameters initParams);
        IBuilding CreatePlacementBuilding(IBuilding buildingPrefab, Quaternion spawnRotation, InitBuildingParameters initParams);

        IBorderObject SpawnBorderObject(IBorderObject prefab, BorderObjectSpawnInput input);
        void Despawn(IBorderObject instance, bool destroyed = false);
    }
}