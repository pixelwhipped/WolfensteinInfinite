using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{

    public class Decal
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int TextureIndex { get; set; }
        public bool LightSource { get; set; } = false;
        public bool Passable { get; set; } = true;
        public Direction Direction { get; set; } = Direction.NONE;
    }
}