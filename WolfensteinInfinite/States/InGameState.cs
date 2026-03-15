//WIP Work in progress
using Melanchall.DryWetMidi.Core;
using SFML.Window;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameHelpers;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.States
{
    public class InGameState : GameState
    {
        public const int DOOR_TILE = -6;
        public Game Game { get; init; }
        private Texture32 HudBuffer { get; init; }
        public LinearPointTween DamageTween { get; init; }
        public LinearPointTween PickupTween { get; init; }
        public WeaponTransition WeaponTransitionState { get; init; }

        public RGBA8[] MapColors;
        // private readonly Texture32 Buffer;
        //1D Zbuffer
        private readonly float[] ZBuffer;

        //arrays used to sort the sprites
        public List<DynamicObject> DynamicObjects { get; private set; } = [];
        private int[] SpriteOrder;
        private float[] SpriteDistance;

        public const int RenderDistance = 10;
        private float PlaneX { get => Game.Player.PlaneX; set => Game.Player.PlaneX = value; }
        private float PlaneY { get => Game.Player.PlaneY; set => Game.Player.PlaneY = value; }

        private readonly Texture32 Floor;
        private readonly Texture32 Ceiling;

        private float[,]? _lightMap;
        private const float LightRadius = 2f;
        private const float LightIntensity = 0.5f; // increase for brighter, decrease for dimmer (0.0 - 1.0)


        private int _lastLightTileX = -1;
        private int _lastLightTileY = -1;

        // Add these fields
        private readonly float _spawnX;
        private readonly float _spawnY;
        private readonly float _spawnDirX;
        private readonly float _spawnDirY;
        private bool _pendingReset = false;
        private float _resetTimer = 0f;
        private const float ResetHoldDuration = 1.5f;

        private float _dynamiteCountdown = 0f;
        private bool _dynamiteCountdownActive = false;
        private const float SecondsPerTile = 1.5f;
        private const float MinCountdownSeconds = 30f;

        private bool _mapVisible = false;
        private readonly bool[][] _visited;

        private bool _exitActivated = false;
        private float _exitDelay = 0f;
        private const float ExitDelayDuration = 2.0f;

        private readonly Tween _exitFadeTween = new(1.5f, null);

        private int _enemiesKilled = 0;
        private int _enemiesTotal = 0;
        private int _itemsCollected = 0;
        private int _itemsTotal = 0;

        public void OnEnemyKilled() => _enemiesKilled++;
        public void OnItemCollected() => _itemsCollected++;
        public InGameState(Wolfenstein wolfenstein, Game game) : base(wolfenstein)
        {
            Game = game;
            HudBuffer = new Texture32(wolfenstein.GameResources.Hud.Width, wolfenstein.GameResources.Hud.Height);
            ReturnState = this;
            NextState = this;
            DamageTween = new(1f, null, [1f, 0f]);
            DamageTween.End();
            PickupTween = new(0.75f, null, [1f, 0f]);
            PickupTween.End();
            WeaponTransitionState = new WeaponTransition(wolfenstein.GetWeapon(game.Player.Weapon), WeaponChanged);
            Wolfenstein.WeaponAnimations[Game.Player.Weapon].OnFire = new Action(DoAttack);

            //PlaneX = 0.0f;   //the 2d raycaster version of camera plane
            //PlaneY = 0.66f;
            (PlaneX, PlaneY) = game.Player.DirX switch
            {
                1f => (0f, 0.66f),   // Facing East
                -1f => (0f, -0.66f),   // Facing West
                _ => game.Player.DirY > 0
                       ? (0.66f, 0f)  // Facing South
                       : (-0.66f, 0f)  // Facing North
            };

            MapColors = GetTextureMapColors();

            ZBuffer = new float[wolfenstein.Graphics.Width];
            SpriteOrder = new int[Game.Map.Decals.Length];
            SpriteDistance = new float[Game.Map.Decals.Length];

            Floor = new Texture32(64, 64);
            Floor.Clear(128, 128, 128);

            Ceiling = new Texture32(64, 64);
            Ceiling.Clear(96, 96, 96);

            RebuildDynamicObjects();
            _spawnX = game.Player.PosX;
            _spawnY = game.Player.PosY;
            _spawnDirX = game.Player.DirX;
            _spawnDirY = game.Player.DirY;

            _visited = new bool[Game.Map.WorldMap.Length][];
            for (int i = 0; i < _visited.Length; i++)
                _visited[i] = new bool[Game.Map.WorldMap[i].Length];

        }

        // Lower to GameState
        private string? _hudMessage = null;
        private float _hudMessageTimer = 0f;
        private const float HudMessageDuration = 3f;

        public void ShowHudMessage(string message)
        {
            _hudMessage = message;
            _hudMessageTimer = HudMessageDuration;
        }

        private void UpdateHudMessage(Texture32 buffer, float frameTime)
        {
            if (_hudMessage == null) return;
            _hudMessageTimer -= frameTime;
            if (_hudMessageTimer <= 0f)
            {
                _hudMessage = null;
                return;
            }
            var (w, h) = Wolfenstein.GameResources.TinyFont.MeasureString(_hudMessage);
            var x = (buffer.Width - w) / 2;
            var y = buffer.Height - (int)(HudBuffer.Height * Wolfenstein.UIScale) - h - 6;
            buffer.RectFill(x - 4, y - 4, w + 8, h + 8, 0, 0, 0, 180);
            buffer.DrawString(x, y, _hudMessage, Wolfenstein.GameResources.TinyFont, RGBA8.YELLOW);
        }
        private void RebuildDynamicObjects()
        {

            DynamicObjects.Clear();

            foreach (var d in Game.Map.Decals)
            {
                var sprite = new StaticSprite(Game.Map.DecalTextures[d.TextureIndex]);
                DynamicObjects.Add(new DecalObject(d, sprite));
            }

            foreach (var d in Game.Map.Items)
            {
                var itemType = Wolfenstein.PickupItemTypes[d.ItemType];
                var sprite = new StaticSprite(Game.Map.ItemTextures[d.TextureIndex]);

                if (itemType.Name == "Radio")
                {
                    // Radio stays on map as interactable — never gets picked up
                    DynamicObjects.Add(new RadioObject(d.X, d.Y, sprite));
                }
                else if (itemType.Name == "DynamiteToPlace")
                {
                    // Find placed sprite
                    var placedKvp = Wolfenstein.PickupItemTypes
                        .FirstOrDefault(p => p.Value.Name == "DynamitePlaced");
                    ISprite placedSprite = placedKvp.Value != null &&
                        Wolfenstein.PickupItems.TryGetValue(placedKvp.Key, out var pt)
                        ? new StaticSprite(pt)
                        : sprite;
                    DynamicObjects.Add(new DynamitePlacementObject(d.X, d.Y, sprite, placedSprite));
                }
                else
                {
                    DynamicObjects.Add(new PickupItemObject(d.X + 0.5f, d.Y + 0.5f, sprite, itemType));
                }
            }

            foreach (var e in Game.Map.Enemies)
            {
                if (!Wolfenstein.CharacterSprites.TryGetValue(e.Mod, out var modSprites)) continue;
                if (!modSprites.TryGetValue(e.EnemyMapId, out var sprite)) continue;
                if (!Wolfenstein.Mods.TryGetValue(e.Mod, out var mod)) continue;
                var enemy = mod.Enemies.FirstOrDefault(en => en.MapID == e.EnemyMapId);
                if (enemy == null) continue;

                DynamicObjects.Add(new EnemyObject(
    e.X + 0.5f, e.Y + 0.5f, sprite, enemy,
    Game.Map.Difficulty, e.Mod, Wolfenstein, Game.Map.Level));
            }

            SpriteOrder = new int[DynamicObjects.Count];
            SpriteDistance = new float[DynamicObjects.Count];

            _enemiesTotal = DynamicObjects.OfType<EnemyObject>().Count();
            _itemsTotal = DynamicObjects.OfType<PickupItemObject>().Count();
        }

        public void RecordHighScore()
        {
            const int MaxEntries = 10;
            var newScore = new HighScore(
                Game.GameId,
                Game.Player.Name,
                Game.Map.Level,
                Game.Player.Score);

            var updated = Wolfenstein.Config.HighScores
                .Append(newScore)
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.Level)
                .Take(MaxEntries)
                .ToArray();

            if (updated.Any(s => s.GameId == Game.GameId))
                Wolfenstein.Config.HighScores = updated;
        }

        public void StartDynamiteCountdown()
        {
            if (_dynamiteCountdownActive) return;

            // Manhattan distance from player to nearest exit
            var dist = Game.Map.Exits
                .Select(e => Math.Abs(e.X - (int)Game.Player.PosX) +
                             Math.Abs(e.Y - (int)Game.Player.PosY))
                .DefaultIfEmpty(20)
                .Min();

            _dynamiteCountdown = Math.Max(dist * SecondsPerTile, MinCountdownSeconds);
            _dynamiteCountdownActive = true;
            ShowHudMessage("DYNAMITE PLACED - GET OUT!");
        }

        private void UpdateDynamiteCountdown(float frameTime)
        {
            if (!_dynamiteCountdownActive) return;
            _dynamiteCountdown -= frameTime;
            if (_dynamiteCountdown <= 0)
            {
                _dynamiteCountdownActive = false;
                Game.Map.ObjectivesComplete[MapFlags.HAS_BOOM] = false;
                ApplyDamage(999); // boom — triggers reset or game over
            }
        }
        public void AutoSave()
        {
            SaveGame.FromGame(Game).Save();
        }

        private void WeaponChanged()
        {
            Game.Player.Weapon = WeaponTransitionState.CurrentWeapon.Name;
            Wolfenstein.WeaponAnimations[Game.Player.Weapon].OnFire = new Action(DoAttack);
        }

        private void DoAttack()
        {
            var weapon = WeaponTransitionState.TransitionWeapon;
            var t = weapon.AmmoType;
            if (Game.Player.Ammo.TryGetValue(t, out int value))
                Game.Player.Ammo[t] = Math.Max(value - 1, 0);
            if (Wolfenstein.WeaponAudio.TryGetValue(weapon.Name, out CachedSound? audio))
                AudioPlaybackEngine.Instance.PlaySound(audio);

            // Find projectile by AmmoType match
            Projectile? projectile = null;
            foreach (var modName in Game.Mods)
            {
                if (!Wolfenstein.Mods.TryGetValue(modName, out var m)) continue;
                projectile = m.Projectiles.FirstOrDefault(p => p.AmmoType == weapon.AmmoType);
                if (projectile != null) break;
            }
            if (projectile == null) return;

            if (weapon.AmmoType == AmmoType.MELEE)
            {
                foreach (var obj in DynamicObjects.OfType<EnemyObject>().Where(e => e.IsAlive))
                {
                    var dx = obj.X - Game.Player.PosX;
                    var dy = obj.Y - Game.Player.PosY;
                    var dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist > projectile.RangeMod) continue;
                    var dot = (dx / dist) * Game.Player.DirX + (dy / dist) * Game.Player.DirY;
                    if (dot > 0.5f)
                        obj.TakeDamage(projectile.GetDamage((int)dist), this);
                }
            }
            else if (weapon.AmmoType == AmmoType.BULLET)
            {
                var rayX = Game.Player.PosX;
                var rayY = Game.Player.PosY;
                var steps = (int)(projectile.RangeMod * 8);
                var stepX = Game.Player.DirX / 8f;
                var stepY = Game.Player.DirY / 8f;

                for (int i = 0; i < steps; i++)
                {
                    rayX += stepX;
                    rayY += stepY;
                    var mx = (int)rayX;
                    var my = (int)rayY;
                    if (my < 0 || my >= Game.Map.WorldMap.Length ||
                        mx < 0 || mx >= Game.Map.WorldMap[0].Length) break;
                    if (Game.Map.WorldMap[my][mx] >= 0) break;

                    var hit = DynamicObjects.OfType<EnemyObject>()
                        .FirstOrDefault(e => e.IsAlive &&
                            (int)e.X == mx && (int)e.Y == my);
                    if (hit != null)
                    {
                        var tileDist = (int)MathF.Sqrt(
                            MathF.Pow(rayX - Game.Player.PosX, 2) +
                            MathF.Pow(rayY - Game.Player.PosY, 2));
                        hit.TakeDamage(projectile.GetDamage(tileDist), this);
                        break;
                    }
                }
            }
            else
            {
                // Physical projectile — SERUM, FLAME, ROCKET
                ISprite? sprite = null;
                foreach (var modName in Game.Mods)
                {
                    if (Wolfenstein.ProjectileSprites.TryGetValue(modName, out var sprites) &&
                        sprites.TryGetValue(projectile.Name, out var ps))
                    {
                        sprite = ps;
                        break;
                    }
                }
                if (sprite == null) return;

                DynamicObjects.Add(new ProjectileObject(
                    Game.Player.PosX, Game.Player.PosY,
                    Game.Player.DirX, Game.Player.DirY,
                    speed: 10f,
                    damage: projectile.GetDamage(0),
                    maxRange: projectile.RangeMod,
                    isEnemyProjectile: false,
                    sprite: sprite));
            }
        }

        private void WeaponTransition(string v)
        {
            if (Game.Player.Weapon == v && !WeaponTransitionState.Transitioning) return;
            Wolfenstein.WeaponAnimations[Game.Player.Weapon].Reset();
            Wolfenstein.WeaponAnimations[v].Reset();
            WeaponTransitionState.TranstionTo(Wolfenstein.GetWeapon(v));
        }

        private bool _pendingExit = false;

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            if (_pendingExit)
            {
                _pendingExit = false;
                return HandleExit();
            }

            if (_pendingReset)
            {
                _resetTimer -= frameTime;
                UpdateScene(buffer, 0f);
                UpdateWeapon(buffer, 0f);
                UpdateHud(buffer, 0f);
                UpdateHudMessage(buffer, frameTime);
                // DamageTween not updated — frozen at Value=1f (full red)
                buffer.RectFill(0, 0, buffer.Width, buffer.Height, 255, 0, 0,
                    (byte)(255 * DamageTween.Value));
                if (_resetTimer <= 0f)
                {
                    _pendingReset = false;
                    ResetGame();
                    DamageTween.Reset(); // restarts from 1f, now fades out cleanly
                }
                return NextState;
            }

            UpdateScene(buffer, frameTime);
            UpdateWeapon(buffer, frameTime);
            UpdateHud(buffer, frameTime);
            if (_exitActivated)
                buffer.RectFill(0, 0, buffer.Width, buffer.Height,
                    0, 0, 0, (byte)(255 * _exitFadeTween.Value));
            DamageTween.Update(frameTime);
            PickupTween.Update(frameTime);
            buffer.RectFill(0, 0, buffer.Width, buffer.Height, 255, 0, 0,
                (byte)(255 * DamageTween.Value));
            return NextState;
        }

        private void UpdatePushWalls(float frameTime)
        {
            foreach (var wall in Game.Map.PushWalls)
                wall.Update(frameTime, Game.Map);
        }

        private LevelStats BuildLevelStats() => new(
    _enemiesKilled, _enemiesTotal,
    _itemsCollected, _itemsTotal,
    Game.Map.PushWalls.Count(w => w.IsComplete),
    Game.Map.PushWalls.Count, Game.Map.LevelScore);

        private LevelCompleteState HandleExit()
        {
            RecordHighScore();
            AutoSave();
            Game.Map.Level++;

            GameState nextLevel;

            if (Game.Map.Level % 10 == 0)
            {
                var specials = Game.Mods
                    .Where(m => Wolfenstein.SpecialMaps.ContainsKey(m))
                    .SelectMany(m => Wolfenstein.SpecialMaps[m].MapSections
                        .Select(s => (Mod: m, Section: s)))
                    .ToArray();

                if (specials.Length > 0)
                {
                    var chosen = specials[Random.Shared.Next(specials.Length)];
                    nextLevel = new SpecialLevelState(
                    Wolfenstein, Game.Player, Game.Map.Difficulty,
                    Game.Map.Level, chosen.Mod, chosen.Section);
                }
                else
                {
                    nextLevel = new GameGenerationState(
                        Wolfenstein, Game.Player, Game.Map.Difficulty, Game.Map.Level);
                }
            }
            else
            {
                nextLevel = new GameGenerationState(
                    Wolfenstein, Game.Player, Game.Map.Difficulty, Game.Map.Level);
            }

            return new LevelCompleteState(Wolfenstein, Game.Player, Game.Map, BuildLevelStats(), nextLevel);
        }

        public void UpdateWeapon(Texture32 buffer, float frameTime)
        {
            WeaponTransitionState.Update(frameTime);

            var texture = Wolfenstein.GetWeaponTexture(WeaponTransitionState.TransitioningOut ? WeaponTransitionState.CurrentWeapon.Name : WeaponTransitionState.TransitionWeapon.Name);
            var w = (int)(texture.Width * Wolfenstein.UIScale);
            var h = (int)(texture.Height * Wolfenstein.UIScale);
            buffer.Blit((int)(((buffer.Width) / 2) - (w / 2)), (int)((buffer.Height) - (h + (int)(HudBuffer.Height * Wolfenstein.UIScale))) + (int)(WeaponTransitionState.CurrentHeightOffset * Wolfenstein.UIScale), w, h, texture);
            if (WeaponTransitionState.Transitioning) return;
            Wolfenstein.WeaponAnimations[Game.Player.Weapon].InLoop = false;
            //need to check if as ammo
            var t = WeaponTransitionState.TransitionWeapon.AmmoType;
            if (!Game.Player.Ammo.TryGetValue(t, out int ammo)) ammo = 1;
            if (Wolfenstein.Graphics.IsKeyDown(Wolfenstein.Config.KeyFire) && ammo > 0)
            {
                Wolfenstein.WeaponAnimations[Game.Player.Weapon].InLoop = true;
                Wolfenstein.WeaponAnimations[Game.Player.Weapon].Update(frameTime);
            }
            else if (!Wolfenstein.Graphics.IsKeyDown(Wolfenstein.Config.KeyFire) && Wolfenstein.WeaponAnimations[Game.Player.Weapon].CurrentFrame != 0)
            {
                Wolfenstein.WeaponAnimations[Game.Player.Weapon].Update(frameTime);
            }
            else
            {
                Wolfenstein.WeaponAnimations[Game.Player.Weapon].Reset();
            }
        }



        public void EndGame()
        {
            RecordHighScore();
            SaveGame.Delete();
            NextState = new MenuState(Wolfenstein, null);
        }
        public void ResetGame()
        {
            // Reset player state — keep score, lives, objectives
            Game.Player.Health = 100;
            Game.Player.Weapon = "Pistol";
            Game.Player.Weapons = ["Knife", "Pistol"];
            Game.Player.Ammo = new Dictionary<AmmoType, int>
    {
        { AmmoType.BULLET, 16 }
    };

            // Restore spawn position and direction
            Game.Player.PosX = _spawnX;
            Game.Player.PosY = _spawnY;
            Game.Player.DirX = _spawnDirX;
            Game.Player.DirY = _spawnDirY;

            // Restore camera plane from direction
            (PlaneX, PlaneY) = Game.Player.DirX switch
            {
                1f => (0f, 0.66f),
                -1f => (0f, -0.66f),
                _ => Game.Player.DirY > 0 ? (0.66f, 0f) : (-0.66f, 0f)
            };

            // Reset weapon animation state
            WeaponTransitionState.TranstionTo(Wolfenstein.GetWeapon("Pistol"));
            Wolfenstein.WeaponAnimations[Game.Player.Weapon].Reset();
            Wolfenstein.WeaponAnimations[Game.Player.Weapon].OnFire = new Action(DoAttack);

            // Rebuild enemies, pickups — push walls and doors keep their current state
            // since objectives are preserved and doors the player opened should stay open
            RebuildDynamicObjects();

            // Invalidate light map so it rebuilds from current state
            InvalidateLightMap();

        }
        public void UpdateHud(Texture32 buffer, float frameTime)
        {

            Texture32 GetFace()
            {
                Animation face;
                if (Game.Player.GodMode) face = Wolfenstein.GameResources.HudFaces[0];
                else if (Game.Player.Health >= 70) face = Wolfenstein.GameResources.HudFaces[1];
                else if (Game.Player.Health >= 55) face = Wolfenstein.GameResources.HudFaces[2];
                else if (Game.Player.Health >= 40) face = Wolfenstein.GameResources.HudFaces[3];
                else if (Game.Player.Health >= 25) face = Wolfenstein.GameResources.HudFaces[4];
                else if (Game.Player.Health >= 15) face = Wolfenstein.GameResources.HudFaces[5];
                else face = Wolfenstein.GameResources.HudFaces[6];
                face.Update(frameTime);
                return face.GetTexture(0);
            }


            HudBuffer.Draw(0, 0, Wolfenstein.GameResources.Hud);
            //Face offset 146,1
            HudBuffer.Draw(146, 1, GetFace());
            var x = 26 - (Wolfenstein.GameResources.NumberFont.MeasureString($"{Game.Map.Level}").Width / 2);
            HudBuffer.DrawString(x, 13, $"{Game.Map.Level}", Wolfenstein.GameResources.NumberFont, null);

            x = 80 - (Wolfenstein.GameResources.NumberFont.MeasureString($"{Game.Player.Score}").Width / 2);
            HudBuffer.DrawString(x, 13, $"{Game.Player.Score}", Wolfenstein.GameResources.NumberFont, null);

            x = 125 - (Wolfenstein.GameResources.NumberFont.MeasureString($"{Game.Player.Lives}").Width / 2);
            HudBuffer.DrawString(x, 13, $"{Game.Player.Lives}", Wolfenstein.GameResources.NumberFont, null);

            x = 200 - Wolfenstein.GameResources.NumberFont.MeasureString($"{Game.Player.Health}").Width;
            HudBuffer.DrawString(x, 13, $"{Game.Player.Health}", Wolfenstein.GameResources.NumberFont, null);

            var t = WeaponTransitionState.TransitionWeapon.AmmoType;
            if (Game.Player.Ammo.TryGetValue(t, out var am))
            {
                x = 231 - (Wolfenstein.GameResources.NumberFont.MeasureString($"{am}").Width / 2);
                HudBuffer.DrawString(x, 13, $"{am}", Wolfenstein.GameResources.NumberFont, null);
            }

            // Current weapon HUD sprite — drawn at x=255, y=4
            if (Wolfenstein.WeaponHudTextures.TryGetValue(WeaponTransitionState.TransitionWeapon.Name, out Texture32? value))
            {
                HudBuffer.Draw(264, 4, value);
            }            
                

            // Objective icons — On when map has objective, flash when complete
            bool HasObj(MapFlags f) => Game.Map.Objectives.ContainsKey(f);
            bool DoneObj(MapFlags f) => Game.Map.ObjectivesComplete.GetValueOrDefault(f);
            // Objective icons — stacked vertically, first at 251,3 then +20
            int objY = 3;
            if (HasObj(MapFlags.HAS_LOCKED_DOOR))
            {
                HudBuffer.Draw(251, objY, HasObj(MapFlags.HAS_LOCKED_DOOR)
                    ? Wolfenstein.GameResources.KeyOn
                    : Wolfenstein.GameResources.KeyOff);
                objY += 20;
            }
            if (HasObj(MapFlags.HAS_BOOM))
            {
                HudBuffer.Draw(251, objY, DoneObj(MapFlags.HAS_BOOM)
                    ? Wolfenstein.GameResources.DynamiteOn
                    : Wolfenstein.GameResources.DynamiteOff);
                objY += 20;
                if (_dynamiteCountdownActive)
                {
                    var countStr = ((int)_dynamiteCountdown).ToString();
                    var (cw, _) = Wolfenstein.GameResources.TinyFont.MeasureString(countStr);
                    HudBuffer.DrawString(241 - cw / 2, 14, countStr,
                        Wolfenstein.GameResources.TinyFont,
                        _dynamiteCountdown < 10f ? RGBA8.RED : RGBA8.YELLOW);
                }
            }
            if (HasObj(MapFlags.HAS_SECRET_MESSAGE))
            {
                HudBuffer.Draw(251, objY, DoneObj(MapFlags.HAS_SECRET_MESSAGE)
                    ? Wolfenstein.GameResources.SecretOn
                    : Wolfenstein.GameResources.SecretOff);
                objY += 20;
            }
            if (HasObj(MapFlags.HAS_POW))
            {
                HudBuffer.Draw(251, objY, DoneObj(MapFlags.HAS_POW)
                    ? Wolfenstein.GameResources.PrisonerOfWarOn
                    : Wolfenstein.GameResources.PrisonerOfWarOff);
            }

            HudBuffer.RectFill(0, 0, HudBuffer.Width, HudBuffer.Height, 255, 255, 255, (byte)(128 * PickupTween.Value));
            (byte[] pixels, byte[] pallet) = Quantization.Quantize32BitAI(HudBuffer.Pixels, 48);
            var b = new Texture8(HudBuffer.Width, HudBuffer.Height, pixels, pallet);
            buffer.Blit(0, buffer.Height - (int)(HudBuffer.Height * Wolfenstein.UIScale), buffer.Width, (int)(HudBuffer.Height * Wolfenstein.UIScale), b);
            Wolfenstein.PreserveColors = b.GetUsedColors();
        }

        private readonly bool DebugMode = true;
        public void OnDebugKeyPresed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.D)
            {
                ApplyDamage(2);
            }
            else if (k.Code == Keyboard.Key.H)
            {
                if (Wolfenstein.PickupItemTypes.TryGetValue(0, out PickupItem? item)) PickupItem(item);
            }
            else if (k.Code == Keyboard.Key.W)
            {
                if (Wolfenstein.PickupItemTypes.TryGetValue(3, out PickupItem? item)) PickupItem(item);
                if (Wolfenstein.PickupItemTypes.TryGetValue(4, out item)) PickupItem(item);
            }
        }

        private bool PickupItem(PickupItem? item)
        {
            if (item == null) return false;
            return item.ItemType switch
            {
                PickupItemType.AMMO => ApplyAmmo(item),
                PickupItemType.HEALTH => ApplyHealth(item),
                PickupItemType.POINTS => ApplyPoints(item),
                PickupItemType.WEAPON => ApplyWeapon(item),
                PickupItemType.LIFE => ApplyLife(item),
                PickupItemType.MISSION_OBJECTIVE => ApplyObjective(item),
                PickupItemType.SPAWNER => false,
                _ => false,
            };
        }

        public void ApplyDamage(int damage)
        {
            if (Game.Player.GodMode) return;
            Game.Player.Health = Math.Max(Game.Player.Health - damage, 0);
            DamageTween.Reset();
            if (Game.Player.Health <= 0)
            {
                Game.Player.Lives--;
                if (Game.Player.Lives < 0)
                {
                    EndGame();
                    return;
                }
                _pendingReset = true;
                _resetTimer = ResetHoldDuration;
            }
        }

        public bool ApplyHealth(PickupItem health)
        {
            if (Game.Player.Health >= 100) return false;
            if (health.Modifier > 0 && Game.Player.Health >= health.Modifier) return false;
            Game.Player.Health = Math.Min(Game.Player.Health + health.Value, 100);
            PickupTween.Reset();
            return true;
        }

        private bool ApplyObjective(PickupItem item)
        {
            switch (item.Name)
            {
                case "Key":
                    Game.Map.Objectives[MapFlags.HAS_LOCKED_DOOR] = true;
                    break;

                case "Secret":
                    Game.Map.Objectives[MapFlags.HAS_SECRET_MESSAGE] = true;
                    // Radio stays on map — player must interact with it to complete
                    break;

                case "Dynamite":
                    Game.Map.Objectives[MapFlags.HAS_BOOM] = true;
                    // Placement spots already in scene — player uses Space to place
                    break;

                case "POW":
                    Game.Map.Objectives[MapFlags.HAS_POW] = true;
                    // Spawn companion at pickup location
                    var powKvp = Wolfenstein.PickupItemTypes
                        .FirstOrDefault(p => p.Value.Name == "POW");
                    if (powKvp.Value != null &&
                        Wolfenstein.PickupItems.TryGetValue(powKvp.Key, out var powTex))
                    {
                        DynamicObjects.Add(new POWCompanionObject(
                            Game.Player.PosX, Game.Player.PosY,
                            new StaticSprite(powTex)));
                    }
                    break;
            }

            PickupTween.Reset();
            return true;
        }
        private bool ApplyLife(PickupItem item)
        {
            Game.Player.Lives += item.Value;
            PickupTween.Reset();
            return true;
        }

        private bool ApplyWeapon(PickupItem item)
        {
            if (!Wolfenstein.PlayerWeapons.TryGetValue(item.Name, out PlayerWeapon? value)) return false;
            if (Game.Player.Weapons.Contains(item.Name)) return false;
            Game.Player.Weapons.Add(item.Name);
            Game.Player.Weapons = [.. Wolfenstein.PlayerWeapons.Values.OrderBy(p => p.PreferedOrder).Where(p => Game.Player.Weapons.Contains(p.Name)).Select(p => p.Name)];
            if (item.Modifier != 0)
            {
                var ammo = value.AmmoType;
                Game.Player.Ammo.TryAdd(ammo, 0);
                Game.Player.Ammo[ammo] = Math.Min(Game.Player.Ammo[ammo] + item.Modifier, 999);
            }
            PickupTween.Reset();
            return true;
        }

        private bool ApplyPoints(PickupItem item)
        {
            Game.Player.Score += item.Value;
            PickupTween.Reset();
            return true;
        }

        private bool ApplyAmmo(PickupItem item)
        {
            if (item.AmmoType is null) return false;
            var t = (AmmoType)item.AmmoType;
            if (Game.Player.Ammo[t] >= 999) return false;
            Game.Player.Ammo[t] = Math.Min(Game.Player.Ammo[t] + item.Value, 999);
            PickupTween.Reset();
            return true;
        }
        public bool TryPickupItem(PickupItem item) => PickupItem(item);
        public override void OnKeyPressed(KeyEventArgs k)
        {

            if (k.Code == Keyboard.Key.Escape || k.Code == Wolfenstein.Config.KeyPause)
            {
                NextState = new PauseState(Wolfenstein, this);
                return;
            }
            if (DebugMode) OnDebugKeyPresed(k);

            if (k.Code == Wolfenstein.Config.KeyWeaponUp)
            {
                var wi = Game.Player.Weapons.IndexOf(WeaponTransitionState.TransitionWeapon.Name);
                if (wi + 1 > Game.Player.Weapons.Count - 1) WeaponTransition(Game.Player.Weapons.First());
                else WeaponTransition(Game.Player.Weapons[wi + 1]);
            }
            else if (k.Code == Wolfenstein.Config.KeyWeaponDown)
            {
                var wi = Game.Player.Weapons.IndexOf(WeaponTransitionState.TransitionWeapon.Name);
                if (wi - 1 < 0) WeaponTransition(Game.Player.Weapons.Last());
                else WeaponTransition(Game.Player.Weapons[wi - 1]);
            }
        }

        public static void SortSprites(int[] order, float[] dist, int amount)
        {
            // Create array of (distance, order) pairs
            var sprites = new (float distance, int order)[amount];
            for (int i = 0; i < amount; i++)
            {
                sprites[i] = (dist[i], order[i]);
            }

            // Sort by distance
            Array.Sort(sprites, (a, b) => a.distance.CompareTo(b.distance));

            // Restore in reverse order (farthest to nearest)
            for (int i = 0; i < amount; i++)
            {
                dist[i] = sprites[amount - i - 1].distance;
                order[i] = sprites[amount - i - 1].order;
            }
        }
        private RGBA8[] GetTextureMapColors()
        {
            var colors = new List<RGBA8>
            {
                new() { R = 0, G = 0, B = 0, A = 255 }
            };
            foreach (var t in Game.Map.WallTextures)
            {
                var ar = 0;
                var ag = 0;
                var ab = 0;
                var c = t.Width * t.Height;
                for (int x = 0; x < t.Width; x++)
                {
                    for (int y = 0; y < t.Height; y++)
                    {
                        t.GetPixel(x, y, out byte r, out byte g, out byte b, out _);
                        ar += r;
                        ag += g;
                        ab += b;
                    }
                }
                colors.Add(new RGBA8 { R = (byte)(ar / c), G = (byte)(ag / c), B = (byte)(ab / c), A = 255 });

            }
            return [.. colors];
        }

        public void UpdateScene(Texture32 buffer, float frameTime)
        {

            Array.Fill(ZBuffer, float.MaxValue);

            var px = (int)Game.Player.PosX;
            var py = (int)Game.Player.PosY;

            // Mark current tile and all 8 neighbours as visited
            // so surrounding walls become visible
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    var nx = px + dx;
                    var ny = py + dy;
                    if (ny >= 0 && ny < _visited.Length &&
                        nx >= 0 && nx < _visited[ny].Length)
                        _visited[ny][nx] = true;
                }
            }

            BuildLightMapIfNeeded();
            UpdateDynamicObjects(frameTime);
            //Casting Slower but trying to make more managable chunks of code..
            for (int x = 0; x < buffer.Width; x++)
            {
                CastFloors(buffer, x);
                CastDoors(buffer, x);
                CastWalls(buffer, x);
            }
            CastSprites(buffer);
            DrawMap(buffer);
            //DrawZBuffer(buffer);
            UpdateInput(frameTime);
            UpdateDoors(frameTime);
            UpdatePushWalls(frameTime);
            UpdateDynamiteCountdown(frameTime);
            if (_exitActivated)
            {
                _exitFadeTween.Update(frameTime);
                UpdateExitDelay(frameTime);
            }
        }

        private void UpdateDynamicObjects(float frameTime)
        {
            foreach (var obj in DynamicObjects.ToArray())
                obj.Update(frameTime, this);

            DynamicObjects.RemoveAll(o => !o.IsAlive && o.ObjectType == DynamicObjectType.Projectile);

            // Boss objective — complete when all boss-type enemies are dead
            if (Game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_BOSS) &&
                !Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOSS))
            {
                var bossEnemies = DynamicObjects
                    .OfType<EnemyObject>()
                    .Where(e => e.Enemy.EnemyType.IsBoss());

                if (bossEnemies.Any() && bossEnemies.All(e => !e.IsAlive))
                    Game.Map.ObjectivesComplete[MapFlags.HAS_BOSS] = true;
            }
        }


        public void UpdateInput(float frameTime)
        {
            float moveSpeed = frameTime * 5.0f;
            float rotSpeed = frameTime * 2.0f;

            if (Wolfenstein.Graphics.IsKeyDown(SFML.Window.Keyboard.Key.Up))
            {
                var nextX = (int)(Game.Player.PosX + Game.Player.DirX * moveSpeed);
                var nextY = (int)(Game.Player.PosY + Game.Player.DirY * moveSpeed);
                var curX = (int)Game.Player.PosX;
                var curY = (int)Game.Player.PosY;

                var tileX = Game.Map.WorldMap[curY][nextX];
                if ((tileX == MapSection.ClosedSectionInterior && IsDecalPassable(nextX, curY)) ||
                    (tileX == DOOR_TILE && CanPassThroughDoor(nextX, curY)))
                    Game.Player.PosX += Game.Player.DirX * moveSpeed;

                var tileY = Game.Map.WorldMap[nextY][curX];
                if ((tileY == MapSection.ClosedSectionInterior && IsDecalPassable(curX, nextY)) ||
                    (tileY == DOOR_TILE && CanPassThroughDoor(curX, nextY)))
                    Game.Player.PosY += Game.Player.DirY * moveSpeed;
            }

            if (Wolfenstein.Graphics.IsKeyDown(SFML.Window.Keyboard.Key.Down))
            {
                var nextX = (int)(Game.Player.PosX - Game.Player.DirX * moveSpeed);
                var nextY = (int)(Game.Player.PosY - Game.Player.DirY * moveSpeed);
                var curX = (int)Game.Player.PosX;
                var curY = (int)Game.Player.PosY;

                var tileX = Game.Map.WorldMap[curY][nextX];
                if ((tileX == MapSection.ClosedSectionInterior && IsDecalPassable(nextX, curY)) ||
                    (tileX == DOOR_TILE && CanPassThroughDoor(nextX, curY)))
                    Game.Player.PosX -= Game.Player.DirX * moveSpeed;

                var tileY = Game.Map.WorldMap[nextY][curX];
                if ((tileY == MapSection.ClosedSectionInterior && IsDecalPassable(curX, nextY)) ||
                    (tileY == DOOR_TILE && CanPassThroughDoor(curX, nextY)))
                    Game.Player.PosY -= Game.Player.DirY * moveSpeed;
            }

            //rotate to the right
            if (Wolfenstein.Graphics.IsKeyDown(SFML.Window.Keyboard.Key.Right))
            {
                float oldDirX = Game.Player.DirX;
                Game.Player.DirX = Game.Player.DirX * MathF.Cos(-rotSpeed) - Game.Player.DirY * MathF.Sin(-rotSpeed);
                Game.Player.DirY = oldDirX * MathF.Sin(-rotSpeed) + Game.Player.DirY * MathF.Cos(-rotSpeed);
                float oldPlaneX = PlaneX;
                PlaneX = PlaneX * MathF.Cos(-rotSpeed) - PlaneY * MathF.Sin(-rotSpeed);
                PlaneY = oldPlaneX * MathF.Sin(-rotSpeed) + PlaneY * MathF.Cos(-rotSpeed);
            }
            //rotate to the left
            if (Wolfenstein.Graphics.IsKeyDown(SFML.Window.Keyboard.Key.Left))
            {
                float oldDirX = Game.Player.DirX;
                Game.Player.DirX = Game.Player.DirX * MathF.Cos(rotSpeed) - Game.Player.DirY * MathF.Sin(rotSpeed);
                Game.Player.DirY = oldDirX * MathF.Sin(rotSpeed) + Game.Player.DirY * MathF.Cos(rotSpeed);
                float oldPlaneX = PlaneX;
                PlaneX = PlaneX * MathF.Cos(rotSpeed) - PlaneY * MathF.Sin(rotSpeed);
                PlaneY = oldPlaneX * MathF.Sin(rotSpeed) + PlaneY * MathF.Cos(rotSpeed);
            }

            if (Wolfenstein.Graphics.IsKeyDown(SFML.Window.Keyboard.Key.Space))
            {
                var result = TryInteract();
                if (result == InteractResult.Exited)
                    _pendingExit = true;
                else if (result == InteractResult.Activated)
                {
                    var exitTextureIdx = Array.FindIndex(
                        Game.Map.WallSourceIndicies, k => k.Index == 1003);
                    if (exitTextureIdx >= 0)
                        Game.Map.WallTextures[exitTextureIdx] =
                            Wolfenstein.GameResources.ElevatorSwitchDown;

                    _exitActivated = true;
                    _exitDelay = ExitDelayDuration;
                    _exitFadeTween.Reset();
                }
                else if (result == InteractResult.Locked)
                {
                    ShowHudMessage("COMPLETE OBJECTIVES FIRST");
                }

            }

            if (Wolfenstein.Graphics.IsKeyDown(SFML.Window.Keyboard.Key.Escape))
            {

            }
            _mapVisible = Wolfenstein.Graphics.IsKeyDown(Wolfenstein.Config.KeyMap);
            var mapKeyDown = Wolfenstein.Graphics.IsKeyDown(Wolfenstein.Config.KeyMap);

        }
        private void UpdateExitDelay(float frameTime)
        {
            if (!_exitActivated) return;
            _exitDelay -= frameTime;
            if (_exitDelay <= 0f)
            {
                _exitActivated = false;
                _pendingExit = true;
            }
        }
        private bool IsDecalPassable(int x, int y)
        {
            var decal = Game.Map.Decals.FirstOrDefault(d => d.X == x && d.Y == y);
            return decal == null || decal.Passable;
        }

        public void InvalidateLightMap()
        {
            _lastLightTileX = -1;
            _lastLightTileY = -1;
        }

        private void BuildLightMapIfNeeded()
        {
            int tileX = (int)Game.Player.PosX;
            int tileY = (int)Game.Player.PosY;
            if (tileX == _lastLightTileX && tileY == _lastLightTileY) return;
            _lastLightTileX = tileX;
            _lastLightTileY = tileY;
            BuildLightMap();
        }
        private void BuildLightMap()
        {
            var h = Game.Map.WorldMap.Length;
            var w = Game.Map.WorldMap[0].Length;

            if (_lightMap == null || _lightMap.GetLength(0) != h || _lightMap.GetLength(1) != w)
                _lightMap = new float[h, w];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _lightMap[y, x] = 0f;

            foreach (var decal in Game.Map.Decals.Where(d => d.LightSource))
            {
                int minX = Math.Max(0, decal.X - (int)LightRadius);
                int maxX = Math.Min(w - 1, decal.X + (int)LightRadius);
                int minY = Math.Max(0, decal.Y - (int)LightRadius);
                int maxY = Math.Min(h - 1, decal.Y + (int)LightRadius);

                for (int ty = minY; ty <= maxY; ty++)
                {
                    for (int tx = minX; tx <= maxX; tx++)
                    {
                        var dx = tx - decal.X;
                        var dy = ty - decal.Y;
                        var dist = MathF.Sqrt(dx * dx + dy * dy);
                        if (dist > LightRadius) continue;
                        var contribution = 1f - (dist / LightRadius);
                        contribution *= contribution; // quadratic falloff
                        _lightMap[ty, tx] = Math.Max(_lightMap[ty, tx], contribution);
                    }
                }
            }

            if (Wolfenstein.Config.LightBlur)
            {
                // Separable box blur — horizontal pass into temp, then vertical pass back
                var temp = new float[h, w];

                // Horizontal pass
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float sum = 0f;
                        int count = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= w) continue;
                            sum += _lightMap[y, nx];
                            count++;
                        }
                        temp[y, x] = sum / count;
                    }
                }

                // Vertical pass
                var blurred = new float[h, w];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float sum = 0f;
                        int count = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int ny = y + dy;
                            if (ny < 0 || ny >= h) continue;
                            sum += temp[ny, x];
                            count++;
                        }
                        blurred[y, x] = sum / count;
                    }
                }
                _lightMap = blurred;
            }
        }

        private float GetTileBrightness(int mapX, int mapY)
        {
            if (_lightMap == null) return 0f;
            if (mapY < 0 || mapY >= _lightMap.GetLength(0) ||
                mapX < 0 || mapX >= _lightMap.GetLength(1)) return 0f;
            return _lightMap[mapY, mapX];
        }

        private bool CanPassThroughDoor(int mapX, int mapY)
        {
            var door = GetDoorAt(mapX, mapY);
            if (door == null) return false;
            if (door.IsFake) return true;
            return door.OpenAmount >= 0.8f;
        }
        public InteractResult TryInteract()
        {
            int playerMapX = (int)Game.Player.PosX;
            int playerMapY = (int)Game.Player.PosY;

            int[] dx = [-1, 1, 0, 0];
            int[] dy = [0, 0, -1, 1];

            // Static interactables
            IEnumerable<IInteractable> interactables =
                Game.Map.Doors.Cast<IInteractable>()
                .Concat(Game.Map.Exits.Cast<IInteractable>())
                .Concat(Game.Map.PushWalls.Cast<IInteractable>());

            // Dynamic interactables — Radio and DynamitePlacement
            var dynamicInteractables = DynamicObjects
                .OfType<IInteractable>()
                .Where(o => o is RadioObject || o is DynamitePlacementObject);

            interactables = interactables.Concat(dynamicInteractables);

            for (int i = 0; i < 4; i++)
            {
                int checkX = playerMapX + dx[i];
                int checkY = playerMapY + dy[i];

                var target = interactables.FirstOrDefault(
                    d => d.X == checkX && d.Y == checkY && d.CanInteract(Game));

                if (target == null) continue;

                var result = target.Interact(Game);

                // Show feedback for radio transmission
                if (target is RadioObject &&
                    Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE))
                    ShowHudMessage("MESSAGE TRANSMITTED");

                if (result != InteractResult.None)
                    return result;
            }
            return InteractResult.None;
        }

        private void UpdateDoors(float deltaTime)
        {
            foreach (var door in Game.Map.Doors)
            {
                if (door.IsOpening)
                {
                    door.OpenAmount += door.OpenSpeed * deltaTime;
                    if (door.OpenAmount >= 1.0f)
                    {
                        door.OpenAmount = 1.0f;
                        door.IsOpening = false;
                        door.CloseTimer = door.CloseDelay;
                    }
                }
                else if (door.IsClosing)
                {
                    if (!DoorCanClose(door)) continue;
                    door.OpenAmount -= door.OpenSpeed * deltaTime;
                    if (door.OpenAmount <= 0.0f)
                    {
                        door.OpenAmount = 0.0f;
                        door.IsClosing = false;
                    }
                }
                else if (door.OpenAmount > 0.0f && door.CloseTimer > 0.0f)
                {
                    door.CloseTimer -= deltaTime;
                    if (door.CloseTimer <= 0.0f)
                    {
                        door.IsClosing = true;
                    }
                }
            }
        }

        private bool DoorCanClose(Door door)
        {
            int playerMapX = (int)Game.Player.PosX;
            int playerMapY = (int)Game.Player.PosY;

            int[] dx = [0, -1, 1, 0, 0];
            int[] dy = [0, 0, 0, -1, 1];
            for (int i = 0; i < 5; i++)
            {
                int checkX = playerMapX + dx[i];
                int checkY = playerMapY + dy[i];
                if (door.X == checkX && door.Y == checkY) return false;
            }

            foreach (var e in DynamicObjects.Where(d => d is EnemyObject))
            {
                for (int i = 0; i < 5; i++)
                {
                    int checkX = (int)e.X + dx[i];
                    int checkY = (int)e.Y + dy[i];
                    if (door.X == checkX && door.Y == checkY) return false;
                }
            }
            return true;
        }

        public void DrawZBuffer(Texture32 buffer)
        {
            var min = ZBuffer.Min();
            var max = ZBuffer.Max();
            for (var i = 0; i < ZBuffer.Length; i++)
            {
                var x = (byte)(((ZBuffer[i] - min) / (max - min)) * 255);
                buffer.Line(i, 180, i, 200, x, x, x);
            }
        }
        public void DrawMapOld(Texture32 buffer)
        {
            //return;
            var size = 1;
            for (int y = 0; y < Game.Map.WorldMap.Length; y++)
                for (int x = 0; x < Game.Map.WorldMap[y].Length; x++)
                {

                    {
                        var i = Game.Map.WorldMap[y][x];
                        var c = i >= 0 ? MapColors[i + 1] : i == MapSection.ClosedSectionInterior ? new RGBA8() { R = 128, G = 128, B = 128, A = 255 } : new RGBA8() { R = 0, G = 0, B = 0, A = 64 };

                        buffer.RectFill(x * size, y * size, size, size, c.R, c.G, c.B, c.A);

                    }
                }
            var px = (int)(Game.Player.PosX * size);
            var py = (int)(Game.Player.PosY * size);

            buffer.RectFill(px - 1, py - 1, 3, 3, 255, 255, 0);
            buffer.Line(px, py, (int)(px + (Game.Player.DirX * 20)), (int)(py + (Game.Player.DirY * 20)), 255, 255, 0);

            //Graphics.Debug = MathF.Round(angle, 2).ToString();

        }


        public void DrawMap(Texture32 buffer)
        {
            if (!_mapVisible) return;

            var size = 1;
            var mapWidth = Game.Map.WorldMap[0].Length;

            for (int y = 0; y < Game.Map.WorldMap.Length; y++)
                for (int x = 0; x < Game.Map.WorldMap[y].Length; x++)
                {
                    if (!_visited[y][x]) continue;

                    var i = Game.Map.WorldMap[y][x];
                    var c = i >= 0
                        ? MapColors[i + 1]
                        : i == MapSection.ClosedSectionInterior
                            ? new RGBA8 { R = 128, G = 128, B = 128, A = 255 }
                            : new RGBA8 { R = 0, G = 0, B = 0, A = 64 };

                    var drawX = (mapWidth - 1 - x) * size;
                    buffer.RectFill(drawX, y * size, size, size, c.R, c.G, c.B, c.A);
                }

            var px = (mapWidth - 1 - (int)Game.Player.PosX) * size;
            var py = (int)(Game.Player.PosY * size);
            buffer.RectFill(px - 1, py - 1, 3, 3, 255, 255, 0);
            buffer.Line(px, py,
            (int)(px - (Game.Player.DirX * 10)),
            (int)(py + (Game.Player.DirY * 10)),
            255, 255, 0);
        }

        public void DrawMaplast(Texture32 buffer)
        {
            var size = 1;
            var mapWidth = Game.Map.WorldMap[0].Length;

            for (int y = 0; y < Game.Map.WorldMap.Length; y++)
                for (int x = 0; x < Game.Map.WorldMap[y].Length; x++)
                {
                    if (!_mapVisible) return;
                    var i = Game.Map.WorldMap[y][x];
                    var c = i >= 0 ? MapColors[i + 1]
                        : i == MapSection.ClosedSectionInterior
                            ? new RGBA8 { R = 128, G = 128, B = 128, A = 255 }
                            : new RGBA8 { R = 0, G = 0, B = 0, A = 64 };

                    var drawX = (mapWidth - 1 - x) * size;
                    buffer.RectFill(drawX, y * size, size, size, c.R, c.G, c.B, c.A);
                }

            var px = (mapWidth - 1 - (int)Game.Player.PosX) * size;
            var py = (int)(Game.Player.PosY * size);

            buffer.RectFill(px - 1, py - 1, 3, 3, 255, 255, 0);
            buffer.Line(px, py,
                (int)(px - (Game.Player.DirX * 20)),
                (int)(py + (Game.Player.DirY * 20)),
                255, 255, 0);
        }
        private void CastSprites(Texture32 buffer)
        {
            // Cull to only objects within render distance before sorting
            var living = DynamicObjects
                .Where(o => o.IsAlive)
                .Where(o =>
                {
                    var dx = Game.Player.PosX - o.X;
                    var dy = Game.Player.PosY - o.Y;
                    return (dx * dx + dy * dy) <= RenderDistance * RenderDistance;
                })
                .ToArray();

            if (living.Length == 0) return;

            if (SpriteOrder.Length != living.Length)
            {
                SpriteOrder = new int[living.Length];
                SpriteDistance = new float[living.Length];
            }

            for (int i = 0; i < living.Length; i++)
            {
                SpriteOrder[i] = i;
                var dx = Game.Player.PosX - living[i].X;
                var dy = Game.Player.PosY - living[i].Y;
                SpriteDistance[i] = dx * dx + dy * dy;
            }
            SortSprites(SpriteOrder, SpriteDistance, living.Length);

            for (int i = 0; i < living.Length; i++)
            {
                var obj = living[SpriteOrder[i]];

                // For directional decals, cull if player is on the wrong side
                if (obj is DecalObject decalObj && decalObj.Decal.Direction != Direction.NONE)
                {
                    bool visible = decalObj.Decal.Direction switch
                    {
                        Direction.NORTH => Game.Player.PosY < decalObj.Decal.Y,
                        Direction.SOUTH => Game.Player.PosY > decalObj.Decal.Y,
                        Direction.EAST => Game.Player.PosX > decalObj.Decal.X,
                        Direction.WEST => Game.Player.PosX < decalObj.Decal.X,
                        _ => true
                    };
                    if (!visible) continue;
                }

                // Replace the current angle block and texture fetch:
                float angleToPlayer = 0f;
                if (obj is not DecalObject { Decal.Direction: not Direction.NONE })
                {
                    angleToPlayer = MathF.Atan2(
                        Game.Player.PosY - obj.Y,
                        Game.Player.PosX - obj.X) * (180f / MathF.PI);
                    angleToPlayer = (angleToPlayer + 360f) % 360f;
                }

                // For enemies, sprite frame is relative to their facing direction
                // so walking toward player shows front sprite, away shows back sprite
                float spriteAngle = angleToPlayer;
                if (obj is EnemyObject enemyObj)
                    spriteAngle = (angleToPlayer - enemyObj.FacingAngle + 360f) % 360f;

                var texture = obj.Sprite.GetTexture(spriteAngle);

                if (texture == null) continue;

                var texWidth = texture.Width;
                var texHeight = texture.Height;

                var spriteX = obj.X - Game.Player.PosX;
                var spriteY = obj.Y - Game.Player.PosY;

                var invDet = 1.0f / (PlaneX * Game.Player.DirY - Game.Player.DirX * PlaneY);
                var transformX = invDet * (Game.Player.DirY * spriteX - Game.Player.DirX * spriteY);
                var transformY = invDet * (-PlaneY * spriteX + PlaneX * spriteY);

                if (transformY <= 0) continue;

                int spriteScreenX = (int)((buffer.Width / 2f) * (1f + transformX / transformY));

                int spriteHeight = (int)MathF.Abs(buffer.Height / transformY);

                int rawOffset = (int)(obj.YOffset * spriteHeight);
                int maxLift = Math.Max(0, -spriteHeight / 2 + buffer.Height / 2); // how far up before clipping
                int screenYOffset = -Math.Min(rawOffset, maxLift);
                //if (obj is PickupItemObject)
                //{
                //    System.Diagnostics.Debug.WriteLine($"PickupItem YOffset={obj.YOffset} screenYOffset={screenYOffset}");
                //}

                int drawStartY = Math.Max(0, -spriteHeight / 2 + buffer.Height / 2 + screenYOffset);
                int drawEndY = Math.Min(buffer.Height - 1, spriteHeight / 2 + buffer.Height / 2 + screenYOffset);

                int spriteWidth = (int)MathF.Abs(buffer.Height / transformY);
                int drawStartX = Math.Max(0, -spriteWidth / 2 + spriteScreenX);
                int drawEndX = Math.Min(buffer.Width - 1, spriteWidth / 2 + spriteScreenX);

                if (drawStartX >= drawEndX) continue;

                var rawDist = MathF.Sqrt(SpriteDistance[i]);
                var dist = 1f - (rawDist / RenderDistance);
                var lightBoost = GetTileBrightness((int)obj.X, (int)obj.Y);
                var finalBrightness = Math.Min(dist + lightBoost * LightIntensity, 1f);

                for (int stripe = drawStartX; stripe < drawEndX; stripe++)
                {
                    int texX = Math.Clamp(
                        (int)(256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * texWidth / spriteWidth) / 256,
                        0, texWidth - 1);

                    if (transformY >= ZBuffer[stripe]) continue;
                    for (int y = drawStartY; y < drawEndY; y++)
                    {
                        int d = (y - screenYOffset) * 256 - buffer.Height * 128 + spriteHeight * 128;
                        int texY = Math.Clamp(((d * texHeight) / spriteHeight) / 256, 0, texHeight - 1);
                        texture.GetPixel(texX, texY, out byte r, out byte g, out byte b, out byte a);
                        if (a == 0) continue;
                        buffer.PutPixel(stripe, y,
                            (byte)(r * finalBrightness),
                            (byte)(g * finalBrightness),
                            (byte)(b * finalBrightness), a);
                    }
                    /*for (int y = drawStartY; y < drawEndY; y++)
                    {
                        int d = y * 256 - buffer.Height * 128 + spriteHeight * 128;
                        int texY = Math.Clamp(((d * texHeight) / spriteHeight) / 256, 0, texHeight - 1);
                        texture.GetPixel(texX, texY, out byte r, out byte g, out byte b, out byte a);
                        if (a == 0) continue;
                        buffer.PutPixel(stripe, y,
                            (byte)(r * finalBrightness),
                            (byte)(g * finalBrightness),
                            (byte)(b * finalBrightness), a);
                    }*/
                }
            }
        }

        private void CastWalls(Texture32 buffer, int x)
        {
            //calculate ray position and direction
            float cameraX = 2 * x / (float)buffer.Width - 1; //x-coordinate in camera space
            float rayDirX = Game.Player.DirX + PlaneX * cameraX;
            float rayDirY = Game.Player.DirY + PlaneY * cameraX;
            //which box of the map we're in
            int mapX = (int)Game.Player.PosX;
            int mapY = (int)Game.Player.PosY;
            //length of ray from current position to next x or y-side
            float sideDistX;
            float sideDistY;
            //length of ray from one x or y-side to next x or y-side
            float deltaDistX = (rayDirX == 0) ? float.MaxValue : MathF.Abs(1f / rayDirX);
            float deltaDistY = (rayDirY == 0) ? float.MaxValue : MathF.Abs(1f / rayDirY);
            float perpWallDist;
            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;
            int hit = 0; //was there a wall hit?
            int side = 0; //was a NS or a EW wall hit?

            //calculate step and initial sideDist
            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (Game.Player.PosX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1.0f - Game.Player.PosX) * deltaDistX;
            }
            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (Game.Player.PosY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1.0f - Game.Player.PosY) * deltaDistY;
            }

            //perform DDA
            while (hit == 0)
            {
                //jump to next map square, either in x-direction, or in y-direction
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }
                /*
                //Check if ray has hit a wall
                //if (mapY < 0 || mapX < 0 || mapY >= Game.Map.WorldMap.Length || mapX >= Game.Map.WorldMap[0].Length) return;
                if (Game.Map.WorldMap[mapY][mapX] >= 0 && Game.Map.WorldMap[mapY][mapX] != DOOR_TILE) hit = 1;
                */
                // Cull — stop tracing beyond render distance
                var currentDist = side == 0 ? sideDistX - deltaDistX : sideDistY - deltaDistY;
                if (currentDist > RenderDistance) return;

                if (mapY < 0 || mapX < 0 ||
                    mapY >= Game.Map.WorldMap.Length ||
                    mapX >= Game.Map.WorldMap[0].Length) return;

                if (Game.Map.WorldMap[mapY][mapX] >= 0 &&
                    Game.Map.WorldMap[mapY][mapX] != DOOR_TILE) hit = 1;
            }

            //Calculate distance of perpendicular ray (Euclidean distance would give fisheye effect!)
            if (side == 0) perpWallDist = (sideDistX - deltaDistX);
            else perpWallDist = (sideDistY - deltaDistY);

            //Calculate height of line to draw on screen
            int lineHeight = (int)(buffer.Height / perpWallDist);

            //calculate lowest and highest pixel to fill in current stripe
            int drawStart = -lineHeight / 2 + buffer.Height / 2;
            if (drawStart < 0) drawStart = 0;
            int drawEnd = lineHeight / 2 + buffer.Height / 2;
            if (drawEnd >= buffer.Height) drawEnd = buffer.Height - 1;

            //Modified
            //texturing calculations
            int texNum = Game.Map.WorldMap[mapY][mapX];// - 1; //1 subtracted from it so that texture 0 can be used!
            (int x, int y) door = (-1, -1);
            if (side == 0) //vertical
            {
                if (mapX < Game.Map.WorldMap[0].Length - 1)
                {
                    if (Game.Map.WorldMap[mapY][mapX + 1] == DOOR_TILE)
                        door = (mapX + 1, mapY);
                }
                if (mapX > 0)
                {
                    if (Game.Map.WorldMap[mapY][mapX - 1] == DOOR_TILE)
                        door = (mapX - 1, mapY);
                }
            }
            else
            {
                if (mapY < Game.Map.WorldMap.Length - 1)
                {
                    if (Game.Map.WorldMap[mapY + 1][mapX] == DOOR_TILE)
                        door = (mapX, mapY + 1);
                }
                if (mapY > 0)
                {
                    if (Game.Map.WorldMap[mapY - 1][mapX] == DOOR_TILE)
                        door = (mapX, mapY - 1);
                }
            }
            ISurface? texture = null;
            if (door.x >= 0 && door.y >= 0)
            {
                var d = GetDoorAt(door.x, door.y);
                if (d != null)
                {
                    texture = Game.Map.DoorSideTextures[d.TextureIndex];
                }
            }
            else
            {
                texture = Game.Map.WallTextures[texNum];
            }
            //texture = texture ?? new Texture32(64, 64);
            if (texture == null) return;
            RenderWall(buffer, x, drawStart, drawEnd, lineHeight, perpWallDist, rayDirX, rayDirY, side, texture, mapX, mapY);


        }

        private void RenderWall(Texture32 buffer, int x, int drawStart, int drawEnd, int lineHeight, float perpWallDist,
                               float rayDirX, float rayDirY, int side, ISurface texture, int mapX, int mapY)
        {

            if (ZBuffer[x] < perpWallDist) return;
            //calculate value of wallX
            float wallX;
            if (side == 0) wallX = Game.Player.PosY + perpWallDist * rayDirY;
            else wallX = Game.Player.PosX + perpWallDist * rayDirX;
            wallX -= MathF.Floor(wallX);

            //x coordinate on the texture
            int texX = (int)(wallX * texture.Width);
            if (side == 0 && rayDirX > 0) texX = texture.Width - texX - 1;
            if (side == 1 && rayDirY < 0) texX = texture.Width - texX - 1;

            float step = 1.0f * texture.Height / lineHeight;
            float texPos = (drawStart - buffer.Height / 2 + lineHeight / 2) * step;

            var dist = (float)Math.Min(GraphicsHelpers.GetDistance((int)Game.Player.PosX, (int)Game.Player.PosY, mapX, mapY), RenderDistance);
            dist = 1 - (dist / RenderDistance);
            var lightBoost = GetTileBrightness(mapX, mapY);

            for (int y = drawStart; y <= drawEnd; y++)
            {
                int texY = (int)texPos & (texture.Height - 1);
                texPos += step;
                var bl = Math.Min((side == 1 ? 0.5f * dist : 1f * dist) + lightBoost * LightIntensity, 1f);
                texture.GetPixel(texX, texY, out byte r, out byte g, out byte b, out _);
                buffer.PutPixel(x, y, (byte)(r * bl), (byte)(g * bl), (byte)(b * bl), 255);
            }
            //SET THE ZBUFFER FOR THE SPRITE CASTING
            ZBuffer[x] = perpWallDist; //perpendicular distance is used
        }

        private void CastDoors(Texture32 buffer, int x)
        {
            //calculate ray position and direction
            float cameraX = 2 * x / (float)buffer.Width - 1; //x-coordinate in camera space
            float rayDirX = Game.Player.DirX + PlaneX * cameraX;
            float rayDirY = Game.Player.DirY + PlaneY * cameraX;
            //which box of the map we're in
            int mapX = (int)Game.Player.PosX;
            int mapY = (int)Game.Player.PosY;
            //length of ray from current position to next x or y-side
            float sideDistX;
            float sideDistY;
            //length of ray from one x or y-side to next x or y-side
            float deltaDistX = (rayDirX == 0) ? float.MaxValue : MathF.Abs(1f / rayDirX);
            float deltaDistY = (rayDirY == 0) ? float.MaxValue : MathF.Abs(1f / rayDirY);
            float perpWallDist;
            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;
            int hit = 0; //was there a wall hit?
            int side = 0; //was a NS or a EW wall hit?
            Door? door = null;
            //calculate step and initial sideDist
            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (Game.Player.PosX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1.0f - Game.Player.PosX) * deltaDistX;
            }
            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (Game.Player.PosY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1.0f - Game.Player.PosY) * deltaDistY;
            }
            //perform DDA
            while (hit == 0)
            {
                //jump to next map square, either in x-direction, or in y-direction
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }
                var currentDist = side == 0 ? sideDistX - deltaDistX : sideDistY - deltaDistY;
                if (currentDist > RenderDistance) return;

                if (mapX < 0 || mapX > Game.Map.WorldMap[0].Length - 1 ||
                    mapY < 0 || mapY > Game.Map.WorldMap.Length - 1) return;

                if (Game.Map.WorldMap[mapY][mapX] == DOOR_TILE)
                {
                    door = GetDoorAt(mapX, mapY);
                    if (door != null)
                    {
                        float doorHitPoint = CalculateDoorIntersection(mapX, mapY, rayDirX, rayDirY,
                            side, stepX, stepY, door, out float adjustedDist);
                        if (doorHitPoint >= 0.0f)
                        {
                            hit = 1;
                            if (side == 0) sideDistX = adjustedDist + deltaDistX;
                            else sideDistY = adjustedDist + deltaDistY;
                        }
                    }
                }
            }
            if (door != null)
            {
                //Calculate distance of perpendicular ray (Euclidean distance would give fisheye effect!)
                if (side == 0) perpWallDist = (sideDistX - deltaDistX);
                else perpWallDist = (sideDistY - deltaDistY);

                //Calculate height of line to draw on screen
                int lineHeight = (int)(buffer.Height / perpWallDist);

                //calculate lowest and highest pixel to fill in current stripe
                int drawStart = -lineHeight / 2 + buffer.Height / 2;
                if (drawStart < 0) drawStart = 0;
                int drawEnd = lineHeight / 2 + buffer.Height / 2;
                if (drawEnd >= buffer.Height) drawEnd = buffer.Height - 1;

                // If you have the camera FOV and screen width
                float rayAngleStep = PlaneX / buffer.Width;
                float hitWallRenderedWidthFOV = (float)buffer.Height / (perpWallDist * MathF.Cos(rayAngleStep));

                RenderDoor(buffer, x, drawStart, drawEnd, lineHeight, perpWallDist, rayDirX, rayDirY, side, door, mapX, mapY, hitWallRenderedWidthFOV);
            }

        }
        private Door? GetDoorAt(int mapX, int mapY) => Game.Map.Doors.FirstOrDefault(d => d.X == mapX && d.Y == mapY);

        private float CalculateDoorIntersection(int mapX, int mapY, float rayDirX, float rayDirY, int side, int stepX, int stepY, Door door, out float adjustedPerpWallDist)
        {
            const float DOOR_INSET = 0.5f;// 0. 125f; // How far back the door is from the wall edge

            float wallX;
            if (side == 0) wallX = Game.Player.PosY + ((mapX - Game.Player.PosX + (1 - stepX) / 2) / rayDirX) * rayDirY;
            else wallX = Game.Player.PosX + ((mapY - Game.Player.PosY + (1 - stepY) / 2) / rayDirY) * rayDirX;
            wallX -= MathF.Floor(wallX);

            // Calculate which part of the door is visible based on opening amount
            float doorStart, doorEnd;

            if (door.IsVertical)
            {
                // Door slides horizontally (left/right) into the wall
                doorStart = door.OpenAmount; // Left edge moves right as door opens
                doorEnd = 1.0f; // Right edge stays fixed

                if (wallX >= doorStart && wallX <= doorEnd)
                {
                    // Ray hits the door - calculate adjusted distance for inset
                    if (side == 0)
                    {
                        adjustedPerpWallDist = MathF.Abs((mapX - Game.Player.PosX + (1 - stepX) / 2 + DOOR_INSET * stepX) / rayDirX);
                    }
                    else
                    {
                        adjustedPerpWallDist = MathF.Abs((mapY - Game.Player.PosY + (1 - stepY) / 2 + DOOR_INSET * stepY) / rayDirY);
                    }
                    return wallX;
                }
            }
            else
            {
                // Door slides vertically (up/down) into the wall
                doorStart = door.OpenAmount; // Bottom edge moves up as door opens
                doorEnd = 1.0f; // Top edge stays fixed

                if (wallX >= doorStart && wallX <= doorEnd)
                {
                    // Ray hits the door - calculate adjusted distance for inset
                    if (side == 0)
                    {
                        adjustedPerpWallDist = MathF.Abs((mapX - Game.Player.PosX + (1 - stepX) / 2 + DOOR_INSET * stepX) / rayDirX);
                    }
                    else
                    {
                        adjustedPerpWallDist = MathF.Abs((mapY - Game.Player.PosY + (1 - stepY) / 2 + DOOR_INSET * stepY) / rayDirY);
                    }
                    return wallX;
                }
            }

            adjustedPerpWallDist = 0; // Not used when door not hit
            return -1.0f; // Ray passes through open part of door
        }

        public void RenderDoor(Texture32 buffer, int x, int drawStart, int drawEnd, int lineHeight, float perpWallDist,
                float rayDirX, float rayDirY, int side, Door door, int mapX, int mapY, float renderWidth)
        {
            //calculate value of wallX for the inset door position
            float wallX;
            if (side == 0) wallX = Game.Player.PosY + perpWallDist * rayDirY;
            else wallX = Game.Player.PosX + perpWallDist * rayDirX;
            wallX -= MathF.Floor(wallX);

            // Calculate texture coordinates using door width and position
            int texX;
            var texWidth = Game.Map.DoorTextures[door.TextureIndex].Width;

            if (door.IsVertical)
            {
                // Vertical door slides horizontally
                // Calculate actual door position in world units
                float doorWorldPos = door.OpenAmount * renderWidth; // How far door has moved in world units

                // Convert wallX to world position within the door
                float worldX = wallX * renderWidth; // wallX is 0-1, convert to 0-renderWidth range

                // Subtract door position so texture moves with door
                float textureWorldX = worldX - doorWorldPos;

                // Handle negative values and convert back to texture coordinate (0-1 range)
                while (textureWorldX < 0) textureWorldX += renderWidth;

                // wallX is 0-1 across the door tile
                // Offset by OpenAmount so the texture slides with the door
                // The right edge of the texture stays pinned to the right edge of the door (wallX=1)
                float textureX = wallX - door.OpenAmount;

                // Convert to texture pixel — no wrapping, no scaling
                texX = (int)(textureX * texWidth);
                if (texX < 0 || texX >= texWidth) return;
            }
            else
            {
                // Horizontal door slides vertically
                float doorWorldPos = door.OpenAmount * renderWidth;
                float worldX = wallX * renderWidth;
                float textureWorldX = worldX - doorWorldPos;

                while (textureWorldX < 0) textureWorldX += renderWidth;
                // Offset by OpenAmount so the texture slides with the door
                // The right edge of the texture stays pinned to the right edge of the door (wallX=1)
                float textureX = wallX - door.OpenAmount;

                // Convert to texture pixel — no wrapping, no scaling
                texX = (int)(textureX * texWidth);
                if (texX < 0 || texX >= texWidth) return;

            }

            // Clamp texture coordinates
            texX = Math.Max(0, Math.Min(texX, Game.Map.DoorTextures[door.TextureIndex].Width - 1));



            float step = 1.0f * Game.Map.DoorTextures[door.TextureIndex].Height / lineHeight;
            float texPos = (drawStart - buffer.Height / 2 + lineHeight / 2) * step;

            var dist = (float)Math.Min(GraphicsHelpers.GetDistance((int)Game.Player.PosX, (int)Game.Player.PosY, mapX, mapY), RenderDistance);
            dist = 1 - (dist / RenderDistance);
            var lightBoost = GetTileBrightness(mapX, mapY);

            // Darken door slightly to show it's inset
            float doorDarkening = 0.85f;



            for (int y = drawStart; y <= drawEnd; y++)
            {
                int texY = (int)texPos & (Game.Map.DoorTextures[door.TextureIndex].Height - 1);
                texPos += step;
                var bl = Math.Min((side == 1 ? 0.5f * dist : 1f * dist) * doorDarkening + lightBoost * LightIntensity, 1f);
                Game.Map.DoorTextures[door.TextureIndex].GetPixel(texX, texY, out byte r, out byte g, out byte b, out byte a);
                buffer.PutPixel(x, y, (byte)(r * bl), (byte)(g * bl), (byte)(b * bl), a);
            }

            //SET THE ZBUFFER FOR THE SPRITE CASTING
            ZBuffer[x] = perpWallDist; //perpendicular distance is used
        }

        private float GetWorldBrightness(float worldX, float worldY)
        {
            if (_lightMap == null) return 0f;
            int tx = (int)worldX;
            int ty = (int)worldY;
            if (ty < 0 || ty >= _lightMap.GetLength(0) ||
                tx < 0 || tx >= _lightMap.GetLength(1)) return 0f;
            return _lightMap[ty, tx];
        }

        private void CastFloors(Texture32 buffer, int x)
        {

            //calculate ray position and direction
            float cameraX = 2 * x / (float)buffer.Width - 1; //x-coordinate in camera space
            float rayDirX = Game.Player.DirX + PlaneX * cameraX;
            float rayDirY = Game.Player.DirY + PlaneY * cameraX;

            //which box of the map we're in
            int mapX = (int)Game.Player.PosX;
            int mapY = (int)Game.Player.PosY;

            //length of ray from current position to next x or y-side
            float sideDistX;
            float sideDistY;

            //length of ray from one x or y-side to next x or y-side
            float deltaDistX = (rayDirX == 0) ? float.MaxValue : MathF.Abs(1f / rayDirX);
            float deltaDistY = (rayDirY == 0) ? float.MaxValue : MathF.Abs(1f / rayDirY);
            float perpWallDist;

            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;

            int hit = 0; //was there a wall hit?
            int side = 0; //was a NS or a EW wall hit?

            //calculate step and initial sideDist
            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (Game.Player.PosX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1.0f - Game.Player.PosX) * deltaDistX;
            }
            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (Game.Player.PosY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1.0f - Game.Player.PosY) * deltaDistY;
            }
            //perform DDA
            while (hit == 0)
            {
                //jump to next map square, either in x-direction, or in y-direction
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }
                //Check if ray has hit a wall
                //if (mapY < 0 || mapX < 0 || mapY >= Game.Map.WorldMap.Length || mapX >= Game.Map.WorldMap[0].Length) return;
                if (Game.Map.WorldMap[mapY][mapX] >= 0 && Game.Map.WorldMap[mapY][mapX] != DOOR_TILE) hit = 1;
            }

            //Calculate distance of perpendicular ray (Euclidean distance would give fisheye effect!)
            if (side == 0) perpWallDist = (sideDistX - deltaDistX);
            else perpWallDist = (sideDistY - deltaDistY);

            //Calculate height of line to draw on screen
            int lineHeight = (int)(buffer.Height / perpWallDist);

            int drawEnd = lineHeight / 2 + buffer.Height / 2;
            if (drawEnd >= buffer.Height - 1) return; // wall fills screen, no floor visible
            if (drawEnd >= buffer.Height) drawEnd = buffer.Height - 1;

            //calculate value of wallX
            float wallX; //where exactly the wall was hit
            if (side == 0) wallX = Game.Player.PosY + perpWallDist * rayDirY;
            else wallX = Game.Player.PosX + perpWallDist * rayDirX;
            wallX -= MathF.Floor(wallX);

            //FLOOR CASTING (vertical version, directly after drawing the vertical wall stripe for the current x)
            float floorXWall, floorYWall; //x, y position of the floor texel at the bottom of the wall

            //4 different wall directions possible
            if (side == 0 && rayDirX > 0)
            {
                floorXWall = mapX;
                floorYWall = mapY + wallX;
            }
            else if (side == 0 && rayDirX < 0)
            {
                floorXWall = mapX + 1.0f;
                floorYWall = mapY + wallX;
            }
            else if (side == 1 && rayDirY > 0)
            {
                floorXWall = mapX + wallX;
                floorYWall = mapY;
            }
            else
            {
                floorXWall = mapX + wallX;
                floorYWall = mapY + 1.0f;
            }

            float distWall, distPlayer, currentDist;

            distWall = perpWallDist;
            distPlayer = 0.0f;

            if (drawEnd < 0) drawEnd = buffer.Height; //becomes < 0 when the integer overflows

            //draw the floor from drawEnd to the bottom of the screen
            for (int y = drawEnd + 1; y < buffer.Height; y++)
            {
                currentDist = buffer.Height / (2.0f * y - buffer.Height);

                float weight = (currentDist - distPlayer) / (distWall - distPlayer);
                float currentFloorX = weight * floorXWall + (1.0f - weight) * Game.Player.PosX;
                float currentFloorY = weight * floorYWall + (1.0f - weight) * Game.Player.PosY;

                int floorTexX = (int)(currentFloorX * Floor.Width) % Floor.Width;
                int floorTexY = (int)(currentFloorY * Floor.Height) % Floor.Height;

                // Distance darkening — further = darker
                var distFade = Math.Clamp(1f - (currentDist / RenderDistance), 0f, 1f);
                var lightBoost = GetWorldBrightness(currentFloorX, currentFloorY);
                var brightness = Math.Min(distFade + lightBoost * 0.5f, 1f);

                // Floor base is already dark (0.075) — scale that by brightness
                var floorBl = 0.075f + brightness * (LightIntensity * 0.3f);
                var ceilBl = 0.075f + brightness * (LightIntensity * 0.2f);

                Floor.GetPixel(floorTexX, floorTexY, out byte r, out byte g, out byte b, out _);
                buffer.PutPixel(x, y,
                    (byte)(r * floorBl), (byte)(g * floorBl), (byte)(b * floorBl), 255);

                Ceiling.GetPixel(floorTexX, floorTexY, out r, out g, out b, out _);
                buffer.PutPixel(x, buffer.Height - y,
                    (byte)(r * ceilBl), (byte)(g * ceilBl), (byte)(b * ceilBl), 255);
            }
        }
    }

}


