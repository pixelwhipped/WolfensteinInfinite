//Clean
namespace WolfensteinInfinite.Engine.Graphics
{
    public partial class GraphicsSurface
    {
        // THE EXTREMELY FAST LINE ALGORITHM Variation D (Addition Fixed Point)
        private void LineEFLAD(int x, int y, int x2, int y2, byte index)
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
                    var off = (px + (py * Width));

                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
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
                    var off = (px + (py * Width));
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                    j += decInc;
                }
            }
        }
        // THE EXTREMELY FAST LINE ALGORITHM Variation C (Addition)
        private void LineEFLAC(int x, int y, int x2, int y2, byte index)
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

            float decInc;
            if (longLen == 0) decInc = shortLen;
            else decInc = shortLen / (float)longLen;
            float j = 0.0f;
            if (yLonger)
            {
                for (int i = 0; i != endVal; i += incrementVal)
                {

                    int px = x + (int)j;
                    int py = y + i;
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    var off = (px + (py * Width));
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                    j += decInc;
                }
            }
            else
            {
                for (int i = 0; i != endVal; i += incrementVal)
                {
                    int px = x + i;
                    int py = y + (int)j;
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    var off = (px + (py * Width));
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                    j += decInc;
                }
            }


        }
        // THE EXTREMELY FAST LINE ALGORITHM Variation B (Multiplication)
        private void LineEFLAB(int x, int y, int x2, int y2, byte index)
        {
            bool yLonger = false;
            int incrementVal;
            int shortLen = y2 - y;
            int longLen = x2 - x;

            if (MathF.Abs(shortLen) > MathF.Abs(longLen))
            {
                (longLen, shortLen) = (shortLen, longLen);
                yLonger = true;
            }

            if (longLen < 0) incrementVal = -1;
            else incrementVal = 1;

            float multDiff;
            if (longLen == 0.0) multDiff = shortLen;
            else multDiff = shortLen / (float)longLen;
            if (yLonger)
            {
                for (int i = 0; i != longLen; i += incrementVal)
                {
                    int px = x + (int)(i * multDiff);
                    int py = y + i;
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    var off = (px + (py * Width));
                    // if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                }
            }
            else
            {
                for (int i = 0; i != longLen; i += incrementVal)
                {
                    int px = x + i;
                    int py = y + (int)(i * multDiff);
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    var off = (px + (py * Width));
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;

                }
            }
        }

        // THE EXTREMELY FAST LINE ALGORITHM Variation A (Division)
        private void LineEFLAA(int x, int y, int x2, int y2, byte index)
        {

            bool yLonger = false;
            int incrementVal;

            int shortLen = y2 - y;
            int longLen = x2 - x;
            if (MathF.Abs(shortLen) > MathF.Abs(longLen))
            {
                (longLen, shortLen) = (shortLen, longLen);
                yLonger = true;
            }

            if (longLen < 0) incrementVal = -1;
            else incrementVal = 1;

            float divDiff;
            if (shortLen == 0) divDiff = longLen;
            else divDiff = longLen / (float)shortLen;
            if (yLonger)
            {
                for (int i = 0; i != longLen; i += incrementVal)
                {
                    int px = x + (int)(i / divDiff);
                    int py = y + i;
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    var off = (px + (py * Width));
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                }
            }
            else
            {
                for (int i = 0; i != longLen; i += incrementVal)
                {
                    int px = x + i;
                    int py = y + (int)(i / divDiff);
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    var off = (px + (py * Width));
                    //if (off > -VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                }
            }
        }
        // DDA Line Algorithm
        private void LineDDA(int x1, int y1, int x2, int y2, byte index)
        {
            int length, i;
            float x, y;
            float xincrement;
            float yincrement;

            length = (int)MathF.Abs(x2 - x1);
            if (MathF.Abs(y2 - y1) > length) length = (int)MathF.Abs(y2 - y1);
            xincrement = (x2 - x1) / (float)length;
            yincrement = (y2 - y1) / (float)length;
            x = x1 + 0.5f;
            y = y1 + 0.5f;
            for (i = 1; i <= length; ++i)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                var off = ((int)x + ((int)y) * Width);
                //if (off >= VideoMemory.Length || off < 0) return;
                Pixels[off] = index;
                x += xincrement;
                y += yincrement;
            }

        }

        // Bresenham Line Algorithm
        private void LineBRES(int x1, int y1, int x2, int y2, byte index)
        {
            int x, y;
            int dx, dy;
            int incx, incy;
            int balance;
            int off;

            if (x2 >= x1)
            {
                dx = x2 - x1;
                incx = 1;
            }
            else
            {
                dx = x1 - x2;
                incx = -1;
            }

            if (y2 >= y1)
            {
                dy = y2 - y1;
                incy = 1;
            }
            else
            {
                dy = y1 - y2;
                incy = -1;
            }

            x = x1;
            y = y1;

            if (dx >= dy)
            {
                dy <<= 1;
                balance = dy - dx;
                dx <<= 1;

                while (x != x2)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    off = x + (y * Width);
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                    if (balance >= 0)
                    {
                        y += incy;
                        balance -= dx;
                    }
                    balance += dy;
                    x += incx;
                }
                if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                off = x + (y * Width);
                //if (off >= VideoMemory.Length || off < 0) return;
                Pixels[off] = index;
            }
            else
            {
                dx <<= 1;
                balance = dx - dy;
                dy <<= 1;

                while (y != y2)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                    off = x + (y * Width);
                    //if (off >= VideoMemory.Length || off < 0) return;
                    Pixels[off] = index;
                    if (balance >= 0)
                    {
                        x += incx;
                        balance -= dy;
                    }
                    balance += dx;
                    y += incy;
                }
                if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                off = x + (y * Width);
                //if (off >= VideoMemory.Length || off < 0) return;
                Pixels[off] = index;
            }
        }

        // Wu Line Algorithm
        private void LineWU(int x0, int y0, int x1, int y1, byte index)
        {
            bool exit = false;
            void PutPixel(int x, int y, byte index)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                var off = (x + (y * Width));
                //if (off >= VideoMemory.Length || off < 0) { exit = true; return; };
                Pixels[off] = index;
            }
            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
            if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }

            PutPixel(x0, y0, index);
            PutPixel(x1, y1, index);
            if (exit) return;
            if (dx > dy)
            {
                int length = (dx - 1) >> 2;
                int extras = (dx - 1) & 3;
                int incr2 = (dy << 2) - (dx << 1);
                if (incr2 < 0)
                {
                    int c = dy << 1;
                    int incr1 = c << 1;
                    int d = incr1 - dx;
                    for (int i = 0; i < length; i++)
                    {
                        x0 += stepx;
                        x1 -= stepx;
                        if (d < 0)
                        {                                     // Pattern:
                            PutPixel(x0, y0, index);                    //
                            PutPixel(x0 += stepx, y0, index);                 //  x o o
                            PutPixel(x1, y1, index);                          //
                            PutPixel(x1 -= stepx, y1, index);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {                                 // Pattern:
                                PutPixel(x0, y0, index);                      //      o
                                PutPixel(x0 += stepx, y0 += stepy, index);    //  x o
                                PutPixel(x1, y1, index);                      //
                                PutPixel(x1 -= stepx, y1 -= stepy, index);
                            }
                            else
                            {
                                PutPixel(x0, y0 += stepy, index);             // Pattern:
                                PutPixel(x0 += stepx, y0, index);             //    o o 
                                PutPixel(x1, y1 -= stepy, index);             //  x
                                PutPixel(x1 -= stepx, y1, index);             //
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d < 0)
                        {
                            PutPixel(x0 += stepx, y0, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1, index);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0 += stepx, y0, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1, index);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy, index);
                        }
                    }
                    if (exit) return;
                }
                else
                {
                    int c = (dy - dx) << 1;
                    int incr1 = c << 1;
                    int d = incr1 + dx;
                    for (int i = 0; i < length; i++)
                    {
                        x0 += stepx;
                        x1 -= stepx;
                        if (d > 0)
                        {
                            PutPixel(x0, y0 += stepy, index);                      // Pattern:
                            PutPixel(x0 += stepx, y0 += stepy, index);             //      o
                            PutPixel(x1, y1 -= stepy, index);                      //    o
                            PutPixel(x1 -= stepx, y1 -= stepy, index);           //  x
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0, index);                           // Pattern:
                                PutPixel(x0 += stepx, y0 += stepy, index);         //      o
                                PutPixel(x1, y1, index);                           //  x o
                                PutPixel(x1 -= stepx, y1 -= stepy, index);         //
                            }
                            else
                            {
                                PutPixel(x0, y0 += stepy, index);                  // Pattern:
                                PutPixel(x0 += stepx, y0, index);                  //    o o
                                PutPixel(x1, y1 -= stepy, index);                  //  x
                                PutPixel(x1 -= stepx, y1, index);                  //
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy, index);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0 += stepx, y0, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1, index);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0, index);
                            if (extras > 2)
                            {
                                if (d > c)
                                    PutPixel(x1 -= stepx, y1 -= stepy, index);
                                else
                                    PutPixel(x1 -= stepx, y1, index);
                            }
                        }
                    }
                    if (exit) return;
                }
            }
            else
            {
                int length = (dy - 1) >> 2;
                int extras = (dy - 1) & 3;
                int incr2 = (dx << 2) - (dy << 1);
                if (incr2 < 0)
                {
                    int c = dx << 1;
                    int incr1 = c << 1;
                    int d = incr1 - dy;
                    for (int i = 0; i < length; i++)
                    {
                        y0 += stepy;
                        y1 -= stepy;
                        if (d < 0)
                        {
                            PutPixel(x0, y0, index);
                            PutPixel(x0, y0 += stepy, index);
                            PutPixel(x1, y1, index);
                            PutPixel(x1, y1 -= stepy, index);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0, index);
                                PutPixel(x0 += stepx, y0 += stepy, index);
                                PutPixel(x1, y1, index);
                                PutPixel(x1 -= stepx, y1 -= stepy, index);
                            }
                            else
                            {
                                PutPixel(x0 += stepx, y0, index);
                                PutPixel(x0, y0 += stepy, index);
                                PutPixel(x1 -= stepx, y1, index);
                                PutPixel(x1, y1 -= stepy, index);
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d < 0)
                        {
                            PutPixel(x0, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1, y1 -= stepy, index);
                        }
                        else
                        if (d < c)
                        {
                            //PutPixel(stepx, y0 += stepy, index);
                            PutPixel(x0, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1, y1 -= stepy, index);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy, index);
                        }
                    }
                    if (exit) return;
                }
                else
                {
                    int c = (dx - dy) << 1;
                    int incr1 = c << 1;
                    int d = incr1 + dy;
                    for (int i = 0; i < length; i++)
                    {
                        y0 += stepy;
                        y1 -= stepy;
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0, index);
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            PutPixel(x1 -= stepx, y1, index);
                            PutPixel(x1 -= stepx, y1 -= stepy, index);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0, index);
                                PutPixel(x0 += stepx, y0 += stepy, index);
                                PutPixel(x1, y1, index);
                                PutPixel(x1 -= stepx, y1 -= stepy, index);
                            }
                            else
                            {
                                PutPixel(x0 += stepx, y0, index);
                                PutPixel(x0, y0 += stepy, index);
                                PutPixel(x1 -= stepx, y1, index);
                                PutPixel(x1, y1 -= stepy, index);
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy, index);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 2) PutPixel(x1, y1 -= stepy, index);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy, index);
                            if (extras > 1) PutPixel(x0, y0 += stepy, index);
                            if (extras > 2)
                            {
                                if (d > c)
                                    PutPixel(x1 -= stepx, y1 -= stepy, index);
                                else
                                    PutPixel(x1, y1 -= stepy, index);
                            }
                        }
                    }
                    if (exit) return;
                }
            }

        }

        private byte[] CreateGradent(int length, byte[] indices)
        {
            if (indices.Length == length) return indices;

            var rgbs = indices.Select(p =>
            {
                GetPelletIndex(p, out byte r, out byte g, out byte b);
                return (red: r, green: g, blue: b);
            }).ToArray();

            var grad = new byte[length];
            var x = 0;
            var x2 = 1;
            if (indices.Length > length)
            {
                var step = (float)indices.Length / length;
                step += step / grad.Length;
                var iStep = ((int)step) + 1;
                var off = 0f;
                for (int i = 0; i < grad.Length - 1; i++)
                {

                    x = (int)off;
                    var (r, g, b) = rgbs[x];
                    for (int j = 1; j < iStep; j++)
                    {
                        var (r2, g2, b2) = rgbs[x + j];
                        r += r2;
                        g += g2;
                        b += b2;
                    }
                    grad[i] = FindNearestColor((byte)(r / iStep), (byte)(g / iStep), (byte)(b / iStep));
                    off += step;
                }
                return grad;

            }



            var jumps = length / (indices.Length - 1);
            var jOffset = 0;
            for (int i = 0; i < grad.Length; i++)
            {
                var p = (float)jOffset / jumps;
                grad[i] = FindNearestColor(GraphicsHelpers.Lerp(rgbs[x].red, rgbs[x2].red, p),
                    GraphicsHelpers.Lerp(rgbs[x].green, rgbs[x2].green, p),
                    GraphicsHelpers.Lerp(rgbs[x].blue, rgbs[x2].blue, p));
                if (jOffset > jumps)
                {
                    jOffset = 0;
                    x++;
                    x2++;
                }
                jOffset++;
            }
            return grad;
        }
        // Wu Line Algorithm Gradient
        private void LineWUGradient(int x0, int y0, int x1, int y1, byte[] indicies)
        {
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                Line(x0, y0, x1, y1, indicies[0]);
                return;
            }
            var ox = x0;
            var oy = y0;
            bool exit = false;
            var lineLenght = GraphicsHelpers.GetDistance(x0, y0, x1, y1);
            var grad = CreateGradent(lineLenght + 1, indicies);
            void PutPixel(int x, int y)
            {
                var d = GraphicsHelpers.GetDistance(ox, oy, x, y);
                var off = (x + (y * Width));
                if (x < 0 || x >= Width || y < 0 || y >= Height) { exit = true; return; };
                //if (off >= VideoMemory.Length || off < 0) { exit = true; return; };
                Pixels[off] = grad[Math.Min(d, grad.Length - 1)];
            }
            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
            if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }

            PutPixel(x0, y0);
            PutPixel(x1, y1);
            if (exit) return;
            if (dx > dy)
            {
                int length = (dx - 1) >> 2;
                int extras = (dx - 1) & 3;
                int incr2 = (dy << 2) - (dx << 1);
                if (incr2 < 0)
                {
                    int c = dy << 1;
                    int incr1 = c << 1;
                    int d = incr1 - dx;
                    for (int i = 0; i < length; i++)
                    {
                        x0 += stepx;
                        x1 -= stepx;
                        if (d < 0)
                        {                                     // Pattern:
                            PutPixel(x0, y0);                    //
                            PutPixel(x0 += stepx, y0);                 //  x o o
                            PutPixel(x1, y1);                          //
                            PutPixel(x1 -= stepx, y1);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {                                 // Pattern:
                                PutPixel(x0, y0);                      //      o
                                PutPixel(x0 += stepx, y0 += stepy);    //  x o
                                PutPixel(x1, y1);                      //
                                PutPixel(x1 -= stepx, y1 -= stepy);
                            }
                            else
                            {
                                PutPixel(x0, y0 += stepy);             // Pattern:
                                PutPixel(x0 += stepx, y0);             //    o o 
                                PutPixel(x1, y1 -= stepy);             //  x
                                PutPixel(x1 -= stepx, y1);             //
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d < 0)
                        {
                            PutPixel(x0 += stepx, y0);
                            if (extras > 1) PutPixel(x0 += stepx, y0);
                            if (extras > 2) PutPixel(x1 -= stepx, y1);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0 += stepx, y0);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                    }
                    if (exit) return;
                }
                else
                {
                    int c = (dy - dx) << 1;
                    int incr1 = c << 1;
                    int d = incr1 + dx;
                    for (int i = 0; i < length; i++)
                    {
                        x0 += stepx;
                        x1 -= stepx;
                        if (d > 0)
                        {
                            PutPixel(x0, y0 += stepy);                      // Pattern:
                            PutPixel(x0 += stepx, y0 += stepy);             //      o
                            PutPixel(x1, y1 -= stepy);                      //    o
                            PutPixel(x1 -= stepx, y1 -= stepy);           //  x
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0);                           // Pattern:
                                PutPixel(x0 += stepx, y0 += stepy);         //      o
                                PutPixel(x1, y1);                           //  x o
                                PutPixel(x1 -= stepx, y1 -= stepy);         //
                            }
                            else
                            {
                                PutPixel(x0, y0 += stepy);                  // Pattern:
                                PutPixel(x0 += stepx, y0);                  //    o o
                                PutPixel(x1, y1 -= stepy);                  //  x
                                PutPixel(x1 -= stepx, y1);                  //
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0 += stepx, y0);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0);
                            if (extras > 2)
                            {
                                if (d > c)
                                    PutPixel(x1 -= stepx, y1 -= stepy);
                                else
                                    PutPixel(x1 -= stepx, y1);
                            }
                        }
                    }
                    if (exit) return;
                }
            }
            else
            {
                int length = (dy - 1) >> 2;
                int extras = (dy - 1) & 3;
                int incr2 = (dx << 2) - (dy << 1);
                if (incr2 < 0)
                {
                    int c = dx << 1;
                    int incr1 = c << 1;
                    int d = incr1 - dy;
                    for (int i = 0; i < length; i++)
                    {
                        y0 += stepy;
                        y1 -= stepy;
                        if (d < 0)
                        {
                            PutPixel(x0, y0);
                            PutPixel(x0, y0 += stepy);
                            PutPixel(x1, y1);
                            PutPixel(x1, y1 -= stepy);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0);
                                PutPixel(x0 += stepx, y0 += stepy);
                                PutPixel(x1, y1);
                                PutPixel(x1 -= stepx, y1 -= stepy);
                            }
                            else
                            {
                                PutPixel(x0 += stepx, y0);
                                PutPixel(x0, y0 += stepy);
                                PutPixel(x1 -= stepx, y1);
                                PutPixel(x1, y1 -= stepy);
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d < 0)
                        {
                            PutPixel(x0, y0 += stepy);
                            if (extras > 1) PutPixel(x0, y0 += stepy);
                            if (extras > 2) PutPixel(x1, y1 -= stepy);
                        }
                        else
                        if (d < c)
                        {
                            //PutPixel(stepx, y0 += stepy);
                            PutPixel(x0, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1, y1 -= stepy);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                    }
                    if (exit) return;
                }
                else
                {
                    int c = (dx - dy) << 1;
                    int incr1 = c << 1;
                    int d = incr1 + dy;
                    for (int i = 0; i < length; i++)
                    {
                        y0 += stepy;
                        y1 -= stepy;
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0);
                            PutPixel(x0 += stepx, y0 += stepy);
                            PutPixel(x1 -= stepx, y1);
                            PutPixel(x1 -= stepx, y1 -= stepy);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0);
                                PutPixel(x0 += stepx, y0 += stepy);
                                PutPixel(x1, y1);
                                PutPixel(x1 -= stepx, y1 -= stepy);
                            }
                            else
                            {
                                PutPixel(x0 += stepx, y0);
                                PutPixel(x0, y0 += stepy);
                                PutPixel(x1 -= stepx, y1);
                                PutPixel(x1, y1 -= stepy);
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1, y1 -= stepy);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0, y0 += stepy);
                            if (extras > 2)
                            {
                                if (d > c)
                                    PutPixel(x1 -= stepx, y1 -= stepy);
                                else
                                    PutPixel(x1, y1 -= stepy);
                            }
                        }
                    }
                    if (exit) return;
                }
            }

        }

        private void LineWUStrip(int x0, int y0, int x1, int y1, byte[] indicies)
        {
            if (indicies.Length == 0) return;
            if (indicies.Length == 1)
            {
                Line(x0, y0, x1, y1, indicies[0]);
                return;
            }
            var ox = x0;
            var oy = y0;
            bool exit = false;
            var lineLenght = GraphicsHelpers.GetDistance(x0, y0, x1, y1);

            void PutPixel(int x, int y)
            {
                var d = (float)GraphicsHelpers.GetDistance(ox, oy, x, y);
                var off = (x + (y * Width));
                if (x < 0 || x >= Width || y < 0 || y >= Height) { exit = true; return; };
                
                Pixels[off] = indicies[(int)((d / lineLenght)*(indicies.Length-1))];
            }
            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
            if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }

            PutPixel(x0, y0);
            PutPixel(x1, y1);
            if (exit) return;
            if (dx > dy)
            {
                int length = (dx - 1) >> 2;
                int extras = (dx - 1) & 3;
                int incr2 = (dy << 2) - (dx << 1);
                if (incr2 < 0)
                {
                    int c = dy << 1;
                    int incr1 = c << 1;
                    int d = incr1 - dx;
                    for (int i = 0; i < length; i++)
                    {
                        x0 += stepx;
                        x1 -= stepx;
                        if (d < 0)
                        {                                     // Pattern:
                            PutPixel(x0, y0);                    //
                            PutPixel(x0 += stepx, y0);                 //  x o o
                            PutPixel(x1, y1);                          //
                            PutPixel(x1 -= stepx, y1);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {                                 // Pattern:
                                PutPixel(x0, y0);                      //      o
                                PutPixel(x0 += stepx, y0 += stepy);    //  x o
                                PutPixel(x1, y1);                      //
                                PutPixel(x1 -= stepx, y1 -= stepy);
                            }
                            else
                            {
                                PutPixel(x0, y0 += stepy);             // Pattern:
                                PutPixel(x0 += stepx, y0);             //    o o 
                                PutPixel(x1, y1 -= stepy);             //  x
                                PutPixel(x1 -= stepx, y1);             //
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d < 0)
                        {
                            PutPixel(x0 += stepx, y0);
                            if (extras > 1) PutPixel(x0 += stepx, y0);
                            if (extras > 2) PutPixel(x1 -= stepx, y1);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0 += stepx, y0);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                    }
                    if (exit) return;
                }
                else
                {
                    int c = (dy - dx) << 1;
                    int incr1 = c << 1;
                    int d = incr1 + dx;
                    for (int i = 0; i < length; i++)
                    {
                        x0 += stepx;
                        x1 -= stepx;
                        if (d > 0)
                        {
                            PutPixel(x0, y0 += stepy);                      // Pattern:
                            PutPixel(x0 += stepx, y0 += stepy);             //      o
                            PutPixel(x1, y1 -= stepy);                      //    o
                            PutPixel(x1 -= stepx, y1 -= stepy);           //  x
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0);                           // Pattern:
                                PutPixel(x0 += stepx, y0 += stepy);         //      o
                                PutPixel(x1, y1);                           //  x o
                                PutPixel(x1 -= stepx, y1 -= stepy);         //
                            }
                            else
                            {
                                PutPixel(x0, y0 += stepy);                  // Pattern:
                                PutPixel(x0 += stepx, y0);                  //    o o
                                PutPixel(x1, y1 -= stepy);                  //  x
                                PutPixel(x1 -= stepx, y1);                  //
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0 += stepx, y0);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0);
                            if (extras > 2)
                            {
                                if (d > c)
                                    PutPixel(x1 -= stepx, y1 -= stepy);
                                else
                                    PutPixel(x1 -= stepx, y1);
                            }
                        }
                    }
                    if (exit) return;
                }
            }
            else
            {
                int length = (dy - 1) >> 2;
                int extras = (dy - 1) & 3;
                int incr2 = (dx << 2) - (dy << 1);
                if (incr2 < 0)
                {
                    int c = dx << 1;
                    int incr1 = c << 1;
                    int d = incr1 - dy;
                    for (int i = 0; i < length; i++)
                    {
                        y0 += stepy;
                        y1 -= stepy;
                        if (d < 0)
                        {
                            PutPixel(x0, y0);
                            PutPixel(x0, y0 += stepy);
                            PutPixel(x1, y1);
                            PutPixel(x1, y1 -= stepy);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0);
                                PutPixel(x0 += stepx, y0 += stepy);
                                PutPixel(x1, y1);
                                PutPixel(x1 -= stepx, y1 -= stepy);
                            }
                            else
                            {
                                PutPixel(x0 += stepx, y0);
                                PutPixel(x0, y0 += stepy);
                                PutPixel(x1 -= stepx, y1);
                                PutPixel(x1, y1 -= stepy);
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d < 0)
                        {
                            PutPixel(x0, y0 += stepy);
                            if (extras > 1) PutPixel(x0, y0 += stepy);
                            if (extras > 2) PutPixel(x1, y1 -= stepy);
                        }
                        else
                        if (d < c)
                        {
                            //PutPixel(stepx, y0 += stepy);
                            PutPixel(x0, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1, y1 -= stepy);
                        }   
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                    }
                    if (exit) return;
                }
                else
                {
                    int c = (dx - dy) << 1;
                    int incr1 = c << 1;
                    int d = incr1 + dy;
                    for (int i = 0; i < length; i++)
                    {
                        y0 += stepy;
                        y1 -= stepy;
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0);
                            PutPixel(x0 += stepx, y0 += stepy);
                            PutPixel(x1 -= stepx, y1);
                            PutPixel(x1 -= stepx, y1 -= stepy);
                            d += incr1;
                        }
                        else
                        {
                            if (d < c)
                            {
                                PutPixel(x0, y0);
                                PutPixel(x0 += stepx, y0 += stepy);
                                PutPixel(x1, y1);
                                PutPixel(x1 -= stepx, y1 -= stepy);
                            }
                            else
                            {
                                PutPixel(x0 += stepx, y0);
                                PutPixel(x0, y0 += stepy);
                                PutPixel(x1 -= stepx, y1);
                                PutPixel(x1, y1 -= stepy);
                            }
                            d += incr2;
                        }
                        if (exit) return;
                    }
                    if (extras > 0)
                    {
                        if (d > 0)
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1 -= stepx, y1 -= stepy);
                        }
                        else
                        if (d < c)
                        {
                            PutPixel(x0, y0 += stepy);
                            if (extras > 1) PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 2) PutPixel(x1, y1 -= stepy);
                        }
                        else
                        {
                            PutPixel(x0 += stepx, y0 += stepy);
                            if (extras > 1) PutPixel(x0, y0 += stepy);
                            if (extras > 2)
                            {
                                if (d > c)
                                    PutPixel(x1 -= stepx, y1 -= stepy);
                                else
                                    PutPixel(x1, y1 -= stepy);
                            }
                        }
                    }
                    if (exit) return;
                }
            }

        }
    }
}
