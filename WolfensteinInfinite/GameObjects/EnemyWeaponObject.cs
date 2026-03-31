using WolfensteinInfinite.GameBible;


namespace WolfensteinInfinite.GameObjects
{
    public class EnemyWeaponObject(Weapon weapon, Projectile projectile)
    {
        public float AttackCooldownDuration { get; init; }
        public float ShotInterval { get; init; }
        public float MaxFireTime { get; init; }
        //public bool IsSustainedFire { get; init; }
        public bool IsRanged { get; init; }
        public Weapon Weapon { get; init; } = weapon;
        public Projectile Projectile { get; init; } = projectile;
        public float AttackCooldown { get; set; }
        public float FireTimer { get; set; } = 0f;   // total time spent firing (for sustained fire cutoff)        
        public float FrameWait { get; set; } = 0f;
    }
}