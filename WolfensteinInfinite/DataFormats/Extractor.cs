namespace WolfensteinInfinite.DataFormats
{
    public class Extractor
    {
        public static int GetMapPlanes() => Enum.GetValues(typeof(MapPlanes)).Length + 1;
        public GameVersion[] GameVersions { get; init; }
        public Extractor(bool rebuild)
        {
            var validVersion = new List<GameVersion>();
            foreach (var version in GameVersion.KnownVersion)
            {
                if (!version.IsAvailable) continue;
                if (rebuild)
                {
                    var gd = new GameData(version);
                    if (gd.IsValid) validVersion.Add(version);
                }
                else if (GameData.GameDataExists(version)) 
                {
                    validVersion.Add(version);
                }                
            }
            GameVersions = [.. validVersion];
        }
    }
}