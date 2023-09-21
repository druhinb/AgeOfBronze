using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Terrain;

namespace RTSEngine.BuildingExtension
{
    public class BuildingPlacerRallypointCondition : MonoBehaviour, IEntityPreInitializable, IBuildingPlacerCondition
    {
        // Game services
        protected ITerrainManager terrainMgr { private set; get; }
        
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.terrainMgr = gameMgr.GetService<ITerrainManager>();
        }

        public void Disable()
        {
        }

        public bool CanPlaceBuilding(IBuilding building)
        {
            return !building.Rallypoint.IsValid() 
                || terrainMgr.GetTerrainAreaPosition(building.Rallypoint.GotoPosition, building.Rallypoint.ForcedTerrainAreas, out _);
        }
    }
}
