using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite
{
    public class ProjectileSprite : ISprite
    {
        private SpriteAnimation? Animation { get; init; }
        public ProjectileSprite(string? path, int start, ProjectileSpriteType type)
        {
            if (path == null)
            {
                Animation = null;
                return;
            }
            path = FileHelpers.Shared.GetModDataFilePath(path);
            switch (type)
            {
                case ProjectileSpriteType.NONE:
                case ProjectileSpriteType.BULLET:
                    Animation = null;
                    break;
                case ProjectileSpriteType.ROCKET:
                    Animation = ReadAnimations(path, start, 8, 1);
                    break;
                case ProjectileSpriteType.SERUM:
                    Animation = ReadAnimations(path, start, 1, 4);
                    break;
                case ProjectileSpriteType.FLAME:
                    Animation = ReadAnimations(path, start, 1, 2);
                    break;
            }
        }

        private SpriteAnimation ReadRocketAnimations(string path, int start)
        {
            var animation = new List<Texture32>();
            int end = start + 8;
            for (int i = start; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            return new SpriteAnimation([.. animation], 8, 1, 1);
        }
        private SpriteAnimation ReadAnimations(string path, int start, int directions, int frames)
        {
            var animation = new List<Texture32>();
            int end = start + directions * frames;
            for (int i = start; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            return new SpriteAnimation([.. animation], directions, frames, 1);
        }
        public void Update(float frameTimeSeconds) => Animation?.Update(frameTimeSeconds);
        public Texture32 GetTexture(float angle) => Animation?.GetTexture(angle) ?? Texture32.NonNullTexture;
    }
}
