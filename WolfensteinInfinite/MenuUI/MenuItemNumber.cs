using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.MenuUI
{
    public class MenuItemNumber(string text, string endText, Action<IMenuItem> action, int number, int min, int max, int designWidth, IGameFont font, bool enabled = true, RGBA8? color = null) : IMenuItem
    {
        public bool Enabled { get; set; } = enabled;
        public RGBA8? Color { get; set; } = color;
        public string Text { get; init; } = text;
        public string EndText { get; init; } = endText;
        public int Number { get; set; } = number;
        public int Min { get; init; } = min;
        public int Max { get; init; } = max;
        public IGameFont Font { get; init; } = font;
        public Action<IMenuItem> Action { get; init; } = action;
        public int DesignWidth { get; init; } = designWidth;
        public int Height => Font.Height;
        public int GetWidth()
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = Font.MeasureString(Number.ToString()).Width;
            var w3 = string.IsNullOrWhiteSpace(EndText) ? 0 : Font.MeasureString(EndText).Width;
            return Math.Max(w1 + w2 + w3, DesignWidth);
        }
        public int Draw(int x, int y, Texture32 buffer)
        {
            var w1 = Font.MeasureString(Text).Width;
            var w2 = Font.MeasureString(Number.ToString()).Width;
            var w3 = string.IsNullOrWhiteSpace(EndText) ? 0 : Font.MeasureString(EndText).Width;
            var r = Math.Max(DesignWidth - (w1 + w2 + w3), 0);
            buffer.DrawString(x, y, Text, Font, Color);
            x += r + w1;
            buffer.DrawString(x, y, Number.ToString(), Font, Color);
            x += w2;
            if (!string.IsNullOrWhiteSpace(EndText))
                buffer.DrawString(x, y, EndText.ToString(), Font, Color);
            return Font.Height + Font.Height / 6;
        }
    }
}