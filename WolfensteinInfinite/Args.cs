//Clean
namespace WolfensteinInfinite
{
    public static class Args
    {
        private static readonly string[] _args = Environment.GetCommandLineArgs();

        public static bool EditorEnabled =>
            _args.Any(a => a.Equals("-e", StringComparison.OrdinalIgnoreCase));

        public static bool TestMode =>
            _args.Any(a => a.Equals("-t", StringComparison.OrdinalIgnoreCase) ||
                           a.Equals("-test", StringComparison.OrdinalIgnoreCase));

        public static bool Rebuild =>
            _args.Any(a => a.Equals("-r", StringComparison.OrdinalIgnoreCase));

        public static bool RebuildWithMapImage =>
           _args.Any(a => a.Equals("-ri", StringComparison.OrdinalIgnoreCase));

        public static bool GenerateMapImage =>
           _args.Any(a => a.Equals("-g", StringComparison.OrdinalIgnoreCase));
    }
}