using SFML.Window;

namespace WolfensteinInfinite.States
{
    public class HighScores : GameState
    {
        public HighScores(Wolfenstein wolfenstein, GameState returnState) : base(wolfenstein)
        {
            ReturnState = returnState;
            NextState = this;
        }
        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1f);
            var yOff = 1;
            buffer.Draw((buffer.Width - Wolfenstein.GameResources.TitleHighScores.Width) / 2, yOff, Wolfenstein.GameResources.TitleHighScores);
            yOff += Wolfenstein.GameResources.TitleHighScores.Height + 1;
            var (nWidth, nHeight) = Wolfenstein.GameResources.MenuFont.MeasureString("NAME");
            var (lWidth, lHeight) = Wolfenstein.GameResources.MenuFont.MeasureString("LEVEL");
            var (sWidth, sHeight) = Wolfenstein.GameResources.MenuFont.MeasureString("SCORE");
            var lx = (buffer.Width / 2) - (lWidth / 2);
            var nx = 10;
            var sx = buffer.Width - (sWidth +10);
            buffer.DrawString(nx, yOff, "NAME", Wolfenstein.GameResources.MenuFont, RGBA8.YELLOW);
            buffer.DrawString(lx, yOff, "LEVEL", Wolfenstein.GameResources.MenuFont, RGBA8.YELLOW);
            buffer.DrawString(sx, yOff, "SCORE", Wolfenstein.GameResources.MenuFont, RGBA8.YELLOW);
            yOff += nHeight + 2;
            lx = (buffer.Width / 2);
            foreach (var score in Wolfenstein.Config.HighScores.OrderByDescending(p => p.Score))
            {
                var (w, h) = Wolfenstein.GameResources.TinyFont.MeasureString($"{score.Level}");
                buffer.DrawString(nx, yOff, score.Name, Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);
                buffer.DrawString(lx-w, yOff, $"{score.Level}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);
                buffer.DrawString(sx, yOff, $"{score.Score}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);
                yOff += h+3;
            }
            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            ReturnState.NextState = ReturnState;
            NextState = ReturnState;
        }
    }
}
