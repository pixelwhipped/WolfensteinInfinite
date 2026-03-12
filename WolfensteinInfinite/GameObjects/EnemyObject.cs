using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class EnemyObject : DynamicObject
    {
        public Enemy Enemy { get; }
        public int HitPoints { get; set; }
        public CharacterAnimationState AnimationState { get; set; } = CharacterAnimationState.STANDING;
        public EnemyAIState AIState { get; private set; } = EnemyAIState.Idle;
        public string Mod { get; }

        private bool _isDying = false;
        private bool _isCorpse = false;
        private bool _isAttacking = false;
        private float _alertTimer = 0f;
        private float _attackCooldown;           // initialised to full duration — no instant first attack
        private EnemyAIState _lastAIState = EnemyAIState.Idle;
        private readonly Projectile? _projectile;

        private const float AlertPauseDuration = 0.5f;
        private const float AttackCooldownDuration = 1.5f;
        private const float MeleeAttackRange = 1.5f;
        private const float AlertRadius = 5f;
        private const float LineOfSightDistance = 12f;

        private float WorldSpeed => Enemy.Speed / (512f * 4f);

        private bool IsRanged =>
            _projectile != null &&
            _projectile.AmmoType != AmmoType.MELEE;

        public float FacingAngle { get; private set; } = 180f;

        public EnemyObject(float x, float y, CharacterSprite sprite, Enemy enemy,
            Difficulties difficulty, string mod, Wolfenstein wolfenstein, int level)
            : base(x, y, DynamicObjectType.Enemy, sprite)
        {
            Enemy = enemy;
            Mod = mod;
            HitPoints = enemy.HitPoints.TryGetValue(difficulty, out int hp)
                ? hp : enemy.HitPoints.Values.First();

            var scale = 1f + level / (level + 10f);
            HitPoints = (int)(HitPoints * scale);
            PointsReward = (int)(enemy.Points * scale);

            // First attack waits a full cooldown — prevents instant attack on alert
            _attackCooldown = AttackCooldownDuration;

            if (wolfenstein.Mods.TryGetValue(mod, out var m))
                _projectile = m.Projectiles.FirstOrDefault(p =>
                    Enemy.Weapons.Contains(p.Name));
        }

        public int PointsReward { get; }

        public CharacterSprite CharacterSprite => (CharacterSprite)Sprite;

        // ---- Helpers --------------------------------------------------------

        private void PlaySound(string[] sounds, InGameState state)
        {
            if (sounds.Length == 0) return;
            var name = sounds[Random.Shared.Next(sounds.Length)];
            var key = $"{Mod}:{name}";
            if (state.Wolfenstein.EnemySounds.TryGetValue(key, out var audio))
                AudioPlaybackEngine.Instance.PlaySound(audio);
        }

        private void SetAnimation(CharacterAnimationState anim)
        {
            if (AnimationState == anim) return;
            AnimationState = anim;
            CharacterSprite.AnimationState = anim;
            CharacterSprite.ResetAnimation();
        }

        private void SetAnimationForState(EnemyAIState aiState)
        {
            if (aiState == _lastAIState) return;
            _lastAIState = aiState;
            switch (aiState)
            {
                case EnemyAIState.Idle:
                    SetAnimation(CharacterAnimationState.STANDING);
                    break;
                case EnemyAIState.Chase:
                    SetAnimation(CharacterAnimationState.WALKING);
                    break;
                case EnemyAIState.Attack:
                    SetAnimation(CharacterAnimationState.ATTACKING);
                    break;
            }
        }

        private bool HasLineOfSight(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > LineOfSightDistance) return false;

            var steps = Math.Max(1, (int)(dist * 4));
            var stepX = dx / steps;
            var stepY = dy / steps;
            var rx = X;
            var ry = Y;

            for (int i = 0; i < steps; i++)
            {
                rx += stepX;
                ry += stepY;
                var mx = (int)rx;
                var my = (int)ry;
                if (my < 0 || my >= state.Game.Map.WorldMap.Length ||
                    mx < 0 || mx >= state.Game.Map.WorldMap[0].Length) return false;
                var tile = state.Game.Map.WorldMap[my][mx];
                if (tile >= 0) return false;
                if (tile == InGameState.DOOR_TILE)
                {
                    var door = state.Game.Map.Doors
                        .FirstOrDefault(d => d.X == mx && d.Y == my);
                    if (door != null && door.OpenAmount < 0.5f) return false;
                }
            }
            return true;
        }

        private void TryMoveTowardPlayer(float frameTime, InGameState state)
        {
            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return;

            var nx = dx / dist;
            var ny = dy / dist;

            FacingAngle = MathF.Atan2(ny, nx) * (180f / MathF.PI);
            FacingAngle = (FacingAngle + 360f) % 360f;

            var speed = WorldSpeed * frameTime;
            var newX = X + nx * speed;
            var newY = Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)X;
            var curY = (int)Y;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                state.Game.Map.WorldMap[mapY][curX] == MapSection.ClosedSectionInterior)
                Y = newY;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                state.Game.Map.WorldMap[curY][mapX] == MapSection.ClosedSectionInterior)
                X = newX;
        }

        private ProjectileSprite? FindProjectileSprite(Projectile projectile, InGameState state)
        {
            if (state.Wolfenstein.ProjectileSprites.TryGetValue(Mod, out var sprites) &&
                sprites.TryGetValue(projectile.Name, out var ps))
                return ps;
            return null;
        }

        private void TryAttack(float frameTime, InGameState state)
        {
            _attackCooldown -= frameTime;

            // While on cooldown, revert to walking animation once attack anim completes
            if (_attackCooldown > 0)
            {
                if (_isAttacking && CharacterSprite.IsAttackAnimationComplete)
                {
                    _isAttacking = false;
                    _lastAIState = EnemyAIState.Chase; // allow re-trigger on next attack
                    SetAnimation(CharacterAnimationState.WALKING);
                }
                return;
            }

            // Cooldown expired — fire
            _attackCooldown = AttackCooldownDuration;
            _isAttacking = true;
            _lastAIState = EnemyAIState.Chase; // force animation reset
            SetAnimationForState(EnemyAIState.Attack);
            //PlaySound(Enemy., state); nneds to be attack sound

            if (_projectile == null)
            {
                // No projectile defined — flat melee fallback
                state.ApplyDamage(5);
                return;
            }

            var distToPlayer = MathF.Sqrt(
                MathF.Pow(state.Game.Player.PosX - X, 2) +
                MathF.Pow(state.Game.Player.PosY - Y, 2));

            // Melee always uses distance 0 — GetDamage returns 0 at range otherwise
            var tileDist = _projectile.AmmoType == AmmoType.MELEE ? 0 : (int)distToPlayer;
            var damage = _projectile.GetDamage(tileDist);
            if (damage <= 0) return;

            if (_projectile.AmmoType == AmmoType.MELEE ||
                _projectile.AmmoType == AmmoType.BULLET)
            {
                state.ApplyDamage(damage);
            }
            else
            {
                var dx = state.Game.Player.PosX - X;
                var dy = state.Game.Player.PosY - Y;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist <= 0) return;

                var sprite = FindProjectileSprite(_projectile, state);
                if (sprite == null) return;

                state.DynamicObjects.Add(new ProjectileObject(
                    X, Y,
                    dx / dist, dy / dist,
                    speed: 8f,
                    damage: damage,
                    maxRange: _projectile.RangeMod,
                    isEnemyProjectile: true,
                    sprite: sprite));
            }
        }

        // ---- Public API -----------------------------------------------------

        public void Alert(InGameState state)
        {
            if (AIState != EnemyAIState.Idle) return;
            AIState = EnemyAIState.Alert;
            _alertTimer = AlertPauseDuration;
            SetAnimation(CharacterAnimationState.STANDING);
            PlaySound(Enemy.AlertSounds, state);

            foreach (var obj in state.DynamicObjects)
            {
                if (obj is EnemyObject other && other != this &&
                    other.AIState == EnemyAIState.Idle)
                {
                    var dx = other.X - X;
                    var dy = other.Y - Y;
                    if (dx * dx + dy * dy <= AlertRadius * AlertRadius)
                        other.Alert(state);
                }
            }
        }

        public void TakeDamage(int amount, InGameState state)
        {
            if (_isDying || _isCorpse) return;
            Alert(state);
            HitPoints -= amount;
            if (HitPoints <= 0)
            {
                HitPoints = 0;
                _isDying = true;
                AIState = EnemyAIState.Dead;
                SetAnimation(Random.Shared.Next(2) == 0
                    ? CharacterAnimationState.DYING_LEFT
                    : CharacterAnimationState.DYING_RIGHT);
                state.Game.Player.Score += PointsReward;
                state.Game.Map.LevelScore += PointsReward;
                PlaySound(Enemy.DeathSounds, state);
                state.OnEnemyKilled();
            }
        }

        // ---- Update ---------------------------------------------------------

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;

            // Corpse — static, no further processing
            if (_isCorpse) return;

            Sprite.Update(frameTime);

            if (_isDying)
            {
                if (CharacterSprite.IsDeathAnimationComplete)
                {
                    _isCorpse = true;
                    SpawnDrops(state);
                    // Stay IsAlive=true so corpse renders — just stops all updates above
                }
                return;
            }

            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var distToPlayer = MathF.Sqrt(dx * dx + dy * dy);

            switch (AIState)
            {
                case EnemyAIState.Idle:
                    SetAnimationForState(EnemyAIState.Idle);
                    if (HasLineOfSight(state))
                        Alert(state);
                    break;

                case EnemyAIState.Alert:
                    _alertTimer -= frameTime;
                    if (_alertTimer <= 0)
                        AIState = EnemyAIState.Chase;
                    break;

                case EnemyAIState.Chase:
                    SetAnimationForState(EnemyAIState.Chase);
                    if (IsRanged)
                    {
                        if (HasLineOfSight(state))
                            AIState = EnemyAIState.Attack;
                        else
                            TryMoveTowardPlayer(frameTime, state);
                    }
                    else
                    {
                        if (distToPlayer <= MeleeAttackRange)
                            AIState = EnemyAIState.Attack;
                        else
                            TryMoveTowardPlayer(frameTime, state);
                    }
                    break;

                case EnemyAIState.Attack:
                    if (IsRanged)
                    {
                        if (!HasLineOfSight(state))
                        {
                            _isAttacking = false;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, state);
                    }
                    else
                    {
                        if (distToPlayer > MeleeAttackRange)
                        {
                            _isAttacking = false;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, state);
                    }
                    break;
            }
        }

        private void SpawnDrops(InGameState state)
        {
            foreach (var (itemName, probability) in Enemy.DropItemProbability)
            {
                if (Random.Shared.Next(100) >= probability) continue;
                var kvp = state.Wolfenstein.PickupItemTypes
                    .FirstOrDefault(p => p.Value.Name == itemName);
                if (kvp.Value == null) continue;
                if (!state.Wolfenstein.PickupItems.TryGetValue(kvp.Key, out var texture)) continue;
                state.DynamicObjects.Add(
                    new PickupItemObject(X, Y, new StaticSprite(texture), kvp.Value));
            }
        }
    }
}