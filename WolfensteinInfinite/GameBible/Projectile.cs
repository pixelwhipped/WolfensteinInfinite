namespace WolfensteinInfinite.GameBible
{
    public class Projectile(string name, byte damageMod, byte rangeMod, AmmoType ammoType, ProjectileSpriteType spriteType, string? spritePath, int startSprite, string? hitSound, string? trailAnimation, string? impactAnimation)
    {
        public string Name { get; init; } = name;
        public byte DamageMod { get; init; } = damageMod;
        public byte RangeMod { get; init; } = rangeMod;
        public ProjectileSpriteType SpriteType { get; init; } = spriteType;
        public AmmoType AmmoType { get; init; } = ammoType;
        public string? SpritePath { get; init; } = spritePath;
        public int StartSprite { get; init; } = startSprite;
        public string? HitSound { get; init; } = hitSound;
        public string? TrailAnimation { get; init; } = trailAnimation;
        public string? ImpactAnimation { get; init; } = impactAnimation;
        //Todo Impove
        public int GetDamage(int tileDist)
        {
            switch (AmmoType)
            {
                case AmmoType.MELEE:
                    if (tileDist > (RangeMod - 1)) return 0;
                    //if (tileDist > RangeMod || Random.Shared.Next(0, 255) < 60) return 0;
                    return Random.Shared.Next(0, 255) / DamageMod;                    
                case AmmoType.BULLET:
                    if(tileDist >= RangeMod && (Random.Shared.Next(0, 255)/12)<tileDist) return Random.Shared.Next(0, 255) / (DamageMod*3);
                    if (tileDist < (RangeMod / 2)) return Random.Shared.Next(0, 255) / DamageMod;
                    return Random.Shared.Next(0, 255) / (DamageMod * 2);
                case AmmoType.SERUM:
                    if(tileDist > RangeMod) return (Random.Shared.Next(0, 255) / (DamageMod/2)) + (DamageMod*3);
                    return (Random.Shared.Next(0, 255) / DamageMod) + (DamageMod * 3);
                case AmmoType.FLAME:
                    if (tileDist > RangeMod) return Random.Shared.Next(0, 255) / (DamageMod / 2);
                    return Random.Shared.Next(0, 255) / DamageMod;
                case AmmoType.ROCKET:
                    if (tileDist > RangeMod) return (Random.Shared.Next(0, 255) / (DamageMod / 2)) + (DamageMod * 4);
                    return (Random.Shared.Next(0, 255) / DamageMod) + (DamageMod * 4);
            }
            return 0;
        }
    }
}
