using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameMap
{
    public class GeneratorSectionTypes(MapGeneratorSection[] playerStarts, MapGeneratorSection[] playerExits,
        MapGeneratorSection[] keyLocations, MapGeneratorSection[] keyLockedDoors, MapGeneratorSection[] boss, MapGeneratorSection[] pow, MapGeneratorSection[] secret,
        MapGeneratorSection[] radio, MapGeneratorSection[] dynamite, MapGeneratorSection[] dynamitePlacement, MapGeneratorSection[] other)
    {
        public MapGeneratorSection[] PlayerStarts { get; init; } = playerStarts;
        public MapGeneratorSection[] PlayerExits { get; init; } = playerExits;
        public MapGeneratorSection[] KeyLocations { get; init; } = keyLocations;
        public MapGeneratorSection[] KeyLockedDoors { get; init; } = keyLockedDoors;
        public MapGeneratorSection[] Boss { get; init; } = boss;
        public MapGeneratorSection[] Pow { get; init; } = pow;
        public MapGeneratorSection[] Secret { get; init; } = secret;
        public MapGeneratorSection[] Radio { get; init; } = radio;
        public MapGeneratorSection[] Dynamite { get; init; } = dynamite;
        public MapGeneratorSection[] DynamitePlacement { get; init; } = dynamitePlacement;
        public MapGeneratorSection[] Other { get; init; } = other;

        private MapGeneratorSection[]? all = null;
        public MapGeneratorSection[] All => all ??= [.. PlayerStarts, .. PlayerExits, .. KeyLocations, .. KeyLockedDoors, .. Boss, .. Pow, .. Secret, .. Radio, .. Dynamite, .. DynamitePlacement, .. Other];
        public static GeneratorSectionTypes? GetSectionTypes(Mod mod, MapSection[] sections, int level, out string[] errors)
        {
            static GeneratorSectionTypes? Validate(GeneratorSectionTypes s, out string[] e)
            {
                var err = new List<string>();
                if (s.PlayerStarts.Length == 0) err.Add("No Player Starts");
                if (s.PlayerExits.Length == 0) err.Add("No Player Exits");
                e = [.. err];
                return e.Length == 0 ? s : null;
            }
            sections = [.. sections.Where(s => !s.IsFullMap)];
            var starts = sections.Where(p => p.HasPlayerStart && p.IntendedMinLevel <= level).ToArray();
            var ends = sections.Where(p => p.HasPlayerExit && p.IntendedMinLevel <= level).ToArray();
            var keyLocations = sections.Where(p => p.HasKeys && p.IntendedMinLevel <= level).ToArray();
            var keyLockedDoors = sections.Where(p => p.HasLockedDoor && p.IntendedMinLevel <= level).ToArray();
            var boss = sections.Where(p => p.HasBoss(mod) && p.IntendedMinLevel <= level).ToArray();
            var secret = sections.Where(p => p.HasSecret && p.IntendedMinLevel <= level).ToArray();
            var pow = sections.Where(p => p.HasPow && p.IntendedMinLevel <= level).ToArray();
            var radio = sections.Where(p => p.HasRadio && p.IntendedMinLevel <= level).ToArray();
            var dynamite = sections.Where(p => p.HasDynamite && p.IntendedMinLevel <= level).ToArray();
            var dynamitePlacement = sections.Where(p => p.HasDynamitePlacement && p.IntendedMinLevel <= level).ToArray();
            var other = sections.Where(p => p.HasNothing(mod) && p.IntendedMinLevel <= level).ToArray();

            return Validate(new GeneratorSectionTypes(
                [.. starts.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. ends.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. keyLocations.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. keyLockedDoors.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. boss.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. pow.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. secret.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. radio.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. dynamite.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. dynamitePlacement.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))],
                [.. other.Select(p => new MapGeneratorSection(0, 0, mod, p, p.GetConnections()))]
                ), out errors);
        }
        public static GeneratorSectionTypes? GetSectionTypes(MapGenerator builder, int level, out string[] errors)
        {
            static GeneratorSectionTypes? Validate(GeneratorSectionTypes s, out string[] e)
            {
                var err = new List<string>();
                if (s.PlayerStarts.Length == 0) err.Add("No Player Starts");
                if (s.PlayerExits.Length == 0) err.Add("No Player Exits");
                e = [.. err];
                return e.Length == 0 ? s : null;
            }

            var starts = builder.Sections.Where(p => p.Section.HasPlayerStart && p.Section.IntendedMinLevel <= level).ToArray();
            var ends = builder.Sections.Where(p => p.Section.HasPlayerExit && p.Section.IntendedMinLevel <= level).ToArray();
            var keyLocations = builder.Sections.Where(p => p.Section.HasKeys && p.Section.IntendedMinLevel <= level).ToArray();
            var keyLockedDoors = builder.Sections.Where(p => p.Section.HasLockedDoor && p.Section.IntendedMinLevel <= level).ToArray();
            var boss = builder.Sections.Where(p => p.Section.HasBoss(p.Mod) && p.Section.IntendedMinLevel <= level).ToArray();
            var secret = builder.Sections.Where(p => p.Section.HasSecret && p.Section.IntendedMinLevel <= level).ToArray();
            var pow = builder.Sections.Where(p => p.Section.HasPow && p.Section.IntendedMinLevel <= level).ToArray();
            var radio = builder.Sections.Where(p => p.Section.HasRadio && p.Section.IntendedMinLevel <= level).ToArray();
            var dynamite = builder.Sections.Where(p => p.Section.HasDynamite && p.Section.IntendedMinLevel <= level).ToArray();
            var dynamitePlacement = builder.Sections.Where(p => p.Section.HasDynamitePlacement && p.Section.IntendedMinLevel <= level).ToArray();
            var other = builder.Sections.Where(p => p.Section.HasNothing(p.Mod) && !p.Section.HasPlayerStart && !p.Section.HasPlayerExit && p.Section.IntendedMinLevel <= level).ToArray();

            //These are suffled in MapGenerator
            return Validate(new GeneratorSectionTypes(starts, ends, keyLocations, keyLockedDoors, boss, pow, secret, radio, dynamite, dynamitePlacement, other), out errors);
        }
    }

}
