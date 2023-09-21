using RTSEngine.Game;

namespace RTSEngine.Entities
{
    public interface IEntityPriorityPreInitializable : IEntityInitializable
    {
        void OnEntityPreInit(IGameManager gameMgr, IEntity entity);
        byte PreInitPriority { get; }
    }
}
