using RTSEngine.Game;

namespace RTSEngine.Save.IO
{
    public interface ISaveFormatter : IPreRunGameService
    {
        string ToSaveFormat<T>(T input);
        T FromSaveFormat<T>(string input);
    }
}