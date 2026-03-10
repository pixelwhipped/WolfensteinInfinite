namespace WolfensteinInfinite.MenuUI
{
    public class MenuItemOptionSelector(string text, Action<IMenuItem> action, string[] options, int initial, int designWidth, IGameFont font, bool enabled = true, RGBA8? color = null) : IMenuItem
    {
        public bool Enabled { get; set; } = enabled;
        public RGBA8? Color { get; set; } = color;
        public string Text { get; init; } = text;
        public string[] Options { get; init; } = options;
        public int Current { get; set; } = initial;
        public IGameFont Font { get; init; } = font;
        public Action<IMenuItem> Action { get; init; } = action;
        public int DesignWidth { get; init; } = designWidth;
        public int Height => Font.Height;
        public int GetWidth()
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = Options.Max(p => Font.MeasureString(p).Width);
            return Math.Max(w1 + w2, DesignWidth);
        }
        public int Draw(int x, int y, Texture32 buffer)
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = Font.MeasureString(Options[Current]).Width;
            var r = Math.Max(DesignWidth - (w1 + w2), 0);
            buffer.DrawString(x, y, Text, Font, Color);

            buffer.DrawString(x + w1 + r, y, Options[Current], Font, Color);

            return Font.Height + Font.Height / 6;
        }
    }
}