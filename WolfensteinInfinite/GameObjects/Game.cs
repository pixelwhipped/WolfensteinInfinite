namespace WolfensteinInfinite.GameObjects
{
    public sealed class Game(Guid gameId, Map map, Player player, string[] mods)
    {
        public Player Player { get; init; } = player;
        public Guid GameId { get; init; } = gameId;
        public Map Map { get; init; } = map;
        public string[] Mods { get; set; } = mods;
    }
}

