using RTSEngine.Game;

namespace RTSEngine.Save.Game.Service
{
    public interface ISavableGameService : IGameService 
    {
        string SaveCode { get; }
        string Save();
        void Load(string data);
    }
}
