//Clean
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.Engine.Graphics
{
    public static class ColorHelpers
    {
        public static int ToInt(byte r, byte g, byte b, byte a) => r << 24 | g << 16 | b << 8 | a;
        public static RGBA8 RGBA8FromHSL(float hue, float saturation, float lightness, float alpha = 1.0f)
        {
            hue = Math.Clamp(hue, 0f, 360f);
            var chroma = (1f - Math.Abs(2f * lightness - 1f)) * saturation;
            var h1 = hue / 60f;
            var x = chroma * (1f - Math.Abs(h1 % 2f - 1f));
            var m = lightness - 0.5f * chroma;
            float r1, g1, b1;

            if (h1 < 1f)
            {
                r1 = chroma;
                g1 = x;
                b1 = 0f;
            }
            else if (h1 < 2f)
            {
                r1 = x;
                g1 = chroma;
                b1 = 0f;
            }
            else if (h1 < 3f)
            {
                r1 = 0f;
                g1 = chroma;
                b1 = x;
            }
            else if (h1 < 4f)
            {
                r1 = 0f;
                g1 = x;
                b1 = chroma;
            }
            else if (h1 < 5f)
            {
                r1 = x;
                g1 = 0f;
                b1 = chroma;
            }
            else
            {
                r1 = chroma;
                g1 = 0;
                b1 = x;
            }

            return new RGBA8
            {
                R = (byte)((r1 + m) * 255),
                G = (byte)((g1 + m) * 255),
                B = (byte)((b1 + m) * 255),
                A = (byte)(alpha * 255)
            };
        }
        public static HSL RGBA8ToHSL(this RGBA8 rgba)
        {
            var r = (float)rgba.R / 255;
            var g = (float)rgba.G / 255;
            var b = (float)rgba.B / 255;

            var max = new[] { r, g, b }.Max();
            var min = new[] { r, g, b }.Min();
            var chroma = max - min;
            float h1;


            if (MathHelpers.IsClose(chroma, 0))
                h1 = 0f;
            else if (Math.Abs(max - r) < double.Epsilon)
                h1 = (g - b) / chroma % 6f;
            else if (Math.Abs(max - g) < double.Epsilon)
                h1 = 2f + (b - r) / chroma;
            else //if (max == b)
                h1 = 4f + (r - g) / chroma;

            var lightness = 0.5f * (max - min);
            var saturation = MathHelpers.IsClose(chroma, 0) ? 0f : chroma / (1f - Math.Abs(2f * lightness - 1));
            HSL ret;
            ret.H = 60f * h1;
            ret.S = saturation;
            ret.L = lightness;
            ret.A = rgba.A;
            return ret;
        }
    }

}
