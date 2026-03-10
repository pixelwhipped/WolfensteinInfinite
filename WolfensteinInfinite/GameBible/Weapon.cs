namespace WolfensteinInfinite.GameBible
{
    public class Weapon(string name, WeaponType weaponType, string projectile, string? sound, int fireRate, int cooldown)
    {
        public string Name { get; init; } = name;
        public string? Sound { get; init; } = sound;
        public WeaponType WeaponType { get; init; } = weaponType;
        public string Projectile { get; init; } = projectile;
        public int FireRate { get; init; } = fireRate;
        public int Cooldown { get; init; } = cooldown;

    }
}
