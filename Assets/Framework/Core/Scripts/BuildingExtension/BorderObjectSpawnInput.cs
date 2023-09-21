using UnityEngine;
using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{
    public class BorderObjectSpawnInput : PoolableObjectSpawnInput
    {
        public IBorder border { private set; get; }

        public BorderObjectSpawnInput(IBorder border, Quaternion spawnRotation)
            : base(null, false, Vector3.zero, spawnRotation)
        {
            this.border = border;
        }
    }
}
