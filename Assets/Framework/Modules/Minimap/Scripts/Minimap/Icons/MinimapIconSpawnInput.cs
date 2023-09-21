
using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Utilities;

namespace RTSEngine.Minimap.Icons
{
    public class MinimapIconSpawnInput : PoolableObjectSpawnInput
    {
        public IEntity sourceEntity { private set;  get; }
        public float height { get; }

        public MinimapIconSpawnInput(IEntity sourceEntity, float height, Quaternion spawnRotation)
            : base(parent: null, useLocalTransform: false, spawnPosition: Vector3.zero, spawnRotation)
        {
            this.sourceEntity = sourceEntity;
            this.height = height;
        }
    }
}