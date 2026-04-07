using Newtonsoft.Json.Linq;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameMap;

namespace WolfensteinInfinite.GameObjects
{
    public sealed class Map
    {
        public int LevelScore { get; set; } = 0;
        public Dictionary<MapFlags, bool> ObjectivesComplete { get; set; } = [];
        public required int Level { get; set; }
        public required Difficulties Difficulty { get; set; }
        public required Dictionary<MapFlags, bool> Objectives { get; set; }
        public required List<Door> Doors { get; set; }
        public required List<ExitWall> Exits { get; set; }
        public required List<PushWall> PushWalls { get; set; }
        public required Decal[] Decals { get; set; }
        public required Item[] Items { get; set; }
        public required EnemyPlacement[] Enemies { get; set; }
        public required int[][] WorldMap { get; set; }

        public required Dictionary<string, int> ItemNamesKey { get; set; }
        public required Dictionary<string, int> EnemyNamesKey { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] WallTextures { get; set; } = [];
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] DecalTextures { get; set; } = [];
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] ItemTextures { get; set; } = [];
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] DoorTextures { get; set; } = [];
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] DoorSideTextures { get; set; } = [];
        public ModKeyIndex[] WallSourceIndicies { get; set; } = [];
        public ModKeyIndex[] DecalSourceIndicies { get; set; } = [];
        public ModKeyIndex[] ItemSourceIndicies { get; set; } = [];
        public ModKeyIndex[] DoorSourceIndicies { get; set; } = [];
        public Map Trim(out int shiftX, out int shiftY) => Trim(this, out shiftX, out shiftY);
        public static Map Trim(Map map, out int shiftX, out int shiftY)
        {
            int minX = map.WorldMap[0].Length;
            int minY = map.WorldMap.Length;
            int maxX = 0;
            int maxY = 0;

            for (int y = 0; y < map.WorldMap.Length; y++)
            {
                for (int x = 0; x < map.WorldMap[0].Length; x++)
                {
                    var v = map.WorldMap[y][x];
                    if (v < 0) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            maxY++;
            maxX++;

            minX = Math.Max(0, minX - 1);
            minY = Math.Max(0, minY - 1);
            maxX = Math.Min(map.WorldMap[0].Length, maxX + 1);
            maxY = Math.Min(map.WorldMap.Length, maxY + 1);

            var h = Math.Max(maxY - minY, 0);
            var w = Math.Max(maxX - minX, 0);

            var yOff = 0;
            int[][] worldMap = new int[h][];
            shiftX = minX;
            shiftY = minY;

            for (int y = minY; y < maxY; y++)
            {
                var xOff = 0;
                worldMap[yOff] = new int[w];
                //Array.Fill(worldMap[yOff], -1);
                for (int x = minX; x < maxX; x++)
                {
                    worldMap[yOff][xOff] = map.WorldMap[y][x];
                    xOff++;
                }
                yOff++;
            }
            var doors = map.Doors.Select(p => new Door
            {
                X = p.X - minX,
                Y = p.Y - minY,
                OpenAmount = p.OpenAmount,
                IsOpening = p.IsOpening,
                IsClosing = p.IsClosing,
                OpenSpeed = p.OpenSpeed,
                CloseDelay = p.CloseDelay,
                CloseTimer = p.CloseTimer,
                TextureIndex = p.TextureIndex,
                IsVertical = p.IsVertical,
                IsLocked = p.IsLocked,
                IsFake = p.IsFake
            }).ToList();
            var exits = map.Exits.Select(p => new ExitWall()
            {
                X = p.X - minX,
                Y = p.Y - minY
            }).ToList();
            var pushWalls = map.PushWalls.Select(p => new PushWall()
            {
                X = p.X - minX,
                Y = p.Y - minY,
                Direction = p.Direction,
                TextureIndex = p.TextureIndex,
                RenderX = p.RenderX - minX,
                RenderY = p.RenderY - minY
            }).ToList();
            var decals = map.Decals.Select(p => new Decal()
            {
                X = p.X - minX,
                Y = p.Y - minY,
                TextureIndex = p.TextureIndex,
                LightSource = p.LightSource,
                Passable = p.Passable,
                Direction = p.Direction
            }).ToArray();
            var items = map.Items.Select(p => new Item()
            {
                X = p.X - minX,
                Y = p.Y - minY,
                TextureIndex = p.TextureIndex,
                ItemType = p.ItemType
            }).ToArray();
            var enimies = map.Enemies.Select(p => new EnemyPlacement()
            {
                X = p.X - minX,
                Y = p.Y - minY,
                EnemyMapId = p.EnemyMapId,
                Mod = p.Mod,
                ExperimentalEnemy = p.ExperimentalEnemy,
                ExperimentalSprite = p.ExperimentalSprite
            }).ToArray();
            return new Map()
            {
                WorldMap = worldMap,
                LevelScore = map.LevelScore,
                Level = map.Level,
                Difficulty = map.Difficulty,
                Objectives = map.Objectives,
                Doors = doors,
                Exits = exits,
                PushWalls = pushWalls,
                Decals = decals,
                Items = items,
                Enemies = enimies,
                ItemNamesKey = map.ItemNamesKey,
                EnemyNamesKey = map.EnemyNamesKey,
                WallSourceIndicies = map.WallSourceIndicies,
                DecalSourceIndicies = map.DecalSourceIndicies,
                ItemSourceIndicies = map.ItemSourceIndicies,
                DoorSourceIndicies = map.DoorSourceIndicies
            };

        }


        internal void LoadResources(Wolfenstein wolfenstein)
        {
            var walls = new List<ISurface>();
            var decals = new List<ISurface>();
            var items = new List<ISurface>();
            var doors = new List<ISurface>();
            var doorSides = new List<ISurface>();
            foreach (var v in WallSourceIndicies)
            {
                ISurface wallTex = v.Index switch
                {
                    1001 => wolfenstein.GameResources.ElevatorDoor,
                    1002 => wolfenstein.GameResources.ElevatorSide,
                    1003 => wolfenstein.GameResources.ElevatorSwitchUp,
                    _ => wolfenstein.Textures[v.Mod][v.Index]
                };
                walls.Add(wallTex);
            }
            foreach (var v in DecalSourceIndicies)
            {
                decals.Add(wolfenstein.Decals[v.Mod][v.Index]);
            }
            foreach (var v in DoorSourceIndicies)
            {
                doors.Add(wolfenstein.Doors[v.Index].DoorTexture);
                doorSides.Add(wolfenstein.Doors[v.Index].SideTexture);
            }
            foreach (var v in ItemSourceIndicies)
            {
                items.Add(wolfenstein.PickupItems[v.Index]);
            }
            WallTextures = [.. walls];
            DecalTextures = [.. decals];
            ItemTextures = [.. items];
            DoorTextures = [.. doors];
            DoorSideTextures = [.. doorSides];
        }
    }
}

