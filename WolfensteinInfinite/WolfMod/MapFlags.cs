namespace WolfensteinInfinite.WolfMod
{
    /*
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
                int[][]? area = MapSection.GetClosedSection(s[0].Value, s[5].Value, out bool closed, out bool noDoors, out bool multiple);
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
                    options = options.Where(p =>
                        p.y >= 0 && p.y < h &&
                        p.x >= 0 && p.x < w
                        ).ToArray();
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
        public static bool GenerateMap(Wolfenstein wolfenstein, string[] mods, int level, MapFlags[] attemptObjectives, out GameMap? generatedMap, out string[] errors)
        {
            var sections = new Dictionary<Mod, SectionTypes>();
            var allErrors = new List<string>();
            var useLevel = level;

            foreach (var mName in mods)
            {
                if (!wolfenstein.Mods.TryGetValue(mName, out var mod)) continue;
                if (!wolfenstein.BuilderMods.TryGetValue(mName, out var builder)) continue;
                if (!builder.Validate(out string[] validationErrors))
                {
                    allErrors.AddRange(validationErrors);
                    continue;
                }
                SectionTypes? modSections;
                string[] sectionErrors;
                var l = useLevel;
                while ((modSections = SectionTypes.GetSectionTypes(mod, builder, l, out sectionErrors)) == null)
                {
                    l++;
                    if (l > 100) break;
                }
                if (modSections == null)
                {
                    allErrors.AddRange(sectionErrors);
                    continue;
                }
                sections.Add(mod, modSections);
            }
            if (sections.Count == 0)
            {
                errors = [.. allErrors];
                generatedMap = null;
                return false;
            }

            generatedMap = GenerateMap(sections, level, attemptObjectives, out string[] finalPassErrors);
            if (generatedMap == null)
            {
                allErrors.AddRange(finalPassErrors);
                errors = [.. allErrors];
                return false;
            }
            errors = [.. allErrors];
            generatedMap = errors.Length > 0 ? null : generatedMap; //Here we need to be able to connect nodes
            return generatedMap != null;
        }

        private static MapSectionNode[] GetMapSectionNode(Dictionary<Mod, SectionTypes> sections, Func<SectionTypes, MapSection[]> get)
        {
            var ret = new List<MapSectionNode>();
            foreach (var m in sections)
            {
                foreach (var s in get(m.Value))
                {
                    ret.Add(new MapSectionNode(m.Key, s));
                }
            }
            return [.. ret];
        }
        private static T[] Shuffle<T>(T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = Random.Shared.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }
        private static GameMap? GenerateMap(Dictionary<Mod, SectionTypes> sections, int level, MapFlags[] attemptObjectives, out string[] finalPassErrors)
        {
            MapSectionNode[] playerStarts = GetMapSectionNode(sections, (s) => s.PlayerStarts);
            MapSectionNode[] playerExits = GetMapSectionNode(sections, (s) => s.PlayerExits);
            if (playerStarts.Length == 0)
            {
                finalPassErrors = ["No player starting locations"];
                return null;
            }
            if (playerExits.Length == 0)
            {
                finalPassErrors = ["No player exit locations"];
                return null;
            }
            MapSectionNode[] keyLocations = GetMapSectionNode(sections, (s) => s.KeyLocations);
            MapSectionNode[] keyLockedDoors = GetMapSectionNode(sections, (s) => s.KeyLockedDoors);
            MapSectionNode[] boss = GetMapSectionNode(sections, (s) => s.Boss);
            MapSectionNode[] pow = GetMapSectionNode(sections, (s) => s.Pow);
            MapSectionNode[] secret = GetMapSectionNode(sections, (s) => s.Secret);
            MapSectionNode[] radio = GetMapSectionNode(sections, (s) => s.Radio);
            MapSectionNode[] dynamite = GetMapSectionNode(sections, (s) => s.Dynamite);
            MapSectionNode[] dynamitePlacement = GetMapSectionNode(sections, (s) => s.DynamitePlacement);
            MapSectionNode[] other = GetMapSectionNode(sections, (s) => s.Other);
            var exposedDoors = 0; //doors that not connected
            var placeLockedDoor = keyLocations.Length > 0 && keyLockedDoors.Length > 0 && attemptObjectives.Contains(MapFlags.HAS_LOCKED_DOOR);
            //need to ensure locked door can not be bypassed do count doors that are not joined.
            var placeDynamite = dynamite.Length > 0 && dynamitePlacement.Length > 0 && attemptObjectives.Contains(MapFlags.HAS_BOOM);
            var dyanmitePlacements = ((Math.Clamp(level, 1, 100) / 100) * 3); //up to 3 sections depending on level
            var placeSecret = secret.Length > 0 && radio.Length > 0 && attemptObjectives.Contains(MapFlags.HAS_SECRET_MESSAGE);
            var placePow = pow.Length > 0 && attemptObjectives.Contains(MapFlags.HAS_POW);
            var placeBoss = pow.Length > 0 && attemptObjectives.Contains(MapFlags.HAS_BOSS);
            var bossPlacements = !placeBoss ? 0 : level < 30 ? 1 : //up to 4 boss depends on level progression
                level < 60 ? ((Math.Clamp(level, 1, 100) / 100) * Random.Shared.Next(Math.Min(boss.Length, 2))) :
                ((Math.Clamp(level, 1, 100) / 100) * Random.Shared.Next(Math.Min(boss.Length, 4)));

            //node range = 10 to 40 level section preference is defined between 1 to 100
            var minNodes = 10 + ((Math.Clamp(level, 1, 100) / 100) * 25);
            var maxNodes = 10 + ((Math.Clamp(level, 1, 100) / 100) * 35);

            var placeLockedDoorAfter = maxNodes / 2;
            var placeBossAfter = bossPlacements == 0 ? (int)Math.Max(placeLockedDoorAfter + 1, maxNodes * 0.70) : (int)maxNodes * 0.45;
            var fullMap = new MultiModMapSection(1024, 1024); //Big enough
            var startNode = playerStarts[Random.Shared.Next(0, playerStarts.Length)];

            var map = new MapPlacementNode(fullMap, MapSection.Empty(1024, 1024), 512 - (startNode.Width / 2), 512 - (startNode.Height / 2), startNode, null, (-1, -1), null, out SpecialPlacements hasPlaced);

            bool CanPlaceLockedDoor() => map != null && keyLockedDoors.Length > 0 && keyLocations.Length > 0 && placeLockedDoor && hasPlaced.Key && hasPlaced.LockedDoor == false && exposedDoors == 1 && map.CountNodes() >= placeLockedDoorAfter;
            bool CanPlaceKey() => keyLocations.Length > 0 && keyLockedDoors.Length > 0 && placeLockedDoor && hasPlaced.Key == false;
            bool CanPlaceDynamite() => dynamite.Length > 0 && dynamitePlacement.Length > 0 && placeDynamite && hasPlaced.Dynamite == false;
            bool CanPlaceDynamitePlacements() => dynamitePlacement.Length > 0 && dynamite.Length > 0 && placeDynamite && dyanmitePlacements > 0;
            bool CanPlaceSecret() => secret.Length > 0 && radio.Length > 0 && placeSecret && hasPlaced.Secret == false;
            bool CanPlaceRadio()
            {
                if (!(secret.Length > 0 && radio.Length > 0)) return false;
                if (placeSecret && hasPlaced.Radio == false) return placeLockedDoor ? hasPlaced.LockedDoor : true;
                return false;
            }
            bool CanPlacePow() => pow.Length > 0 && placePow && hasPlaced.Pow == false;
            bool CanPlaceBoss() => map != null && boss.Length > 0 && placeBoss && bossPlacements > 0 && map.CountNodes() >= placeBossAfter;
            bool CanPlaceExit()
            {
                if (map == null) return false;
                if (placeLockedDoor && hasPlaced.LockedDoor == false) return false;
                if (placeBoss && bossPlacements > 0) return map.CountNodes() / maxNodes > 0.9;
                return map.CountNodes() / maxNodes > 0.8;

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

            //max run length/open doors? MapSection nodes needs to know how many doors are no linked and prune on fail
            MapSectionNode[] GetNextNodes(SpecialPlacements placeable)
            {
                void Add(MapSectionNode[] add, int multiple, List<MapSectionNode> to)
                {
                    multiple = Math.Max(multiple, 1);
                    for (int i = 0; i < multiple; i++)
                    {
                        to.AddRange(add);
                    }
                }
                var useNodes = new List<MapSectionNode>(other);
                var nCount = Math.Max(useNodes.Count(), 1);

                if (placeable.LockedDoor) Add(keyLockedDoors, nCount / keyLockedDoors.Length, useNodes);
                if (placeable.Key) Add(keyLocations, nCount / keyLocations.Length, useNodes);
                if (placeable.Dynamite) Add(dynamite, nCount / dynamite.Length, useNodes);
                if (placeable.DynamitePlacements) Add(dynamitePlacement, nCount / dynamitePlacement.Length, useNodes);
                if (placeable.Secret) Add(secret, nCount / secret.Length, useNodes);
                if (placeable.Radio) Add(radio, nCount / radio.Length, useNodes);
                if (placeable.Pow) Add(pow, nCount / pow.Length, useNodes);
                if (placeable.Boss) Add(boss, nCount / boss.Length, useNodes);
                if (placeable.Exit) Add(playerExits, nCount / playerExits.Length, useNodes);
                return Shuffle([.. useNodes]);
            }

            //Post Checks
            //if key placed but not locked door remove key
            //if secret placed but not radio remove radio
            //if dynamite placed and no placements remove dynamite or id placemnts placed but no dynamite remove placements
            //were here putting it all togeather
            throw new NotImplementedException();
        }
    }
    */
    public enum MapFlags
    {
        HAS_POW,    //Requires Item Pow(15)
        HAS_LOCKED_DOOR,    //Requires Item Key(21) and Door(2)
        HAS_BOSS,   //Requires and Experimental or Any Enemy Type 5 to 12
        HAS_SECRET_MESSAGE, //Requires Item Secret(16) and Radio(17)
        HAS_BOOM    //Requres Item Dynamite(18) and DynamiteToPlace(19)
    }
    /*
    public class SectionTypes(MapSection[] playerStarts, MapSection[] playerExits,
        MapSection[] keyLocations, MapSection[] keyLockedDoors, MapSection[] boss, MapSection[] pow, MapSection[] secret,
        MapSection[] radio, MapSection[] dynamite, MapSection[] dynamitePlacement, MapSection[] other)
    {
        public MapSection[] PlayerStarts { get; init; } = playerStarts;
        public MapSection[] PlayerExits { get; init; } = playerExits;
        public MapSection[] KeyLocations { get; init; } = keyLocations;
        public MapSection[] KeyLockedDoors { get; init; } = keyLockedDoors;
        public MapSection[] Boss { get; init; } = boss;
        public MapSection[] Pow { get; init; } = pow;
        public MapSection[] Secret { get; init; } = secret;
        public MapSection[] Radio { get; init; } = radio;
        public MapSection[] Dynamite { get; init; } = dynamite;
        public MapSection[] DynamitePlacement { get; init; } = dynamitePlacement;
        public MapSection[] Other { get; init; } = other;
        public static SectionTypes? GetSectionTypes(Mod mod, MapBuilder builder, int level, out string[] errors)
        {
            static SectionTypes? Validate(SectionTypes s, out string[] e)
            {
                var err = new List<string>();
                if (s.PlayerStarts.Length == 0) err.Add("No Player Starts");
                if (s.PlayerExits.Length == 0) err.Add("No Player Exits");
                e = [.. err];
                return e.Length == 0 ? s : null;
            }

            var starts = builder.MapSections.Where(p => p.HasPlayerStart && p.IntendedMinLevel <= level).ToArray();
            var ends = builder.MapSections.Where(p => p.HasPlayerExit && p.IntendedMinLevel <= level).ToArray();
            var keyLocations = builder.MapSections.Where(p => p.HasKeys && p.IntendedMinLevel <= level).ToArray();
            var keyLockedDoors = builder.MapSections.Where(p => p.HasLockedDoor && p.IntendedMinLevel <= level).ToArray();
            var boss = builder.MapSections.Where(p => p.HasBoss(mod) && p.IntendedMinLevel <= level).ToArray();
            var secret = builder.MapSections.Where(p => p.HasSecret && p.IntendedMinLevel <= level).ToArray();
            var pow = builder.MapSections.Where(p => p.HasPow && p.IntendedMinLevel <= level).ToArray();
            var radio = builder.MapSections.Where(p => p.HasRadio && p.IntendedMinLevel <= level).ToArray();
            var dynamite = builder.MapSections.Where(p => p.HasDynamite && p.IntendedMinLevel <= level).ToArray();
            var dynamitePlacement = builder.MapSections.Where(p => p.HasDynamitePlacement && p.IntendedMinLevel <= level).ToArray();
            var other = builder.MapSections.Where(p => p.HasNothing(mod) && p.IntendedMinLevel <= level).ToArray();

            return Validate(new SectionTypes(
                starts,
                ends,
                keyLocations,
                keyLockedDoors,
                boss,
                pow,
                secret,
                radio,
                dynamite,
                dynamitePlacement,
                other
                ), out errors);
        }
    }
    */
}
