using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameBible
{
    public class DoorType(int id, DoorTypes doorType, Texture32 doorTexture, Texture32 sideTexture)
    {
        public int MapID { get; init; } = id;
        public DoorTypes Type { get; init; } = doorType;
        public Texture32 DoorTexture { get; init; } = doorTexture;
        public Texture32 SideTexture { get; init; } = sideTexture;
    }
}
