using RTSEngine.EntityComponent;

namespace RTSEngine.Save.Game.EntityComponent
{
    public interface ISavableEntityComponent : IEntityComponent
    {
        string Save();
        void Load(string data);
    }
}
