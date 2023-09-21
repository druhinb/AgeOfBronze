using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Minimap.Icons
{
    public interface IMinimapIcon : IPoolableObject, IMonoBehaviour
    {
        void OnSpawn(MinimapIconSpawnInput input);
    }
}
