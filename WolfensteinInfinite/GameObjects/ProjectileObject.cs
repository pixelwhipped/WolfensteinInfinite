using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // ProjectileObject
    // -------------------------------------------------------------------------
    public class ProjectileObject(float x, float y, float dirX, float dirY,
        float speed, int damage, float maxRange, bool isEnemyProjectile, ISprite sprite) : DynamicObject(x, y, DynamicObjectType.Projectile, sprite)
    {
        public float DirX { get; } = dirX;
        public float DirY { get; } = dirY;
        public float Speed { get; } = speed;
        public int Damage { get; } = damage;
        public bool IsEnemyProjectile { get; } = isEnemyProjectile;
        private float _distanceTravelled = 0;
        private readonly float _maxRange = maxRange;

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);

            var moveX = DirX * Speed * frameTime;
            var moveY = DirY * Speed * frameTime;
            X += moveX;
            Y += moveY;
            _distanceTravelled += MathF.Sqrt(moveX * moveX + moveY * moveY);

            var mx = (int)X;
            var my = (int)Y;

            // Out of bounds or hit wall
            if (my < 0 || my >= state.Game.Map.WorldMap.Length ||
                mx < 0 || mx >= state.Game.Map.WorldMap[0].Length ||
                state.Game.Map.WorldMap[my][mx] >= 0)
            {
                IsAlive = false;
                return;
            }
            var tile = state.Game.Map.WorldMap[my][mx];
            if (tile >= 0)
            {
                IsAlive = false;
                return;
            }
            if (tile == InGameState.DOOR_TILE)
            {
                var door = state.Game.Map.Doors
                    .FirstOrDefault(d => d.X == mx && d.Y == my);
                if (door != null && door.OpenAmount < 0.5f)
                {
                    IsAlive = false;
                    return;
                }
            }
            // Exceeded max range
            if (_distanceTravelled >= _maxRange)
            {
                IsAlive = false;
                return;
            }

            if (IsEnemyProjectile)
            {
                if ((int)state.Game.Player.PosX == mx &&
                    (int)state.Game.Player.PosY == my)
                {
                    state.ApplyDamage(Damage);
                    IsAlive = false;
                }
            }
            else
            {
                foreach (var obj in state.DynamicObjects)
                {
                    if (obj is EnemyObject enemy && enemy.IsAlive && enemy.AIState != EnemyAIState.Dead &&
                        (int)enemy.X == mx && (int)enemy.Y == my)
                    {
                        enemy.TakeDamage(Damage, state);
                        IsAlive = false;
                        return;
                    }
                }
            }
        }
    }
}