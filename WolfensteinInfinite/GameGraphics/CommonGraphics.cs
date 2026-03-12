using SFML.System;
using WolfensteinInfinite.Engine.Graphics;

//Clean
namespace WolfensteinInfinite.GameGraphics
{
    public static class CommonGraphics
    {
        public static void DrawTtileAnim(Texture32 buffer, GameResources gameResources, Clock clock, float fade)
        {
            if ((int)clock.ElapsedTime.AsSeconds() % 2 == 1)
                buffer.Draw(0, buffer.Height - gameResources.TitleAni1.Height, gameResources.TitleAni1, fade);
            else
                buffer.Draw(0, buffer.Height - gameResources.TitleAni2.Height, gameResources.TitleAni2, fade);
        }
    }
}
