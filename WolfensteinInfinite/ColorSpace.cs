using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WolfensteinInfinite
{
    /// <summary>
    /// Defines a color in RGBA space.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "<Pending>")]
    public struct RGBA8 : IEquatable<RGBA8>
    {
        public static readonly RGBA8 BLACK = new() { R = 0, G = 0, B = 0, A = 255 };
        public static readonly RGBA8 WHITE = new() { R = 255, G = 255, B = 255, A = 255 };
        public static readonly RGBA8 YELLOW = new() { R =255, G = 255, B = 0, A = 255 };
        public static readonly RGBA8 RED = new() { R = 255, G = 0, B = 0, A = 255 };
        public static readonly RGBA8 GREEN = new() { R = 0, G = 255, B = 0, A = 255 };
        public static readonly RGBA8 BLUE = new() { R = 0, G = 0, B = 255, A = 255 };
        public static readonly RGBA8 STEEL_BLUE = new() { R = 60, G = 60, B = 140, A = 255 };
        public static readonly RGBA8 DARK_PURPLE = new() { R = 208, G = 0, B = 62, A = 255 };

        /// <summary>
        /// The Alpha/opacity.
        /// </summary>
        [FieldOffset(3)] public byte A;

        /// <summary>
        /// The Red Value .
        /// </summary>
        [FieldOffset(0)] public byte R;

        /// <summary>
        /// The Green Value.
        /// </summary>
        [FieldOffset(1)] public byte G;

        /// <summary>
        /// The Blue Value.
        /// </summary>
        [FieldOffset(2)] public byte B;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint ToUInt32() => Unsafe.As<RGBA8, uint>(ref Unsafe.AsRef(in this));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RGBA8 FromUInt32(uint value) => Unsafe.As<uint, RGBA8>(ref value);
        public readonly bool Equals(RGBA8 other) => ToUInt32() == other.ToUInt32();
    }

    public struct HSL
    {
        /// <summary>
        /// The Alpha/opacity in 0..1 range.
        /// </summary>
        public double A;

        /// <summary>
        /// The Hue in 0..360 range.
        /// </summary>
        public double H;

        /// <summary>
        /// The Lightness in 0..1 range.
        /// </summary>
        public double L;

        /// <summary>
        /// The Saturation in 0..1 range.
        /// </summary>
        public double S;
    }

    public static class ColorSpace
    {
        //public static int ToInt(RGBA8 c) => (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
        public static int ToInt(byte r, byte g, byte b, byte a) => (r << 24) | (g << 16) | (b << 8) | a;
        public static RGBA8 RGBA8FromHSL(float hue, float saturation, float lightness, float alpha = 1.0f)
        {
            hue = Math.Clamp(hue, 0f, 360f);
            var chroma = (1f - System.Math.Abs(2f * lightness - 1f)) * saturation;
            var h1 = hue / 60f;
            var x = chroma * (1f - System.Math.Abs(h1 % 2f - 1f));
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
                h1 = ((g - b) / chroma) % 6f;
            else if (Math.Abs(max - g) < double.Epsilon)
                h1 = 2f + (b - r) / chroma;
            else //if (max == b)
                h1 = 4f + (r - g) / chroma;

            var lightness = 0.5f * (max - min);
            var saturation = MathHelpers.IsClose(chroma, 0) ? 0f : chroma / (1f - System.Math.Abs(2f * lightness - 1));
            HSL ret;
            ret.H = 60f * h1;
            ret.S = saturation;
            ret.L = lightness;
            ret.A = rgba.A;
            return ret;
        }
    }

}
