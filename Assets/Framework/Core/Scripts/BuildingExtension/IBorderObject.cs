using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{
    public interface IBorderObject : IPoolableObject 
    {
        void OnSpawn(BorderObjectSpawnInput input);
        void Despawn();
    }
}