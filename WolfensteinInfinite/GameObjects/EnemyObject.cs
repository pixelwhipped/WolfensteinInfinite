using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class EnemyObject : DynamicObject
    {
        public Enemy Enemy { get; }
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; private set; }
        public EnemyAIState AIState { get; private set; } = EnemyAIState.Idle;
        public string Mod { get; }

        private bool IsDying =>  CharacterSprite.AnimationState == CharacterAnimationState.DYING_LEFT || CharacterSprite.AnimationState == CharacterAnimationState.DYING_RIGHT;

        private bool IsCorpse => CharacterSprite.AnimationState == CharacterAnimationState.DEAD;
        private bool IsHit => CharacterSprite.AnimationState == CharacterAnimationState.HIT;
        private bool _isAttacking = false;
        private float _alertTimer = 0f;
        private float _fleeTimer = 0f;
        private EnemyAIState _lastAIState = EnemyAIState.Idle;


        private const float AlertPauseDuration = 0.5f;
        private const float MeleeAttackRange = 1.5f;
        private const float AlertRadius = 5f;
        private const float LineOfSightDistance = 12f;
        private const float FleeDuration = 1.5f;
        private const float FleeHealthThreshold = 0.25f;

        private float WorldSpeed => Enemy.Speed / (512f * 4f);

        private bool IsRanged => _activeWeapon.HasValue && _activeWeapon.Value.projectile.AmmoType != AmmoType.MELEE;

        public float FacingAngle { get; private set; } = 180f;

        private readonly List<(Weapon weapon, Projectile projectile)> _weapons = [];
        private (Weapon weapon, Projectile projectile)? _activeWeapon;


        private float _attackCooldown;
        private float _fireTimer = 0f;        // time since current firing started
        private float _fireShotTimer = 0f;    // time until next shot within a burst
        private float _reactionTimer = 0f;
        // How long the enemy pauses the first time it spots the player before firing.
        // Guards hesitate longer; SS soldiers react faster.
        private float ReactionDelay => Enemy.EnemyType switch
        {
            EnemyType.SS_SOLDIER => 0.2f,
            EnemyType.DOG => 0.1f,
            _ => 0.4f   // Guard, Officer, etc.
        };

        // Whether this enemy type is allowed to flee when badly hurt.
        private bool CanFlee => Enemy.EnemyType switch
        {
            EnemyType.DOG => false,  // dogs always attack
            EnemyType.SS_SOLDIER => false,  // fanatical — never flee
            _ => true
        };

        private float AttackCooldownDuration => _activeWeapon?.weapon.Cooldown ?? 1.5f;
        private float ShotInterval => _activeWeapon.HasValue && _activeWeapon.Value.weapon.FireRate > 0
            ? 1f / _activeWeapon.Value.weapon.FireRate
            : 0.5f;
        private float MaxFireTime => _activeWeapon?.weapon.MaxFireTime ?? 0f;
        private bool IsSustainedFire => MaxFireTime > 0f;
        public EnemyObject(float x, float y, CharacterSprite sprite, Enemy enemy,
            Difficulties difficulty, string mod, Wolfenstein wolfenstein, int level)
            : base(x, y, DynamicObjectType.Enemy, sprite.Clone())
        {
            Enemy = enemy;
            Mod = mod;
            int baseHitPoints = enemy.HitPoints.TryGetValue(difficulty, out int hp)
                ? hp : enemy.HitPoints.Values.First();

            var scale = 1f + level / (level + 10f);
            baseHitPoints = (int)(baseHitPoints * scale);
            HitPoints = baseHitPoints;
            MaxHitPoints = baseHitPoints;
            PointsReward = (int)(enemy.Points * scale);

            _attackCooldown = AttackCooldownDuration;

            if (wolfenstein.Mods.TryGetValue(mod, out var m))
            {
                foreach (var weaponName in enemy.Weapons)
                {
                    var weapon = m.Weapons.FirstOrDefault(w => w.Name == weaponName);
                    if (weapon == null) continue;
                    var projectile = m.Projectiles.FirstOrDefault(p => p.Name == weapon.Projectile);
                    if (projectile == null) continue;
                    _weapons.Add((weapon, projectile));
                }
                _activeWeapon = _weapons.FirstOrDefault();
            }
        }


        public int PointsReward { get; }

        public CharacterSprite CharacterSprite => (CharacterSprite)Sprite;

        private void SelectBestWeapon(float distToPlayer, bool hasLineOfSight)
        {
            if (_weapons.Count <= 1) return;

            (Weapon weapon, Projectile projectile)? best = null;
            int bestScore = -1;

            foreach (var wp in _weapons)
            {
                var isMelee = wp.projectile.AmmoType == AmmoType.MELEE;
                var range = wp.projectile.RangeMod;
                int score = 0;

                if (isMelee)
                {
                    // Prefer melee when close
                    if (distToPlayer <= MeleeAttackRange) score = 100;
                    else score = 0; // useless at range
                }
                else
                {
                    // Prefer ranged when we have LOS and target is in range
                    if (hasLineOfSight && distToPlayer <= range) score = 80;
                    else if (hasLineOfSight) score = 40;
                    else score = 10;
                }

                // Prefer higher damage weapons when scores are equal
                score += wp.projectile.DamageMod;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = wp;
                }
            }

            if (best.HasValue && best.Value.weapon.Name != _activeWeapon?.weapon.Name)
            {
                _activeWeapon = best;
            }
        }

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
            if (distSq > LineOfSightDistance * LineOfSightDistance) return false;

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
        private float _smoothedFacingAngle = 180f;
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
                TryOpenDoorToward(mapX, curY, state); // ← blocked on X, try opening door
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
        private void TryOpenDoorToward(int targetMapX, int targetMapY, InGameState state)
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
            SelectBestWeapon(distToPlayer, HasLineOfSight(state));
            if (_activeWeapon == null) return;

            var projectile = _activeWeapon.Value.projectile;
            var weapon = _activeWeapon.Value.weapon;

            // On cooldown — wait it out
            if (_attackCooldown > 0)    
            {
                _attackCooldown -= frameTime;
                return;
            }
            // Reaction delay — enemy hesitates briefly before opening fire
            if (_reactionTimer > 0)
            {
                _reactionTimer -= frameTime;
                return;
            }   
            // Start or continue firing
            _fireTimer += frameTime;
            _fireShotTimer -= frameTime;

            // Trigger attack animation on first shot
            if (!_isAttacking)
            {
                _isAttacking = true;
                _lastAIState = EnemyAIState.Chase;
                SetAnimationForState(EnemyAIState.Attack);
            }

            // Fire a shot when in the visual fire window (last two frames of attack anim)
            // so damage lands when the weapon is visually extended, not during wind-up.
            if (_fireShotTimer <= 0f && CharacterSprite.IsInAttackFireWindow && IsFacingPlayer(state))
            {
                _fireShotTimer = ShotInterval;
                FireShot(projectile, weapon, distToPlayer, state);
            }
            else if (_fireShotTimer <= 0f)
            {
                // Not yet in fire window — keep ticking so we fire as soon as we reach it
                // without adding extra delay on top of the animation timing.
                _fireShotTimer = 0f;
            }
            
           
            // Check if we need to go on cooldown
                        bool shouldCooldown = IsSustainedFire
                ? _fireTimer >= MaxFireTime          // sustained — cooldown after MaxFireTime
                : _fireTimer >= ShotInterval;        // instant — cooldown after one shot

            if (shouldCooldown)
            {
                _fireTimer = 0f;
                _fireShotTimer = 0f;
                _attackCooldown = AttackCooldownDuration;
                _reactionTimer = ReactionDelay;

                // Immediately return to walking animation — don't wait for
                // IsAttackAnimationComplete which never fires on looping animations.
                _isAttacking = false;
                _lastAIState = EnemyAIState.Chase;
                SetAnimation(CharacterAnimationState.WALKING);
            }
        }
        private float CalculateHitChance(float dist, float rangeMod)
        {
            float t = Math.Clamp(dist / Math.Max(rangeMod, 1f), 0f, 1f);
            return MathHelpers      .Lerp(0.85f, 0.20f, t);
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
            if (!string.IsNullOrEmpty(weapon.Sound))
            {
                var key = $"{Mod}:{weapon.Sound}";
                if (state.Wolfenstein.EnemySounds.TryGetValue(key, out var audio))
                    AudioPlaybackEngine.Instance.PlaySound(audio);
            }

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
            _alertTimer = AlertPauseDuration;
            _reactionTimer = ReactionDelay;
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
                    if (dx * dx + dy * dy > AlertRadius * AlertRadius) continue;
                    other.AIState = EnemyAIState.Alert;
                    other._alertTimer = AlertPauseDuration;
                    other._reactionTimer = other.ReactionDelay;
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
                if (AIState == EnemyAIState.Chase && CanFlee &&
                    (float)HitPoints / MaxHitPoints <= FleeHealthThreshold)
                {
                    AIState = EnemyAIState.Flee;
                    _fleeTimer = FleeDuration;
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
                            _fireTimer = 0f;
                            _fireShotTimer = 0f;
                            AIState = EnemyAIState.Chase;
                        }
                        else
                            TryAttack(frameTime, distToPlayer, state);
                    }
                    else
                    {
                        if (distToPlayer > MeleeAttackRange)
                        {
                            _isAttacking = false;
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