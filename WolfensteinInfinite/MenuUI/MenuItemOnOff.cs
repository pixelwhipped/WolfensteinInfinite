namespace WolfensteinInfinite.MenuUI
{
    public class MenuItemOnOff(string text, Action<IMenuItem> action, bool state, int designWidth, IGameFont font, bool enabled = true, RGBA8? color = null) : IMenuItem
    {
        public bool Enabled { get; set; } = enabled;
        public RGBA8? Color { get; set; } = color;
        public string Text { get; init; } = text;
        public bool State { get; set; } = state;
        public IGameFont Font { get; init; } = font;
        public Action<IMenuItem> Action { get; init; } = action;
        public int DesignWidth { get; init; } = designWidth;
        public int Height => Font.Height;
        public int GetWidth()
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = State ? Font.MeasureString("ON").Width : Font.MeasureString("OFF").Width;
            return Math.Max(w1 + w2, DesignWidth);
        }
        public int Draw(int x, int y, Texture32 buffer)
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = State ? Font.MeasureString("ON").Width : Font.MeasureString("OFF").Width;
            var r = Math.Max(DesignWidth - (w1 + w2), 0);
            buffer.DrawString(x, y, Text, Font, Color);
            x += r + w1;
            buffer.DrawString(x, y, State ? "ON" : "OFF", Font, Color);
            return Font.Height + Font.Height / 6;
        }
    }
}