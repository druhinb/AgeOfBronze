using RTSEngine.Game;

namespace RTSEngine.Entities
{
    public interface IEntityPrefabInitializable
    {
        void OnPrefabInit(IEntity entity, IGameManager gameMgr);
    }
}
