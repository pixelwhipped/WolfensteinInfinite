using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // ProjectileObject
    // -------------------------------------------------------------------------
    public sealed class ProjectileObject(float x, float y, float dirX, float dirY,
    string mod, Projectile projectile, int damage, bool isEnemyProjectile, ISprite? sprite) : DynamicObject(x, y, DynamicObjectType.Projectile, sprite)
    {
        public float DirX { get; init; } = dirX;
        public float DirY { get; init; } = dirY;
        public Projectile Projectile { get; init; } = projectile;
        public string Mod { get; init; } = mod;
        public int Damage { get; init; } = damage;
        public bool IsEnemyProjectile { get; init; } = isEnemyProjectile;
        private float _distanceTravelled = 0;        
        public float FacingAngle { get; private set; } = 180f;
        private float _smoothedFacingAngle = 180f;
        private bool ExplsionAdded = false;
        private bool TrailAdded = false;
        private Animation? TrailAnimation = null;

        private void AddExplosion(InGameState state)
        {
            if (ExplsionAdded) return;
            ExplsionAdded = true;
            if(state.Wolfenstein.SpriteAnimations[Mod].TryGetValue(Projectile.ImpactAnimation??string.Empty, out Animation? animation) && animation !=null)
            {
                state.DynamicObjects.Add(new ParticleObject(X, Y, 0, 0, 0, Mod, animation.Clone(), null));
            }
        }

        private static void AddTrail(InGameState state, ProjectileObject projectileObject)
        {
            if (!projectileObject.IsAlive || projectileObject.TrailAnimation == null) return;
            state.DynamicObjects.Add(new ParticleObject(projectileObject.X, projectileObject.Y, projectileObject.DirX, projectileObject.DirY, projectileObject.Projectile.Speed*0.75f, projectileObject.Mod, projectileObject.TrailAnimation.Clone(), (_)=> { AddTrail(state, projectileObject); }));
        }

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            if (!TrailAdded)
            {
                TrailAdded = true;
                if (state.Wolfenstein.SpriteAnimations[Mod].TryGetValue(Projectile.TrailAnimation ?? string.Empty, out Animation? animation) && animation != null)
                {
                    TrailAnimation = animation;
                    AddTrail(state, this);                    
                }
            }
            Sprite?.Update(frameTime);

            var dx = DirX * Projectile.Speed * frameTime;
            var dy = DirY * Projectile.Speed * frameTime;
            X += dx;
            Y += dy;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            _distanceTravelled += dist;
            var nx = dx / dist;
            var ny = dy / dist;

            var targetAngle = MathF.Atan2(ny, nx) * (180f / MathF.PI);
            targetAngle = (targetAngle + 360f) % 360f;
            var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
            if (angleDiff > 180f) angleDiff = 360f - angleDiff;
            if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
            FacingAngle = _smoothedFacingAngle;

            var mx = (int)X;
            var my = (int)Y;

            // Out of bounds or hit wall
            if (my < 0 || my >= state.Game.Map.WorldMap.Length ||
                mx < 0 || mx >= state.Game.Map.WorldMap[0].Length ||
                state.Game.Map.WorldMap[my][mx] >= 0)
            {
                AddExplosion(state);
                IsAlive = false;
                return;
            }
            var tile = state.Game.Map.WorldMap[my][mx];
            if (tile >= 0)
            {
                AddExplosion(state);
                IsAlive = false;
                return;
            }
            if (tile == InGameState.DOOR_TILE)
            {
                state.DoorLookup.TryGetValue((mx, my), out var door);
                if (door != null && door.OpenAmount < 0.5f)
                {
                    AddExplosion(state);
                    IsAlive = false;
                    return;
                }
            }
            // Exceeded max range
            if (_distanceTravelled >= Projectile.RangeMod)
            {
                IsAlive = false;
                return;
            }

            if (IsEnemyProjectile)
            {
                if ((int)state.Game.Player.PosX == mx &&
                    (int)state.Game.Player.PosY == my)
                {
                    AddExplosion(state);
                    state.ApplyDamage(Damage);
                    IsAlive = false;
                }
            }
            else
            {
                foreach (var obj in state.DynamicObjects.ToArray()) //To array prevent collection modification exceptions
                {
                    if (obj is EnemyObject enemy && !(enemy.IsCorpse || enemy.IsDying) &&
                        (int)enemy.X == mx && (int)enemy.Y == my)
                    {
                        AddExplosion(state);
                        enemy.TakeDamage(Damage, state);
                        IsAlive = false;
                        return;
                    }
                }
            }
        }

        
    }
}