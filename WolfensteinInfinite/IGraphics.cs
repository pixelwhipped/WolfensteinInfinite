namespace WolfensteinInfinite
{
    public interface IGraphics: ISurface
    {
        public byte FindNearestColor(byte r, byte g, byte b);
        public void GetPelletIndex(byte index, out byte r, out byte g, out byte b);
        public void Clear(byte index);
        public void Clear(byte r, byte g, byte b);
        public void Clear(RGBA8 c) => Clear(c.R, c.G, c.B);
        public void Line(int x1, int y1, int x2, int y2, RGBA8 c) => Line(x1, y1, x2, y2, c.R, c.G, c.B);
        public void Line(int x1, int y1, int x2, int y2, byte r, byte g, byte b);
        public void Line(int x1, int y1, int x2, int y2, byte index);
        public void LineGradient(int x1, int y1, int x2, int y2, byte[] indicies);
        public void LineGradient(int x1, int y1, int x2, int y2, RGBA8[] colors);
        public void LineStrip(int x1, int y1, int x2, int y2, byte[] indicies);
        public void LineStrip(int x1, int y1, int x2, int y2, RGBA8[] colors);
        public void Circle(int centerX, int centerY, int radius, byte index);
        public void Circle(int centerX, int centerY, int radius, RGBA8 c) => Circle(centerX, centerY, radius, c.R, c.G, c.B);
        public void Circle(int centerX, int centerY, int radius, byte r, byte g, byte b);
        public void CircleFill(int centerX, int centerY, int radius, byte index);
        public void CircleFill(int centerX, int centerY, int radius, RGBA8 c) => CircleFill(centerX, centerY, radius, c.R, c.G, c.B);
        public void CircleFill(int centerX, int centerY, int radius, byte r, byte g, byte b);
        public void CircleGradient(int centerX, int centerY, int radius, byte[] indicies);
        public void Ellipse(int centerX, int centerY, int radiusX, int radiusY, byte index);
        public void Ellipse(int centerX, int centerY, int radiusX, int radiusY, RGBA8 c) => Ellipse(centerX, centerY, radiusX, radiusY, c.R, c.G, c.B);
        public void Ellipse(int centerX, int centerY, int radiusX, int radiusY, byte r, byte g, byte b);
        public void EllipseFill(int centerX, int centerY, int radiusX, int radiusY, byte index);
        public void EllipseFill(int centerX, int centerY, int radiusX, int radiusY, RGBA8 c) => EllipseFill(centerX, centerY, radiusX, radiusY, c.R, c.G, c.B);
        public void EllipseFill(int centerX, int centerY, int radiusX, int radiusY, byte r, byte g, byte b);
        public void EllipseGradient(int centerX, int centerY, int radiusX, int radiusY, byte[] indicies);
        public void Rect(int x, int y, int width, int height, byte index);
        public void Rect(int x, int y, int width, int height, RGBA8 c) => Rect(x, y, width, height, c.R, c.G, c.B);
        public void Rect(int x, int y, int width, int height, byte r, byte g, byte b);
        public void RectFill(int x, int y, int width, int height, byte index);
        public void RectFill(int x, int y, int width, int height, RGBA8 c) => RectFill(x, y, width, height, c.R, c.G, c.B);
        public void RectFill(int x, int y, int width, int height, byte r, byte g, byte b);
        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte index);
        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, RGBA8 c) => Triangle(x1, y1, x2, y2, x3, y3, c.R, c.G, c.B);
        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte r, byte g, byte b);
        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, byte index);
        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, RGBA8 c) => TriangleFill(x1, y1, x2, y2, x3, y3, c.R, c.G, c.B);
        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, byte r, byte g, byte b);
        public void TriangleGradient(int x1, int y1, int x2, int y2, int x3, int y3, byte c1, byte c2, byte c3);
        public void Blit(int x, int y, ISurface surface);
        public void Draw(int x1, int y1, ISurface surface, IImageTransformation[] transformations);
    }
}