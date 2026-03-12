using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
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
        public InosculationTree<(int X, int T), MapGeneratorSection>? Tree { get; init; }
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
                Tree = new InosculationTree<(int X, int Y), MapGeneratorSection>(section, CanConnect, OnConnect, OnDisconnect);
                Success = Tree.TryPopulateRecursive(GetNodes);
            }
            if (!Success) errors.Add("Unable to populate map");
            finalPassErrors = [.. errors];

        }
        private int LastSectionHash = 0;
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
                if (placeBoss && bossPlacements > 0) return MapLayers.Count / TargetRoomCount > 0.9;
                return MapLayers.Count / TargetRoomCount > 0.8;

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
            int useDoors;
            int minDoors = 1;
            if (roomsLeftPercent > 0.8f)
            {
                useDoors = 1; // Near target, use cap pieces only
            }
            else
            {
                // Scale doors based on distance from edge (closer to edge = fewer doors)
                useDoors = (int)Math.Max(1, Math.Ceiling(maxDoors * (distanceFromEdgePercent * 2)));
                if (roomsLeftPercent < 0.2f) minDoors = 2;
            }
            // IMPORTANT: Always include 1-door sections (cap pieces) as a fallback
            // This ensures we can always close off open doors
            foreach (var doorCount in sectionsByDoorCount.Keys.OrderBy(k => k))
            {
                if (doorCount <= useDoors && doorCount >= minDoors)
                {
                    prioritizedSections.AddRange(sectionsByDoorCount[doorCount]);
                }
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
            if (useDoors == 1 && openConnectionCount == 1) nodes.RemoveAll(p => !p.Section.HasPlayerExit);
            else if (useDoors > 1 && openConnectionCount == 1)
            {
                if (nodes.Any(p => p.Section.GetConnections().Length > 1))
                    nodes.RemoveAll(p => p.Section.GetConnections().Length == 1);
            }

            var returnNodes = new List<MapGeneratorSection>();
            foreach (var n in nodes)
            {
                if (n.Section.SectionHash == LastSectionHash)
                {
                    continue;
                }
                returnNodes.Add(n);
            }
            var lastIndex = nodes.FindIndex(p => p.Section.SectionHash == LastSectionHash);
            if (lastIndex >= 0)
                returnNodes.Add(nodes[lastIndex]);
            return [.. returnNodes];
        }

        private void OnDisconnect((int X, int Y) key, MapGeneratorSection section)
        {
            if (MapLayers.ContainsKey(section.Guid))
            {
                MapLayers.Remove(section.Guid);
                FlattenMap();
            }
        }
        private void OnConnect((int X, int Y) key, MapGeneratorSection child) => PutMap(child.X, child.Y, child);

        //This whole function shoudl be simpler.  Just check parant child can connext via door then check for any overlays.
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
                        //Need to check actual wall type and mod
                        if (MapLayers.Any(p => child.Mod == p.Value.Mod && child.Section.Walls[i][j] == p.Value.Section.Walls[mapY][mapX])) continue;
                        return false;
                    }
                    if (existingValue == MapSection.ClosedSectionWallAny)
                    {
                        if (childValue == MapSection.ClosedSectionWallAny) continue;
                        if (childValue == MapSection.ClosedSectionWall) continue;
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
                var cs = l.Section.GetClosedSection(out _, out _, out _);
                if (cs == null) return;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        var layerVal = cs[i][j];
                        if (layerVal == MapSection.ClosedSectionInterior) layerVal = MapSection.ClosedSectionFill;
                        if (layerVal == MapSection.ClosedSectionExterior) layerVal = MapSection.ClosedSectionNothing;
                        if (layerVal == MapSection.ClosedSectionNothing) continue;
                        var currentVal = FlatMap[i][j];
                        if (currentVal != MapSection.ClosedSectionNothing) continue;
                        FlatMap[i][j] = layerVal;
                    }
                }
            }
        }

        public void PutMap(int x, int y, MapGeneratorSection section)
        {
            var map = new MapSection(Width, Height);
            var height = section.Section.Height;
            foreach (var l in Enum.GetValues<MapArrayLayouts>())
            {
                for (int i = 0; i < height; i++)
                {
                    if (i + y < 0) continue;
                    if (i + y >= Height) break;
                    var array = section.Section.GetLayout(l);
                    var mapLayer = map.GetLayout(l);
                    for (int j = 0; j < array[0].Length; j++)
                    {
                        if (j + x < 0) continue;
                        if (j + x >= Width) break;
                        mapLayer[y + i][x + j] = array[i][j];
                    }
                }
            }
            var gm = new MapGeneratorSection(0, 0, section.Mod, map, map.GetConnections());
            if (!MapLayers.TryAdd(section.Guid, gm)) MapLayers[section.Guid] = gm;
            FlattenMap();
            LastSectionHash = section.Section.SectionHash;
        }

        internal Map? ToGameMap(Player player, Difficulties difficulty, int Level)
        {

            var floor = new Texture32(64, 64);
            var playerX = -1;
            var playerY = -1;
            floor.Clear(128, 128, 128);
            var texture = new Texture32(Width * 64, Height * 64); //This will be saved as an image and works correclty
            texture.Clear(0, 0, 0);
            var doorList = new List<(int x, int y, int t)>();
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
            var specialKeyIndicies = new Dictionary<ModKeyIndex, int>(); //required for exits/secrets

            var wallMap = MapSection.Empty(Width, Height);  //This will be the basis for the fullmap
            var doorMap = MapSection.Empty(Width, Height);
            var itemsMap = MapSection.Empty(Width, Height);
            var decalsMap = MapSection.Empty(Width, Height);
            //var enemyMap = MapSection.Empty(Width, Height);
            var specialMap = MapSection.Empty(Width, Height);

            //Set wall map floors, not using MapSection.ClosedSectionFill as it's a positive number and can effect wall tiles.
            for (int y = 0; y < FlatMap.Length; y++)
            {
                for (int x = 0; x < FlatMap[0].Length; x++)
                {
                    if (FlatMap[y][x] == MapSection.ClosedSectionFill)
                    {
                        texture.Draw(x * 64, y * 64, floor);
                        if (wallMap[y][x] < 0) wallMap[y][x] = MapSection.ClosedSectionInterior;
                    }
                }
            }

            //Go over all layers and build up maps and objects like doors, decals etc.
            foreach (var layer in MapLayers.Values)
            {
                if (!requiredMods.Contains(layer.Mod.Name)) requiredMods.Add(layer.Mod.Name);
                var walls = layer.Section.GetLayout(MapArrayLayouts.WALLS);
                var doors = layer.Section.GetLayout(MapArrayLayouts.DOORS);
                var items = layer.Section.GetLayout(MapArrayLayouts.ITEMS);
                var decals = layer.Section.GetLayout(MapArrayLayouts.DECALS);
                var enemy = layer.Section.GetLayout(MapArrayLayouts.ENEMY);
                var special = layer.Section.GetLayout(MapArrayLayouts.SPECIAL);
                var diff = layer.Section.GetLayout(MapArrayLayouts.DIFFICULTY);

                //done
                for (int y = 0; y < walls.Length; y++)
                {
                    for (int x = 0; x < walls[0].Length; x++)
                    {
                        if (walls[y][x] < 0) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, walls[y][x]); //We nned to keep a map to original texture index and new index
                        if (!wallKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = wallKeyIndicies.Count;
                            wallKeyIndicies.Add(key, index);
                        }
                        wallMap[y][x] = index;  
                        texture.Draw(x * 64, y * 64, Wolfenstein.Textures[key.Mod][key.Index]);
                    }
                }
                //done
                for (int y = 0; y < doors.Length; y++)
                {
                    for (int x = 0; x < doors[0].Length; x++)
                    {
                        if (doors[y][x] < 0) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, doors[y][x]);
                        if (!doorKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = doorKeyIndicies.Count;
                            doorKeyIndicies.Add(key, index);
                        }
                        if (!Wolfenstein.Doors.TryGetValue(key.Index, out DoorType? value)) return null;
                        doorMap[y][x] = index;
                        wallMap[y][x] = InGameState.DOOR_TILE; //This is negative so should be fine
                        texture.Draw(x * 64, y * 64, value.DoorTexture);
                        doorList.Add((x, y, index));
                    }
                }

                for (int y = 0; y < items.Length; y++)
                {
                    for (int x = 0; x < items[0].Length; x++)
                    {
                        if (items[y][x] < 0) continue;
                        if (SkipSpecialChance(special, x, y)) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, items[y][x]);
                        if (!itemsKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = itemsKeyIndicies.Count;
                            itemsKeyIndicies.Add(key, index);
                        }
                        if (!Wolfenstein.PickupItems.TryGetValue(key.Index, out Texture32? value)) return null;   
                        
                        itemList.Add(new Item { X = x, Y = y, ItemType = key.Index, TextureIndex = index });
                        itemsMap[y][x] = index;
                        texture.Draw(x * 64, y * 64, value);
                    }
                }
                for (int y = 0; y < decals.Length; y++)
                {
                    for (int x = 0; x < decals[0].Length; x++)
                    {
                        if (decals[y][x] < 0) continue;
                        if (SkipSpecialChance(special, x, y)) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, decals[y][x]);
                        if (!decalKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = decalKeyIndicies.Count;
                            decalKeyIndicies.Add(key, index);
                        }
                        var md = Wolfenstein.Mods[key.Mod].Decals[key.Index];
                        decalList.Add(new Decal { X = x, Y = y, TextureIndex = index, LightSource =  md.LightSource, Passable = md.Passable, Direction = md.Direction});
                        decalsMap[y][x] = index;
                        texture.Draw(x * 64, y * 64, Wolfenstein.Decals[key.Mod][key.Index]);
                    }
                }
                for (int y = 0; y < enemy.Length; y++)
                {
                    for (int x = 0; x < enemy[0].Length; x++)
                    {
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
                            //var t = Wolfenstein.CharacterSprites[layer.Mod.Name][index].GetTexture(0);
                            var t = Wolfenstein.CharacterSprites[layer.Mod.Name][enemy[y][x]].GetTexture(0);
                            texture.Draw(x * 64, y * 64, t);
                            enemyPlacements.Add(new EnemyPlacement
                            {
                                X = x,
                                Y = y,
                                EnemyMapId = enemy[y][x],
                                Mod = layer.Mod.Name
                            });

                        }
                    }
                }
                for (int y = 0; y < special.Length; y++)
                {
                    for (int x = 0; x < special[0].Length; x++)
                    {
                        if (special[y][x] < 0) continue;
                        var key = new ModKeyIndex(layer.Mod.Name, special[y][x]);
                        if (!specialKeyIndicies.TryGetValue(key, out int index))
                        {
                            index = specialKeyIndicies.Count;
                            specialKeyIndicies.Add(key, index);
                        }

                        if (key.Index == 0) //Player Start
                        {
                            specialMap[y][x] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null; ;
                            texture.Draw(x * 64, y * 64, value);
                            playerX = x;
                            playerY = y;
                        }
                        else if (key.Index == 1) //Random enemy
                        {
                            if (diff[y][x] >= (int)difficulty)
                            {
                                specialMap[y][x] = index;
                                if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null; ;
                                texture.Draw(x * 64, y * 64, value);
                            }
                        }
                        else if (key.Index == 2) //Experiment enemy
                        {
                            if (diff[y][x] >= (int)difficulty)
                            {
                                specialMap[y][x] = index;
                                if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null; ;
                                texture.Draw(x * 64, y * 64, value);
                            }
                        }
                        else if (key.Index == 3) //Exit
                        {
                            specialMap[y][x] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null; ;
                            texture.Draw(x * 64, y * 64, value);
                            Map.Exits.Add(new ExitWall() { X =  x, Y = y });
                        }
                        else if (key.Index == 4) // Push North
                        {
                            specialMap[y][x] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            texture.Draw(x * 64, y * 64, value);
                            Map.PushWalls.Add(new PushWall
                            {
                                X = x,
                                Y = y,
                                Direction = Direction.NORTH,
                                TextureIndex = wallMap[y][x] >= 0 ? wallMap[y][x] : 0
                            });
                        }
                        else if (key.Index == 5) // Push East
                        {
                            specialMap[y][x] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            texture.Draw(x * 64, y * 64, value);
                            Map.PushWalls.Add(new PushWall
                            {
                                X = x,
                                Y = y,
                                Direction = Direction.EAST,
                                TextureIndex = wallMap[y][x] >= 0 ? wallMap[y][x] : 0
                            });
                        }
                        else if (key.Index == 6) // Push South
                        {
                            specialMap[y][x] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            texture.Draw(x * 64, y * 64, value);
                            Map.PushWalls.Add(new PushWall
                            {
                                X = x,
                                Y = y,
                                Direction = Direction.SOUTH,
                                TextureIndex = wallMap[y][x] >= 0 ? wallMap[y][x] : 0
                            });
                        }
                        else if (key.Index == 7) // Push West
                        {
                            specialMap[y][x] = index;
                            if (!Wolfenstein.Special.TryGetValue(key.Index, out Texture32? value)) return null;
                            texture.Draw(x * 64, y * 64, value);
                            Map.PushWalls.Add(new PushWall
                            {
                                X = x,
                                Y = y,
                                Direction = Direction.WEST,
                                TextureIndex = wallMap[y][x] >= 0 ? wallMap[y][x] : 0
                            });
                        }

                    }
                }
            }
            var file = FileHelpers.Shared.GetDataFilePath("GeneratedMap.png");
            var image = new SFML.Graphics.Image((uint)texture.Width, (uint)texture.Height, texture.Pixels);
            image.SaveToFile(file);

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
            foreach (var (x, y, t) in doorList)
            {
                Map.Doors.Add(new Door
                {
                    X = x,
                    Y = y,
                    OpenAmount = 0.0f,
                    TextureIndex = t,
                    IsLocked = t == 2,
                    IsVertical = DetermineDoorOrientation(x, y, wallMap)
                });
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
