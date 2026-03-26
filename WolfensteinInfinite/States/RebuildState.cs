using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.States
{
    public class RebuildState: GameState
    {
        public const string WarningString = "Warning this will overwrite mod data\nbased on available\noriginal gamedata.\nPress Y to continue.";
        public RebuildState(Wolfenstein wolfenstein, GameState returnState) : base(wolfenstein)
        {
            ReturnState = returnState;
            NextState = this;
        }
        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            buffer.Clear(136, 0, 0);
            var (Width, Height) = Wolfenstein.GameResources.TinyFont.MeasureString(WarningString);
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
                $"{WarningString}{((((int)Wolfenstein.Clock.ElapsedTime.AsSeconds()) % 2 == 1) ? "_" : "")}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);
            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Y)
            {
                Wolfenstein.RebuildMods();
                NextState = new MenuState(Wolfenstein, null);
                return;
            }
            NextState = ReturnState;
            ReturnState.NextState = ReturnState;
        }
    }
}
