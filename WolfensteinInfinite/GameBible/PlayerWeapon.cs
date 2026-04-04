namespace WolfensteinInfinite.GameBible
{
    public class PlayerWeapon(string name, int preferedOrder, WeaponType weaponType, AmmoType ammoType, string? sound, int baseHeight, float fireRate, float cooldown, float maxFireTime, string hudSprite, string spritePath, int startSprite, int spriteCount, int fireIndex, int firingStart, int firingEnd)
    {
        public string Name { get; init; } = name;
        public int PreferedOrder { get; init; } = preferedOrder;
        public string? Sound { get; init; } = sound;
        public WeaponType WeaponType { get; init; } = weaponType;
        public AmmoType AmmoType { get; init; } = ammoType;
        public float FireRate { get; init; } = fireRate;
        public float Cooldown { get; init; } = cooldown;
        // maxFireTime=0 makes it single-shot
        public float MaxFireTime { get; init; } = maxFireTime;
        public string HudSprite { get; init; } = hudSprite;
        public string SpritePath { get; init; } = spritePath;
        public int StartSprite { get; init; } = startSprite;
        public int SpriteCount { get; init; } = spriteCount;
        public int FireIndex { get; init; } = fireIndex;
        public int FiringStart { get; init; } = firingStart;
        public int FiringEnd { get; init; } = firingEnd;
        public int BaseHeight { get; init; } = baseHeight;
        public float FramesPerSecond { get; init; } = maxFireTime <= 0 && cooldown > 0
            ? spriteCount / cooldown                          // single-shot: spread all frames across cooldown
            : (firingEnd - firingStart + 1) * fireRate;       // sustained: one loop cycle = 1/fireRate seconds
    }
}
