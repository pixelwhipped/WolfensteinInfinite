//Clean
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.Engine.Graphics
{
    public partial class GraphicsSurface : IGraphics
    {
        public byte[] Pallet { get; set; }
        public byte[] Pixels { get; init; } // 256 Pallet Index

        public int Height { get; init; }

        public int Width { get; init; }
        public BitDepth Bits => BitDepth.BIT8;
        public bool HasTransparency => false;
        public byte TransparencyIndex => 0;

        private delegate void LineDrawMode(int x, int y, int x2, int y2, byte index);
        private readonly LineDrawMode LineX;
        private delegate void LineDrawIndiciesMode(int x, int y, int x2, int y2, byte[] indicies);
        private readonly LineDrawIndiciesMode LineGradientX;
        private readonly LineDrawIndiciesMode LineStripX;
        public GraphicsSurface(int width, int height, byte[] pallet)
        {
            Width = width;
            Height = height;
            Pixels = new byte[width * height];
            Pallet = pallet;
            LineX = LineWU;
            LineGradientX = LineWUGradient;
            LineStripX = LineWUStrip;
        }

        public void Circle(int cx, int cy, int r, byte index)
        {
            if (r <= 0) return;
            if (r == 1) { PutPixel(cx, cy, index); return; }
            
            var w = Width;
            var h = Height;
            // Early exit if circle is entirely outside buffer
            if (cx + r < 0 || cx - r >= w || cy + r < 0 || cy - r >= h) return;
            
            int d = (5 - r * 4) / 4;
            int x = 0;
            int y = r;
            do
            {
                int cxX = cx + x, cxY = cx - x;
                int cyY = cy + y, cyY2 = cy - y;
                int cyX = cy + x, cyX2 = cy - x;
                
                // Group 4 x-major positions
                if (cyY >= 0 && cyY < h)
                {
                    if (cxX >= 0 && cxX < w) Pixels[cxX + cyY * w] = index;
                    if (cxY >= 0 && cxY < w) Pixels[cxY + cyY * w] = index;
                }
                if (cyY2 >= 0 && cyY2 < h)
                {
                    if (cxX >= 0 && cxX < w) Pixels[cxX + cyY2 * w] = index;
                    if (cxY >= 0 && cxY < w) Pixels[cxY + cyY2 * w] = index;
                }
                // Group 4 y-major positions  
                if (cyX >= 0 && cyX < h)
                {
                    if (cxX >= 0 && cxX < w) Pixels[cxX + cyX * w] = index;
                    if (cxY >= 0 && cxY < w) Pixels[cxY + cyX * w] = index;
                }
                if (cyX2 >= 0 && cyX2 < h)
                {
                    if (cxX >= 0 && cxX < w) Pixels[cxX + cyX2 * w] = index;
                    if (cxY >= 0 && cxY < w) Pixels[cxY + cyX2 * w] = index;
                }
                
                if (d < 0)
                {
                    d += 2 * x + 1;
                }
                else
                {
                    d += 2 * (x - y) + 1;
                    y--;
                }
                x++;
            } while (x <= y);
        }
        public void Circle(int cx, int cy, int rad, byte r, byte g, byte b) => Circle(cx, cy, rad, FindNearestColor(r, g, b));
        public void CircleFill(int x0, int y0, int r, byte index)
        {
            if (r <= 0) return;
            if (r == 1) PutPixel(x0, y0, index);
            int x = r;
            int y = 0;
            int xChange = 1 - (r << 1);
            int yChange = 0;
            int y2;
            int radiusError = 0;
            var w = Width;
            var h = Height;
            while (x >= y)
            {
                for (int i = x0 - x; i <= x0 + x; i++)
                {
                    y2 = y0 + y;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                        Pixels[i + (y2 * w)] = index;
                    y2 = y0 - y;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                        Pixels[i + (y2 * w)] = index;
                }
                for (int i = x0 - y; i <= x0 + y; i++)
                {
                    y2 = y0 + x;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                        Pixels[i + (y2 * w)] = index;
                    y2 = y0 - x;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                        Pixels[i + (y2 * w)] = index;
                }

                y++;
                radiusError += yChange;
                yChange += 2;
                if (((radiusError << 1) + xChange) > 0)
                {
                    x--;
                    radiusError += xChange;
                    xChange += 2;
                }
            }
        }
        public void CircleFill(int cx, int cy, int rad, byte r, byte g, byte b) => CircleFill(cx, cy, rad, FindNearestColor(r, g, b));
        public void CircleGradient(int x0, int y0, int r, byte[] indicies)
        {
            if (r <= 0) return;
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                CircleFill(x0, y0, r, indicies[0]);
                return;
            }
            if (r == 1)
            {
                int rx = 0;
                int gx = 0;
                int bx = 0;
                for (int i = 0; i < indicies.Length - 1; i++)
                {
                    GetPelletIndex(indicies[i], out byte r1, out byte g1, out byte b1);
                    rx += r1;
                    gx += g1;
                    bx += b1;
                }
                PutPixel(x0, y0, FindNearestColor(
                    (byte)(rx / indicies.Length),
                    (byte)(gx / indicies.Length),
                    (byte)(bx / indicies.Length)));
                return;
            }
            int x = r;
            int y = 0;
            int xChange = 1 - (r << 1);
            int yChange = 0;
            int y2;
            int radiusError = 0;
            var grad = CreateGradent(r + 1, indicies);
            float d;
            var w = Width;
            var h = Height;
            while (x >= y)
            {
                for (int i = x0 - x; i <= x0 + x; i++)
                {
                    y2 = y0 + y;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                    {
                        d = GraphicsHelpers.GetDistance(x0, y0, i, y2);
                        Pixels[i + (y2 * w)] = grad[(int)((d / r) * r)];
                    }
                    y2 = y0 - y;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                    {
                        d = GraphicsHelpers.GetDistance(x0, y0, i, y2);
                        Pixels[i + (y2 * w)] = grad[(int)((d / r) * r)];
                    }
                }
                for (int i = x0 - y; i <= x0 + y; i++)
                {
                    y2 = y0 + x;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                    {
                        d = GraphicsHelpers.GetDistance(x0, y0, i, y2);
                        Pixels[i + (y2 * w)] = grad[(int)((d / r) * r)];
                    }
                    y2 = y0 - x;
                    if (i > 0 && i < w && y2 > 0 && y2 < h)
                    {
                        d = GraphicsHelpers.GetDistance(x0, y0, i, y2);
                        Pixels[i + (y2 * w)] = grad[(int)((d / r) * r)];
                    }
                }

                y++;
                radiusError += yChange;
                yChange += 2;
                if (((radiusError << 1) + xChange) > 0)
                {
                    x--;
                    radiusError += xChange;
                    xChange += 2;
                }
            }
        }

        public void Clear(byte index) => Array.Fill(Pixels, index);

        public void Clear(byte r, byte g, byte b) => Array.Fill(Pixels, FindNearestColor(r, g, b));

        public void Ellipse(int xc, int yc, int rx, int ry, byte index)
        {
            if (rx <= 0 || ry <= 0) return;
            if (rx == 1)
            {
                LineV(xc, yc - ry, ry * 2, index);
                return;
            }
            if (ry == 1)
            {
                LineH(xc - rx, yc, rx * 2, index);
                return;
            }
            double dx, dy, d1, d2;
            int x, y;
            x = 0;
            y = ry;

            // Initial decision parameter of region 1
            d1 = (ry * ry) - (rx * rx * ry) +
                            (0.25f * rx * rx);
            dx = 2 * ry * ry * x;
            dy = 2 * rx * rx * y;
            var w = Width;
            var h = Height;
            while (dx < dy)
            {
                // Print points based on 4-way symmetry
                if (xc + x >= 0 && xc + x <= w - 1 && yc + y >= 0 && yc + y <= h - 1) Pixels[(xc + x) + ((yc + y) * w)] = index;
                if (-x + xc >= 0 && -x + xc <= w - 1 && yc + y >= 0 && yc + y <= h - 1) Pixels[(-x + xc) + ((yc + y) * w)] = index;
                if (x + xc >= 0 && x + xc <= w - 1 && -y + yc >= 0 && -y + yc <= h - 1) Pixels[(x + xc) + ((-y + yc) * w)] = index;
                if (-x + xc >= 0 && -x + xc <= w - 1 && -y + yc >= 0 && -y + yc <= h - 1) Pixels[(-x + xc) + ((-y + yc) * w)] = index;

                // Checking and updating value of
                // decision parameter based on algorithm
                if (d1 < 0)
                {
                    x++;
                    dx += (2 * ry * ry);
                    d1 = d1 + dx + (ry * ry);
                }
                else
                {
                    x++;
                    y--;
                    dx += (2 * ry * ry);
                    dy -= (2 * rx * rx);
                    d1 = d1 + dx - dy + (ry * ry);
                }
            }
            // Decision parameter of region 2
            d2 = ((ry * ry) * ((x + 0.5f) * (x + 0.5f)))
                + ((rx * rx) * ((y - 1) * (y - 1)))
                - (rx * rx * ry * ry);
            // Plotting points of region 2
            while (y >= 0)
            {
                // Print points based on 4-way symmetry
                if (xc + x >= 0 && xc + x <= w - 1 && yc + y >= 0 && yc + y <= h - 1) Pixels[(xc + x) + ((yc + y) * w)] = index;
                if (-x + xc >= 0 && -x + xc <= w - 1 && yc + y >= 0 && yc + y <= h - 1) Pixels[(-x + xc) + ((yc + y) * w)] = index;
                if (x + xc >= 0 && x + xc <= w - 1 && -y + yc >= 0 && -y + yc <= h - 1) Pixels[(x + xc) + ((-y + yc) * w)] = index;
                if (-x + xc >= 0 && -x + xc <= w - 1 && -y + yc >= 0 && -y + yc <= h - 1) Pixels[(-x + xc) + ((-y + yc) * w)] = index;


                // Checking and updating parameter
                // value based on algorithm
                if (d2 > 0)
                {
                    y--;
                    dy -= (2 * rx * rx);
                    d2 = d2 + (rx * rx) - dy;
                }
                else
                {
                    y--;
                    x++;
                    dx += (2 * ry * ry);
                    dy -= (2 * rx * rx);
                    d2 = d2 + dx - dy + (rx * rx);
                }
            }
        }
        public void Ellipse(int cx, int cy, int rx, int ry, byte r, byte g, byte b) => Ellipse(cx, cy, rx, ry, FindNearestColor(r, g, b));
        public void EllipseFill(int xc, int yc, int rx, int ry, byte index)
        {
            if (rx <= 0 || ry <= 0) return;
            if (rx == 1)
            {
                LineV(xc, yc - ry, ry * 2, index);
                return;
            }
            if (ry == 1)
            {
                LineH(xc - rx, yc, rx * 2, index);
                return;
            }

            double dx, dy, d1, d2;
            int x, y;
            x = 0;
            y = ry;

            // Initial decision parameter of region 1
            d1 = (ry * ry) - (rx * rx * ry) +
                            (0.25f * rx * rx);
            dx = 2 * ry * ry * x;
            dy = 2 * rx * rx * y;
            int w = Width;
            int h = Height;
            while (dx < dy)
            {
                for (int xx = xc - x; xx <= xc + x; xx++)
                {
                    if (xx >= 0 && xx <= w - 1 && yc + y >= 0 && yc + y <= h - 1)
                        Pixels[xx + ((yc + y) * w)] = index;
                    if (xx >= 0 && xx <= w - 1 && yc - y >= 0 && yc - y <= h - 1)
                        Pixels[xx + ((yc - y) * w)] = index;

                }
                // Checking and updating value of
                // decision parameter based on algorithm
                if (d1 < 0)
                {
                    x++;
                    dx += (2 * ry * ry);
                    d1 = d1 + dx + (ry * ry);
                }
                else
                {
                    x++;
                    y--;
                    dx += (2 * ry * ry);
                    dy -= (2 * rx * rx);
                    d1 = d1 + dx - dy + (ry * ry);
                }
            }
            // Decision parameter of region 2
            d2 = ((ry * ry) * ((x + 0.5f) * (x + 0.5f)))
                + ((rx * rx) * ((y - 1) * (y - 1)))
                - (rx * rx * ry * ry);
            // Plotting points of region 2
            while (y >= 0)
            {
                for (int xx = xc - x; xx <= xc + x; xx++)
                {
                    if (xx >= 0 && xx <= w - 1 && yc + y >= 0 && yc + y <= h - 1)
                        Pixels[xx + ((yc + y) * w)] = index;
                    if (xx >= 0 && xx <= w - 1 && yc - y >= 0 && yc - y <= h - 1)
                        Pixels[xx + ((yc - y) * w)] = index;

                }

                // Checking and updating parameter
                // value based on algorithm
                if (d2 > 0)
                {
                    y--;
                    dy -= (2 * rx * rx);
                    d2 = d2 + (rx * rx) - dy;
                }
                else
                {
                    y--;
                    x++;
                    dx += (2 * ry * ry);
                    dy -= (2 * rx * rx);
                    d2 = d2 + dx - dy + (rx * rx);
                }
            }
        }
        public void EllipseFill(int cx, int cy, int rx, int ry, byte r, byte g, byte b) => EllipseFill(cx, cy, rx, ry, FindNearestColor(r, g, b));
        private static (float x, float y) ClosestPointToEllipse(float cx, float cy, float a, float b)
        {
            if (cx == 0 && cy == 0) return a < b ? new(a, 0) : new(0, b);

            float px = MathF.Abs(cx);
            float py = MathF.Abs(cy);

            float tx = 0.70710678118f;
            float ty = 0.70710678118f;

            float x, y, ex, ey, rx, ry, qx, qy, r, q, t;

            for (int i = 0; i < 3; ++i)
            {
                x = a * tx;
                y = a * ty;

                ex = (a * a - b * b) * (tx * tx * tx) / a;
                ey = (b * b - a * a) * (ty * ty * ty) / b;

                rx = x - ex;
                ry = y - ey;

                qx = px - ex;
                qy = py - ey;

                r = MathF.Sqrt(rx * rx + ry * ry);
                q = MathF.Sqrt(qy * qy + qx * qx);

                tx = MathF.Min(1, MathF.Max(0, (qx * r / q + ex) / a));
                ty = MathF.Min(1, MathF.Max(0, (qy * r / q + ey) / b));

                t = MathF.Sqrt(tx * tx + ty * ty);

                tx /= t;
                ty /= t;
            }
            return new((a * (cx < 0 ? -tx : tx)), (b * (cy < 0 ? -ty : ty)));
        }
        public void EllipseGradient(int xc, int yc, int rx, int ry, byte[] indicies)
        {
            if (rx <= 0 || ry <= 0) return;
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                EllipseFill(xc, yc, rx, ry, indicies[0]);
                return;
            }
            if (rx == 1)
            {
                LineVGradient(xc, yc - ry, ry * 2, indicies);
                return;
            }
            if (ry == 1)
            {
                LineHGradient(xc - rx, yc, rx * 2, indicies);
                return;
            }
            //Need thes for rx or ry == 1

            float dx, dy, d1, d2;
            int x, y;
            x = 0;
            y = ry;

            // Initial decision parameter of region 1
            d1 = (ry * ry) - (rx * rx * ry) +
                            (0.25f * rx * rx);
            dx = 2 * ry * ry * x;
            dy = 2 * rx * rx * y;
            var lr = Math.Max(rx, ry);
            var grad = CreateGradent(lr + 1, indicies);
            float tl;
            (float x, float y) cp;
            float d;

            var w = Width;
            var h = Height;
            while (dx < dy)
            {
                for (int xx = -x; xx <= x; xx++)
                {
                    if (xx + xc >= 0 && xx + xc <= w - 1 && y + yc >= 0 && y + yc <= h - 1)
                    {
                        d = MathF.Sqrt(MathF.Pow(xx, 2) + MathF.Pow(y, 2));
                        cp = ClosestPointToEllipse(xx, y, rx, ry);
                        tl = MathF.Max((d + MathF.Sqrt(MathF.Pow((cp.x - xx), 2) + MathF.Pow((cp.y - y), 2))), d);
                        Pixels[xx + xc + ((y + yc) * w)] = grad[tl == 0 ? 0 : (int)((d / tl) * lr)];
                    }
                    if (xx + xc >= 0 && xx + xc <= w - 1 && -y + yc >= 0 && -y + yc <= h - 1)
                    {
                        d = MathF.Sqrt(MathF.Pow(xx, 2) + MathF.Pow(-y, 2));
                        cp = ClosestPointToEllipse(xx, -y, rx, ry);
                        tl = MathF.Max((d + MathF.Sqrt(MathF.Pow((cp.x - xx), 2) + MathF.Pow((cp.y - -y), 2))), d);
                        Pixels[xx + xc + ((-y + yc) * w)] = grad[tl == 0 ? 0 : (int)((d / tl) * lr)];
                    }

                }
                // Checking and updating value of
                // decision parameter based on algorithm
                if (d1 < 0)
                {
                    x++;
                    dx += (2 * ry * ry);
                    d1 = d1 + dx + (ry * ry);
                }
                else
                {
                    x++;
                    y--;
                    dx += (2 * ry * ry);
                    dy -= (2 * rx * rx);
                    d1 = d1 + dx - dy + (ry * ry);
                }
            }
            // Decision parameter of region 2
            d2 = ((ry * ry) * ((x + 0.5f) * (x + 0.5f)))
                + ((rx * rx) * ((y - 1) * (y - 1)))
                - (rx * rx * ry * ry);
            // Plotting points of region 2
            while (y >= 0)
            {
                for (int xx = -x; xx <= x; xx++)
                {
                    if (xx + xc >= 0 && xx + xc <= w - 1 && y + yc >= 0 && y + yc <= h - 1)
                    {
                        d = MathF.Sqrt(MathF.Pow(xx, 2) + MathF.Pow(y, 2));
                        cp = ClosestPointToEllipse(xx, y, rx, ry);
                        tl = d + MathF.Max((MathF.Sqrt(MathF.Pow((cp.x - xx), 2) + MathF.Pow((cp.y - y), 2))), 0);

                        Pixels[xx + xc + ((y + yc) * w)] = grad[tl == 0 ? 0 : (int)((d / tl) * lr)];
                    }
                    if (xx + xc >= 0 && xx + xc <= w - 1 && -y + yc >= 0 && -y + yc <= h - 1)
                    {
                        d = MathF.Sqrt(MathF.Pow(xx, 2) + MathF.Pow(-y, 2));
                        cp = ClosestPointToEllipse(xx, -y, rx, ry);
                        tl = d + MathF.Max((MathF.Sqrt(MathF.Pow((cp.x - xx), 2) + MathF.Pow((cp.y - -y), 2))), 0);
                        Pixels[xx + xc + ((-y + yc) * w)] = grad[tl == 0 ? 0 : (int)((d / tl) * lr)];
                    }

                }

                // Checking and updating parameter
                // value based on algorithm
                if (d2 > 0)
                {
                    y--;
                    dy -= (2 * rx * rx);
                    d2 = d2 + (rx * rx) - dy;
                }
                else
                {
                    y--;
                    x++;
                    dx += (2 * ry * ry);
                    dy -= (2 * rx * rx);
                    d2 = d2 + dx - dy + (rx * rx);
                }
            }
        }

        public void Line(int x, int y, int x2, int y2, byte index)
        {
            if (x == x2)
            {
                if (y > y2) (y2, y) = (y, y2);
                LineV(x, y, y2 - y, index);

            }
            else if (y == y2)
            {
                if (x > x2) (x2, x) = (x, x2);
                LineH(x, y, x2 - x, index);
            }
            else
            {
                LineX(x, y, x2, y2, index);
            }
        }
        public void Line(int x, int y, int x2, int y2, byte r, byte g, byte b) => Line(x, y, x2, y2, FindNearestColor(r, g, b));
        private void LineH(int x, int y, int l, byte index)
        {
            if (x < 0)
            {
                l -= x;
                x = 0;
                if (l < 0) return;
            }
            if (y < 0 || y >= Height || x > Width) return;
            var x2 = Math.Min(x + l, Width - 1);
            x = Math.Max(x, 0);
            if (x2 - x < 1) return;
            y *= Width;
            Array.Fill(Pixels, index, x + y, x2 - x);
        }
        private void LineV(int x, int y, int l, byte index)
        {
            if (x < 0 || x >= Width) return;
            var y2 = Math.Min(y + l, Height - 1);
            y = Math.Max(y, 0);
            for (int i = y; i < y2; i++)
            {
                Pixels[(x + (i * Width))] = index;
            }
        }
        public void LineGradient(int x1, int y1, int x2, int y2, RGBA8[] indicies) => LineGradient(x1, y1, x2, y2, indicies.Select((i) => FindNearestColor(i.R, i.G, i.B)).ToArray());
        public void LineGradient(int x, int y, int x2, int y2, byte[] indicies)
        {
            if (x == x2)
            {
                if (y > y2) (y2, y) = (y, y2);
                LineVGradient(x, y, y2 - y, indicies);
            }
            else if (y == y2)
            {
                if (x > x2) (x2, x) = (x, x2);
                LineHGradient(x, y, x2 - x, indicies);
            }
            else
            {
                LineGradientX(x, y, x2, y2, indicies);
            }

        }
        private void LineHGradient(int x, int y, int l, byte[] indicies)
        {
            if (y < 0 || y >= Height) return;
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                LineH(x, y, l, indicies[0]);
                return;
            }
            var grad = CreateGradent(l + 1, indicies);

            var x2 = Math.Min(x + l, Width - 1);
            x = Math.Max(x, 0);
            y *= Width;
            float d = 0;
            for (int i = x; i < x2; i++)
            {
                Pixels[i + y] = grad[(int)((d / l) * l)];
                d++;
            }
        }
        private void LineVGradient(int x, int y, int l, byte[] indicies)
        {
            if (x < 0 || x >= Width) return;
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                LineV(x, y, l, indicies[0]);
                return;
            }
            var grad = CreateGradent(l + 1, indicies);
            var y2 = Math.Min(y + l, Height - 1);
            y = Math.Max(y, 0);
            float d = 0;
            for (int i = y; i < y2; i++)
            {
                Pixels[(x + (i * Width))] = grad[(int)((d / l) * l)];
                d++;
            }
        }
        public void LineStrip(int x, int y, int x2, int y2, byte[] indicies)
        {
            {
                if (x == x2)
                {
                    if (y > y2) (y2, y) = (y, y2);
                    LineVStrip(x, y, y2 - y, indicies);
                }
                else if (y == y2)
                {
                    if (x > x2) (x2, x) = (x, x2);
                    LineHStrip(x, y, x2 - x, indicies);
                }
                else
                {
                    LineStripX(x, y, x2, y2, indicies);
                }

            }
        }
        public void LineStrip(int x1, int y1, int x2, int y2, RGBA8[] indicies) => LineStrip(x1, y1, x2, y2, indicies.Select((i) => FindNearestColor(i.R, i.G, i.B)).ToArray());
        private void LineHStrip(int x, int y, int l, byte[] indicies)
        {
            if (y < 0 || y >= Height) return;
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                LineH(x, y, l, indicies[0]);
                return;
            }
            var x2 = Math.Min(x + l, Width - 1);
            x = Math.Max(x, 0);
            y *= Width;
            float d = 0;
            for (int i = x; i < x2; i++)
            {
                Pixels[i + y] = indicies[(int)((d / l) * (indicies.Length - 1))];
                d++;
            }
        }
        private void LineVStrip(int x, int y, int l, byte[] indicies)
        {
            if (x < 0 || x >= Width) return;
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                LineV(x, y, l, indicies[0]);
                return;
            }
            var y2 = Math.Min(y + l, Height - 1);
            y = Math.Max(y, 0);
            float d = 0;
            for (int i = y; i < y2; i++)
            {
                Pixels[(x + (i * Width))] = indicies[(int)((d / l) * (indicies.Length - 1))];
                d++;
            }
        }

        public void PutPixel(int x, int y, int index)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            var off = (x + (y * Width));
            Pixels[off] = (byte)index;
        }

        public void PutPixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            var off = (x + (y * Width));
            if (a != 255)
            {
                var amt = a / 255f;
                var i = Pixels[off] * 3;
                byte rb = Pallet[i];
                byte gb = Pallet[i + 1];
                byte bb = Pallet[i + 2];
                r = (byte)(rb + (r - rb) * amt);
                g = (byte)(gb + (g - gb) * amt);
                b = (byte)(bb + (b - bb) * amt);
            }
            Pixels[off] = FindNearestColor(r, g, b);
        }

        public void Rect(int x, int y, int width, int height, byte index)
        {
            if (x + width < 0 || x >= Width || y>= Height) return;
            var x2 = Math.Min(x + width, Width);
            x = Math.Max(x, 0);
            Array.Fill(Pixels, index, x + (y * Width), x2 - x);
            int e = (y + height);
            if (x >= 0 && e <= Height - 1)
                Array.Fill(Pixels, index, x + (e * Width), x2 - x);
            e = Math.Min(e, Height - 1);
            x2 = x + width - 1;
            for (int i = y; i < e; i++)
            {
                if (x >= 0 && x <= Width)
                    Pixels[x + (i * Width)] = index;
                if (x2 >= 0 && x2 <= Width)
                    Pixels[x2 + (i * Width)] = index;
            }
        }
        public void Rect(int x, int y, int width, int height, byte r, byte g, byte b) => Rect(x, y, width, height, FindNearestColor(r, g, b));
        public void RectFill(int x, int y, int width, int height, byte index)
        {
            if (x + width < 0 || x >= Width) return;
            var x2 = Math.Min(x + width, Width - 1);
            x = Math.Max(x, 0);
            int e = Math.Min(y + height, Height - 1);
            for (int i = y; i < e; i++)
            {
                y = i * Width;
                Array.Fill(Pixels, index, x + y, x2 - x);
            }
        }
        public void RectFill(int x, int y, int width, int height, byte r, byte g, byte b) => RectFill(x, y, width, height, FindNearestColor(r, g, b));
        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte index)
        {
            Line(x1, y1, x2, y2, index);
            Line(x2, y2, x3, y3, index);
            Line(x3, y3, x1, y1, index);
        }
        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte r, byte g, byte b) => Triangle(x1, y1, x2, y2, x3, y3, FindNearestColor(r, b, g));
        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, byte index)
        {
            if (y2 < y1)
            {
                (y2, y1) = (y1, y2);
                (x2, x1) = (x1, x2);
            }
            if (y3 < y1)
            {
                (y3, y1) = (y1, y3);
                (x3, x1) = (x1, x3);
            }
            if (y3 < y2)
            {
                (y3, y2) = (y2, y3);
                (x3, x2) = (x2, x3);
            }
            int total_height = y3 - y1;

            for (int y = y1; y <= y2; y++)
            {
                int segment_height = y2 - y1 + 1;
                float alpha = (float)(y - y1) / total_height;
                float beta = (float)(y - y1) / segment_height; // be careful with divisions by zero 
                int ax = (int)(x1 + (x3 - x1) * alpha);
                int bx = (int)(x1 + (x2 - x1) * beta);
                if (ax > bx)
                {
                    (bx, ax) = (ax, bx);
                }
                LineH(ax, y, (bx - ax), index);
            }
            for (int y = y2; y <= y3; y++)
            {
                int segment_height = y3 - y2 + 1;
                float alpha = (float)(y - y1) / total_height;
                float beta = (float)(y - y2) / segment_height; // be careful with divisions by zero 

                int ax = (int)(x1 + (x3 - x1) * alpha);
                int bx = (int)(x2 + (x3 - x2) * beta);
                if (ax > bx)
                {
                    (bx, ax) = (ax, bx);
                }
                LineH(ax, y, (bx - ax), index);
            }
        }
        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, byte r, byte g, byte b) => TriangleFill(x1, y1, x2, y2, x3, y3, FindNearestColor(r, b, g));
        public void TriangleGradient(int x1, int y1, int x2, int y2, int x3, int y3, byte c1, byte c2, byte c3)
        {
            float d1, d2, d3;
            if (y2 < y1)
            {
                (y2, y1) = (y1, y2);
                (x2, x1) = (x1, x2);
                (c2, c1) = (c1, c2);
            }
            if (y3 < y1)
            {
                (y3, y1) = (y1, y3);
                (x3, x1) = (x1, x3);
                (c3, c1) = (c1, c3);
            }
            if (y3 < y2)
            {
                (y3, y2) = (y2, y3);
                (c3, c2) = (c2, c3);
                x3 = x2;
            }

            int total_height = y3 - y1;

            for (int y = y1; y <= y2; y++)
            {
                if (y < 0) continue;
                if (y >= Height) return;
                int segment_height = y2 - y1 + 1;
                float alpha = (float)(y - y1) / total_height;
                float beta = (float)(y - y1) / segment_height; // be careful with divisions by zero 
                int ax = (int)(x1 + (x3 - x1) * alpha);
                int bx = (int)(x1 + (x2 - x1) * beta);
                if (ax > bx)
                {
                    (bx, ax) = (ax, bx);
                }

                for (int x = ax; x < bx; x++)
                {
                    if (x < 0 || x >= Width) return;
                    var off = (x + (y * Width));
                    d1 = MathF.Sqrt(MathF.Pow(x - x1, 2) + MathF.Pow(y - y1, 2));
                    d2 = MathF.Sqrt(MathF.Pow(x - x2, 2) + MathF.Pow(y - y2, 2));
                    d3 = MathF.Sqrt(MathF.Pow(x - x3, 2) + MathF.Pow(y - y3, 2));
                    var s = d1 + d2 + d3;
                    d1 /= s;
                    d2 /= s;
                    d3 /= s;
                    GetPelletIndex(c1, out byte r1, out byte g1, out byte b1);
                    GetPelletIndex(c2, out byte r2, out byte g2, out byte b2);
                    GetPelletIndex(c3, out byte r3, out byte g3, out byte b3);
                    r1 = (byte)((r1 * d1) + (r2 * d2) + (r3 * d3));
                    g1 = (byte)((g1 * d1) + (g2 * d2) + (g3 * d3));
                    b1 = (byte)((b1 * d1) + (b2 * d2) + (b3 * d3));
                    Pixels[off] = FindNearestColor(r1, g1, b1);

                }
            }
            for (int y = y2; y <= y3; y++)
            {
                if (y < 0) continue;
                if (y >= Height) return;
                int segment_height = y3 - y2 + 1;
                float alpha = (float)(y - y1) / total_height;
                float beta = (float)(y - y2) / segment_height; // be careful with divisions by zero 

                int ax = (int)(x1 + (x3 - x1) * alpha);
                int bx = (int)(x2 + (x3 - x2) * beta);
                if (ax > bx)
                {
                    (bx, ax) = (ax, bx);
                }

                for (int x = ax; x < bx; x++)
                {
                    if (x < 0 || x >= Width) return;
                    var off = (x + (y * Width));
                    d1 = MathF.Sqrt(MathF.Pow(x - x1, 2) + MathF.Pow(y - y1, 2));
                    d2 = MathF.Sqrt(MathF.Pow(x - x2, 2) + MathF.Pow(y - y2, 2));
                    d3 = MathF.Sqrt(MathF.Pow(x - x3, 2) + MathF.Pow(y - y3, 2));
                    var s = d1 + d2 + d3;
                    d1 /= s;
                    d2 /= s;
                    d3 /= s;
                    GetPelletIndex(c1, out byte r1, out byte g1, out byte b1);
                    GetPelletIndex(c2, out byte r2, out byte g2, out byte b2);
                    GetPelletIndex(c3, out byte r3, out byte g3, out byte b3);
                    r1 = (byte)((r1 * d1) + (r2 * d2) + (r3 * d3));
                    g1 = (byte)((g1 * d1) + (g2 * d2) + (g3 * d3));
                    b1 = (byte)((b1 * d1) + (b2 * d2) + (b3 * d3));
                    Pixels[off] = FindNearestColor(r1, g1, b1);
                }
            }
        }

        public byte FindNearestColor(byte r, byte g, byte b) => GraphicsHelpers.FindNearestColor(r, g, b, Pallet);

        public void GetPelletIndex(byte index, out byte r, out byte g, out byte b)
        {
            var i = index * 3;
            r = Pallet[i];
            g = Pallet[i + 1];
            b = Pallet[i + 2];
        }
        public int GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return TransparencyIndex;
            var off = (x + (y * Width));
            return Pixels[off];
        }
        public void GetPixel(int x, int y, out byte r, out byte g, out byte b, out byte a)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                GetPelletIndex(TransparencyIndex, out r, out g, out b);
                a = 0;
                return;
            }

            var off = (x + (y * Width));
            GetPelletIndex(Pixels[off], out r, out g, out b);
            a = 255;
        }
        public void Blit(int x, int y, ISurface surface)
        {
            if (surface.Bits == Bits && surface.Pallet == Pallet) //Surface known to be 8 bits
            {
                switch (surface.Bits)
                {
                    case BitDepth.BIT8:
                        {
                            var sourceSpan = surface.Pixels.AsSpan();
                            var destSpan = Pixels.AsSpan();

                            for (int i = 0; i < surface.Height; i++)
                            {
                                var destY = y + i;
                                if (destY < 0 || destY >= Height) continue;

                                var sourceRow = sourceSpan.Slice(i * surface.Width, surface.Width);
                                var destRowStart = destY * Width + x;

                                if (!surface.HasTransparency)
                                {
                                    // Direct memory copy - fastest possible
                                    sourceRow.CopyTo(destSpan.Slice(destRowStart, surface.Width));
                                }
                                else
                                {
                                    // Copy with transparency check
                                    var transparencyIndex = surface.TransparencyIndex;
                                    for (int j = 0; j < sourceRow.Length; j++)
                                    {
                                        var pixel = sourceRow[j];
                                        if (pixel != transparencyIndex)
                                        {
                                            destSpan[destRowStart + j] = pixel;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case BitDepth.BIT32:
                        {
                            //Shouldn't Happen
                            break;
                        }
                }
            }
            else
            {
                switch (surface.Bits)
                {
                    case BitDepth.BIT8: //Same bits but different pallet
                        {
                            for (int i = 0; i < surface.Height; i++)
                            {
                                for (int j = 0; j < surface.Width; j++)
                                {
                                    if (surface.HasTransparency && surface.GetPixel(j, i) == surface.TransparencyIndex) continue;
                                    surface.GetPixel(j, i, out byte r, out byte g, out byte b, out byte _);
                                    var c = FindNearestColor(r, g, b);
                                    PutPixel(x + j, y + i, c);
                                }
                            }
                            break;
                        }
                    case BitDepth.BIT32:
                        {
                            for (int i = 0; i < surface.Height; i++)
                            {
                                for (int j = 0; j < surface.Width; j++)
                                {
                                    surface.GetPixel(j, i, out byte r, out byte g, out byte b, out byte a);
                                    PutPixel(x + j, y + i, r,g,b,a);
                                }
                            }
                            break;
                        }
                }
            }

        }
        public void Draw(int x1, int y1, ISurface surface, IImageTransformation[] transformations)
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
                Blit(x1, y1, surface);
                return;
            }

            // filtering transformations  
            var pointTransformations = transformations.ToArray();
            float[,] pointTransMatrix = CreateTransformationMatrix(pointTransformations, 2); // x, y  

            float[][,] products = new float[surface.Pixels.Length][,];
            if (surface.Bits == Bits && surface.Pallet == Pallet) //Surface known to be 8 bits
            {
                switch (surface.Bits)
                {
                    case BitDepth.BIT8:
                        {
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
                                        var pixelValue = surface.Pixels[off];
                                        PutPixel(destX+x1, destY+y1, pixelValue);
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
                                                        PutPixel(nx + x1, ny + y1, pixelValue);
                                                        covered[idx] = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                            
                    case BitDepth.BIT32: break; //Shouldn't Happen                            

                        
                }
            }
            else
            {
                switch (surface.Bits)
                {
                    case BitDepth.BIT8: //Same bits but different pallet                        
                    case BitDepth.BIT32: 
                        {
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
                                        
                                        surface.GetPixel(x, y, out byte r, out byte g, out byte b, out _);
                                        var pixelValue = FindNearestColor(r, g, b);
                                        PutPixel(destX + x1, destY + y1, pixelValue);
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
                                                        PutPixel(nx + x1, ny + y1, pixelValue);
                                                        covered[idx] = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }
    }
}
