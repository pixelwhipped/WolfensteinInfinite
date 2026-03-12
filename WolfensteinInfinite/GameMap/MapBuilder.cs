using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameMap
{
    public class MapBuilder
    {
        public MapSection[] MapSections { get; set; } = [];
        public bool Validate(out string[] errors)
        {
            var allErrors = new List<string>();
            foreach (var section in MapSections)
            {
                var s = MapSection.Trim(section);
                if (s[0].Value.Length < 3 || s[0].Value[0].Length < 3)
                {
                    allErrors.Add($"section {section.Id} dimension to small width and height must be >= 3");
                    continue;
                }
                int[][]? area = MapSection.GetClosedSection(s[0].Value, s[5].Value, s[6].Value, out bool closed, out bool noDoors, out bool multiple);
                if (area == null)
                {
                    if (!closed) allErrors.Add($"section {section.Id} Area not closed or extruded");
                    if (noDoors) allErrors.Add($"section {section.Id} Area missing door");
                    if (multiple) allErrors.Add($"section {section.Id} found multiple areas or orphanded block");
                }
                else
                {
                    if (!CheckDoors(area))
                        allErrors.Add($"section {section.Id} door blocked");
                    if (!CheckObjectives(section))
                        allErrors.Add($"section {section.Id} multiple objectives");
                }

            }
            errors = [.. allErrors];
            return errors.Length == 0;
        }
        private static bool CheckDoors(int[][] area)
        {
            var h = area.Length;
            var w = area[0].Length;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (area[y][x] != 1) continue;
                    var options = new (int y, int x)[]
                    {
                            //No diagonals
                            (y+1,x),
                            (y-1,x),
                            (y,x+1),
                            (y,x-1),
                    };
                    options = [.. options.Where(p =>
                        p.y >= 0 && p.y < h &&
                        p.x >= 0 && p.x < w
                        )];
                    var connections = 0;
                    foreach (var o in options)
                    {
                        if (area[o.y][o.x] == 0 || area[o.y][o.x] == 1) connections++;
                    }
                    if (connections > 2) return false;
                }
            }
            return true;
        }
        private static bool CheckObjectives(MapSection section)
        {
            var c = 0;
            c += section.HasKeys ? 1 : 0;
            c += section.HasLockedDoor ? 1 : 0;
            c += section.HasSecret ? 1 : 0;
            c += section.HasRadio ? 1 : 0;
            c += section.HasDynamite ? 1 : 0;
            c += section.HasDynamitePlacement ? 1 : 0;
            c += section.HasPow ? 1 : 0;
            return c <= 1;
        }
    }

}
