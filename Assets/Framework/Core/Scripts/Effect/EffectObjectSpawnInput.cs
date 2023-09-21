using RTSEngine.Utilities;
using UnityEngine;

namespace RTSEngine.Effect
{
    public class EffectObjectSpawnInput : PoolableObjectSpawnInput
    {
        public bool enableLifeTime { get; private set; }
        public bool useDefaultLifeTime { get; private set; }
        public float customLifeTime { get; private set; }

        public EffectObjectSpawnInput(Transform parent,

                                      bool useLocalTransform,
                                      Vector3 spawnPosition,
                                      RotationData spawnRotation,

                                      bool enableLifeTime = true,
                                      bool useDefaultLifeTime = true,
                                      float customLifeTime = 0.0f)
            : base(parent,
                  
                  useLocalTransform,
                  spawnPosition,
                  spawnRotation)
        {

            this.enableLifeTime = enableLifeTime;
            this.useDefaultLifeTime = useDefaultLifeTime;
            this.customLifeTime = customLifeTime;
        }

        public EffectObjectSpawnInput(Transform parent,

                                      bool useLocalTransform,
                                      Vector3 spawnPosition,
                                      Quaternion spawnRotation,

                                      bool enableLifeTime = true,
                                      bool useDefaultLifeTime = true,
                                      float customLifeTime = 0.0f)
            : base(parent,
                  
                  useLocalTransform,
                  spawnPosition,
                  spawnRotation)
        {

            this.enableLifeTime = enableLifeTime;
            this.useDefaultLifeTime = useDefaultLifeTime;
            this.customLifeTime = customLifeTime;
        }
    }
}