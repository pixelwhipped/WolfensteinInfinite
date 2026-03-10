namespace WolfensteinInfinite.WolfMod
{
    public class MapSectionNode
    {
        public Mod Mod { get; init; }
        public MapSection Section { get; init; }
        public int Width => Section.Width;
        public int Height => Section.Height;

        public MapSectionNode(Mod mod, MapSection section)
        {
            Mod = mod;
            Section = section;
        }
    }
}