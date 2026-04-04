//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameObjects;

namespace WolfensteinInfinite.States
{
    public class GameGenerationRetryState : GameState
    {
        public readonly Player Player;
        public readonly Difficulties Difficulty;
        public readonly int Level;
        private readonly Guid GameGuild;
        public const string FailedString = "Defeated by map generation.\nPress Y to try again.\nPress N to give up.";
        public GameGenerationRetryState(Wolfenstein wolfenstein, Player player, Guid gameGuild, Difficulties difficulty, int level) : base(wolfenstein)
        {
            Player = player;
            Difficulty = difficulty;
            Level = level;
            GameGuild = gameGuild;
            ReturnState = this;
            NextState = this;
            AudioPlaybackEngine.Instance.PlayMusic(Wolfenstein.LevelCompleteMusic);
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {

            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1);

            var (Width, Height) = Wolfenstein.GameResources.TinyFont.MeasureString(FailedString);
            var uw = Wolfenstein.GameResources.TinyFont.MeasureString("_").Width;

            var rWidth = Width + uw + 10;
            var rHeight = Height + 10;
            var xOff = (buffer.Width - rWidth) / 2;
            var yOff = (buffer.Height - rHeight) / 2; ;
            buffer.RectFill(xOff, yOff, rWidth, rHeight, 20, 20, 20);
            buffer.Line(xOff, yOff, xOff + rWidth, yOff, 52, 52, 52);
            buffer.Line(xOff, yOff, xOff, yOff + rHeight, 52, 52, 52);
            buffer.Line(xOff, yOff + rHeight, xOff + rWidth, yOff + rHeight, 16, 16, 16);
            buffer.Line(xOff + rWidth, yOff, xOff + rWidth, yOff + rHeight, 16, 16, 16);
            buffer.DrawString(xOff + 5, yOff + 5,
                $"{FailedString}{((((int)Wolfenstein.Clock.ElapsedTime.AsSeconds()) % 2 == 1) ? "_" : "")}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);

            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {

            if (k.Code == Keyboard.Key.Y)
            {
                NextState = new GameGenerationState(Wolfenstein, Player, GameGuild,  Difficulty, Level);
                return;
            }
            if (k.Code == Keyboard.Key.N)
            {
                NextState = new MenuState(Wolfenstein, null);
                return;
            }
        }
    }
}
