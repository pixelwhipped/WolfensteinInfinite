//Clean
using System.IO;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.Utilities
{
    public static class Logger
    {
        private class LogFile(string file) : ILogger
        {
            private readonly StreamWriter Writer = new(file);
            public void Log(string message)
            {
                Writer.WriteLine(message);
                Writer.Flush();
            }
            public void Dispose() => Writer.Dispose();
        }
        private static LogFile ApplicationLogger = new LogFile(Path.Combine(FileHelpers.Shared.BaseDirectory, "log.txt"));
        private static Dictionary<string, LogFile> Loggers = [];
        public static ILogger GetLogger() => ApplicationLogger;
        public static ILogger GetLogger(string mod)
        {
            if (mod == null) return ApplicationLogger;
            if (!Loggers.ContainsKey(mod))
                Loggers.Add(mod, new LogFile(FileHelpers.Shared.GetModDataFilePath($"{mod}\\log.txt")));
            return Loggers[mod];
        }
        public static ILogger GetLogger(Mod mod) => GetLogger(mod.Name);
        public static void Shutdown()
        {
            foreach (var l in Loggers.Values)
                l.Dispose();
        }
    }
}
