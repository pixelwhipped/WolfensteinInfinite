
namespace WolfensteinInfinite.GameObjects
{
    public class StaticSprite : ISprite
    {
        private readonly Texture32 _texture;
        public StaticSprite(ISurface texture) => _texture = (Texture32)texture;
        public void Update(float frameTimeSeconds) { }
        public Texture32 GetTexture(float angle) => _texture;
    }
}
