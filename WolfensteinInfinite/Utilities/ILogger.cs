//Clean
namespace WolfensteinInfinite.Utilities
{
    public interface ILogger : IDisposable
    {
        void Log(string message);
    }
}
