//WIP Work in progress
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameObjects;

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
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            if (_ready)
                return _nextLevelState;

            _readyTimer += frameTime;

            // Draw background
            buffer.Clear(0, 0, 0);

            var centerX = buffer.Width / 2;
            var y = buffer.Height / 4;

            // Level complete heading
            var heading = $"LEVEL {_completedLevel} COMPLETE";
            var (hw, _) = Wolfenstein.GameResources.LargeFont.MeasureString(heading);
            buffer.DrawString(centerX - hw / 2, y,
                heading, Wolfenstein.GameResources.LargeFont, RGBA8.YELLOW);

            y += 24;

            // Score
            var score = $"SCORE: {_player.Score}";
            var (sw, _) = Wolfenstein.GameResources.SmallFont.MeasureString(score);
            buffer.DrawString(centerX - sw / 2, y,
                score, Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);
            y += 14;
            var enemies = $"ENEMIES  {_stats.EnemiesKilled}/{_stats.EnemiesTotal} " +
                $"({Pct(_stats.EnemiesKilled, _stats.EnemiesTotal)}%)";
            var (ew, _) = Wolfenstein.GameResources.SmallFont.MeasureString(enemies);
            buffer.DrawString(centerX - ew / 2, y, enemies,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

            y += 14;
            var items = $"ITEMS    {_stats.ItemsCollected}/{_stats.ItemsTotal} " +
                $"({Pct(_stats.ItemsCollected, _stats.ItemsTotal)}%)";
            var (iw, _) = Wolfenstein.GameResources.SmallFont.MeasureString(items);
            buffer.DrawString(centerX - iw / 2, y, items,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

            y += 14;
            var secrets = $"SECRETS  {_stats.SecretsFound}/{_stats.SecretsTotal} " +
                $"({Pct(_stats.SecretsFound, _stats.SecretsTotal)}%)";
            var (secw, _) = Wolfenstein.GameResources.SmallFont.MeasureString(secrets);
            buffer.DrawString(centerX - secw / 2, y, secrets,
                Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);
            y += 24;

            // Next level
            var next = $"ENTERING LEVEL {_nextLevel}";
            var (nw, _) = Wolfenstein.GameResources.SmallFont.MeasureString(next);
            buffer.DrawString(centerX - nw / 2, y,
                next, Wolfenstein.GameResources.SmallFont, RGBA8.WHITE);

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
        }
    }
}
