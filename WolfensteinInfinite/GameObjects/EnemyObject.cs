using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;


namespace WolfensteinInfinite.GameObjects
{
    public class EnemyWeaponObject
    {
        public float AttackCooldownDuration { get; init; }
        public float ShotInterval { get; init; }
        public float MaxFireTime { get; init; }
        //public bool IsSustainedFire { get; init; }
        public bool IsRanged { get; init; }
        public Weapon Weapon { get; init; }
        public Projectile Projectile { get; init; }
        public float AttackCooldown { get; set; }
        public float FireTimer { get; set; } = 0f;   // total time spent firing (for sustained fire cutoff)
        //public float ShotTimer { get; set; } = 0f;   // countdown between shots within a burst
    }

    public class EnemyObject : DynamicObject
    {
        public string Mod { get; }
        public Enemy Enemy { get; }
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; private set; }
        public int PointsReward { get; init; }
        public EnemyAIState AIState { get; private set; } = EnemyAIState.Idle;
        private bool IsDying => CharacterSprite.AnimationState == CharacterAnimationState.DYING_LEFT || CharacterSprite.AnimationState == CharacterAnimationState.DYING_RIGHT;
        private bool IsCorpse => CharacterSprite.AnimationState == CharacterAnimationState.DEAD;
        private bool IsHit => CharacterSprite.AnimationState == CharacterAnimationState.HIT;
        private float WorldSpeed { get; init; }

        private EnemyAIState _lastAIState = EnemyAIState.Idle;

        public float FacingAngle { get; private set; } = 180f;
        private bool IsRanged => Weapons.Any(w => w.IsRanged);
        private EnemyWeaponObject[] Weapons { get; init; }
        private float LineOfSightDistanceSquared { get; init; }

        private float _reactionTimer = 0f;
        private float _alertTimer = 0f;
        private float _fleeTimer = 0f;
        private float _smoothedFacingAngle = 180f;

        public CharacterSprite CharacterSprite { get; init; }
        public EnemyObject(float x, float y, CharacterSprite sprite, Enemy enemy,
            Difficulties difficulty, string mod, Wolfenstein wolfenstein, int level)
            : base(x, y, DynamicObjectType.Enemy, sprite.Clone())
        {
            Enemy = enemy;
            Mod = mod;
            CharacterSprite = (CharacterSprite)Sprite;
            int baseHitPoints = enemy.HitPoints.TryGetValue(difficulty, out int hp)
                ? hp : enemy.HitPoints.Values.First();

            var scale = 1f + level / (level + 10f);
            baseHitPoints = (int)(baseHitPoints * scale);
            HitPoints = baseHitPoints;
            MaxHitPoints = baseHitPoints;
            PointsReward = (int)(enemy.Points * scale);
            WorldSpeed = Enemy.Speed / (512f * 4f);

            var weaponList = new List<EnemyWeaponObject>();
            if (wolfenstein.Mods.TryGetValue(mod, out var m))
            {
                foreach (var weaponName in enemy.Weapons)
                {
                    var weapon = m.Weapons.FirstOrDefault(w => w.Name == weaponName);
                    if (weapon == null) continue;
                    var projectile = m.Projectiles.FirstOrDefault(p => p.Name == weapon.Projectile);
                    if (projectile == null) continue;
                    var maxFireTime = weapon.MaxFireTime;
                    weaponList.Add(new EnemyWeaponObject
                    {
                        Weapon = weapon,
                        Projectile = projectile,
                        MaxFireTime = maxFireTime,
                        ShotInterval = weapon.FireRate > 0 ? 1f / weapon.FireRate : 0.5f,
                        IsRanged = projectile.AmmoType != AmmoType.MELEE,
                        AttackCooldownDuration = weapon.Cooldown > 0 ? weapon.Cooldown : 1.5f,
                        AttackCooldown = 0f,
                    });
                }
            }
            Weapons = weaponList.ToArray();
            LineOfSightDistanceSquared = Enemy.LineOfSightDistance * Enemy.LineOfSightDistance;
        }

        private void PlaySound(string sounds, InGameState state)
        {
            var key = $"{Mod}:{sounds}";
            if (state.Wolfenstein.EnemySounds.TryGetValue(key, out var audio))
                AudioPlaybackEngine.Instance.PlaySound(audio);
        }
        private void PlaySound(string[] sounds, InGameState state)
        {
            if (sounds.Length == 0) return;
            var name = sounds[Random.Shared.Next(sounds.Length)];
            PlaySound(name, state);
        }

