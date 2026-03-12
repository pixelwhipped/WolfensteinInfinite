//Clean
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameGraphics
{
    public class WeaponAnimation(Texture32[] textures, int frames, float framesPerSecond, int fireIndex, int loopStart, int loopEnd) : ISprite
    {
        public int Frames { get; init; } = frames;
        public int CurrentFrame { get; set; }
        private float CurrentFrameTime { get; set; }
        private float FramesPerSecond { get; init; } = framesPerSecond;
        public bool InLoop { get; set; } = false;
        public int LoopStart { get; set; } = loopStart;
        public int LoopEnd { get; set; } = loopEnd;
        public int FireIndex { get; set; } = fireIndex;
        public Action? OnFire { get; set; } = null;
        private Texture32[] Textures { get; init; } = textures;

        public void Update(float frameTimeSeconds)
        {
            CurrentFrameTime += frameTimeSeconds;
            float timePerFrame = 1.0f / FramesPerSecond;

            if (CurrentFrameTime >= timePerFrame)
            {
                if (!InLoop)
                {
                    CurrentFrame = (CurrentFrame + 1) % Frames;
                    
                }
                else
                {
                    if (CurrentFrame < LoopStart || CurrentFrame > LoopEnd)
                    {
                        CurrentFrame = (CurrentFrame + 1) % Frames;
                    }
                    else
                    {
                        var nf = CurrentFrame + 1;
                        if (nf > LoopEnd) nf = LoopStart;
                        CurrentFrame = nf;
                        if (CurrentFrame == FireIndex) OnFire?.Invoke();
                    }
                }
                CurrentFrameTime -= timePerFrame; // Keep remainder for smooth timing
            }
        }
        public Texture32 GetTexture(float angle)
        {
            return Textures[CurrentFrame];
        }
        public void Reset()
        {
            CurrentFrame = 0;
            CurrentFrameTime = 0;
        }
    }
}
