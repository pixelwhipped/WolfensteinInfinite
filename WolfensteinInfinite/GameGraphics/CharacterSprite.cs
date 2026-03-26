using System.Windows.Controls;
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

        public bool IsInAttackFireFrames(int[] frames) =>
            AnimationState == CharacterAnimationState.ATTACKING &&
            Animations.TryGetValue(CharacterAnimationState.ATTACKING, out var anim) &&
            frames.Any(p=>p== anim.CurrentFrame);
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
