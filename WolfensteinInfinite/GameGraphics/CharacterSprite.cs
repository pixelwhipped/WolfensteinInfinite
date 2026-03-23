using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameGraphics
{
    public class CharacterSprite(Dictionary<CharacterAnimationState, Animation> animations) : ISprite
    {

        private readonly Dictionary<CharacterAnimationState, Animation> Animations = animations;
        public CharacterAnimationState AnimationState { get; set; } = CharacterAnimationState.STANDING;
        public bool IsDeathAnimationComplete => AnimationState ==  CharacterAnimationState.DEAD || (
            (AnimationState == CharacterAnimationState.DYING_LEFT ||
             AnimationState == CharacterAnimationState.DYING_RIGHT) &&
             Animations[AnimationState].IsComplete);
        public bool IsHitAnimationComplete =>
            AnimationState == CharacterAnimationState.HIT &&
            Animations[AnimationState].IsComplete;
        public bool IsAttackAnimationComplete =>
            AnimationState == CharacterAnimationState.ATTACKING &&
            Animations[AnimationState].IsComplete;

        public bool HasAnimation(CharacterAnimationState state) => Animations.ContainsKey(state);

        // True when the attack animation is on its last two frames — the wind-up is
        // done and the weapon is visually at full extension. Shots fire here so the
        // animation and damage land together, and the cycle can loop naturally.
        public bool IsInAttackFireWindow =>
            AnimationState == CharacterAnimationState.ATTACKING &&
            Animations.TryGetValue(CharacterAnimationState.ATTACKING, out var anim) &&
            anim.CurrentFrame >= anim.Frames - 2;
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

        public CharacterSprite Clone() => new(Animations.ToDictionary(kvp => kvp.Key,kvp => new Animation(kvp.Value)));

    }
}
