namespace WolfensteinInfinite.WolfMod
{
    public class Texture(int id, string name, string file, int groupId)
    {
        public int MapID { get; init; } = id;
        public int GroupId { get; init; } = groupId;
        public string Name { get; init; } = name;
        public string File { get; init; } = file;
    }
}
