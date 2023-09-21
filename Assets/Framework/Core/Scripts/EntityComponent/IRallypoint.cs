using RTSEngine.Entities;
using RTSEngine.Terrain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IRallypoint : IEntityTargetComponent
    {
        Vector3 GotoPosition { get; }
        IReadOnlyList<TerrainAreaType> ForcedTerrainAreas { get; }

        ErrorMessage SendAction (IUnit entity, bool playerCommand);

        void SetGotoTransformActive(bool active);
    }
}
