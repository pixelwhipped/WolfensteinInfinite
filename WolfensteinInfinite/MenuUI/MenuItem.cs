namespace WolfensteinInfinite.MenuUI
{
    public class MenuItem(string text, Action<IMenuItem> action, IGameFont font, bool enabled = true, RGBA8? color = null) : IMenuItem
    {
        public bool Enabled { get; set; } = enabled;
        public RGBA8? Color { get; set; } = color;
        public string Text { get; init; } = text;
        public IGameFont Font { get; init; } = font;
        public Action<IMenuItem> Action { get; init; } = action;
        public int Height => Font.Height;
        public int GetWidth() => Font.MeasureString(Text).Width;
        public virtual int Draw(int x, int y, Texture32 buffer)
        {
            buffer.DrawString(x, y, Text, Font, Color);
            return Font.Height + Font.Height / 6;
        }
    }
}
