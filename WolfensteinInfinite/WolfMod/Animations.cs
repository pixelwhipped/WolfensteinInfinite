using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.WolfMod
{
    public static class Animations
    {
        public static SpriteAnimation Create(string name, string spritePath, string[] sprites, float framesPerSecond) => new(name, spritePath, sprites, framesPerSecond);
        public static SpriteAnimation Create(string name, string spritePath, int start, int count, float framesPerSecond)
        {
            var sprites = new List<string>();
            for (int i = start; i < start + count; i++)
            {
                sprites.Add($"{i}.png");
            }
            return new SpriteAnimation(name, spritePath, [.. sprites], framesPerSecond);
        }
        public static WolfensteinInfinite.SpriteAnimation Create(SpriteAnimation sprite)
        {
            var animation = new List<Texture32>();
            foreach (var s in sprite.Sprites)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(sprite.SpritePath, s)));
            }
            return new WolfensteinInfinite.SpriteAnimation([.. animation], 1, animation.Count, sprite.FramesPerSecond);
        }

        public static WeaponAnimation Create(PlayerWeapon playerWeapon)
        {
            var animation = new List<Texture32>();
            for (int i = playerWeapon.StartSprite; i < playerWeapon.StartSprite + playerWeapon.SpriteCount; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(playerWeapon.SpritePath, $"{i}.png")));
            }
            return new([.. animation], playerWeapon.SpriteCount, playerWeapon.FramesPerSecond, playerWeapon.FireIndex, playerWeapon.FiringStart, playerWeapon.FiringEnd);
        }
    }
}
