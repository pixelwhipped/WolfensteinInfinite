//Clean
namespace WolfensteinInfinite.Engine.Graphics
{
    public interface ISurface
    {
        public BitDepth Bits { get; }
        public byte[]? Pallet { get; }
        public byte[] Pixels { get; }
        public bool HasTransparency { get; }
        public byte TransparencyIndex { get; }
        public int Height { get; }
        public int Width { get; }
                
        public void PutPixel(int x, int y, RGBA8 c) => PutPixel(x, y, c.R, c.G, c.B, c.A);
        public void PutPixel(int x, int y, byte r, byte g, byte b, byte a);
        public int GetPixel(int x, int y);
        public void PutPixel(int x, int y, int index);
        public void GetPixel(int x, int y, out RGBA8 c)
        {
            GetPixel(x, y, out byte r, out byte g, out byte b, out byte a);
            c = new RGBA8 { R = r, G = g, B = b, A = a };
        }
        public void GetPixel(int x, int y, out byte r, out byte g, out byte b, out byte a);
        
    }
}