        private void SetAnimation(CharacterAnimationState anim)
        {
            if (CharacterSprite.AnimationState == anim) return;
            if (!CharacterSprite.HasAnimation(anim)) return;
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
        private bool IsFacingPlayer(InGameState state)
        {
            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var angleToPlayer = MathF.Atan2(dy, dx) * (180f / MathF.PI);
            angleToPlayer = (angleToPlayer + 360f) % 360f;

            var diff = MathF.Abs(angleToPlayer - FacingAngle) % 360f;
            if (diff > 180f) diff = 360f - diff;

            // Within 45 degrees is close enough to "facing"
            return diff <= 45f;
        }

        private bool HasLineOfSight(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var distSq = dx * dx + dy * dy;
            var dist = MathF.Sqrt(distSq);
            if (distSq > LineOfSightDistanceSquared) return false;

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

            // Facing angle smoothing
            var targetAngle = MathF.Atan2(ny, nx) * (180f / MathF.PI);
            targetAngle = (targetAngle + 360f) % 360f;
            var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
            if (angleDiff > 180f) angleDiff = 360f - angleDiff;
            if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
            FacingAngle = _smoothedFacingAngle;

            var speed = WorldSpeed * frameTime;
            var newX = X + nx * speed;
            var newY = Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)X;
            var curY = (int)Y;

            const float Radius = 0.3f;

            // Check passable using corners of bounding box rather than centre tile
            bool CanMoveX(float tx, float ty) =>
                IsPassable(state, (int)(tx - Radius), (int)ty) &&
                IsPassable(state, (int)(tx + Radius), (int)ty);

            bool CanMoveY(float tx, float ty) =>
                IsPassable(state, (int)tx, (int)(ty - Radius)) &&
                IsPassable(state, (int)tx, (int)(ty + Radius));

            bool movedX = false, movedY = false;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveX(newX, curY))
            {
                X = newX;
                movedX = true;
            }
            else
            {
                TryOpenDoorToward(mapX, curY, state); // ← blocked on X, try opening door
            }
            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveY(curX, newY))
            {
                Y = newY;
                movedY = true;
            }
            else
            {
                TryOpenDoorToward(curX, mapY, state);
            }

            // Corner nudge — if completely blocked try sliding along either axis
            if (!movedX && !movedY)
            {
                if (MathF.Abs(nx) > MathF.Abs(ny) && CanMoveX(newX, curY))
                    X = newX;
                else if (CanMoveY(curX, newY))
                    Y = newY;
            }
        }
        private static void TryOpenDoorToward(int targetMapX, int targetMapY, InGameState state)
        {
            // Check the tile the enemy is trying to enter
            if (targetMapY < 0 || targetMapY >= state.Game.Map.WorldMap.Length ||
                targetMapX < 0 || targetMapX >= state.Game.Map.WorldMap[0].Length) return;

            if (state.Game.Map.WorldMap[targetMapY][targetMapX] != InGameState.DOOR_TILE) return;

            var door = state.Game.Map.Doors.FirstOrDefault(d => d.X == targetMapX && d.Y == targetMapY);
            if (door == null) return;
            if (door.IsFake) return;
            if (door.TextureIndex == 3) return; // prison doors stay shut for enemies
            if (door.IsLocked) return;          // locked doors need the key
            if (door.IsOpening || door.OpenAmount > 0f) return; // already opening/open

            door.IsOpening = true;
        }
        private void TryMoveAwayFromPlayer(float frameTime, InGameState state)
        {
            var dx = X - state.Game.Player.PosX;
            var dy = Y - state.Game.Player.PosY;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return;

            var nx = dx / dist;
            var ny = dy / dist;

            // Facing angle smoothing (opposite direction)
            var targetAngle = MathF.Atan2(ny, nx) * (180f / MathF.PI);
            targetAngle = (targetAngle + 360f) % 360f;
            var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
            if (angleDiff > 180f) angleDiff = 360f - angleDiff;
            if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
            FacingAngle = _smoothedFacingAngle;

            var speed = WorldSpeed * frameTime * 1.5f; // Move faster when fleeing
            var newX = X + nx * speed;
            var newY = Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)X;
            var curY = (int)Y;

            const float Radius = 0.3f;

            bool CanMoveX(float tx, float ty) =>
                IsPassable(state, (int)(tx - Radius), (int)ty) &&
                IsPassable(state, (int)(tx + Radius), (int)ty);

            bool CanMoveY(float tx, float ty) =>
                IsPassable(state, (int)tx, (int)(ty - Radius)) &&
                IsPassable(state, (int)tx, (int)(ty + Radius));

            bool movedX = false, movedY = false;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveX(newX, curY))
            {
                X = newX;
                movedX = true;
            }
            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveY(curX, newY))
            {
                Y = newY;
                movedY = true;
            }

            // Corner nudge — if completely blocked try sliding along either axis
            if (!movedX && !movedY)
            {
                if (MathF.Abs(nx) > MathF.Abs(ny) && CanMoveX(newX, curY))
                    X = newX;
                else if (CanMoveY(curX, newY))
                    Y = newY;
            }
        }
        private static bool IsPassable(InGameState state, int mapX, int mapY)
        {
            if (mapY < 0 || mapY >= state.Game.Map.WorldMap.Length) return false;
            if (mapX < 0 || mapX >= state.Game.Map.WorldMap[0].Length) return false;

            // Pushwalls always block
            if (state.Game.Map.PushWalls.Any(w => (int)w.X == mapX && (int)w.Y == mapY)) return false;

            var tile = state.Game.Map.WorldMap[mapY][mapX];
            if (tile == MapSection.ClosedSectionInterior) return true;
            if (tile == InGameState.DOOR_TILE)
            {
                var door = state.Game.Map.Doors.FirstOrDefault(d => d.X == mapX && d.Y == mapY);
                return door != null && door.OpenAmount >= 0.5f;
            }
            return false;
        }
        private ProjectileSprite? FindProjectileSprite(Projectile projectile, InGameState state)
        {
            if (state.Wolfenstein.ProjectileSprites.TryGetValue(Mod, out var sprites) &&
                sprites.TryGetValue(projectile.Name, out var ps))
                return ps;
            return null;
        }

        private void UpdateFacingAngleTowardPlayer(InGameState state)
        {
            var fdx = state.Game.Player.PosX - X;
            var fdy = state.Game.Player.PosY - Y;
            var fdist = MathF.Sqrt(fdx * fdx + fdy * fdy);
            if (fdist <= 0.001f) return;
            var targetAngle = MathF.Atan2(fdy, fdx) * (180f / MathF.PI);
            targetAngle = (targetAngle + 360f) % 360f;
            var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
            if (angleDiff > 180f) angleDiff = 360f - angleDiff;
            if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
            FacingAngle = _smoothedFacingAngle;
        }

        private void EndWeaponAttack(EnemyWeaponObject w)
        {
            w.AttackCooldown = w.AttackCooldownDuration;
            w.FireTimer = 0f;
            //w.ShotTimer = 0f;
        }
        private void UpdateWeapons(float frameTime)
        {
            // Tick all weapon attack cooldowns
            foreach (var w in Weapons)
                if (w.AttackCooldown > 0f) w.AttackCooldown -= frameTime;
        }


        private static float CalculateHitChance(float dist, float rangeMod)
        {
            float t = Math.Clamp(dist / Math.Max(rangeMod, 1f), 0f, 1f);
            return MathHelpers.Lerp(0.95f, 0.45f, t);
        }

        private void FireShot(Projectile projectile, Weapon weapon, float distToPlayer, InGameState state)
        {
            if (!string.IsNullOrEmpty(weapon.Sound)) PlaySound($"{Mod}:{weapon.Sound}", state);

            var tileDist = projectile.AmmoType == AmmoType.MELEE ? 0 : (int)distToPlayer;
            var damage = projectile.GetDamage(tileDist);
            if (damage <= 0) return;

            if (projectile.AmmoType == AmmoType.MELEE)
            {
                state.ApplyDamage(damage);
                return;
            }

            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist <= 0) return;

            var sprite = FindProjectileSprite(projectile, state);
            if (sprite == null)
            {
                float hitChance = IsFacingPlayer(state) ? CalculateHitChance(distToPlayer, projectile.RangeMod) : 0;
                if (Random.Shared.NextSingle() > hitChance) return;
                state.ApplyDamage(damage);
                return;
            }

            state.DynamicObjects.Add(new ProjectileObject(
                X, Y, dx / dist, dy / dist,
                speed: 8f, damage: damage,
                maxRange: projectile.RangeMod,
                isEnemyProjectile: true,
                sprite: sprite));
        }

        public void Alert(InGameState state)
        {
            if (AIState != EnemyAIState.Idle) return;
            AIState = EnemyAIState.Alert;
            _alertTimer = Enemy.AlertPauseDuration;
            _reactionTimer = Enemy.ReactionDelay;
            SetAnimation(CharacterAnimationState.STANDING);
            PlaySound(Enemy.AlertSounds, state);

            // Iterative flood — no recursion risk
            var toAlert = new Queue<EnemyObject>();
            toAlert.Enqueue(this);
            while (toAlert.Count > 0)
            {
                var current = toAlert.Dequeue();
                foreach (var obj in state.DynamicObjects)
                {
                    if (obj is not EnemyObject other || other.AIState != EnemyAIState.Idle) continue;
                    var dx = other.X - current.X;
                    var dy = other.Y - current.Y;
                    if (dx * dx + dy * dy > Enemy.AlertRadius * Enemy.AlertRadius) continue;
                    other.AIState = EnemyAIState.Alert;
                    other._alertTimer = Enemy.AlertPauseDuration;
                    other._reactionTimer = other.Enemy.ReactionDelay;
                    other.SetAnimation(CharacterAnimationState.STANDING);
                    toAlert.Enqueue(other);
                }
            }
        }

        public void TakeDamage(int amount, InGameState state)
        {
            if (IsDying || IsCorpse) return;
            Alert(state);
            HitPoints -= amount;
            if (HitPoints <= 0)
            {
                HitPoints = 0;
                AIState = EnemyAIState.Dead;
                SpawnDrops(state);
                var dyingOptions = new List<CharacterAnimationState>();
                if (CharacterSprite.HasAnimation(CharacterAnimationState.DYING_LEFT))
                    dyingOptions.Add(CharacterAnimationState.DYING_LEFT);
                if (CharacterSprite.HasAnimation(CharacterAnimationState.DYING_RIGHT))
                    dyingOptions.Add(CharacterAnimationState.DYING_RIGHT);

                if (dyingOptions.Count > 0)
                    SetAnimation(dyingOptions[Random.Shared.Next(dyingOptions.Count)]);
                else
                {
                    if (CharacterSprite.HasAnimation(CharacterAnimationState.DEAD))
                        SetAnimation(CharacterAnimationState.DEAD);
                }
                state.AddToScore(PointsReward);
                state.Game.Map.LevelScore += PointsReward;
                PlaySound(Enemy.DeathSounds, state);
                state.OnEnemyKilled();
            }
            else
            {
                // Start fleeing if below health threshold and not already fleeing
                if (AIState == EnemyAIState.Chase && Enemy.CanFlee &&
                    (float)HitPoints / MaxHitPoints <= Enemy.FleeHealthThreshold)
                {
                    AIState = EnemyAIState.Flee;
                    _fleeTimer = Enemy.FleeDuration;
                    _lastAIState = EnemyAIState.Idle; // Force animation update
                }

                if (CharacterSprite.HasAnimation(CharacterAnimationState.HIT))
                    SetAnimation(CharacterAnimationState.HIT);
            }
        }


        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);
            if (IsDying)
            {
                if (CharacterSprite.IsDeathAnimationComplete)
                {
                    if (CharacterSprite.HasAnimation(CharacterAnimationState.DEAD))
                        SetAnimation(CharacterAnimationState.DEAD);
                }
                return;
            }

            if (IsCorpse) return;
            UpdateWeapons(frameTime);
            if (IsHit && CharacterSprite.IsHitAnimationComplete)
            {
                AIState = EnemyAIState.Chase;
                _lastAIState = EnemyAIState.Idle; // Force animation update
                SetAnimationForState(AIState);
            }

            if (IsHit) return;

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
                        if (distToPlayer <= Enemy.MeleeAttackRange)
                            AIState = EnemyAIState.Attack;
                        else
                            TryMoveTowardPlayer(frameTime, state);
                    }
                    break;

                case EnemyAIState.Attack:
                    if (IsRanged)
                    {
                        if (!IsFacingPlayer(state) || !HasLineOfSight(state))
                        {
                            foreach (var w in Weapons) { w.FireTimer = 0f; }
                            _reactionTimer = Enemy.ReactionDelay;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, distToPlayer, state);
                    }
                    else
                    {
                        if (distToPlayer > Enemy.MeleeAttackRange || !IsFacingPlayer(state))
                        {
                            foreach (var w in Weapons) { w.FireTimer = 0f; }
                            _reactionTimer = Enemy.ReactionDelay;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, distToPlayer, state);
                    }
                    break;

                case EnemyAIState.Flee:
                    _fleeTimer -= frameTime;
                    if (_fleeTimer <= 0)
                    {
                        // Return to chasing
                        AIState = EnemyAIState.Chase;
                    }
                    else
                    {
                        // Move away from player
                        SetAnimation(CharacterAnimationState.WALKING);
                        TryMoveAwayFromPlayer(frameTime, state);
                    }
                    break;
            }
        }

        private void TryAttack(float frameTime, float distToPlayer, InGameState state)
        {
            if (Weapons.Length == 0) return;

            // Keep facing the player while attacking
            UpdateFacingAngleTowardPlayer(state);

            // All weapons still cooling down — wait
            if (Weapons.All(w => w.AttackCooldown > 0f)) return;

            // Reaction delay before committing to attack
            if (_reactionTimer > 0f)
            {
                _reactionTimer -= frameTime;
                return;
            }

            // Start the attack animation if not already in it
            if (CharacterSprite.AnimationState != CharacterAnimationState.ATTACKING)
            {
                _lastAIState = EnemyAIState.Chase;
                SetAnimationForState(EnemyAIState.Attack);
                foreach (var w in Weapons) w.FireTimer = 0f;
            }

            bool inFireFrame = CharacterSprite.IsInAttackFireFrames(Enemy.FireFrames);
            foreach (var w in Weapons)
            {
                if (w.AttackCooldown > 0f) continue;
                w.FireTimer += frameTime;
                if (inFireFrame)
                {
                    int shots = Math.Max(1, (int)MathF.Round(w.Weapon.FireRate));
                    for (int i = 0; i < shots; i++)
                        FireShot(w.Projectile, w.Weapon, distToPlayer, state);
                }
                if (w.FireTimer >= w.MaxFireTime)
                    EndWeaponAttack(w);
            }
        }
        private void SpawnDrops(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);

            // Nudge 0.3 units toward player so item isn't hidden behind corpse
            float dropX = X;
            float dropY = Y;
            if (dist > 0.001f)
            {
                dropX += (dx / dist) * 0.3f;
                dropY += (dy / dist) * 0.3f;
            }

            foreach (var (itemName, probability) in Enemy.DropItemProbability)
            {
                if (Random.Shared.Next(100) >= probability) continue;
                var kvp = state.Wolfenstein.PickupItemTypes
                    .FirstOrDefault(p => p.Value.Name == itemName);
                if (kvp.Value == null) continue;
                if (!state.Wolfenstein.PickupItems.TryGetValue(kvp.Key, out var texture)) continue;
                state.DynamicObjects.Add(
                    new PickupItemObject(dropX, dropY, new StaticSprite(texture), kvp.Value, true));
            }
        }
    }
}

