using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // DecalObject
    // -------------------------------------------------------------------------
    public class DecalObject(Decal decal, ISprite sprite) : DynamicObject(GetFaceX(decal), GetFaceY(decal), DynamicObjectType.Decal, sprite)
    {
        public Decal Decal { get; } = decal;

        private static float GetFaceX(Decal d) => d.Direction switch
        {
            Direction.EAST => d.X + 1f,
            Direction.WEST => d.X,
            _ => d.X + 0.5f
        };

        private static float GetFaceY(Decal d) => d.Direction switch
        {
            Direction.NORTH => d.Y,
            Direction.SOUTH => d.Y + 1f,
            _ => d.Y + 0.5f
        };

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);
        }
    }
}