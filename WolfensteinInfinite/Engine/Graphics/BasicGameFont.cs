//Clean
namespace WolfensteinInfinite.Engine.Graphics
{
    public class BasicGameFont : IGameFont
    {
        private readonly Dictionary<char, (int Width, int Height, bool[] Image)> Chars = [];
        public int Height { get; set; } = 0;
        public int Width { get; set; } = 0;
        public BasicGameFont(Texture32 texture, char[] map)
        {
            static int Seek(int start, Texture32 t)
            {
                var sum = 0;
                while (sum == 0)
                {
                    for (int y = 0; y < t.Height; y++)
                    {
                        t.GetPixel(start,y, out byte r, out byte g, out byte b, out byte a);
                        sum += r+g+b+a;
                    }
                    if (sum > 0) break;
                    start++;
                    if (start >= t.Width) return t.Width;
                }
                return start;
            }
            static int SeekEnd(int end, Texture32 t)
            {
                var sum = 1;
                while (sum != 0)
                {
                    sum = 0;
                    for (int y = 0; y < t.Height; y++)
                    {
                        t.GetPixel(end, y, out byte r, out byte g, out byte b, out byte a);
                        sum += r + g + b + a;
                    }
                    if (sum == 0) break;
                    end++;
                    if (end >= t.Width) return t.Width;
                }
                return end;
            }
            var xstart = 0;
            var xend = 0;
            foreach (var c in map)
            {
                xstart = Seek(xend, texture);
                xend = SeekEnd(xstart, texture);
                var w = xend - xstart;
                var tmp = (Width: w, texture.Height, Image: new bool[w * texture.Height]);
                var xoff = 0;
                for (var x = xstart; x < xend; x++)
                {
                    for (int y = 0; y < texture.Height; y++)
                    {
                        texture.GetPixel(x, y, out byte r, out byte _, out byte _, out byte a);
                        tmp.Image[xoff + y * w] = a == 255 && r != 255;
                    }
                    xoff++;
                }
                Chars.Add(c, tmp);
            }
            Height = texture.Height;
            Width = (int)Chars.Average(p => p.Value.Width);
        }
        public void DrawString(int sx, int y, Texture32 buffer, string text, RGBA8? color = null)
        {
            var x = sx;
            var cx = color ?? new RGBA8 { R = 255, G = 255, B = 255, A = 255 };
            if (Chars.Count == 0) return;
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char item = chars[i];
                if (sx > buffer.Width) continue;
                if (y > buffer.Height) return;
                if (item == '\n' || item == '\r')
                {
                    y += Height;
                    x = sx;
                    continue;
                }
                if (!Chars.ContainsKey(item))
                {
                    x += Width;
                    continue;
                }
                var c = Chars[item];
                if (x + c.Width < 0) continue;
                for (int y0 = 0; y0 < c.Height; y0++)
                {
                    for (int x0 = 0; x0 < c.Width; x0++)
                    {
                        var p = c.Image[x0 + y0 * c.Width];
                        if (p)
                        {
                            buffer.PutPixel(x + x0, y + y0, cx.R, cx.G, cx.B, cx.A);
                        }
                    }
                }
                x += c.Width+2;
            }
        }
        public (int Width, int Height) MeasureString(string text)
        {
            if (Chars.Count == 0) return (Width: 0, Height: 0);
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
                if (!Chars.ContainsKey(item))
                {
                    x += Width;
                    continue;
                }
                var c = Chars[item];
                x += c.Width+2;
            }
            finalX = Math.Max(finalX, x);
            return (Width: finalX, Height: y + Height);
        }
        public bool HasChar(char c) => Chars.ContainsKey(c);
    }
}
