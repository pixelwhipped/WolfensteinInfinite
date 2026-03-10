namespace WolfensteinInfinite.WolfMod
{
    public class Texture(int id, string name, string file)
    {
        public int MapID { get; init; } = id;
        public string Name { get; init; } = name;
        public string File { get; init; } = file;
    }
}
