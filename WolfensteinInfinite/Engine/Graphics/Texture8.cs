//Clean
using System.Runtime.InteropServices;

namespace WolfensteinInfinite.Engine.Graphics
{
    public class Texture8(int width, int height, byte[] pixels, byte[] pallet) : ISurface
    {
        public BitDepth Bits => BitDepth.BIT8;
        public byte[] Pallet { get; init; } = pallet;
        public byte[] Pixels { get; init; } = pixels;
        public bool HasTransparency { get; set; }
        public byte TransparencyIndex { get; set; }
        public int Height { get; init; } = height;
        public int Width { get; init; } = width;
        public int GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0;
            return Pixels[x + y * Width];
        }
        public RGBA8[] GetUsedColors()
        {
            Span<byte> memory = Pallet;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            return pixelsrgba.ToArray();
        }

        public void PutPixel(int x, int y, int c)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Pixels[x + y * Width] = (byte)c;
        }
        public void PutPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Pixels[x + y * Width] = GraphicsHelpers.FindNearestColor(r, g, b, Pallet);
        }
        public void GetPixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
        { 
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                r = g = b = a = 0;
                return;
            }
            var off = Pixels[x + y * Width] * 3;
            if(HasTransparency && off == TransparencyIndex)
            {
                r=g=b=a=0;
                return;
            }
            r = Pallet[off];
            g = Pallet[off + 1];
            b = Pallet[off + 2];
            a = 255;
        }
    }
}
