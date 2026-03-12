//Clean
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameGraphics
{
    public interface ISprite
    {
        public void Update(float frameTimeSeconds);
        public Texture32 GetTexture(float angle);
    }
}
