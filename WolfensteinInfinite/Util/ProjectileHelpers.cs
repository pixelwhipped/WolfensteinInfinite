using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.Util
{
    public static class ProjectileHelpers
    {
        public static Projectile CreateBullet(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 6, 4, AmmoType.BULLET, ProjectileSpriteType.BULLET, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateBite(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 16, 1, AmmoType.MELEE, ProjectileSpriteType.NONE, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateDrain(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 24, 1, AmmoType.MELEE, ProjectileSpriteType.NONE, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateKnife(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 16, 1, AmmoType.MELEE, ProjectileSpriteType.NONE, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateRocket(string name, ProjectileSpriteType spriteType, string spritePath, int spriteStart, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 8, 20, AmmoType.ROCKET, spriteType, spritePath, spriteStart, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateSerum(string name, ProjectileSpriteType spriteType, string spritePath, int spriteStart, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 8, 6, AmmoType.SERUM, spriteType, spritePath, spriteStart, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateFlame(string name, ProjectileSpriteType spriteType, string spritePath, int spriteStart, string? hitsound, string? trailAnimation, string? impactAnimation) => new Projectile(name, 8, 10, AmmoType.FLAME, spriteType, spritePath, spriteStart, hitsound, trailAnimation, impactAnimation);
    }
}
