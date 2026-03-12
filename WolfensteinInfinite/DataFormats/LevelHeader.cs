//Clean
using System.Text;

namespace WolfensteinInfinite.DataFormats
{
    public class LevelHeader
    {
        public readonly int[] MapOffsets;         // Offsets of the maps, relative to the beginning of the file.
        public readonly ushort[] CCLenght;        // Carmack-compressed length of the maps.
        public ushort Width;             // Width of the level.
        public ushort Height;            // Height of the level.
        public readonly byte[] NameData; // Name of the level
        public string Name => Encoding.UTF8.GetString(NameData);
        public LevelHeader()
        {
            //should be 3
            var planes = Extractor.GetMapPlanes();
            MapOffsets = new int[planes];
            CCLenght = new ushort[planes];
            NameData = new byte[16];
        }
    }
}