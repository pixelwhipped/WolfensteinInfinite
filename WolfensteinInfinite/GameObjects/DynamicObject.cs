using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    public abstract class DynamicObject(float x, float y, DynamicObjectType type, ISprite sprite)
    {
        public float X { get; set; } = x;
        public float Y { get; set; } = y;
        public float YOffset { get; set; } = 0f;
        public bool IsAlive { get; set; } = true;
        public DynamicObjectType ObjectType { get; } = type;
        public ISprite Sprite { get; } = sprite;

        public abstract void Update(float frameTime, InGameState state);
    }
}