/*
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class EnemyWeaponObject
    {
        public float AttackCooldownDuration { get; init; }
        public float ShotInterval { get; init; }
        public float MaxFireTime { get; init; }
        public bool IsSustainedFire { get; init; }
        public Weapon Weapon { get; init; }
        public Projectile Projectile { get; init; }
        public float AttackCooldown { get; set; }
        public float FireTimer { get; set; } = 0f;        // time since current firing started
    }
    public class EnemyObject : DynamicObject
    {       
        public string Mod { get; }
        public Enemy Enemy { get; }
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; private set; }
        public int PointsReward { get; init; }
        public EnemyAIState AIState { get; private set; } = EnemyAIState.Idle;        
        private bool IsDying =>  CharacterSprite.AnimationState == CharacterAnimationState.DYING_LEFT || CharacterSprite.AnimationState == CharacterAnimationState.DYING_RIGHT;
        private bool IsCorpse => CharacterSprite.AnimationState == CharacterAnimationState.DEAD;
        private bool IsHit => CharacterSprite.AnimationState == CharacterAnimationState.HIT;
        private float WorldSpeed { get; init; }        

        private EnemyAIState _lastAIState = EnemyAIState.Idle;

        public float FacingAngle { get; private set; } = 180f;
        private bool IsRanged { get; init; }
        private readonly List<(Weapon weapon, Projectile projectile)> Weapons = [];
        private (Weapon weapon, Projectile projectile)? PrimaryWeapon { get; init; }
        private float AttackCooldownDuration { get; init; }
        private float ShotInterval { get; init; }
        private float MaxFireTime { get; init; }
        private bool IsSustainedFire { get; init; }
        private float LineOfSightDistanceSquared { get; init; }

        private float _attackCooldown;
        private float _fireTimer = 0f;        // time since current firing started
        private float _fireShotTimer = 0f;    // time until next shot within a burst
        private float _reactionTimer = 0f;
        private bool _isAttacking = false;
        private bool _hasFiredThisAttack = false;
        private float _alertTimer = 0f;
        private float _fleeTimer = 0f;
        private float _smoothedFacingAngle = 180f;
        
        public CharacterSprite CharacterSprite { get; init; }
        public EnemyObject(float x, float y, CharacterSprite sprite, Enemy enemy,
            Difficulties difficulty, string mod, Wolfenstein wolfenstein, int level)
            : base(x, y, DynamicObjectType.Enemy, sprite.Clone())
        {
            Enemy = enemy;
            Mod = mod;
            CharacterSprite = (CharacterSprite)Sprite;
            int baseHitPoints = enemy.HitPoints.TryGetValue(difficulty, out int hp)
                ? hp : enemy.HitPoints.Values.First();

            var scale = 1f + level / (level + 10f);
            baseHitPoints = (int)(baseHitPoints * scale);
            HitPoints = baseHitPoints;
            MaxHitPoints = baseHitPoints;
            PointsReward = (int)(enemy.Points * scale);
            WorldSpeed = Enemy.Speed / (512f * 4f);

            if (wolfenstein.Mods.TryGetValue(mod, out var m))
            {
                foreach (var weaponName in enemy.Weapons)
                {
                    var weapon = m.Weapons.FirstOrDefault(w => w.Name == weaponName);
                    if (weapon == null) continue;
                    var projectile = m.Projectiles.FirstOrDefault(p => p.Name == weapon.Projectile);
                    if (projectile == null) continue;
                    Weapons.Add((weapon, projectile));
                }
                PrimaryWeapon = Weapons.FirstOrDefault();
            }
            MaxFireTime = PrimaryWeapon?.weapon.MaxFireTime ?? 0f;
            IsSustainedFire = MaxFireTime > 0f;
            ShotInterval = PrimaryWeapon.HasValue && PrimaryWeapon.Value.weapon.FireRate > 0
            ? 1f / PrimaryWeapon.Value.weapon.FireRate
            : 0.5f;
            IsRanged = PrimaryWeapon.HasValue && PrimaryWeapon.Value.projectile.AmmoType != AmmoType.MELEE;
            AttackCooldownDuration = PrimaryWeapon?.weapon.Cooldown ?? 1.5f;
            _attackCooldown = AttackCooldownDuration;
            LineOfSightDistanceSquared = Enemy.LineOfSightDistance * Enemy.LineOfSightDistance;
        }

        private void PlaySound(string sounds, InGameState state)
        {
            var key = $"{Mod}:{sounds}";
            if (state.Wolfenstein.EnemySounds.TryGetValue(key, out var audio))
                AudioPlaybackEngine.Instance.PlaySound(audio);
        }
        private void PlaySound(string[] sounds, InGameState state)
        {
            if (sounds.Length == 0) return;
            var name = sounds[Random.Shared.Next(sounds.Length)];
            PlaySound(name, state);
        }

        private void SetAnimation(CharacterAnimationState anim)
        {
            if (CharacterSprite.AnimationState == anim) return;
            if (!CharacterSprite.HasAnimation(anim)) return;
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
            var distSq = dx * dx + dy * dy;
            var dist = MathF.Sqrt(distSq);
            if (distSq > LineOfSightDistanceSquared) return false;

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

            // Facing angle smoothing
            var targetAngle = MathF.Atan2(ny, nx) * (180f / MathF.PI);
            targetAngle = (targetAngle + 360f) % 360f;
            var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
            if (angleDiff > 180f) angleDiff = 360f - angleDiff;
            if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
            FacingAngle = _smoothedFacingAngle;

            var speed = WorldSpeed * frameTime;
            var newX = X + nx * speed;
            var newY = Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)X;
            var curY = (int)Y;

            const float Radius = 0.3f;

            // Check passable using corners of bounding box rather than centre tile
            bool CanMoveX(float tx, float ty) =>
                IsPassable(state, (int)(tx - Radius), (int)ty) &&
                IsPassable(state, (int)(tx + Radius), (int)ty);

            bool CanMoveY(float tx, float ty) =>
                IsPassable(state, (int)tx, (int)(ty - Radius)) &&
                IsPassable(state, (int)tx, (int)(ty + Radius));

            bool movedX = false, movedY = false;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveX(newX, curY))
            {
                X = newX;
                movedX = true;
            }
            else
            {
                TryOpenDoorToward(mapX, curY, state); // ← blocked on X, try opening door
            }
            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveY(curX, newY))
            {
                Y = newY;
                movedY = true;
            }
            else
            {
                TryOpenDoorToward(curX, mapY, state); 
            }

            // Corner nudge — if completely blocked try sliding along either axis
            if (!movedX && !movedY)
            {
                if (MathF.Abs(nx) > MathF.Abs(ny) && CanMoveX(newX, curY))
                    X = newX;
                else if (CanMoveY(curX, newY))
                    Y = newY;
            }           
        }
        private static void TryOpenDoorToward(int targetMapX, int targetMapY, InGameState state)
        {
            // Check the tile the enemy is trying to enter
            if (targetMapY < 0 || targetMapY >= state.Game.Map.WorldMap.Length ||
                targetMapX < 0 || targetMapX >= state.Game.Map.WorldMap[0].Length) return;

            if (state.Game.Map.WorldMap[targetMapY][targetMapX] != InGameState.DOOR_TILE) return;

            var door = state.Game.Map.Doors.FirstOrDefault(d => d.X == targetMapX && d.Y == targetMapY);
            if (door == null) return;
            if (door.IsFake) return;
            if (door.TextureIndex == 3) return; // prison doors stay shut for enemies
            if (door.IsLocked) return;          // locked doors need the key
            if (door.IsOpening || door.OpenAmount > 0f) return; // already opening/open

            door.IsOpening = true;
        }
        private void TryMoveAwayFromPlayer(float frameTime, InGameState state)
        {
            var dx = X - state.Game.Player.PosX;
            var dy = Y - state.Game.Player.PosY;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return;

            var nx = dx / dist;
            var ny = dy / dist;

            // Facing angle smoothing (opposite direction)
            var targetAngle = MathF.Atan2(ny, nx) * (180f / MathF.PI);
            targetAngle = (targetAngle + 360f) % 360f;
            var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
            if (angleDiff > 180f) angleDiff = 360f - angleDiff;
            if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
            FacingAngle = _smoothedFacingAngle;

            var speed = WorldSpeed * frameTime * 1.5f; // Move faster when fleeing
            var newX = X + nx * speed;
            var newY = Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)X;
            var curY = (int)Y;

            const float Radius = 0.3f;

            bool CanMoveX(float tx, float ty) =>
                IsPassable(state, (int)(tx - Radius), (int)ty) &&
                IsPassable(state, (int)(tx + Radius), (int)ty);

            bool CanMoveY(float tx, float ty) =>
                IsPassable(state, (int)tx, (int)(ty - Radius)) &&
                IsPassable(state, (int)tx, (int)(ty + Radius));

            bool movedX = false, movedY = false;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveX(newX, curY))
            {
                X = newX;
                movedX = true;
            }
            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length &&
                CanMoveY(curX, newY))
            {
                Y = newY;
                movedY = true;
            }

            // Corner nudge — if completely blocked try sliding along either axis
            if (!movedX && !movedY)
            {
                if (MathF.Abs(nx) > MathF.Abs(ny) && CanMoveX(newX, curY))
                    X = newX;
                else if (CanMoveY(curX, newY))
                    Y = newY;
            }
        }
        private static bool IsPassable(InGameState state, int mapX, int mapY)
        {
            if (mapY < 0 || mapY >= state.Game.Map.WorldMap.Length) return false;
            if (mapX < 0 || mapX >= state.Game.Map.WorldMap[0].Length) return false;

            // Pushwalls always block
            if (state.Game.Map.PushWalls.Any(w => (int)w.X == mapX && (int)w.Y == mapY)) return false;

            var tile = state.Game.Map.WorldMap[mapY][mapX];
            if (tile == MapSection.ClosedSectionInterior) return true;
            if (tile == InGameState.DOOR_TILE)
            {
                var door = state.Game.Map.Doors.FirstOrDefault(d => d.X == mapX && d.Y == mapY);
                return door != null && door.OpenAmount >= 0.5f;
            }
            return false;
        }
        private ProjectileSprite? FindProjectileSprite(Projectile projectile, InGameState state)
        {
            if (state.Wolfenstein.ProjectileSprites.TryGetValue(Mod, out var sprites) &&
                sprites.TryGetValue(projectile.Name, out var ps))
                return ps;
            return null;
        }
        private void TryAttack(float frameTime, float distToPlayer, InGameState state)
        {
            if (PrimaryWeapon == null) return;

            // Keep facing the player while attacking
            var fdx = state.Game.Player.PosX - X;
            var fdy = state.Game.Player.PosY - Y;
            var fdist = MathF.Sqrt(fdx * fdx + fdy * fdy);
            if (fdist > 0.001f)
            {
                var targetAngle = MathF.Atan2(fdy / fdist, fdx / fdist) * (180f / MathF.PI);
                targetAngle = (targetAngle + 360f) % 360f;
                var angleDiff = MathF.Abs(targetAngle - _smoothedFacingAngle) % 360f;
                if (angleDiff > 180f) angleDiff = 360f - angleDiff;
                if (angleDiff > 15f) _smoothedFacingAngle = targetAngle;
                FacingAngle = _smoothedFacingAngle;
            }

            // On cooldown — wait it out
            if (_attackCooldown > 0)
            {
                _attackCooldown -= frameTime;
                return;
            }

            // Reaction delay
            if (_reactionTimer > 0)
            {
                _reactionTimer -= frameTime;
                return;
            }

            // Trigger attack animation
            if (!_isAttacking)
            {
                _isAttacking = true;
                _hasFiredThisAttack = false; // Reset for new attack
                _lastAIState = EnemyAIState.Chase;
                SetAnimationForState(EnemyAIState.Attack);
            }

            _fireTimer += frameTime;
            _fireShotTimer -= frameTime;

            bool inFireFrame = CharacterSprite.IsInAttackFireFrames(Enemy.FireFrames);
            bool isMelee = PrimaryWeapon.Value.projectile.AmmoType == AmmoType.MELEE;

            // Melee: if player moved out of range during attack, return to chase
            if (isMelee && distToPlayer > Enemy.MeleeAttackRange)
            {
                _fireTimer = 0f;
                _fireShotTimer = 0f;
                _attackCooldown = AttackCooldownDuration;
                _reactionTimer = Enemy.ReactionDelay;
                _isAttacking = false;
                _hasFiredThisAttack = false;
                _lastAIState = EnemyAIState.Chase;
                SetAnimation(CharacterAnimationState.WALKING);
                return;
            }

            // Melee skips facing check — dog is already on top of the player
            // Only fire once per attack sequence
            bool canFire = inFireFrame && _fireShotTimer <= 0f && !_hasFiredThisAttack &&
                           (isMelee || IsFacingPlayer(state));

            if (canFire)
            {
                _hasFiredThisAttack = true; // Mark that we fired
                _fireShotTimer = ShotInterval;

                // Fire every weapon in the list — bosses with dual weapons fire both
                foreach (var (weapon, projectile) in Weapons)
                    FireShot(projectile, weapon, distToPlayer, state);
            }
            else if (_fireShotTimer <= 0f)
            {
                _fireShotTimer = 0f;
            }

            // Wait for attack animation to complete before returning to walking
            if (!IsSustainedFire && CharacterSprite.IsAttackAnimationComplete)
            {
                _fireTimer = 0f;
                _fireShotTimer = 0f;
                _attackCooldown = AttackCooldownDuration;
                _reactionTimer = Enemy.ReactionDelay;
                _isAttacking = false;
                _hasFiredThisAttack = false;
                _lastAIState = EnemyAIState.Chase;
                SetAnimation(CharacterAnimationState.WALKING);
                return;
            }

            // Sustained fire cooldown
            if (IsSustainedFire && _fireTimer >= MaxFireTime)
            {
                _fireTimer = 0f;
                _fireShotTimer = 0f;
                _attackCooldown = AttackCooldownDuration;
                _reactionTimer = Enemy.ReactionDelay;
                _isAttacking = false;
                _hasFiredThisAttack = false;
                _lastAIState = EnemyAIState.Chase;
                SetAnimation(CharacterAnimationState.WALKING);
            }
        }       
        private static float CalculateHitChance(float dist, float rangeMod)
        {
            float t = Math.Clamp(dist / Math.Max(rangeMod, 1f), 0f, 1f);
            return MathHelpers.Lerp(0.95f, 0.45f, t);
        }
        private bool IsFacingPlayer(InGameState state)
        {
            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var angleToPlayer = MathF.Atan2(dy, dx) * (180f / MathF.PI);
            angleToPlayer = (angleToPlayer + 360f) % 360f;

            var diff = MathF.Abs(angleToPlayer - FacingAngle) % 360f;
            if (diff > 180f) diff = 360f - diff;

            // Within 45 degrees is close enough to "facing"
            return diff <= 45f;
        }
        private void FireShot(Projectile projectile, Weapon weapon, float distToPlayer, InGameState state)
        {
            if (!string.IsNullOrEmpty(weapon.Sound)) PlaySound($"{Mod}:{weapon.Sound}", state);
            
            var tileDist = projectile.AmmoType == AmmoType.MELEE ? 0 : (int)distToPlayer;
            var damage = projectile.GetDamage(tileDist);
            if (damage <= 0) return;

            if (projectile.AmmoType == AmmoType.MELEE)
            {
                state.ApplyDamage(damage);
                return;
            }
            // Miss chance — longer shots miss more often
            float hitChance = CalculateHitChance(distToPlayer, projectile.RangeMod);
            if (Random.Shared.NextSingle() > hitChance) return;

            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist <= 0) return;

            var sprite = FindProjectileSprite(projectile, state);
            if (sprite == null)
            {
                state.ApplyDamage(damage);
                return;
            }

            state.DynamicObjects.Add(new ProjectileObject(
                X, Y, dx / dist, dy / dist,
                speed: 8f, damage: damage,
                maxRange: projectile.RangeMod,
                isEnemyProjectile: true,
                sprite: sprite));
        }

        public void Alert(InGameState state)
        {
            if (AIState != EnemyAIState.Idle) return;
            AIState = EnemyAIState.Alert;
            _alertTimer = Enemy.AlertPauseDuration;
            _reactionTimer = Enemy.ReactionDelay;
            SetAnimation(CharacterAnimationState.STANDING);
            PlaySound(Enemy.AlertSounds, state);

            // Iterative flood — no recursion risk
            var toAlert = new Queue<EnemyObject>();
            toAlert.Enqueue(this);
            while (toAlert.Count > 0)
            {
                var current = toAlert.Dequeue();
                foreach (var obj in state.DynamicObjects)
                {
                    if (obj is not EnemyObject other || other.AIState != EnemyAIState.Idle) continue;
                    var dx = other.X - current.X;
                    var dy = other.Y - current.Y;
                    if (dx * dx + dy * dy > Enemy.AlertRadius * Enemy.AlertRadius) continue;
                    other.AIState = EnemyAIState.Alert;
                    other._alertTimer = Enemy.AlertPauseDuration;
                    other._reactionTimer = other.Enemy.ReactionDelay;
                    other.SetAnimation(CharacterAnimationState.STANDING);
                    toAlert.Enqueue(other);
                }
            }
        }

        public void TakeDamage(int amount, InGameState state)
        {
            if (IsDying || IsCorpse) return;
            Alert(state);
            HitPoints -= amount;
            if (HitPoints <= 0)
            {
                HitPoints = 0;
                AIState = EnemyAIState.Dead;
                SpawnDrops(state);
                var dyingOptions = new List<CharacterAnimationState>();
                if (CharacterSprite.HasAnimation(CharacterAnimationState.DYING_LEFT))
                    dyingOptions.Add(CharacterAnimationState.DYING_LEFT);
                if (CharacterSprite.HasAnimation(CharacterAnimationState.DYING_RIGHT))
                    dyingOptions.Add(CharacterAnimationState.DYING_RIGHT);

                if (dyingOptions.Count > 0)
                    SetAnimation(dyingOptions[Random.Shared.Next(dyingOptions.Count)]);
                else
                {
                    if (CharacterSprite.HasAnimation(CharacterAnimationState.DEAD))
                        SetAnimation(CharacterAnimationState.DEAD);
                }
                state.AddToScore(PointsReward);
                state.Game.Map.LevelScore += PointsReward;
                PlaySound(Enemy.DeathSounds, state);
                state.OnEnemyKilled();
            }
            else
            {
                // Start fleeing if below health threshold and not already fleeing
                if (AIState == EnemyAIState.Chase && Enemy.CanFlee &&
                    (float)HitPoints / MaxHitPoints <= Enemy.FleeHealthThreshold)
                {
                    AIState = EnemyAIState.Flee;
                    _fleeTimer = Enemy.FleeDuration;
                    _lastAIState = EnemyAIState.Idle; // Force animation update
                }

                if (CharacterSprite.HasAnimation(CharacterAnimationState.HIT))
                    SetAnimation(CharacterAnimationState.HIT);
            }
        }


        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);
            if (IsDying)
            {
                if (CharacterSprite.IsDeathAnimationComplete)
                {
                    if (CharacterSprite.HasAnimation(CharacterAnimationState.DEAD))
                        SetAnimation(CharacterAnimationState.DEAD);
                }
                return;
            }

            if (IsCorpse) return;

            if (IsHit && CharacterSprite.IsHitAnimationComplete)
            {
                AIState = EnemyAIState.Chase;
                _lastAIState = EnemyAIState.Idle; // Force animation update
                SetAnimationForState(AIState);
            }

            if (IsHit) return;

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
                        if (distToPlayer <= Enemy.MeleeAttackRange)
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
                            _hasFiredThisAttack = false;
                            _fireTimer = 0f;
                            _fireShotTimer = 0f;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, distToPlayer, state);
                    }
                    else
                    {
                        if (distToPlayer > Enemy.MeleeAttackRange)
                        {
                            _isAttacking = false;
                            _hasFiredThisAttack = false;
                            _fireTimer = 0f;
                            _fireShotTimer = 0f;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, distToPlayer, state);
                    }
                    break;

                case EnemyAIState.Flee:
                    _fleeTimer -= frameTime;
                    if (_fleeTimer <= 0)
                    {
                        // Return to chasing
                        AIState = EnemyAIState.Chase;
                    }
                    else
                    {
                        // Move away from player
                        SetAnimation(CharacterAnimationState.WALKING);
                        TryMoveAwayFromPlayer(frameTime, state);
                    }
                    break;
            }
        }

        private void SpawnDrops(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);

            // Nudge 0.3 units toward player so item isn't hidden behind corpse
            float dropX = X;
            float dropY = Y;
            if (dist > 0.001f)
            {
                dropX += (dx / dist) * 0.3f;
                dropY += (dy / dist) * 0.3f;
            }

            foreach (var (itemName, probability) in Enemy.DropItemProbability)
            {
                if (Random.Shared.Next(100) >= probability) continue;
                var kvp = state.Wolfenstein.PickupItemTypes
                    .FirstOrDefault(p => p.Value.Name == itemName);
                if (kvp.Value == null) continue;
                if (!state.Wolfenstein.PickupItems.TryGetValue(kvp.Key, out var texture)) continue;
                state.DynamicObjects.Add(
                    new PickupItemObject(dropX, dropY, new StaticSprite(texture), kvp.Value, true));
            }
        }
    }
}
*/