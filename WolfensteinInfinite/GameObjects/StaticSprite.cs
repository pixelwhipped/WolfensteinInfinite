
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;

namespace WolfensteinInfinite.GameObjects
{
    public sealed class StaticSprite(ISurface texture) : ISprite
    {
        private readonly Texture32 _texture = (Texture32)texture;
        public void Update(float frameTimeSeconds) { }
        public Texture32 GetTexture(float angle) => _texture;
    }
}
