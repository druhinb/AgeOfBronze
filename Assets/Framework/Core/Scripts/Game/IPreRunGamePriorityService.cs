namespace RTSEngine.Game
{
    public interface IPreRunGamePriorityService : IPreRunGameService 
    {
        int Priority { get; }
    }
}
