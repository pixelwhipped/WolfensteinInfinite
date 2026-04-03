//Clean
using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.GameHelpers
{
    public static class ProjectileHelpers
    {
        public static Projectile CreateBullet(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 6, 13, AmmoType.BULLET, 8f, ProjectileSpriteType.BULLET, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateBite(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 16, 1, AmmoType.MELEE, 8f, ProjectileSpriteType.NONE, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateDrain(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 24, 1, AmmoType.MELEE, 8f, ProjectileSpriteType.NONE, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateKnife(string name, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 16, 1, AmmoType.MELEE, 8f, ProjectileSpriteType.NONE, null, -1, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateRocket(string name, ProjectileSpriteType spriteType, string spritePath, int spriteStart, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 8, 20, AmmoType.ROCKET, 5f, spriteType, spritePath, spriteStart, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateSerum(string name, ProjectileSpriteType spriteType, string spritePath, int spriteStart, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 8, 9, AmmoType.SERUM, 5f, spriteType, spritePath, spriteStart, hitsound, trailAnimation, impactAnimation);
        public static Projectile CreateFlame(string name, ProjectileSpriteType spriteType, string spritePath, int spriteStart, string? hitsound, string? trailAnimation, string? impactAnimation) => new(name, 8, 10, AmmoType.FLAME, 5f, spriteType, spritePath, spriteStart, hitsound, trailAnimation, impactAnimation);
    }
}
