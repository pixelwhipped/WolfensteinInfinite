namespace WolfensteinInfinite
{
    public class GameFont : IGameFont
    {
        private readonly Dictionary<char, Texture32> Chars = [];
        public int Height { get; set; } = 0;
        public int Width { get; set; } =  0;
        public GameFont() { }
        public void AddChar(char c, Texture32 tex)
        {
            c = Char.ToUpper(c);
            if(Chars.ContainsKey(c))
            {
                Chars[c] = tex;
                return;
            }
            Chars.Add(c, tex);
            Width = (int)Chars.Values.Average(x => x.Width);
            Height = (int)(Chars.Values.Max(y => y.Height));
        }
        public void DrawString(int sx,int y,Texture32 buffer, string text, RGBA8? color)
        {
            var x = sx;
            if (Chars.Count == 0) return;
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char item = chars[i];
                if (x > buffer.Width) continue;
                if (y > buffer.Height) return;
                if (item == '\n' || item == '\r') {
                    y += Height;
                    x = sx;
                    continue;
                }
                if(!Chars.ContainsKey(Char.ToUpper(item))) 
                {
                    x += Width;
                    continue;
                }
                var c = Chars[Char.ToUpper(item)];
                if (x + c.Width < 0) continue;
                if(Char.IsUpper(item))
                {
                    buffer.Blit(x, y, c.Width,c.Height, c);
                }
                else
                {
                    buffer.Blit(x, y+2, c.Width, c.Height-4, c);
                }
                
                x += c.Width;
            }
        }
        public (int Width, int Height) MeasureString(string text)
        {
            if (Chars.Count == 0) return (Width: 0, Height :0);
            var chars = text.ToCharArray();
            var y = 0;
            var x = 0;
            var finalX = 0;
            for (int i = 0; i < chars.Length; i++)
            {
                char item = chars[i];
                if (item == '\n' || item == '\r')
                {
                    y += Height;
                    finalX = Math.Max(finalX, x);
                    x = 0;
                    continue;
                }
                if (!Chars.ContainsKey(Char.ToUpper(item)))
                {
                    x += Width;
                    continue;
                }
                var c = Chars[Char.ToUpper(item)];
                x += c.Width;
            }
            finalX = Math.Max(finalX, x);
            return (Width: finalX, Height: y+Height);
        }
        public bool HasChar(char c) => Chars.ContainsKey(c);
    }
}
