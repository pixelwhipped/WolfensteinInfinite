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
        public required int[][] WorldMap { get; set; }        
        public required EnemyPlacement[] Enemies { get; set; }

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

