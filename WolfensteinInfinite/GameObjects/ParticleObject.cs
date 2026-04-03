using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    public sealed class ParticleObject : DynamicObject
    { 

        public float DirX { get; init; }
        public float DirY { get; init; }
        public float Speed { get; init; }
        public string Mod { get; init; }
        public Action<DynamicObject>? OnFinish { get; init; }
        public Animation ParticleSprite { get; init; }
        public ParticleObject(float x, float y, float dirX, float dirY, float speed, string mod, Animation? sprite, Action<DynamicObject>? onFinish): base(x, y, DynamicObjectType.Particle, sprite?.Clone())
        {            
            DirX = dirX;
            DirY = dirY;
            Speed = speed;
            Mod = mod;
            OnFinish = onFinish;
            if (Sprite is Animation s) ParticleSprite = s;
            else ArgumentNullException.ThrowIfNull(ParticleSprite);

        }
        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            ParticleSprite.Update(frameTime);
            if (ParticleSprite.IsComplete)
            {
                IsAlive = false;
                OnFinish?.Invoke(this);
            }
            var dx = DirX * Speed * frameTime;
            var dy = DirY * Speed * frameTime;
            X += dx;
            Y += dy;
        }
    }
}
