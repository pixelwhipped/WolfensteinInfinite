using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.MenuUI
{
    public interface IMenuItem
    {
        public int Height { get; }
        public bool Enabled { get; set; }
        public string Text { get; init; }
        public Action<IMenuItem> Action { get; init; }
        public int GetWidth();
        public int Draw(int x, int y, Texture32 buffer);
    }
}