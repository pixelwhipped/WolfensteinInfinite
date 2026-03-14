//Clean
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameGraphics
{
    public class Animation: ISprite
    {
        public bool Loop { get; init; } = true;
        public bool IsComplete => !Loop && CurrentFrame == Frames - 1;
        public int Directions { get; init; }
        public int Frames { get; init; }
        public int CurrentFrame { get; set; }
        private float CurrentFrameTime { get; set; }
        private float FramesPerSecond { get; init; }
        private Texture32[][] Textures { get; init; }

        public Animation(Animation source)
        {
            Loop = source.Loop;
            Directions = source.Directions;
            Frames = source.Frames;
            FramesPerSecond = source.FramesPerSecond;
            Textures = source.Textures; // shared — textures are read-only
            CurrentFrame = 0;
            CurrentFrameTime = 0;
        }
        public Animation(Texture32[] textures, int directions, int frames, float framesPerSecond)
        {
            FramesPerSecond = framesPerSecond;
            Directions = directions;
            Frames = frames;
            Textures = new Texture32[directions][];
            int d = 0;
            for (; d < directions; d++)
            {
                Textures[d] = new Texture32[frames];
            }
            for (int i = 0; i < textures.Length; i++)
            {
                d = i % directions;
                int f = i / directions;
                Textures[d][f] = textures[i];
            }
        }
        public void Update(float frameTimeSeconds)
        {
            if (!Loop && CurrentFrame == Frames - 1) return; 
            CurrentFrameTime += frameTimeSeconds;
            float timePerFrame = 1.0f / FramesPerSecond;

            if (CurrentFrameTime >= timePerFrame)
            {
                CurrentFrame = (CurrentFrame + 1) % Frames;
                CurrentFrameTime -= timePerFrame; // Keep remainder for smooth timing
            }
        }
        public Texture32 GetTexture(float angle)
        {
            var u = (int)Math.Round(angle / Directions / (360 / Directions) * (Directions - 1));
            return Textures[u][CurrentFrame];
        }
        public void Reset()
        {
            CurrentFrame = 0;
            CurrentFrameTime = 0;
        }
    }
}
