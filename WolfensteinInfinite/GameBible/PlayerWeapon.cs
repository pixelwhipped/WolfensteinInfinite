namespace WolfensteinInfinite.GameBible
{
    public class PlayerWeapon(string name, int preferedOrder, WeaponType weaponType, AmmoType ammoType, string? sound, int baseHeight, int fireRate, float cooldown, float maxFireTime, string hudSprite, string spritePath, int startSprite, int spriteCount, int fireIndex, int firingStart, int firingEnd)
    {
        public string Name { get; init; } = name;
        public int PreferedOrder { get; init; } = preferedOrder;
        public string? Sound { get; init; } = sound;
        public WeaponType WeaponType { get; init; } = weaponType;
        public AmmoType AmmoType { get; init; } = ammoType;
        public int FireRate { get; init; } = fireRate;
        public float Cooldown { get; init; } = cooldown;
        public float MaxFireTime { get; init; } = maxFireTime;
        public string HudSprite { get; init; } = hudSprite;
        public string SpritePath { get; init; } = spritePath;
        public int StartSprite { get; init; } = startSprite;
        public int SpriteCount { get; init; } = spriteCount;
        public int FireIndex { get; init; } = fireIndex;
        public int FiringStart { get; init; } = firingStart;
        public int FiringEnd { get; init; } = firingEnd;
        public int BaseHeight { get; init; } = baseHeight;
        public float FramesPerSecond { get; init; } = (60f / spriteCount) * fireRate;
    }
}
