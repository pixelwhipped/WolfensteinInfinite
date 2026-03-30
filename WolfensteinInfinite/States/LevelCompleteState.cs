//WIP Work in progress
using SFML.Window;
using System.Windows.Controls.Primitives;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.States
{
    public class LevelCompleteState : GameState
    {
        private readonly int _completedLevel;
        private bool _ready = false;
        private float _readyTimer = 0f;
        private const float MinDisplayTime = 2.0f;
        private readonly LevelStats _stats;
        private readonly Game Game;
        //Score, Enemy, Items, Secrets
        private readonly Tween[] Tweens = [new(0.75f, null), new(0.75f, null), new(0.75f, null), new(0.75f, null)];
        private readonly Tween FireSound = new(0.05f, null);
        public LevelCompleteState(Wolfenstein wolfenstein, Game game,
            LevelStats stats) : base(wolfenstein)
        {
            Game = game;
            _completedLevel = game.Map.Level - 1;
            _stats = stats;
            ReturnState = this;
            NextState = this;
            Wolfenstein.PlayMusic(Wolfenstein.LevelCompleteMusic);
            if (stats.LevelScore == 0) Tweens[0].End();
            if (stats.EnemiesKilled == 0) Tweens[1].End();
            if (stats.ItemsCollected == 0) Tweens[2].End();
            if (stats.SecretsFound == 0) Tweens[3].End();
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            if (_ready)
            {
                GameState nextLevel;

                var mods = Wolfenstein.Config.Mods.Where(p => p.Enabled);
                var tmods = Wolfenstein.TestMapSections == null ? [] : Wolfenstein.TestMapSections.Where(p => mods.Any(mo => mo.Name == p.mod)).ToArray();
                if (tmods != null && tmods.Length >= Game.Map.Level)
                {
                    nextLevel = new SpecialLevelState(
                                Wolfenstein,
                                Game.Player,
                                Difficulties.CAN_I_PLAY_DADDY,
                                Game.Map.Level,
                                tmods[Game.Map.Level - 1].mod,
                                tmods[Game.Map.Level - 1].section);
                }
                else if (Game.Map.Level % 10 == 0)
                {
                    var smods = Wolfenstein.SpecialMaps == null ? [] : Wolfenstein.SpecialMaps.Where(p => mods.Any(mo => mo.Name == p.Key)).ToDictionary();
                    var specials = new List<(string Mod, MapSection Section)>();
                    foreach(var m in smods)
                    {
                        foreach (var section in m.Value.MapSections)
                        {
                            specials.Add((Mod: m.Key, Section: section));
                        }
                    }
                    if (specials.Count > 0)
                    {
                        var chosen = specials[Random.Shared.Next(specials.Count)];
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
                return nextLevel;
            }


            _readyTimer += frameTime;

            foreach (var t in Tweens)
            {
                if (t.IsFinished) continue;
                t.Update(frameTime);
                FireSound.Update(frameTime);
                if (FireSound.IsFinished)
                {
                    FireSound.Reset();
                    AudioPlaybackEngine.Instance.PlaySound(Wolfenstein.GameResources.Effects["ChangeMenu"]);
                }
                break;
            }


            // Draw background
            buffer.Clear(0, 0, 0);
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1f);
            var centerX = buffer.Width / 2;
            var y = 8;

            // Level complete heading
            var heading = $"LEVEL {_completedLevel} COMPLETE";
            var (hw, hh) = Wolfenstein.GameResources.MenuFont.MeasureString(heading);
            buffer.DrawString(centerX - hw / 2, y,
                heading, Wolfenstein.GameResources.MenuFont, RGBA8.WHITE);

            y += hh + 6;

            // Score  should be level score
            var score = $"SCORE: {(int)(_stats.LevelScore * Tweens[0].Value)}";
            var (sw, sh) = Wolfenstein.GameResources.SmallFont.MeasureString(score);
            buffer.DrawString(centerX - sw / 2, y,
                score, Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);
            y += sh + 6;
            var enemies = $"KILL RATIO {(int)(Pct(_stats.EnemiesKilled, _stats.EnemiesTotal) * Tweens[1].Value)}%";
            var (ew, eh) = Wolfenstein.GameResources.SmallFont.MeasureString(enemies);
            buffer.DrawString(centerX - ew / 2, y, enemies,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

            y += eh + 6;
            var items = $"ITEMS      {(int)(Pct(_stats.ItemsCollected, _stats.ItemsTotal) * Tweens[2].Value)}%";
            var (iw, ih) = Wolfenstein.GameResources.SmallFont.MeasureString(items);
            buffer.DrawString(centerX - iw / 2, y, items,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

            y += ih + 6;
            var secrets = $"SECRETS    {(int)(Pct(_stats.SecretsFound, _stats.SecretsTotal) * Tweens[3].Value)}%";
            var (secw, _) = Wolfenstein.GameResources.SmallFont.MeasureString(secrets);
            buffer.DrawString(centerX - secw / 2, y, secrets,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);


            // Continue prompt — only after minimum display time
            if (_readyTimer >= MinDisplayTime)
            {
                var prompt = "PRESS ANY KEY TO CONTINUE";
                var (pw, _) = Wolfenstein.GameResources.TinyFont.MeasureString(prompt);
                buffer.DrawString(centerX - pw / 2, buffer.Height - 20,
                    prompt, Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);
            }

            return NextState;
        }
        private static int Pct(int val, int total) => total == 0 ? 100 : (int)(val / (float)total * 100f);
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (_readyTimer >= MinDisplayTime)
                _ready = true;
            foreach (var t in Tweens) t.End();

        }
    }
}
