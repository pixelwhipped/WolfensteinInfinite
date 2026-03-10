namespace WolfensteinInfinite.GameBible
{
    public class PickupItem(string name, PickupItemType itemType, int value, int modifier, string? spritePath, AmmoType? ammoType)
    {
        public string Name { get; init; } = name;
        public PickupItemType ItemType { get; init; } = itemType;
        public AmmoType? AmmoType { get; init; } = ammoType;
        public int Value { get; init; } = value;
        public int Modifier { get; init; } = modifier;
        public string? SpritePath { get; init; } = spritePath;

    }
}
