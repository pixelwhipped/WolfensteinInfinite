//Clean
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WolfensteinInfinite.Engine.Graphics
{
    /// <summary>
    /// Defines a color in RGBA space.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]    
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
        public static readonly RGBA8 TRANSPARENT = new() { R = 0, G = 0, B = 0, A = 0 };
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

        public override readonly bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);
        public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
        public override readonly string? ToString() => $"RGBA8({R},{G},{B},{A})";
        public static bool operator ==(RGBA8 left, RGBA8 right) => left.Equals(right);
        public static bool operator !=(RGBA8 left, RGBA8 right) => !(left == right);
    }

}
