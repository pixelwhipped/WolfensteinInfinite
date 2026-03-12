using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.MenuUI
{
    public class MenuItemKeyBinder(string text, Keyboard.Key key, Action<IMenuItem> action, int designWidth, IGameFont font, bool enabled = true, RGBA8? color = null) : IMenuItem
    {
        public bool Selecting = false;
        public bool Enabled { get; set; } = enabled;
        public RGBA8? Color { get; set; } = color;
        public string Text { get; init; } = text;
        public Keyboard.Key Key { get; set; } = key;
        public Keyboard.Key? InputKey { get; set; } = null;
        public IGameFont Font { get; init; } = font;
        public Action<IMenuItem> Action { get; init; } = action;
        public int DesignWidth { get; init; } = designWidth;
        public int Height => Font.Height;
        public int GetWidth()
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = Font.MeasureString("Apostrophe").Width;
            return Math.Max(w1 + w2, DesignWidth);
        }
        public int Draw(int x, int y, Texture32 buffer)
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = Font.MeasureString("Apostrophe").Width + 6;
            var r = Math.Max(DesignWidth - (w1 + w2), 0);
            buffer.DrawString(x, y, Text, Font, Color);
            if (!Selecting)
            {
                x += r + w1 + w2 / 2 - Font.MeasureString(Enum.GetName(Key)??string.Empty).Width / 2;
                buffer.DrawString(x, y, Enum.GetName(Key) ?? string.Empty, Font, Color);
            }
            return Font.Height + Font.Height / 6;
        }
    }
}