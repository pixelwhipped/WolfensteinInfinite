namespace WolfensteinInfinite
{
    public interface IGameFont
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public void DrawString(int x, int y, Texture32 buffer, string text, RGBA8? color = null);
        public (int Width, int Height) MeasureString(string text);
        public bool HasChar(char c);
    }
}
