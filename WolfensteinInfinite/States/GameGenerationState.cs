//Clean
using SFML.Window;
using System.ComponentModel;
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
        public readonly MapGenerator[] PreGenerated;
        private MapGenerator? Builder;
        public readonly int Size;
        public GameGenerationState(Wolfenstein wolfenstein, Player player, Guid gameGuid, Difficulties difficulty, int level, int size, MapGenerator[] preGenerated) : base(wolfenstein)
        {
            Player = player;
            Difficulty = difficulty;
            Level = level;
            Size = size;
            ReturnState = this;
            NextState = this;
            GameGuid = gameGuid;
            PreGenerated = preGenerated;
            new Thread(new ThreadStart(() => { GenerateMap(); })) { IsBackground = true }.Start();
        }
        public void GenerateMap()
        {
            MapFlags[] attemptObjectives = [];
            var mods = Wolfenstein.ActiveMods;
            var modBuilders = Wolfenstein.BuilderMods
                .Where(p => mods.Any(mo => mo == p.Key) && p.Value.MapSections.Length > 0)
                .ToArray();
            if (modBuilders.Length == 0) throw new Exception("No Mods with Level Sections");


            Wolfenstein.PlayLevelMusic(mods.Select(m => m));
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
                    Wolfenstein, Player, GameGuid, Difficulty, Level,
                    chosen.Mod.Name, chosen.Section, PreGenerated);
                return;
            }

            // Mode 2: mix — 50% chance to use a full map if one is available
            if (fullMaps.Length > 0 && hasComposable && Random.Shared.Next(2) == 0)
            {
                var chosen = fullMaps[Random.Shared.Next(fullMaps.Length)];
                NextState = new SpecialLevelState(
                    Wolfenstein, Player, GameGuid, Difficulty, Level,
                    chosen.Mod.Name, chosen.Section, PreGenerated);
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
                NextState = new GameGenerationRetryState(Wolfenstein, Player, GameGuid, Difficulty, Level);
                return;
            }

            var (m, s) = rootOptions[Random.Shared.Next(0, rootOptions.Count)];

            double avgRoomDim = Math.Ceiling(sections.Average(p =>
            {
                static int selector(MapSection k) => k.Width * k.Height;
                return p.Value.Average(selector);
            }));


            var maxRooms = Math.Ceiling(
                (Size * Size) / avgRoomDim);
            var targetRooms = Math.Max(
                (int)Math.Ceiling((Math.Clamp(Level, 1, 100) / 100f) * maxRooms), 15);

            Progress = 20;
            Thread.Sleep(50);

            if (PreGenerated != null && PreGenerated.Length > 0)
            {
                Builder = SelectBestPreGenerated(targetRooms);
            }
            else
            {
                Builder = MapGenerator.GetMapGenerator(Wolfenstein, 64, 64, m, s, sections, Level, targetRooms, attemptObjectives);
                Builder?.TryBuild();
            }
            if (Builder == null || Builder.Success)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, GameGuid, Difficulty, Level);
                return;
            }
            Progress = 60;
            Thread.Sleep(50);

            var map = Builder.ToGameMap(Player, Difficulty, Level);
            if (map == null)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, GameGuid, Difficulty, Level);
                return;
            }
            Progress = 80;
            Thread.Sleep(50);

            map.LoadResources(Wolfenstein);
            Progress = 100;
            Thread.Sleep(50);

            var game = new Game(GameGuid, map, Player, mods);
            NextState = new InGameState(Wolfenstein, game);
        }

        private MapGenerator SelectBestPreGenerated(int targetRooms)
        {
            const int BossLevelInterval = 9;
            const float SizeSmallMaxArea = 65 * 65;
            const float SizeLargeMaxArea = 97 * 97;
            const float SizeSmallWeight = 1f;
            const float SizeMediumWeight = 2f;
            const float SizeLargeWeight = 3f;
            const float RoomProximityMaxWeight = 3f;
            const float ObjectiveZeroWeight = 1f;
            const float ObjectiveOneWeight = 2f;
            const float ObjectiveTwoWeight = 2.5f;
            const float ObjectiveThreePlusWeight = 3f;
            const float BossLevelWeight = 10f;
            const float BossNonLevelWeight = 1.5f;
            const float TextureConsistencyMax = 2f;
            const float TextureConsistencyPenalty = 0.25f;
            const float DoorRoomRatioMaxWeight = 3f;
            const float DoorRoomRatioTarget = 2f; // ideal average doors per room

            bool isBossLevel = Level % BossLevelInterval == 0;

            var scored = PreGenerated
                .Where(g => g.Success)
                .Select(g =>
                {
                    float score = 0f;

                    // --- Size weight ---
                    int area = g.Width * g.Height;
                    score += area < SizeSmallMaxArea ? SizeSmallWeight :
                             area < SizeLargeMaxArea ? SizeMediumWeight : SizeLargeWeight;

                    // --- Room count proximity weight ---
                    float roomDiff = MathF.Abs(g.MapLayers.Count - targetRooms);
                    score += MathF.Max(0f, RoomProximityMaxWeight - (roomDiff / targetRooms) * RoomProximityMaxWeight);

                    // --- Objective weight ---
                    int objCount = 0;
                    if (g.HasPlaced.Key && g.HasPlaced.LockedDoor) objCount++;
                    if (g.HasPlaced.Secret && g.HasPlaced.Radio) objCount++;
                    if (g.HasPlaced.Dynamite) objCount++;
                    if (g.HasPlaced.Pow) objCount++;
                    score += objCount switch
                    {
                        0 => ObjectiveZeroWeight,
                        1 => ObjectiveOneWeight,
                        2 => ObjectiveTwoWeight,
                        _ => ObjectiveThreePlusWeight
                    };

                    // --- Boss weight ---
                    if (isBossLevel && g.HasPlaced.Boss)
                        score += BossLevelWeight;
                    else if (g.HasPlaced.Boss)
                        score += BossNonLevelWeight;

                    // --- Texture group consistency ---
                    var groupIds = g.MapLayers.Values
                        .SelectMany(layer =>
                        {
                            var walls = layer.Section.GetLayout(MapArrayLayouts.WALLS);
                            var results = new List<int>();
                            if (!Wolfenstein.Mods.TryGetValue(layer.Mod.Name, out var mod)) return results;
                            for (int y = 0; y < walls.Length; y++)
                                for (int x = 0; x < walls[0].Length; x++)
                                {
                                    if (walls[y][x] < 0) continue;
                                    var tex = mod.Textures.FirstOrDefault(t => t.MapID == walls[y][x]);
                                    if (tex != null && tex.GroupId > 0)
                                        results.Add(tex.GroupId);
                                }
                            return results;
                        })
                        .Distinct()
                        .Count();

                    score += groupIds <= 1
                        ? TextureConsistencyMax
                        : MathF.Max(0f, TextureConsistencyMax - (groupIds - 1) * TextureConsistencyPenalty);

                    // --- Door to room ratio weight ---
                    // Scores highest when average doors per room is near DoorRoomRatioTarget
                    int totalDoors = g.MapLayers.Values.Sum(layer =>
                    {
                        static int selector(int[] row) => row.Count(d => d >= 0);
                        return layer.Section.GetLayout(MapArrayLayouts.DOORS)
                                                    .Sum(selector);
                    });
                    float avgDoorsPerRoom = g.MapLayers.Count > 0
                        ? (float)totalDoors / g.MapLayers.Count : 0f;
                    float ratioDiff = MathF.Abs(avgDoorsPerRoom - DoorRoomRatioTarget);
                    score += MathF.Max(0f, DoorRoomRatioMaxWeight - ratioDiff * DoorRoomRatioMaxWeight);
                    return (Generator: g, Score: score);
                })
                .OrderByDescending(x => x.Score)
                .ToArray();

            return scored.Length > 0
                ? scored[0].Generator
                : PreGenerated[Random.Shared.Next(PreGenerated.Length)];
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
                if (Builder != null) Builder.Bail = true;
                return;
            }
        }
    }
}
