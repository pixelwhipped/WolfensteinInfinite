//Clean only 1 internal static method no need to move to helper class
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.GameGraphics
{
    public class ProjectileSprite : ISprite
    {
        private Animation? Animation { get; init; }
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
                case ProjectileSpriteType.ROCKET: // Patch, Rocket directions incomplete  
                    Animation = ReadAnimationsRocket(path, start, 8, 1, 4); 
                    break;
                case ProjectileSpriteType.SERUM:
                    Animation = ReadAnimations(path, start, 1, 4, 5);
                    break;
                case ProjectileSpriteType.FLAME:
                    Animation = ReadAnimations(path, start, 1, 2, 4);
                    break;
            }
        }
        private static Animation ReadAnimationsRocket(string path, int start, int directions, int frames, float fps)
        {
            var animation = new List<Texture32>();
            int end = start + directions * frames;
            for (int i = start; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            return new Animation([.. animation], directions, frames, fps);
        }
        private static Animation ReadAnimations(string path, int start, int directions, int frames, float fps)
        {
            var animation = new List<Texture32>();
            int end = start + directions * frames;
            for (int i = start; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            return new Animation([.. animation], directions, frames, fps);
        }
        public void Update(float frameTimeSeconds) => Animation?.Update(frameTimeSeconds);
        public Texture32 GetTexture(float angle) => Animation?.GetTexture(angle) ?? Texture32.NonNullTexture;
    }
}
