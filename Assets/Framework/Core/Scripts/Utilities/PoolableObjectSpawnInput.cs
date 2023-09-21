using UnityEngine;

namespace RTSEngine.Utilities
{
    public enum RotationType { free, lookAtPosition, lookAwayFromPosition }
    [System.Serializable]
    public struct RotationData
    {
        public RotationType type;
        public Quaternion value;
        public Vector3 lookPosition;
        public bool fixYRotation;

        // Get rotation from prefab
        public RotationData(IMonoBehaviour prefab)
        {
            this.type = RotationType.free;
            this.value = prefab.IsValid() ? prefab.transform.rotation : Quaternion.identity;

            this.lookPosition = default;
            fixYRotation = false;
        }

        public RotationData(Quaternion value)
        {
            this.type = RotationType.free;
            this.value = value;
            this.lookPosition = default;
            fixYRotation = false;
        }

        public RotationData(RotationType type, Vector3 lookPosition, bool fixYRotation)
        {
            this.type = type;
            this.value = Quaternion.identity;
            this.lookPosition = lookPosition;
            this.fixYRotation = fixYRotation;
        }

        public void Apply(Transform source, bool localTransform)
        {
            switch(type)
            {
                case RotationType.free:
                    if(localTransform)
                        source.localRotation = value;
                    else
                        source.rotation = value;
                    break;
                default:
                    source.rotation = RTSHelper.GetLookRotation(source, lookPosition, type == RotationType.lookAwayFromPosition, fixYRotation);
                    break;

            }    
        }
    }

    public class PoolableObjectSpawnInput
    {
        public Transform parent { get; }

        public bool useLocalTransform { get; }
        public Vector3 spawnPosition { get; }
        public RotationData spawnRotation { get; }

        public PoolableObjectSpawnInput(Transform parent,

                                      bool useLocalTransform,
                                      Vector3 spawnPosition,
                                      Quaternion spawnRotation)
        {
            this.parent = parent;

            this.useLocalTransform = useLocalTransform;
            this.spawnPosition = spawnPosition;
            this.spawnRotation = new RotationData(spawnRotation);
        }

        public PoolableObjectSpawnInput(Transform parent,

                                      bool useLocalTransform,
                                      Vector3 spawnPosition,
                                      RotationData spawnRotation)
        {
            this.parent = parent;

            this.useLocalTransform = useLocalTransform;
            this.spawnPosition = spawnPosition;
            this.spawnRotation = spawnRotation;
        }
    }
}
