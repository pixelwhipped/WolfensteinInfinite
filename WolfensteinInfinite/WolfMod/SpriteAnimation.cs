namespace WolfensteinInfinite.WolfMod
{
    public class SpriteAnimation(string name, string spritePath, string[] sprites, float framesPerSecond)
    {
        public string Name { get; init; } = name;
        public float FramesPerSecond { get; init; } = framesPerSecond;
        public string SpritePath { get; init; } = spritePath;
        public string[] Sprites { get; init; } = sprites;
    }
}
