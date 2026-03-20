using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.GameHelpers
{
    public static class WeaponHelpers
    {
        public static Weapon CreateKnife(string? sound) => new("Knife", WeaponType.KNIFE, "Stab", sound, 1, 0);
        public static Weapon CreatePistol(string? sound) => new("Pistol", WeaponType.PISTOL, "Bullet", sound, 1, 0);
        public static Weapon CreateBite(string? sound) => new("Bite", WeaponType.MELEE, "Bite", sound, 1, 0);
        public static Weapon CreateMachineGun(string? sound) => new("MachineGun", WeaponType.MACHINE_GUN, "Bullet", sound, 4, 2,1.5f);
        public static Weapon CreateChainGun(string? sound) => new("ChainGun", WeaponType.CHAIN_GUN, "Bullet", sound, 8, 2,2);
        public static Weapon CreateKorpsokineticSerum(string? sound) => new("KorpsokineticSerum", WeaponType.THROW, "KorpsokineticSerum", sound, 1, 1);
        public static Weapon CreateRocketLauncher(string? sound) => new("RocketLauncher", WeaponType.ROCKET_LAUNCHER, "Rocket", sound, 1, 1);
        public static Weapon CreateFlameThrower(string? sound) => new("FlameThrower", WeaponType.FLAME_THROWER, "Flame", sound, 1, 1,3);
        public static Weapon CreateDrainLife(string? sound) => new("DrainLife", WeaponType.MELEE, "DrainLife", sound, 1, 1);
    }
}
