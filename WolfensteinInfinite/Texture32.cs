using System.Runtime.InteropServices;
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite
{
    public class Texture32 : ISurface
    {
        public static readonly Texture32 NonNullTexture = new(1, 1, [0, 0, 0, 0]);
        public byte[] Pixels { get; init; }
        public int Height { get; init; }
        public int Width { get; init; }

        public BitDepth Bits => BitDepth.BIT32;

        public byte[]? Pallet => null;

        public bool HasTransparency => false;

        public byte TransparencyIndex => 0;

        public Texture32(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new byte[Width * Height * 4];
        }
        public Texture32(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
        public RGBA8[] GetUsedColors()
        {
            Span<byte> memory = Pixels;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            var uniqueColors = new HashSet<RGBA8>();
            foreach (var pixel in pixelsrgba)
            {
                uniqueColors.Add(pixel);
            }

            return [.. uniqueColors];
        }

        public int GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0;
            var off = (x + (y * Width)) << 2;
            return ColorSpace.ToInt(Pixels[off], Pixels[off + 1], Pixels[off + 2], Pixels[off + 3]);
        }
        public void PutPixel(int x, int y, int c)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            var off = (x + (y * Width)) << 2;
            Pixels[off] = (byte)((c & 0xFF000000) >> 24);
            Pixels[off + 1] = (byte)((c & 0x00FF0000) >> 16);
            Pixels[off + 2] = (byte)((c & 0x0000FF00) >> 8);
            Pixels[off + 3] = (byte)(c & 0x000000FF);
        }
        public void PutPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            var off = (x + (y * Width)) << 2;
            if (a != 255)
            {
                var amt = a / 255f;
                byte rb = Pixels[off];
                byte gb = Pixels[off + 1];
                byte bb = Pixels[off + 2];
                byte ab = Pixels[off + 3];
                r = (byte)(rb + (r - rb) * amt);
                g = (byte)(gb + (g - gb) * amt);
                b = (byte)(bb + (b - bb) * amt);
                a = (byte)(ab + (a - ab) * amt);
            }
            Pixels[off] = r;
            Pixels[off + 1] = g;
            Pixels[off + 2] = b;
            Pixels[off + 3] = a;
        }
        public void GetPixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                r = g = b = a = 0;
                return;
            }
            var off = (x + (y * Width)) << 2;
            r = Pixels[off];
            g = Pixels[off + 1];
            b = Pixels[off + 2];
            a = Pixels[off + 3];
        }

        public void Clear(byte r, byte g, byte b) => RectFill(0, 0, Width, Height, r, g, b);

        public void RectFill(int x, int y, int width, int height, byte r, byte g, byte b) => RectFill(x, y, width, height, r, g, b, 255);
        public void RectFill(int x, int y, int width, int height, byte r, byte g, byte b, byte a)
        {
            if (x + width < 0 || x >= Width) return;
            if (y + height < 0 || y >= Height) return;
            Span<byte> px = Pixels;
            var x2 = Math.Min(x + width, Width);
            x = Math.Max(x, 0);
            var y2 = Math.Min(y + height, Height);
            y = Math.Max(y, 0);
            if (a == 255)
            {
                for (int j = y; j < y2; j++)
                {
                    for (int i = x; i < x2; i++)
                    {
                        var off = (i + (j * Width)) << 2;
                        px[off] = r;
                        px[off + 1] = g;
                        px[off + 2] = b;
                        px[off + 3] = 255;
                    }
                }
            }
            else
            {
                var alpha = a/255f;
                for (int j = y; j < y2; j++)
                {
                    for (int i = x; i < x2; i++)
                    {

                        var off = (i + (j * Width)) << 2;
                        px[off] = GraphicsHelpers.Lerp(px[off], r, alpha);
                        px[off + 1] = GraphicsHelpers.Lerp(px[off + 1], g, alpha);
                        px[off + 2] = GraphicsHelpers.Lerp(px[off + 2], b, alpha);
                        px[off + 3] = 255;
                    }
                }
            }
        }
        public void Blit(int x, int y, Texture32 surface) => Blit(x, y, surface, 1f);
        public void Blit(int x, int y, Texture32 surface, float alpha)
        {
            for (int i = 0; i < surface.Height; i++)
            {
                for (int j = 0; j < surface.Width; j++)
                {
                    surface.GetPixel(j, i, out byte r, out byte g, out byte b, out byte a);
                    a = (byte)(a * Math.Clamp(alpha, 0f, 1f));
                    PutPixel(x + j, y + i, r, g, b, a);
                }
            }
        }
        public void Blit(int x, int y, int width, int height, Texture32 surface) => Blit(x, y, width, height, surface, 1f);
        public void Blit(int x, int y, int width, int height, Texture32 surface, float alpha)
        {
            var scaleX = surface.Width / (float)width;
            var scaleY= surface.Height / (float)height;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int srcX = (int)(j * scaleX);
                    int srcY = (int)(i * scaleY);
                    surface.GetPixel(srcX, srcY, out byte r, out byte g, out byte b, out byte a);
                    a = (byte)(a * Math.Clamp(alpha, 0f, 1f));
                    PutPixel(x + j, y + i, r, g, b, a);
                }
            }
        }
        public void Blit(int x, int y, Texture8 surface) => Blit(x, y, surface.Width, surface.Height, surface);
        internal void Blit(int x, int y, int width, int height, Texture8 surface)
        {
            var scaleX = surface.Width / (float)width;
            var scaleY = surface.Height / (float)height;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int srcX = (int)(j * scaleX);
                    int srcY = (int)(i * scaleY);
                    surface.GetPixel(srcX, srcY, out byte r, out byte g, out byte b, out byte a);
                    PutPixel(x + j, y + i, r, g, b, a);
                }
            }
        }
        public void Draw(int x1, int y1, Texture32 surface) => Draw(x1, y1, surface, 1f, []);
        public void Draw(int x1, int y1, Texture32 surface, float alpha) => Draw(x1, y1, surface, alpha, []);
        public void Draw(int x1, int y1, Texture32 surface, IImageTransformation[] transformations) => Draw(x1, y1, surface, 1f, transformations);
        public void Draw(int x1, int y1, Texture32 surface, float alpha, IImageTransformation[] transformations)
        {
            static float[,] CreateTransformationMatrix
          (IImageTransformation[] vectorTransformations, int dimensions)
            {
                float[,] vectorTransMatrix =
                  Matrices.CreateIdentityMatrix(dimensions);

                // combining transformations works by multiplying them  
                foreach (var trans in vectorTransformations)
                    vectorTransMatrix =
                      Matrices.MultiplyUnsafe(vectorTransMatrix, trans.CreateTransformationMatrix());

                return vectorTransMatrix;
            }
            if (transformations == null || transformations.Length == 0)
            {
                Blit(x1, y1, surface, alpha);
                return;
            }

            // filtering transformations  
            var pointTransformations = transformations.ToArray();
            float[,] pointTransMatrix = CreateTransformationMatrix(pointTransformations, 2); // x, y  

            float[][,] products = new float[surface.Pixels.Length][,];



            // First pass: calculate bounds and collect all transformed points
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            // scanning points and applying transformations  
            for (int x = 0; x < surface.Width; x++)
            { // row by row  
                for (int y = 0; y < surface.Height; y++)
                { // column by column  

                    // applying the point transformations  
                    var product = Matrices.MultiplyUnsafe(pointTransMatrix, new float[,] { { x }, { y } });
                    products[x + (y * surface.Width)] = product;
                    var newX = (int)Math.Round(product[0, 0]);
                    var newY = (int)Math.Round(product[1, 0]);

                    // saving stats  
                    minX = Math.Min(minX, newX);
                    minY = Math.Min(minY, newY);
                    maxX = Math.Max(maxX, newX);
                    maxY = Math.Max(maxY, newY);

                }
            }
            // new bitmap width and height  
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;

            var covered = new bool[width * height]; // Track which pixels were set

            for (int x = 0; x < surface.Width; x++)
            {
                for (int y = 0; y < surface.Height; y++)
                {
                    var off = x + (y * surface.Width);
                    var destX = (int)products[off][0, 0] - minX;
                    var destY = (int)products[off][1, 0] - minY;
                    if (destX >= 0 && destX < width && destY >= 0 && destY < height)
                    {

                        surface.GetPixel(x, y, out byte r, out byte g, out byte b, out byte a);
                        a = (byte)(a * Math.Clamp(alpha, 0f, 1f));
                        PutPixel(destX + x1, destY + y1, r, g, b, a);
                        covered[destX + (destY * width)] = true;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                int nx = destX + dx;
                                int ny = destY + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    int idx = ny * width + nx;
                                    if (!covered[idx])
                                    {
                                        PutPixel(nx + x1, ny + y1, r, g, b, a);
                                        covered[idx] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DrawString(int x, int y, string text, IGameFont font, RGBA8? c) => font.DrawString(x, y, this, text,c);
        public void Line(int x, int y, int x2, int y2, byte r, byte g, byte b) => Line(x, y, x2, y2, r, g, b, 255);
        public void Line(int x, int y, int x2, int y2, byte r, byte g, byte b, byte a)
        {
            bool yLonger = false;
            int incrementVal, endVal;
            int shortLen = y2 - y;
            int longLen = x2 - x;
            if (MathF.Abs(shortLen) > MathF.Abs(longLen))
            {
                (longLen, shortLen) = (shortLen, longLen);
                yLonger = true;
            }
            endVal = longLen;
            if (longLen < 0)
            {
                incrementVal = -1;
                longLen = -longLen;
            }
            else incrementVal = 1;
            int decInc;
            if (longLen == 0) decInc = 0;
            else decInc = (shortLen << 16) / longLen;
            int j = 0;
            if (yLonger)
            {
                for (int i = 0; i != endVal; i += incrementVal)
                {
                    int px = x + (j >> 16);
                    int py = y + i;
                    if (px < 0 || px >= Width || py < 0 || py >= Height) return;
                    PutPixel(px, py, r, g, b, a);
                    j += decInc;
                }
            }
            else
            {
                for (int i = 0; i != endVal; i += incrementVal)
                {
                    int px = x + i;
                    int py = y + (j >> 16);
                    if (px < 0 || px >= Width || py < 0 || py >= Height) return;
                    PutPixel(px, py, r, g, b, a);
                    j += decInc;
                }
            }
        }

        
    }
}
