using System.IO;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.GameObjects
{
    public class SaveGame
    {
        public required Guid GameId { get; set; }
        public required Player Player { get; set; }
        public required int Level { get; set; }
        public required GameBible.Difficulties Difficulty { get; set; }
        public required string[] Mods { get; set; }
        public required DateTime SavedAt { get; set; }
        public required Map Map { get; set; }

        public static SaveGame FromGame(Game game) => new()
        {
            GameId = game.GameId,
            Player = game.Player,
            Level = game.Map.Level,
            Difficulty = game.Map.Difficulty,
            Mods = game.Mods,
            Map = game.Map,
            SavedAt = DateTime.Now
        };

        public bool ValidateMods(Wolfenstein wolfenstein, out string[] missingMods)
        {
            missingMods = [.. Mods
                .Where(m => !wolfenstein.Mods.ContainsKey(m) ||
                            !wolfenstein.Config.Mods.Any(c => c.Name == m && c.Enabled))];
            return missingMods.Length == 0;
        }

        public static SaveGame? Load()
        {
            var file = GetPath();
            if (!File.Exists(file)) return null;
            return FileHelpers.Shared.Deserialize<SaveGame>(file);
        }

        public void Save() => FileHelpers.Shared.Serialize(this, GetPath());

        public static bool Exists() => File.Exists(GetPath());

        public static void Delete()
        {
            var file = GetPath();
            if (File.Exists(file)) File.Delete(file);
        }

        private static string GetPath() => Path.Combine(FileHelpers.Shared.BaseDirectory, "savegame.json");
    }
}