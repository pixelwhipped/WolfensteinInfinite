using SFML.Graphics;
using System.Diagnostics;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.States;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameMap
{
    public partial class MapGenerator
    {
        public int TargetRoomCount { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int Level { get; init; }
        public InosculationTree<(int X, int ), MapGeneratorSection>? Tree { get; init; }
        public Dictionary<Guid, MapGeneratorSection> MapLayers { get; init; } = [];
        public MapGeneratorSection[] Sections { get; init; }
        public GeneratorSectionTypes SectionByTypes { get; init; }
        public MapFlags[] AttemptObjectives { get; init; }
        public SpecialPlacements HasPlaced { get; init; }
        public bool Success { get; init; }
        public Wolfenstein Wolfenstein { get; init; }
        public int[][] FlatMap { get; set; }


        public MapGenerator(Wolfenstein wolf, int width, int height, Mod rootNodeMod, MapSection rootNode, Dictionary<Mod, MapSection[]> sections, int level, int targetRooms, MapFlags[] attemptObjectives, out string[] finalPassErrors)
        {
            var errors = new List<string>();

            Wolfenstein = wolf;
            Width = width;
            Height = height;
            FlatMap = MapSection.Empty(Width, Height, MapSection.ClosedSectionNothing);
            TargetRoomCount = targetRooms;
            Level = level;
            AttemptObjectives = attemptObjectives;
            HasPlaced = new();
            var x = Width / 2;
            var y = Height / 2;
            var (map, keys) = MapGeneratorSection.ToMap(x, y, rootNode);
            var section = new MapGeneratorSection(x, y, rootNodeMod, map, keys);
            var allSection = new List<MapGeneratorSection>();
            {
                foreach (var item in sections)
                {
                    foreach (var sec in item.Value)
                    {
                        allSection.Add(new MapGeneratorSection(0, 0, item.Key, sec, sec.GetConnections()));
                        if (sec.IsRotatable)
                        {
                            for (int rot = 1; rot <= 3; rot++)
                            {
                                var rotated = sec.RotateSection(rot * 90);
                                var overrides = BuildDecalOverrides(item.Key, sec, rot);
                                var mgs = new MapGeneratorSection(0, 0, item.Key, rotated, rotated.GetConnections());
                                // inject overrides
                                foreach (var kvp in overrides) mgs.DecalDirectionOverrides[kvp.Key] = kvp.Value;
                                allSection.Add(mgs);
                            }
                        }
                    }
                }
            }
            Sections = [.. allSection.OrderBy(x => Random.Shared.Next())];
            var sectionsByType = GeneratorSectionTypes.GetSectionTypes(this, level, out string[] sectionTypeErrors);
            errors.AddRange(sectionTypeErrors);
            SectionByTypes = sectionsByType ?? new GeneratorSectionTypes([], [], [], [], [], [], [], [], [], [], []);
            if (sectionsByType == null)
            {
                Success = false;
            }
            else
            {
                PutMap(x, y, section);
                if (section.Connections.Length == 0 && section.Section.HasPlayerStart && section.Section.HasPlayerExit)
                {
                    Success = true;
                }
                else
                {
                    Tree = new InosculationTree<(int X, int Y), MapGeneratorSection>(section, CanConnect, OnConnect, OnDisconnect);
                    Success = Tree.TryPopulateRecursive(GetNodes);
                }
            }
            if (!Success) errors.Add("Unable to populate map");
            finalPassErrors = [.. errors];

        }

        private static Dictionary<int, Direction> BuildDecalOverrides(Mod mod, MapSection src, int rotations)
        {
            var overrides = new Dictionary<int, Direction>();
            var decalLayer = src.GetLayout(MapArrayLayouts.DECALS);
            for (int y = 0; y < decalLayer.Length; y++)
                for (int x = 0; x < decalLayer[0].Length; x++)
                {
                    int mapId = decalLayer[y][x];
                    if (mapId < 0) continue;
                    var decal = mod.Decals.FirstOrDefault(d => d.MapID == mapId);
                    if (decal == null || decal.Direction == Direction.NONE) continue;
                    var dir = decal.Direction;
                    for (int r = 0; r < rotations; r++)
                        dir = RotateDirection90(dir);
                    overrides[mapId] = dir;
                }
            return overrides;
        }

        private static Direction RotateDirection90(Direction d) => d switch
        {
            Direction.NORTH => Direction.EAST,
            Direction.EAST => Direction.SOUTH,
            Direction.SOUTH => Direction.WEST,
            Direction.WEST => Direction.NORTH,
            _ => d
        };

        // Tracks how many times each section template has been used this generation
        private readonly Dictionary<int, int> _sectionUsageCount = [];
        private MapGeneratorSection[] GetNodes(MapGeneratorSection origin)
        {
            var nodes = new List<MapGeneratorSection>();
            // Count open connections in origin
            var openConnectionCount = origin.Connections.Count(c => c.Node == null);

            // Categorize sections by door count
            var sectionsByDoorCount = new Dictionary<int, List<int>>();
            if (SectionByTypes.PlayerExits.Length == 0) return [];

            var placeLockedDoorAfter = TargetRoomCount / 2;

            var placeLockedDoor = SectionByTypes.KeyLocations.Length > 0 && SectionByTypes.KeyLockedDoors.Length > 0 && AttemptObjectives.Contains(MapFlags.HAS_LOCKED_DOOR);
            var placeDynamite = SectionByTypes.Dynamite.Length > 0 && SectionByTypes.DynamitePlacement.Length > 0 && AttemptObjectives.Contains(MapFlags.HAS_BOOM);
            var dyanmitePlacements = (int)(Math.Clamp(Level, 1, 100) / 100f * 3); //up to 3 sections depending on level
            var placeSecret = SectionByTypes.Secret.Length > 0 && SectionByTypes.Radio.Length > 0 && AttemptObjectives.Contains(MapFlags.HAS_SECRET_MESSAGE);
            var placePow = SectionByTypes.Pow.Length > 0 && AttemptObjectives.Contains(MapFlags.HAS_POW);
            var placeBoss = SectionByTypes.Pow.Length > 0 && AttemptObjectives.Contains(MapFlags.HAS_BOSS);
            var bossPlacements = !placeBoss ? 0 : Level < 30 ? 1 : //up to 4 boss depends on level progression
                Level < 60 ? Math.Clamp(Level, 1, 100) / 100 * Random.Shared.Next(Math.Min(SectionByTypes.Boss.Length, 2)) :
                Math.Clamp(Level, 1, 100) / 100 * Random.Shared.Next(Math.Min(SectionByTypes.Boss.Length, 4));
            var placeBossAfter = bossPlacements == 0 ? (int)Math.Max(placeLockedDoorAfter + 1, TargetRoomCount * 0.70) : TargetRoomCount * 0.45;
            bool CanPlaceLockedDoor() => SectionByTypes.KeyLockedDoors.Length > 0 && SectionByTypes.KeyLocations.Length > 0 && placeLockedDoor && HasPlaced.Key && HasPlaced.LockedDoor == false && openConnectionCount == 1 && MapLayers.Count >= placeLockedDoorAfter;
            bool CanPlaceKey() => SectionByTypes.KeyLocations.Length > 0 && SectionByTypes.KeyLockedDoors.Length > 0 && placeLockedDoor && HasPlaced.Key == false;
            bool CanPlaceDynamite() => SectionByTypes.Dynamite.Length > 0 && SectionByTypes.DynamitePlacement.Length > 0 && placeDynamite && HasPlaced.Dynamite == false;
            bool CanPlaceDynamitePlacements() => SectionByTypes.DynamitePlacement.Length > 0 && SectionByTypes.Dynamite.Length > 0 && placeDynamite && dyanmitePlacements > 0;
            bool CanPlaceSecret() => SectionByTypes.Secret.Length > 0 && SectionByTypes.Radio.Length > 0 && placeSecret && HasPlaced.Secret == false;
            bool CanPlaceRadio()
            {
                if (!(SectionByTypes.Secret.Length > 0 && SectionByTypes.Radio.Length > 0)) return false;
                if (placeSecret && HasPlaced.Radio == false) return !placeLockedDoor || HasPlaced.LockedDoor;
                return false;
            }
            bool CanPlacePow() => SectionByTypes.Pow.Length > 0 && placePow && HasPlaced.Pow == false;
            bool CanPlaceBoss() => SectionByTypes.Boss.Length > 0 && placeBoss && bossPlacements > 0 && MapLayers.Count >= placeBossAfter;
            bool CanPlaceExit()
            {
                if (placeLockedDoor && HasPlaced.LockedDoor == false) return false;
                if (placeBoss && bossPlacements > 0) return (float)MapLayers.Count / TargetRoomCount > 0.9f;
                return (float)MapLayers.Count / TargetRoomCount > 0.8f;
            }

            var canPlace = new SpecialPlacements
            {
                Key = CanPlaceKey(),
                LockedDoor = CanPlaceLockedDoor(),
                Dynamite = CanPlaceDynamite(),
                DynamitePlacements = CanPlaceDynamitePlacements(),
                Secret = CanPlaceSecret(),
                Radio = CanPlaceRadio(),
                Pow = CanPlacePow(),
                Boss = CanPlaceBoss(),
                Exit = CanPlaceExit()
            };

            var maxDoors = 1;

            MapGeneratorSection[] GetAvailableNodes(SpecialPlacements placeable)
            {
                var useNodes = new List<MapGeneratorSection>(SectionByTypes.Other);
                if (placeable.LockedDoor) useNodes.AddRange(SectionByTypes.KeyLockedDoors);
                if (placeable.Key) useNodes.AddRange(SectionByTypes.KeyLocations);
                if (placeable.Dynamite) useNodes.AddRange(SectionByTypes.Dynamite);
                if (placeable.DynamitePlacements) useNodes.AddRange(SectionByTypes.DynamitePlacement);
                if (placeable.Secret) useNodes.AddRange(SectionByTypes.Secret);
                if (placeable.Radio) useNodes.AddRange(SectionByTypes.Radio);
                if (placeable.Pow) useNodes.AddRange(SectionByTypes.Pow);
                if (placeable.Boss) useNodes.AddRange(SectionByTypes.Boss);
                if (placeable.Exit) useNodes.AddRange(SectionByTypes.PlayerExits);
                return [.. useNodes];
            }
            var sections = GetAvailableNodes(canPlace);

            for (int i = 0; i < sections.Length; i++)
            {
                var doorCount = MapGeneratorSection.ToMap(0, 0, sections[i]).keys.Count();
                if (!sectionsByDoorCount.TryGetValue(doorCount, out List<int>? value))
                {
                    value = [];
                    sectionsByDoorCount[doorCount] = value;
                    if (maxDoors < doorCount) maxDoors = doorCount;
                }

                value.Add(i);
            }

            // Prioritize sections based on context
            var prioritizedSections = new List<int>();
            var originCenterX = origin.X + origin.Width / 2;
            var originCenterY = origin.Y + origin.Height / 2;

            // Test weather y or x is closer to edge as a percent so 50% is the center and 0% is the edge
            var distanceFromEdgePercent = 1f -
                Math.Max((float)Math.Max(Width - originCenterX, originCenterX) / Width,
                (float)Math.Max(Height - originCenterY, originCenterY) / Height);

            // Calculate distance from edge as a percentage
            var roomsLeftPercent = (float)Math.Min(MapLayers.Count, TargetRoomCount) / TargetRoomCount;

            // distanceFromEdgePercent should only ever be 0% to 50% so we need to multiply by 2 to get full 100% range
            // Calculate how many doors to allow
            // When we're near the target room count (>80%), prefer cap pieces (1 door)
            // Otherwise, scale based on distance from edge
            var globalMaxDoors = SectionByTypes.All.Max(s => s.Section.GetConnections().Length);
            int useDoors;
            int minDoors = 1;
            if (roomsLeftPercent > 0.8f)
            {
                useDoors = 1; // Near target, use cap pieces only
            }
            else
            {
                // Scale doors based on distance from edge (closer to edge = fewer doors)
                useDoors = (int)Math.Max(1, Math.Ceiling(globalMaxDoors * (distanceFromEdgePercent * 2)));
            }

            foreach (var doorCount in GetDoorPriority(sectionsByDoorCount, roomsLeftPercent, useDoors, minDoors))
            {
                    prioritizedSections.AddRange(
                    sectionsByDoorCount[doorCount]);
            }
            
            // If we filtered out everything, ensure we at least have cap pieces
            if (prioritizedSections.Count == 0 && sectionsByDoorCount.TryGetValue(1, out List<int>? indicies))
            {
                prioritizedSections.AddRange(indicies);
            }

            // Generate candidates from prioritized sections
            foreach (var i in prioritizedSections)
            {
                var (map, keys) = MapGeneratorSection.ToMap(0, 0, sections[i]);

                // For each door in the template
                foreach (var (X, Y) in keys)
                {
                    // For each open connection in the origin
                    foreach (var originConnection in origin.Connections.Where(c => c.Node == null))
                    {
                        var originDoorX = originConnection.Key.X;
                        var originDoorY = originConnection.Key.Y;

                        // Calculate offset to align this template's door with origin's door
                        var xOffset = originDoorX - X;
                        var yOffset = originDoorY - Y;

                        var translatedKeys = MapGeneratorSection.TranslateKeys(xOffset, yOffset, keys);
                        var section = new MapGeneratorSection(xOffset, yOffset, map.Mod, map.Section, translatedKeys);
                        nodes.Add(section);
                    }
                }
            }
            if (useDoors == 1 && openConnectionCount == 1) nodes.RemoveAll(p => !p.Section.HasPlayerExit); //At the end
            else if (useDoors > 1 && openConnectionCount == 1)
            {
                if (nodes.Any(p => p.Section.GetConnections().Length > 1))
                    nodes.RemoveAll(p => p.Section.GetConnections().Length == 1); //Remove all the terminators
            }

            var usageSorted = nodes
                .OrderBy(n =>
                {
                    _sectionUsageCount.TryGetValue(n.Section.SectionHash, out int uses);
                    return uses;
                })
                .ToList();       
            return [.. usageSorted];
        }

        private int _lastPlacedDoorCount = 1;
        private IEnumerable<int> GetDoorPriority(Dictionary<int, List<int>> sectionsByDoorCount,float roomsLeftPercent,int useDoors,int minDoors)
        {
            var priority = GetDoorPriorityBiased(sectionsByDoorCount, roomsLeftPercent);
            return priority.Where(k => k <= useDoors && k >= minDoors);
        }
        private IEnumerable<int> GetDoorPriorityBiased(Dictionary<int, List<int>> sectionsByDoorCount, float roomsLeftPercent)
        {
            if (roomsLeftPercent > 0.8f)
                return sectionsByDoorCount.Keys.OrderBy(k => k);

            var avgRecentDoors = _recentDoorCounts.Count > 0
                ? _recentDoorCounts.Average() : 1.0;

            // Only cap branching if the last 3 rooms averaged 2.5+ doors
            var branchChance = (int)MathHelpers.Lerp(60, 20, roomsLeftPercent);
            var maxBranchDoors = roomsLeftPercent < 0.7f ? 3 : 2;

            if (avgRecentDoors >= 2.5)
            {
                var cap = Random.Shared.Next(100) < branchChance ? maxBranchDoors : 2;
                return sectionsByDoorCount.Keys.Where(k => k <= cap).OrderBy(k => k);
            }
            else
            {
                return sectionsByDoorCount.Keys.Where(k => k <= maxBranchDoors).OrderBy(k => k);
            }
        }
        private void OnDisconnect((int X, int Y) key, MapGeneratorSection section)
        {
            if (MapLayers.ContainsKey(section.Guid))
            {
                MapLayers.Remove(section.Guid);
                FlattenMap();
                if (_sectionUsageCount.TryGetValue(section.Section.SectionHash, out int current))
                    _sectionUsageCount[section.Section.SectionHash] = Math.Max(0, current - 1);
            }
        }
        private void OnConnect((int X, int Y) key, MapGeneratorSection child) => PutMap(child.X, child.Y, child);

        private bool CanConnect((int X, int Y) key, MapGeneratorSection parent, MapGeneratorSection child)
        {

            // Bounds check
            if (child.X < 0 || child.Y < 0) return false;
            if (child.X + child.Width > Width || child.Y + child.Height > Height) return false;

            // Verify child has a door at the connection point
            var childLocalX = key.X - child.X;
            var childLocalY = key.Y - child.Y;

            if (childLocalY < 0 || childLocalY >= child.Height ||
                childLocalX < 0 || childLocalX >= child.Width)
            {
                return false;
            }

            var childSection = child.GetOrComputeClosedSection();//.Section.GetClosedSection(out bool _, out bool _, out bool _);
            if (childSection == null) return false;
            //verify point is a door
            if (childSection[childLocalY][childLocalX] != MapSection.ClosedSectionDoor)
            {
                return false;
            }

            // Verify parent has a door at the connection point
            var parentLocalX = key.X - parent.X;
            var parentLocalY = key.Y - parent.Y;

            if (parentLocalY < 0 || parentLocalY >= parent.Height ||
                parentLocalX < 0 || parentLocalX >= parent.Width)
            {
                return false;
            }

            var parentSection = parent.GetOrComputeClosedSection();//.Section.GetClosedSection(out bool _, out bool _, out bool _);
            if (parentSection == null) return false;
            if (parentSection[parentLocalY][parentLocalX] != MapSection.ClosedSectionDoor)
            {
                return false;
            }

            //FlatMap
            for (int i = 0; i < child.Height; i++)
            {
                for (int j = 0; j < child.Width; j++)
                {
                    var mapX = child.X + j;
                    var mapY = child.Y + i;

                    if (mapX < 0 || mapX >= Width || mapY < 0 || mapY >= Height) continue;

                    var childValue = childSection[i][j];
                    if (childValue == MapSection.ClosedSectionExterior) childValue = MapSection.ClosedSectionNothing;
                    if (childValue == MapSection.ClosedSectionInterior) childValue = MapSection.ClosedSectionFill;
                    if (childValue == MapSection.ClosedSectionNothing) continue;

                    var existingValue = FlatMap[mapY][mapX];
                    if (existingValue == MapSection.ClosedSectionExterior) existingValue = MapSection.ClosedSectionNothing;
                    if (existingValue == MapSection.ClosedSectionInterior) existingValue = MapSection.ClosedSectionFill;
                    if (existingValue == MapSection.ClosedSectionNothing) continue;

                    //Test walls
                    if (existingValue == MapSection.ClosedSectionWall)
                    {
                        if (childValue == MapSection.ClosedSectionWallAny) continue;
                        if (child.Section.Walls[i][j] < 0) continue; // child is empty here — no conflict
                        var match = MapLayers.FirstOrDefault(p =>
                        {
                            var localY = mapY - p.Value.Y;
                            var localX = mapX - p.Value.X;
                            if (localY < 0 || localY >= p.Value.Section.Height) return false;
                            if (localX < 0 || localX >= p.Value.Section.Width) return false;
                            return child.Mod == p.Value.Mod &&
                                   child.Section.Walls[i][j] == p.Value.Section.Walls[localY][localX];
                        });
                        if (match.Value != null) continue;
                        // Log what we actually found vs what we needed
                        var existing = MapLayers.FirstOrDefault(p =>
                        {
                            var localY = mapY - p.Value.Y;
                            var localX = mapX - p.Value.X;
                            return localY >= 0 && localY < p.Value.Section.Height &&
                                   localX >= 0 && localX < p.Value.Section.Width &&
                                   p.Value.Section.Walls[localY][localX] >= 0;
                        });                       
                        return false;
                    }
                    
                    if (existingValue == MapSection.ClosedSectionWallAny)
                    {
                        if (childValue == MapSection.ClosedSectionWallAny) continue;
                        if (childValue == MapSection.ClosedSectionWall) continue;
                        if (child.Section.Walls[i][j] < 0) continue; // child is empty here — no conflict
                        // Translate world coords to local coords for the placed section
                        if (MapLayers.Any(p =>
                        {
                            var localY = mapY - p.Value.Y;
                            var localX = mapX - p.Value.X;
                            if (localY < 0 || localY >= p.Value.Section.Height) return false;
                            if (localX < 0 || localX >= p.Value.Section.Width) return false;
                            return child.Mod == p.Value.Mod &&
                                   child.Section.Walls[i][j] == p.Value.Section.Walls[localY][localX];
                        })) continue;
                        return false;
                    }


                    //Test doors
                    if (existingValue == MapSection.ClosedSectionDoor && childValue == MapSection.ClosedSectionDoor) continue;
                    return false;  //must be inside filled. 


                }
            }
            return true;
        }

        private void FlattenMap()
        {
            FlatMap = MapSection.Empty(Width, Height, MapSection.ClosedSectionNothing);
            foreach (var l in MapLayers.Values)
            {
                var cs = l.GetOrComputeClosedSection();
                if (cs == null) continue;
                for (int i = 0; i < l.Section.Height; i++)
                {
                    for (int j = 0; j < l.Section.Width; j++)
                    {
                        var worldY = l.Y + i;
                        var worldX = l.X + j;
                        if (worldX < 0 || worldX >= Width || worldY < 0 || worldY >= Height) continue;
                        var layerVal = cs[i][j];
                        if (layerVal == MapSection.ClosedSectionInterior) layerVal = MapSection.ClosedSectionFill;
                        if (layerVal == MapSection.ClosedSectionExterior) layerVal = MapSection.ClosedSectionNothing;
                        if (layerVal == MapSection.ClosedSectionNothing) continue;
                        if (FlatMap[worldY][worldX] != MapSection.ClosedSectionNothing) continue;
                        FlatMap[worldY][worldX] = layerVal;
                    }
                }
            }
        }


        private readonly Queue<int> _recentDoorCounts = new();
        public void PutMap(int x, int y, MapGeneratorSection section)
        {
            _recentDoorCounts.Enqueue(section.Connections.Length);
            if (_recentDoorCounts.Count > 3) _recentDoorCounts.Dequeue();
            _lastPlacedDoorCount = section.Connections.Length;

            // Store the small section at its world position — no expansion needed
            var gm = new MapGeneratorSection(x, y, section.Mod, section.Section, section.Section.GetConnections(x, y));
            if (!MapLayers.TryAdd(section.Guid, gm)) MapLayers[section.Guid] = gm;
            FlattenMap();

            if (_sectionUsageCount.TryGetValue(section.Section.SectionHash, out int current))
                _sectionUsageCount[section.Section.SectionHash] = current + 1;
            else
                _sectionUsageCount.Add(section.Section.SectionHash, 1);
        }
        internal Map? ToGameMap(Player player, Difficulties difficulty, int Level)
        {
            var floor = new Texture32(64, 64);
            var playerX = -1;
            var playerY = -1;   
            floor.Clear(128, 128, 128);
            Texture32? texture = Args.GenerateMapImage ? new Texture32(Width * 64, Height * 64) : null ;
            texture?.Clear(0, 0, 0);
            var doorList = new List<(int x, int y, ModKeyIndex key, int textureIndex)>();
            var decalList = new List<Decal>();
            var itemList = new List<Item>();
            var Map = new Map()
            {
                Difficulty = difficulty,
                Level = Level
            };
            var enemyPlacements = new List<EnemyPlacement>();
            var requiredMods = new List<string>();
            var wallKeyIndicies = new Dictionary<ModKeyIndex, int>();
            var doorKeyIndicies = new Dictionary<ModKeyIndex, int>();
            var itemsKeyIndicies = new Dictionary<ModKeyIndex, int>();
            var decalKeyIndicies = new Dictionary<ModKeyIndex, int>();
            var enemyKeyIndicies = new Dictionary<ModKeyIndex, int>();
            var specialKeyIndicies = new Dictionary<ModKeyIndex, int>();
            var wallMap = MapSection.Empty(Width, Height);
            var doorMap = MapSection.Empty(Width, Height);
            var itemsMap = MapSection.Empty(Width, Height);
            var decalsMap = MapSection.Empty(Width, Height);
            var specialMap = MapSection.Empty(Width, Height);
            var wallSectionId = MapSection.Empty(Width, Height);
            // Set wall map floors from FlatMap (already in world coords)
            for (int y = 0; y < FlatMap.Length; y++)
            {
                for (int x = 0; x < FlatMap[0].Length; x++)
                {
                    if (FlatMap[y][x] == MapSection.ClosedSectionFill)
                    {
                        texture?.Draw(x * 64, y * 64, floor);
                        if (wallMap[y][x] < 0) wallMap[y][x] = MapSection.ClosedSectionInterior;
                    }
                }
            }
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    wallSectionId[y][x] = -1;
            // Go over all layers and build up maps and objects
            foreach (var layer in MapLayers.Values.OrderBy(l => l.Section.Id))
            {                
                if (!requiredMods.Contains(layer.Mod.Name)) requiredMods.Add(layer.Mod.Name);
                var walls = layer.Section.GetLayout(MapArrayLayouts.WALLS);
                var doors = layer.Section.GetLayout(MapArrayLayouts.DOORS);
                var items = layer.Section.GetLayout(MapArrayLayouts.ITEMS);
                var decals = layer.Section.GetLayout(MapArrayLayouts.DECALS);
                var enemy = layer.Section.GetLayout(MapArrayLayouts.ENEMY);
                var special = layer.Section.GetLayout(MapArrayLayouts.SPECIAL);
                var diff = layer.Section.GetLayout(MapArrayLayouts.DIFFICULTY);

                // Walls
                for (int y = 0; y < walls.Length; y++)
                {
                    var worldY = layer.Y + y;
                    if (worldY < 0 || worldY >= Height) continue;
                    for (int x = 0; x < walls[0].Length; x++)
                    {
                        var worldX = layer.X + x;
                        if (worldX < 0 || worldX >= Width) continue;
                        if (walls[y][x] < 0) continue;
                        // WallAny (special=8): higher section ID wins
                        bool isWallAny = special[y][x] == 8;
                        if (isWallAny && wallSectionId[worldY][worldX] >= layer.Section.Id) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, walls[y][x]);
                        if (!wallKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = wallKeyIndicies.Count;
                            wallKeyIndicies.Add(key, index);
                        }
                        wallMap[worldY][worldX] = index;
                        wallSectionId[worldY][worldX] = layer.Section.Id;
                        texture?.Draw(worldX * 64, worldY * 64, Wolfenstein.Textures[key.Mod][key.Index]);
                    }
                }

                // Doors
                for (int y = 0; y < doors.Length; y++)
                {
                    var worldY = layer.Y + y;
                    if (worldY < 0 || worldY >= Height) continue;
                    for (int x = 0; x < doors[0].Length; x++)
                    {
                        var worldX = layer.X + x;
                        if (worldX < 0 || worldX >= Width) continue;
                        if (doors[y][x] < 0) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, doors[y][x]);
                        if (!doorKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = doorKeyIndicies.Count;
                            doorKeyIndicies.Add(key, index);
                        }
                        if (!Wolfenstein.Doors.TryGetValue(key.Index, out DoorType? value)) return null;
                        
                        doorMap[worldY][worldX] = index;
                        wallMap[worldY][worldX] = InGameState.DOOR_TILE;
                        texture?.Draw(worldX * 64, worldY * 64, value.DoorTexture);
                        doorList.Add((worldX, worldY, key, index));
                    }
                }

                // Items
                for (int y = 0; y < items.Length; y++)
                {
                    var worldY = layer.Y + y;
                    if (worldY < 0 || worldY >= Height) continue;
                    for (int x = 0; x < items[0].Length; x++)
                    {
                        var worldX = layer.X + x;
                        if (worldX < 0 || worldX >= Width) continue;
                        if (items[y][x] < 0) continue;
                        if (SkipSpecialChance(special, x, y)) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, items[y][x]);
                        if (!itemsKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = itemsKeyIndicies.Count;
                            itemsKeyIndicies.Add(key, index);
                        }
                        if (!Wolfenstein.PickupItems.TryGetValue(key.Index, out Texture32? value)) return null;
                        
                        itemList.Add(new Item { X = worldX, Y = worldY, ItemType = key.Index, TextureIndex = index });
                        itemsMap[worldY][worldX] = index;
                        texture?.Draw(worldX * 64, worldY * 64, value);
                    }
                }

                // Decals
                for (int y = 0; y < decals.Length; y++)
                {
                    var worldY = layer.Y + y;
                    if (worldY < 0 || worldY >= Height) continue;
                    for (int x = 0; x < decals[0].Length; x++)
                    {
                        var worldX = layer.X + x;
                        if (worldX < 0 || worldX >= Width) continue;
                        if (decals[y][x] < 0) continue;
                        if (SkipSpecialChance(special, x, y)) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, decals[y][x]);
                        if (!decalKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = decalKeyIndicies.Count;
                            decalKeyIndicies.Add(key, index);
                        }
                        var md = Wolfenstein.Mods[key.Mod].Decals[key.Index];
                        var dir = md.Direction;
                        if (layer.DecalDirectionOverrides.TryGetValue(decals[y][x], out var overrideDir))
                            dir = overrideDir;
                        decalList.Add(new Decal
                        {
                            X = worldX,
                            Y = worldY,
                            TextureIndex = index,
                            LightSource = md.LightSource,
                            Passable = md.Passable,
                            Direction = dir
                        });
                        
                        decalsMap[worldY][worldX] = index;
                        texture?.Draw(worldX * 64, worldY * 64, Wolfenstein.Decals[key.Mod][key.Index]);
                    }
                }

                // Enemies
                for (int y = 0; y < enemy.Length; y++)
                {
                    var worldY = layer.Y + y;
                    if (worldY < 0 || worldY >= Height) continue;
                    for (int x = 0; x < enemy[0].Length; x++)
                    {
                        var worldX = layer.X + x;
                        if (worldX < 0 || worldX >= Width) continue;
                        if (enemy[y][x] < 0) continue;
                        if (SkipSpecialChance(special, x, y)) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, enemy[y][x]);
                        if (!enemyKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = enemyKeyIndicies.Count;
                            enemyKeyIndicies.Add(key, index);
                        }
                        if (diff[y][x] >= (int)difficulty)
                        {
                            var t = Wolfenstein.CharacterSprites[layer.Mod.Name][enemy[y][x]].GetTexture(0);
                            texture?.Draw(worldX * 64, worldY * 64, t);
                            enemyPlacements.Add(new EnemyPlacement
                            {
                                X = worldX,
                                Y = worldY,
                                EnemyMapId = enemy[y][x],
                                Mod = layer.Mod.Name
                            });
                        }
                    }
                }

                // Special
                for (int y = 0; y < special.Length; y++)
                {
                    var worldY = layer.Y + y;
                    if (worldY < 0 || worldY >= Height) continue;
                    for (int x = 0; x < special[0].Length; x++)
                    {
                        var worldX = layer.X + x;
                        if (worldX < 0 || worldX >= Width) continue;                        
                        if (special[y][x] < 0) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, special[y][x]);
                        if (!specialKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = specialKeyIndicies.Count;
                            specialKeyIndicies.Add(key, index);
                        }
                        if (key.Index == 0) // Player Start
                        {
                            specialMap[worldY][worldX] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            
                            texture?.Draw(worldX * 64, worldY * 64, value);
                            playerX = worldX;
                            playerY = worldY;
                        }
                        else if (key.Index == 1) // Random enemy
                        {
                            if (!Wolfenstein.Mods.TryGetValue(layer.Mod.Name, out var rmod)) continue;
                            var candidates = rmod.Enemies
                                .Where(e => (int)e.EnemyType < 5) // non-boss only
                                .ToArray();
                            if (candidates.Length == 0) continue;
                            var chosen = candidates[Random.Shared.Next(candidates.Length)];

                            var rkey = new ModKeyIndex(layer.Mod.Name, chosen.MapID);
                            if (!enemyKeyIndicies.TryGetValue(rkey, out int ridx))
                            {
                                ridx = enemyKeyIndicies.Count;
                                enemyKeyIndicies.Add(rkey, ridx);
                            }
                            if (diff[y][x] >= (int)difficulty)
                            {
                                var t = Wolfenstein.CharacterSprites[layer.Mod.Name][chosen.MapID].GetTexture(0);
                                texture?.Draw(worldX * 64, worldY * 64, t);
                                enemyPlacements.Add(new EnemyPlacement
                                {
                                    X = worldX,
                                    Y = worldY,
                                    EnemyMapId = chosen.MapID,
                                    Mod = layer.Mod.Name
                                });
                            }
                        }
                        else if (key.Index == 2) // Experimental enemy
                        {
                            if (!Wolfenstein.Mods.TryGetValue(layer.Mod.Name, out var emod)) continue;
                            if (diff[y][x] >= (int)difficulty)
                            {
                                Wolfenstein.GenerateExperiment(emod, Level,
                                    out Enemy? experiment, out CharacterSprite? experimentSprite);
                                if (experiment == null || experimentSprite == null) continue;

                                texture?.Draw(worldX * 64, worldY * 64,
                                    experimentSprite.GetTexture(0));
                                enemyPlacements.Add(new EnemyPlacement
                                {
                                    X = worldX,
                                    Y = worldY,
                                    EnemyMapId = -2,
                                    Mod = layer.Mod.Name,
                                    ExperimentalEnemy = experiment,
                                    ExperimentalSprite = experimentSprite
                                });
                            }
                        }
                        else if (key.Index == 3) // Exit
                        {
                            specialMap[worldY][worldX] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            
                            texture?.Draw(worldX * 64, worldY * 64, value);
                            Map.Exits.Add(new ExitWall() { X = worldX, Y = worldY });
                        }
                        else if (key.Index == 4) // Push North
                        {
                            specialMap[worldY][worldX] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            
                            texture?.Draw(worldX * 64, worldY * 64, value);
                            var pw = new PushWall
                            {
                                X = worldX,
                                Y = worldY,
                                Direction = Direction.NORTH,
                                TextureIndex = wallMap[worldY][worldX] >= 0 ? wallMap[worldY][worldX] : 0
                            };
                            pw.InitRenderPos();
                            Map.PushWalls.Add(pw);
                            wallMap[worldY][worldX] = -1;
                        }
                        else if (key.Index == 5) // Push East
                        {
                            specialMap[worldY][worldX] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            
                            texture?.Draw(worldX * 64, worldY * 64, value);
                            var pw = new PushWall
                            {
                                X = worldX,
                                Y = worldY,
                                Direction = Direction.EAST,
                                TextureIndex = wallMap[worldY][worldX] >= 0 ? wallMap[worldY][worldX] : 0
                            };
                            pw.InitRenderPos();
                            Map.PushWalls.Add(pw);
                            wallMap[worldY][worldX] = -1;
                        }
                        else if (key.Index == 6) // Push South
                        {
                            specialMap[worldY][worldX] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            
                            texture?.Draw(worldX * 64, worldY * 64, value);
                            var pw = new PushWall
                            {
                                X = worldX,
                                Y = worldY,
                                Direction = Direction.SOUTH,
                                TextureIndex = wallMap[worldY][worldX] >= 0 ? wallMap[worldY][worldX] : 0
                            };
                            pw.InitRenderPos();
                            Map.PushWalls.Add(pw);
                            wallMap[worldY][worldX] = -1;
                        }
                        else if (key.Index == 7) // Push West
                        {
                            specialMap[worldY][worldX] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            
                            texture?.Draw(worldX * 64, worldY * 64, value);
                            var pw = new PushWall
                            {
                                X = worldX,
                                Y = worldY,
                                Direction = Direction.WEST,
                                TextureIndex = wallMap[worldY][worldX] >= 0 ? wallMap[worldY][worldX] : 0
                            };
                            pw.InitRenderPos();
                            Map.PushWalls.Add(pw);
                            wallMap[worldY][worldX] = -1;
                        }
                    }
                }
            }
           
            if (texture != null)
            {
                var file = FileHelpers.Shared.GetDataFilePath("GeneratedMap.png");
                var image = new SFML.Graphics.Image((uint)texture.Width, (uint)texture.Height, texture.Pixels);
                image.SaveToFile(file);
            }

            if (playerX >= 0 && playerY >= 0)
            {
                player.PosX = playerX;
                player.PosY = playerY;
                var direction = DeterminFacingDirection(playerX, playerY, wallMap, doorMap);
                var (dX, dY) = GetXYDirection(direction);
                player.DirX = dX;
                player.DirY = dY;
            }
            else return null;
            

            Map.WorldMap = wallMap;
            Map.WallSourceIndicies = [.. wallKeyIndicies.Keys];
            Map.DecalSourceIndicies = [.. decalKeyIndicies.Keys];
            Map.DoorSourceIndicies = [.. doorKeyIndicies.Keys];
            Map.ItemSourceIndicies = [.. itemsKeyIndicies.Keys];
            Map.Decals = [.. decalList];
            Map.Items = [.. itemList];
            Map.Enemies = [.. enemyPlacements];
            foreach (var (x, y, key, textureIndex) in doorList)
            {
                if (!Wolfenstein.Doors.TryGetValue(key.Index, out DoorType? doorType)) continue;
                Map.Doors.Add(new Door
                {
                    X = x,
                    Y = y,
                    OpenAmount = 0.0f,
                    TextureIndex = textureIndex,
                    IsLocked = doorType.Type == DoorTypes.LOCKED,
                    IsVertical = DetermineDoorOrientation(x, y, wallMap)
                });
            }
            // Pre-populate objectives based on what was actually placed in the map
            foreach (var layer in MapLayers.Values)
            {
                var items = layer.Section.GetLayout(MapArrayLayouts.ITEMS);
                var doors = layer.Section.GetLayout(MapArrayLayouts.DOORS);

                for (int y = 0; y < items.Length; y++)
                    for (int x = 0; x < items[0].Length; x++)
                    {
                        if (items[y][x] < 0) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, items[y][x]);
                        if (!Wolfenstein.PickupItemTypes.TryGetValue(key.Index, out var pickupItem)) continue;
                        switch (pickupItem.Name)
                        {
                            case "Key":
                                Map.Objectives[MapFlags.HAS_LOCKED_DOOR] = true; break;
                            case "Secret":
                                Map.Objectives[MapFlags.HAS_SECRET_MESSAGE] = true; break;
                            case "Dynamite":
                                Map.Objectives[MapFlags.HAS_BOOM] = true; break;
                            case "POW":
                                Map.Objectives[MapFlags.HAS_POW] = true; break;
                        }
                    }

                // Boss objective — check enemy layer for boss-type enemies
                var enemy = layer.Section.GetLayout(MapArrayLayouts.ENEMY);
                var special = layer.Section.GetLayout(MapArrayLayouts.SPECIAL);
                for (int y = 0; y < special.Length; y++)
                    for (int x = 0; x < special[0].Length; x++)
                        if (special[y][x] == 2) // Experiment enemy = boss
                            Map.Objectives[MapFlags.HAS_BOSS] = true;

                for (int y = 0; y < enemy.Length; y++)
                    for (int x = 0; x < enemy[0].Length; x++)
                    {
                        if (enemy[y][x] < 0) continue;
                        var modVal = Wolfenstein.Mods[layer.Mod.Name];
                        var bossIds = modVal.Enemies
                            .Where(p => (int)p.EnemyType >= 5 && (int)p.EnemyType <= 12)
                            .Select(p => p.MapID);
                        if (bossIds.Contains(enemy[y][x]))
                            Map.Objectives[MapFlags.HAS_BOSS] = true;
                    }
            }
            return Map;
        }

        private static bool SkipSpecialChance(int[][] special, int x, int y) => special[y][x] switch //5%
        {
            9 => Random.Shared.Next(100) >= 5,// skip 95%, spawn 5%
            10 => Random.Shared.Next(100) >= 25,// skip 75%, spawn 25%
            11 => Random.Shared.Next(100) >= 50,// skip 50%, spawn 50%
            12 => Random.Shared.Next(100) >= 75,// skip 25%, spawn 75%
            _ => false,
        };

        private static bool DetermineDoorOrientation(int x, int y, int[][] wallMap)
        {
            bool hasWallLeft = x > 0 && wallMap[y][x - 1] > 0;
            bool hasWallRight = x < wallMap[0].Length - 1 && wallMap[y][x + 1] > 0;

            // If walls are on left/right, door opens vertically (slides up/down)
            return hasWallLeft || hasWallRight;
        }

        private static (float X, float Y) GetXYDirection(Direction direction) => direction switch
        {
            Direction.NORTH => ((float X, float Y))(0, -1),
            Direction.EAST => ((float X, float Y))(1, 0),
            Direction.SOUTH => ((float X, float Y))(0, 1),
            Direction.WEST => ((float X, float Y))(-1, 0),
            _ => ((float X, float Y))(0, 0),
        };
        private static Direction DeterminFacingDirection(int startX, int startY, int[][] wallMap, int[][] doorMap)
        {
            var wallNorth = 0;
            var wallEast = 0;
            var wallSouth = 0;
            var wallWest = 0;

            //Texture 1001 is elevator door, typically start
            if (startY - 1 >= 0 && wallMap[startY - 1][startX] == 1001) return Direction.SOUTH;
            if (startY + 1 < wallMap.Length && wallMap[startY + 1][startX] == 1001) return Direction.NORTH;
            if (startX - 1 >= 0 && wallMap[startY][startX - 1] == 1001) return Direction.EAST;
            if (startX + 1 < wallMap[0].Length && wallMap[startY][startX + 1] == 1001) return Direction.WEST;
            //North
            while (true)
            {
                if (startY - wallNorth < 0) break; //Gone as far as can go, should not happen                
                if (wallMap[startY - wallNorth][startX] >= 0) break;
                if (doorMap[startY - wallNorth][startX] >= 0) break;
                wallNorth++;
            }
            //South
            while (true)
            {
                if (startY + wallSouth >= wallMap.Length) break; //Gone as far as can go, should not happen                
                if (wallMap[startY + wallSouth][startX] >= 0) break;
                if (doorMap[startY + wallSouth][startX] >= 0) break;
                wallSouth++;
            }
            //East
            while (true)
            {
                if (startX - wallEast < 0) break; //Gone as far as can go, should not happen                
                if (wallMap[startY][startX - wallEast] >= 0) break;
                if (doorMap[startY][startX - wallEast] >= 0) break;
                wallEast++;
            }
            //West
            while (true)
            {
               // if (startX + wallWest >= 0) break; //Gone as far as can go, should not happen
                if (startX + wallWest >= wallMap[0].Length) break;
                if (wallMap[startY][startX + wallWest] >= 0) break;
                if (doorMap[startY][startX + wallWest] >= 0) break;
                wallWest++;
            }
            var min = (new[] { wallNorth, wallEast, wallSouth, wallWest }).Min();
            if (min == wallNorth) return Direction.SOUTH;
            if (min == wallSouth) return Direction.NORTH;
            if (min == wallEast) return Direction.WEST;
            if (min == wallWest) return Direction.EAST;

            //Just return a random direction
            Direction[] directions = [.. Enum.GetValues<Direction>().Where(p => p != Direction.NONE)];
            return directions[Random.Shared.Next(directions.Length)];
        }
    }

}
