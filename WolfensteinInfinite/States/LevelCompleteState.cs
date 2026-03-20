//WIP Work in progress
using SFML.Window;
using System.Windows.Controls.Primitives;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.States
{
    public class LevelCompleteState : GameState
    {
        private readonly Player _player;
        private readonly int _completedLevel;
        private readonly int _nextLevel;
        private readonly GameState _nextLevelState;
        private bool _ready = false;
        private float _readyTimer = 0f;
        private const float MinDisplayTime = 2.0f;
        private readonly LevelStats _stats;
        //Score, Enemy, Items, Secrets
        private Tween[] Tweens = [new(0.75f, null), new(0.75f, null), new(0.75f, null), new(0.75f, null)];
        private Tween FireSound = new Tween(0.05f,null);
        public LevelCompleteState(Wolfenstein wolfenstein, Player player, Map map,
            LevelStats stats, GameState nextLevelState) : base(wolfenstein)
        {
            _player = player;
            _completedLevel = map.Level - 1;
            _nextLevel = map.Level;
            _stats = stats;
            _nextLevelState = nextLevelState;
            ReturnState = this;
            NextState = this;
           // AudioPlaybackEngine.Instance.PlayMusic(Wolfenstein.LevelCompleteMusic); 
            if(stats.LevelScore == 0) Tweens[0].End();
            if (stats.EnemiesKilled == 0) Tweens[1].End();
            if (stats.ItemsCollected == 0) Tweens[2].End();
            if (stats.SecretsFound == 0) Tweens[3].End();
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            if (_ready)
                return _nextLevelState;

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
            var (hw,hh) = Wolfenstein.GameResources.MenuFont.MeasureString(heading);
            buffer.DrawString(centerX - hw / 2, y,
                heading, Wolfenstein.GameResources.MenuFont, RGBA8.WHITE);

            y += hh + 6;

            // Score  should be level score
            var score = $"SCORE: {(int)(_stats.LevelScore * Tweens[0].Value)}";
            var (sw, sh) = Wolfenstein.GameResources.SmallFont.MeasureString(score);
            buffer.DrawString(centerX - sw / 2, y,
                score, Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);
            y += sh+6;
            var enemies = $"KILL RATIO {(int)(Pct(_stats.EnemiesKilled, _stats.EnemiesTotal) * Tweens[1].Value)}%";
            var (ew, eh) = Wolfenstein.GameResources.SmallFont.MeasureString(enemies);
            buffer.DrawString(centerX - ew / 2, y, enemies,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

            y += eh + 6;
            var items = $"ITEMS      {(int)(Pct(_stats.ItemsCollected, _stats.ItemsTotal) * Tweens[2].Value)}%";
            var (iw, ih) = Wolfenstein.GameResources.SmallFont.MeasureString(items);
            buffer.DrawString(centerX - iw / 2, y, items,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

            y += ih+6;
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
        private static int Pct(int val, int total) =>
    total == 0 ? 100 : (int)(val / (float)total * 100f);
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (_readyTimer >= MinDisplayTime)
                _ready = true;
            foreach (var t in Tweens) t.End();

        }
    }
}
