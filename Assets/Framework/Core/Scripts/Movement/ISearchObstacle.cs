using RTSEngine.Event;
using RTSEngine.Terrain;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Search
{
    public interface ISearchObstacle
    {
        Vector3 Center { get; }
        float Size { get; }
        TerrainAreaMask AreasMask { get; }

        event CustomEventHandler<ISearchObstacle, EventArgs> SearchObstacleDisabled;

        bool IsReserved(Vector3 testPosition, TerrainAreaMask testAreasMask, bool playerCommand);
    }
}