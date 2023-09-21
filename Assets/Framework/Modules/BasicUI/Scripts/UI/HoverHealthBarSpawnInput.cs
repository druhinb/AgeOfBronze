using RTSEngine.Entities;
using RTSEngine.Utilities;
using UnityEngine;

namespace RTSEngine.UI
{
    public class HoverHealthBarSpawnInput : PoolableObjectSpawnInput
    {
        public IEntity entity { get; }

        public HoverHealthBarSpawnInput(IEntity entity,
                                        Vector3 spawnPosition)
            : base(entity.transform, true, spawnPosition, Quaternion.identity)
        {
            this.entity = entity;
        }
    }
}
