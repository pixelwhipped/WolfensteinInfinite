using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameGraphics
{
    /// <summary>
    /// ISprite wrapper around an Animation that can be paused.
    /// Set IsPlaying = false to freeze on the current frame.
    /// </summary>
    public class AnimatedSprite(Animation animation) : ISprite
    {
        public bool IsPlaying { get; set; } = true;

        public void Update(float frameTime)
        {
            if (IsPlaying) animation.Update(frameTime);
        }

        public Texture32 GetTexture(float angle) => animation.GetTexture(0);
    }
}