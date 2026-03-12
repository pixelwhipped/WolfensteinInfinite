using System.Text.Json.Serialization;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class Map
    {
        public int Level { get; set; }
        public Difficulties Difficulty { get; set; }
        public Dictionary<MapFlags, bool> Objectives { get; set; } = [];
        public Dictionary<MapFlags, bool> ObjectivesComplete { get; set; } = [];
        public List<Door> Doors { get; set; } = [];
        public List<ExitWall> Exits { get; set; } = [];
        public Decal[] Decals { get; set; }
        public Item[] Items { get; set; }
        public int[][] WorldMap { get; set; }
        public List<PushWall> PushWalls { get; set; } = [];
        public EnemyPlacement[] Enemies { get; set; } = [];

        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] WallTextures { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] DecalTextures { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] ItemTextures { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] DoorTextures { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public ISurface[] DoorSideTextures { get; set; }
        public ModKeyIndex[] WallSourceIndicies { get; set; }
        public ModKeyIndex[] DecalSourceIndicies { get; set; }
        public ModKeyIndex[] ItemSourceIndicies { get; set; }
        public ModKeyIndex[] DoorSourceIndicies { get; set; }
        public int LevelScore { get; set; } = 0;

        internal void LoadResources(Wolfenstein wolfenstein)
        {
            var walls = new List<ISurface>();
            var decals = new List<ISurface>();
            var items = new List<ISurface>();
            var doors = new List<ISurface>();
            var doorSides = new List<ISurface>();
            foreach (var v in WallSourceIndicies)
            {
                walls.Add(wolfenstein.Textures[v.Mod][v.Index]);
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

