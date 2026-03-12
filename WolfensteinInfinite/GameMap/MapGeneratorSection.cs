using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameMap
{
    public class MapGeneratorSection(int xOffset, int yOffset, Mod mod, MapSection map, IEnumerable<(int X, int T)> keys) : Node<(int X, int Y), MapGeneratorSection>(keys)
    {
        public static (MapSection map, IEnumerable<(int X, int Y)> keys) ToMap(int xOffset, int yOffset, MapSection mapSection) => (mapSection, mapSection.GetConnections(xOffset, yOffset));

        public static (MapGeneratorSection map, IEnumerable<(int X, int Y)> keys) ToMap(int xOffset, int yOffset, MapGeneratorSection mapSection) => (mapSection, mapSection.Section.GetConnections(xOffset, yOffset));

        public static IEnumerable<(int X, int Y)> TranslateKeys(int xOffset, int yOffset, IEnumerable<(int X, int Y)> keys) => keys.Select(p => (p.X + xOffset, p.Y + yOffset));
        public Mod Mod { get; init; } = mod;
        public MapSection Section { get; init; } = map;
        public int Width { get; init; } = map.Width;
        public int Height { get; init; } = map.Height;
        public int X { get; init; } = xOffset;
        public int Y { get; init; } = yOffset;
        public Guid Guid { get; init; } = Guid.NewGuid();

        public override int GetHashCode() => HashCode.Combine(X, Y, Guid);

        public override bool Equals(object? obj)
        {
            if (obj is not MapGeneratorSection other) return false;
            return Guid == other.Guid;
        }

        // Add method to get a content-based hash for tracking failed attempts
        public override int GetContentHash()
        {
            // Hash based on position and which section template this is
            // Two MapSections with same template at same position = same hash
            var sectionHash = 0;
            foreach (var l in Enum.GetValues<MapArrayLayouts>())
            {
                var layer = Section?.GetLayout(l); 
                if (layer != null && layer.Length > 0)
                {
                    // Create a simple hash from the section pattern
                    for (int i = 0; i < layer.Length; i++)
                    {
                        for (int j = 0; j < layer[i].Length; j++)
                        {
                            sectionHash = HashCode.Combine(sectionHash, layer[i][j], i, j);
                        }
                    }
                }
            }               
            return HashCode.Combine(X, Y, sectionHash);
        }
    }
    
}
