namespace WolfensteinInfinite
{
    public class ModConfig(string name, bool enabled)
    {
        public string Name { get; set; } = name;
        public bool Enabled { get; set; } = enabled;
    }
}
