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
                    if (obj is EnemyObject enemy && enemy.IsAlive &&
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
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfensteinInfinite.GameAudio;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public enum DynamicObjectType { Decal, PickupItem, Enemy, Projectile }

    public abstract class DynamicObject
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsAlive { get; set; } = true;
        public DynamicObjectType ObjectType { get; }
        public ISprite Sprite { get; }

        protected DynamicObject(float x, float y, DynamicObjectType type, ISprite sprite)
        {
            X = x;
            Y = y;
            ObjectType = type;
            Sprite = sprite;
        }

        public abstract void Update(float frameTime, InGameState state);
    }

    public class DecalObject : DynamicObject
    {
        public Decal Decal { get; }

        public DecalObject(Decal decal, ISprite sprite)
            : base(GetFaceX(decal), GetFaceY(decal), DynamicObjectType.Decal, sprite)
        {
            Decal = decal;
        }

        // Position directional decals at their wall face rather than tile centre
        private static float GetFaceX(Decal d) => d.Direction switch
        {
            Direction.EAST => d.X + 1f,
            Direction.WEST => d.X,
            _ => d.X + 0.5f
        };

        private static float GetFaceY(Decal d) => d.Direction switch
        {
            Direction.NORTH => d.Y,
            Direction.SOUTH => d.Y + 1f,
            _ => d.Y + 0.5f
        };

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);
        }
    }

    public class PickupItemObject : DynamicObject
    {
        public PickupItem Item { get; }

        public PickupItemObject(float x, float y, ISprite sprite, PickupItem item)
            : base(x, y, DynamicObjectType.PickupItem, sprite)
        {
            Item = item;
        }

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);

            // Check if player is on the same tile
            if ((int)state.Game.Player.PosX == (int)X &&
                (int)state.Game.Player.PosY == (int)Y)
            {
                if (state.TryPickupItem(Item))
                    IsAlive = false;
            }
        }
    }

    public class EnemyObject : DynamicObject
    {
        public Enemy Enemy { get; }
        public int HitPoints { get; set; }
        public CharacterAnimationState AnimationState { get; set; } = CharacterAnimationState.STANDING;
        public EnemyAIState AIState { get; private set; } = EnemyAIState.Idle;
        public string Mod { get; }

        private bool _isDying = false;
        private float _alertTimer = 0f;
        private float _attackCooldown = 0f;
        private const float AttackRange = 2.0f;
        private const float AttackCooldownDuration = 1.0f;
        private const float AlertPauseDuration = 0.5f;
        private const float AlertRadius = 5f;
        private const float LineOfSightDistance = 12f;



        private float WorldSpeed => Enemy.Speed / (512f * 4f);
        private bool IsRanged => Enemy.Weapons.Length > 0 &&
    Enemy.Weapons.Any(w => w != null) &&
    _projectile != null &&
    _projectile.AmmoType != AmmoType.MELEE;
        private readonly Projectile? _projectile;

        public EnemyObject(float x, float y, CharacterSprite sprite, Enemy enemy,
            Difficulties difficulty, string mod, Wolfenstein wolfenstein)
            : base(x, y, DynamicObjectType.Enemy, sprite)
        {
            Enemy = enemy;
            Mod = mod;
            HitPoints = enemy.HitPoints.TryGetValue(difficulty, out int hp)
                ? hp : enemy.HitPoints.Values.First();

            // Cache projectile once
            if (wolfenstein.Mods.TryGetValue(mod, out var m))
                _projectile = m.Projectiles.FirstOrDefault(p =>
                    Enemy.Weapons.Contains(p.Name));
        }

        public CharacterSprite CharacterSprite => (CharacterSprite)Sprite;

        private void PlaySound(string[] sounds, InGameState state)
        {
            if (sounds.Length == 0) return;
            var name = sounds[Random.Shared.Next(sounds.Length)];
            var key = $"{Mod}:{name}";
            if (state.Wolfenstein.EnemySounds.TryGetValue(key, out var audio))
                AudioPlaybackEngine.Instance.PlaySound(audio);
        }

        public void Alert(InGameState state)
        {
            if (AIState != EnemyAIState.Idle) return;
            AIState = EnemyAIState.Alert;
            _alertTimer = AlertPauseDuration;
            SetAnimation(CharacterAnimationState.STANDING);
            PlaySound(Enemy.AlertSounds, state);

            // Wake up nearby idle enemies
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
            if (_isDying) return;
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
                state.Game.Player.Score += Enemy.Points;
                PlaySound(Enemy.DeathSounds, state);
            }
            CharacterSprite.AnimationState = AnimationState;
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

        private void SetAnimation(CharacterAnimationState anim)
        {
            AnimationState = anim;
            CharacterSprite.AnimationState = anim;
        }

        private bool HasLineOfSight(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > LineOfSightDistance) return false;

            var steps = (int)(dist * 4);
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
                if (state.Game.Map.WorldMap[my][mx] >= 0) return false;
            }
            return true;
        }

        private Projectile? FindProjectile(InGameState state)
        {
            // Find projectile definition matching the enemy's first weapon ammo type
            var weaponName = Enemy.Weapons.FirstOrDefault();
            if (weaponName == null) return null;

            // Look up by ammo type in mod projectiles
            if (!state.Wolfenstein.Mods.TryGetValue(Mod, out var mod)) return null;
            return mod.Projectiles.FirstOrDefault(p =>
                string.Equals(p.Name, weaponName, StringComparison.OrdinalIgnoreCase));
        }

        private ISprite? FindProjectileSprite(Projectile projectile, InGameState state)
        {
            if (state.Wolfenstein.ProjectileSprites.TryGetValue(Mod, out var sprites) &&
                sprites.TryGetValue(projectile.Name, out var ps))
                return ps;
            return null;
        }

        private void TryMoveTowardPlayer(float frameTime, InGameState state)
        {
            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return;

            var nx = dx / dist;
            var ny = dy / dist;
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

        private EnemyAIState _lastAIState = EnemyAIState.Idle;

        private void SetAnimationForState(EnemyAIState state)
        {
            if (state == _lastAIState) return; // only change on transition
            _lastAIState = state;
            switch (state)
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
        private void TryAttack(float frameTime, InGameState state)
        {
            _attackCooldown -= frameTime;
            if (_attackCooldown > 0) return;

            _attackCooldown = AttackCooldownDuration;
            SetAnimation(CharacterAnimationState.ATTACKING);
            _lastAIState = EnemyAIState.Attack;

            if (_projectile == null)
            {
                state.ApplyDamage(5);
                return;
            }

            var distToPlayer = MathF.Sqrt(
                MathF.Pow(state.Game.Player.PosX - X, 2) +
                MathF.Pow(state.Game.Player.PosY - Y, 2));
            var tileDist = (int)distToPlayer;
            var damage = _projectile.GetDamage(tileDist);
            if (damage <= 0) return;

            if (_projectile.AmmoType == AmmoType.MELEE || _projectile.AmmoType == AmmoType.BULLET)
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
                    X, Y, dx / dist, dy / dist,
                    speed: 8f,
                    damage: damage,
                    maxRange: _projectile.RangeMod,
                    isEnemyProjectile: true,
                    sprite: sprite));
            }
        }

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);

            if (_isDying)
            {
                if (CharacterSprite.IsDeathAnimationComplete)
                {
                    SetAnimation(CharacterAnimationState.DEAD);
                    SpawnDrops(state);
                    IsAlive = false;
                }
                return;
            }

            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var distToPlayer = MathF.Sqrt(dx * dx + dy * dy);

            switch (AIState)
            {
                case EnemyAIState.Idle:
                    SetAnimation(CharacterAnimationState.STANDING);
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
                        // Ranged — attack whenever LOS is available
                        if (HasLineOfSight(state))
                            AIState = EnemyAIState.Attack;
                        else
                            TryMoveTowardPlayer(frameTime, state);
                    }
                    else
                    {
                        // Melee — must close to within AttackRange
                        if (distToPlayer <= AttackRange)
                            AIState = EnemyAIState.Attack;
                        else
                            TryMoveTowardPlayer(frameTime, state);
                    }
                    break;

                case EnemyAIState.Attack:
                    if (IsRanged)
                    {
                        if (!HasLineOfSight(state))
                            AIState = EnemyAIState.Chase;
                        else
                            TryAttack(frameTime, state);
                    }
                    else
                    {
                        if (distToPlayer > AttackRange)
                            AIState = EnemyAIState.Chase;
                        else
                            TryAttack(frameTime, state);
                    }
                    break;
            }
        }
    }

    public class ProjectileObject : DynamicObject
    {
        public float DirX { get; }
        public float DirY { get; }
        public float Speed { get; }
        public int Damage { get; }
        public bool IsEnemyProjectile { get; }
        private float _distanceTravelled = 0;
        private readonly float _maxRange;

        public ProjectileObject(float x, float y, float dirX, float dirY,
            float speed, int damage, float maxRange, bool isEnemyProjectile, ISprite sprite)
            : base(x, y, DynamicObjectType.Projectile, sprite)
        {
            DirX = dirX;
            DirY = dirY;
            Speed = speed;
            Damage = damage;
            IsEnemyProjectile = isEnemyProjectile;
            _maxRange = maxRange;
        }

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

            if (my < 0 || my >= state.Game.Map.WorldMap.Length ||
                mx < 0 || mx >= state.Game.Map.WorldMap[0].Length ||
                state.Game.Map.WorldMap[my][mx] >= 0)
            {
                IsAlive = false;
                return;
            }

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
                    if (obj is EnemyObject enemy && enemy.IsAlive &&
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
}*/