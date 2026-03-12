using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameGraphics
{
    public class CharacterSprite(Dictionary<CharacterAnimationState, Animation> animations) : ISprite
    {

        private readonly Dictionary<CharacterAnimationState, Animation> Animations = animations;
        public CharacterAnimationState AnimationState { get; set; } = CharacterAnimationState.STANDING;
        public bool IsDeathAnimationComplete =>
            (AnimationState == CharacterAnimationState.DYING_LEFT ||
             AnimationState == CharacterAnimationState.DYING_RIGHT) &&
             Animations[AnimationState].IsComplete;
        public bool IsAttackAnimationComplete =>
            AnimationState == CharacterAnimationState.ATTACKING &&
            Animations[AnimationState].IsComplete;
        public void Update(float frameTimeSeconds)
        {
            if (Animations.TryGetValue(AnimationState, out var anim))
                anim.Update(frameTimeSeconds);
        }
        public void ResetAnimation()
        {
            if (Animations.TryGetValue(AnimationState, out var anim))
                anim.Reset();
        }
        public Texture32 GetTexture(float angle) => Animations[AnimationState].GetTexture(angle);

    }
}
