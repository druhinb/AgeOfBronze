using UnityEngine;

using System.Collections.Generic;

using RTSEngine.Terrain;
using RTSEngine.Search;

namespace RTSEngine.Movement
{
    public interface IMovementTargetPositionMarker
    {
        SearchCell CurrSearchCell { get; }
        bool Enabled { get; }
        Vector3 Position { get; }
        float Radius { get; }
        TerrainAreaMask AreasMask { get; }

        bool IsIn(Vector3 testPosition);
        void Toggle(bool enable, Vector3 position = default);
    }
}