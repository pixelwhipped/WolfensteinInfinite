//Clean
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;

namespace WolfensteinInfinite.States
{
    public class TitleScreen(Wolfenstein wolfenstein) : GameState(wolfenstein)
    {
        private bool FadeIn = true;
        private float MenuFade = 1f;
        private float TitleFadeIn = 0f;

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            MenuFade += FadeIn ? frameTime : frameTime * -1;
            MenuFade = Math.Clamp(MenuFade, 0f, 1f);
            DrawTtile(buffer, MenuFade, frameTime);
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, MenuFade);
            if (TitleFadeIn == 1 && IsKeyDown())
            {
                FadeIn = false;
            }
            else if (TitleFadeIn == 1 && MenuFade == 0)
            {

                FadeIn = true;
                return new MenuState(Wolfenstein, null);
            }
            return this;
        }
        private void DrawTtile(Texture32 buffer, float fade, float frameTime)
        {
            TitleFadeIn += frameTime;
            TitleFadeIn = Math.Clamp(TitleFadeIn, 0f, 1f);
            var w = GameResources.TitleWolfenstein.Width;
            var s = (buffer.Width - w) / 2;
            buffer.Draw(s, 0, GameResources.TitleWolfenstein, fade);
            s += GameResources.TitleWolfenstein.Width - GameResources.Title3D.Width;
            buffer.Draw(s, GameResources.TitleWolfenstein.Height, GameResources.Title3D, fade);
            s -= GameResources.TitleInfinite.Width;
            buffer.Draw(s, GameResources.TitleWolfenstein.Height, GameResources.TitleInfinite, TitleFadeIn * fade);
        }
    }
}
