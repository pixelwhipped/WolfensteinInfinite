//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.States
{
    public class GameGenerationState : GameState
    {
        public readonly Player Player;
        public readonly Difficulties Difficulty;
        public readonly int Level;
        public int Progress = 0;
        public readonly Guid GameGuid;
        public GameGenerationState(Wolfenstein wolfenstein, Player player, Guid gameGuid, Difficulties difficulty, int level) : base(wolfenstein)
        {
            Player = player;
            Difficulty = difficulty;
            Level = level;
            ReturnState = this;
            NextState = this;
            GameGuid = gameGuid;
            new Thread(new ThreadStart(() => { GenerateMap(); })).Start();
        }
        public void GenerateMap()
        {
            MapFlags[] attemptObjectives = [];
            var mods = Wolfenstein.Config.Mods.Where(p => p.Enabled);
            var modBuilders = Wolfenstein.BuilderMods
                .Where(p => mods.Any(mo => mo.Name == p.Key) && p.Value.MapSections.Length > 0)
                .ToArray();
            if (modBuilders.Length == 0) throw new Exception("No Mods with Level Sections");
            

            Wolfenstein.PlayLevelMusic(mods.Select(m => m.Name));
            Progress = 10;
            Thread.Sleep(50);

            bool hasExit = false;
            var sections = new Dictionary<Mod, MapSection[]>();
            var rootOptions = new List<(Mod m, MapSection s)>();
            foreach (var mod in modBuilders)
            {
                var cm = Wolfenstein.Mods[mod.Key];
                var cs = mod.Value.MapSections.ToArray();
                sections.Add(cm, cs);
                if (cs.Any(p => p.HasPlayerExit)) hasExit = true;
                foreach (var section in cs.Where(p => p.HasPlayerStart))
                    rootOptions.Add((cm, section));
            }
            if (rootOptions.Count == 0) throw new Exception("No Mods with Level Sections");
            if (!hasExit) throw new Exception("No Exits");

            // Detect full maps and composable sections across all mods
            var allSections = sections.SelectMany(p => p.Value).ToArray();
            var fullMaps = sections
                .SelectMany(p => p.Value
                    .Where(s => s.IsFullMap)
                    .Select(s => (Mod: p.Key, Section: s)))
                .ToArray();
            var hasComposable = allSections.Any(s => !s.IsFullMap);

            // Mode 1: only full maps — sequential by level, looped
            if (fullMaps.Length > 0 && !hasComposable)
            {
                var chosen = fullMaps[(Level - 1) % fullMaps.Length];
                NextState = new SpecialLevelState(
                    Wolfenstein, Player, Difficulty, Level,
                    chosen.Mod.Name, chosen.Section);
                return;
            }

            // Mode 2: mix — 50% chance to use a full map if one is available
            if (fullMaps.Length > 0 && hasComposable && Random.Shared.Next(2) == 0)
            {
                var chosen = fullMaps[Random.Shared.Next(fullMaps.Length)];
                NextState = new SpecialLevelState(
                    Wolfenstein, Player, Difficulty, Level,
                    chosen.Mod.Name, chosen.Section);
                return;
            }

            // Mode 3: standard generation — filter full maps out of the composable pool
            foreach (var key in sections.Keys.ToArray())
                sections[key] = [.. sections[key].Where(s => !s.IsFullMap)];

            // Rebuild root options from composable sections only
            rootOptions.Clear();
            foreach (var kvp in sections)
                foreach (var section in kvp.Value.Where(p => p.HasPlayerStart))
                    rootOptions.Add((kvp.Key, section));
            if (rootOptions.Count == 0)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, Difficulty, Level);
                return;
            }

            var (m, s) = rootOptions[Random.Shared.Next(0, rootOptions.Count)];

            double avgRoomDim = Math.Ceiling(sections.Average(p =>
            {
                static int selector(MapSection k) => k.Width * k.Height;
                return p.Value.Average(selector);
            }));


            var maxRooms = Math.Ceiling(
                (Wolfenstein.Config.MaxMapSize * Wolfenstein.Config.MaxMapSize) / avgRoomDim);
            var targetRooms = Math.Max(
                (int)Math.Ceiling((Math.Clamp(Level, 1, 100) / 100f) * maxRooms), 15);

            Progress = 20;
            Thread.Sleep(50);

            MapGenerator builder = new(
                Wolfenstein, Wolfenstein.Config.MaxMapSize, Wolfenstein.Config.MaxMapSize,
                m, s, sections, Level, targetRooms, attemptObjectives, out string[] finalPassErrors);

            if (!builder.Success)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, Difficulty, Level);
                return;
            }
            Progress = 60;
            Thread.Sleep(50);

            var map = builder.ToGameMap(Player, Difficulty, Level);
            if (map == null)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, Difficulty, Level);
                return;
            }
            Progress = 80;
            Thread.Sleep(50);

            map.LoadResources(Wolfenstein);
            Progress = 100;
            Thread.Sleep(50);

            var game = new Game(GameGuid, map, Player, [.. mods.Select(p => p.Name)]);
            NextState = new InGameState(Wolfenstein, game);
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            var x = (buffer.Width / 2) - (Wolfenstein.GameResources.GetPsyched.Width / 2);
            var y = (buffer.Height / 2) - (Wolfenstein.GameResources.GetPsyched.Height / 2);
            buffer.Draw(x, y, Wolfenstein.GameResources.GetPsyched);
            buffer.RectFill(x, y + Wolfenstein.GameResources.GetPsyched.Height + 2,
                (int)(Wolfenstein.GameResources.GetPsyched.Width * (Progress / 100f)), 4,
                RGBA8.DARK_PURPLE.R,
                RGBA8.DARK_PURPLE.G,
                RGBA8.DARK_PURPLE.B);

            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape)
            {
                return;
            }
        }
    }
}
