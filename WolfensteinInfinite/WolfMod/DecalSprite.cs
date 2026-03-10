namespace WolfensteinInfinite.WolfMod
{
    public class DecalSprite(int id, string name, string file, bool passable, bool light, Direction direction)
    {
        public bool LightSource { get; init; } = light;
        public bool Passable { get; init; } = passable;
        public Direction Direction { get; init; } = direction;
        public int MapID { get; init; } = id;
        public string Name { get; init; } = name;
        public string File { get; init; } = file;
    }
}
