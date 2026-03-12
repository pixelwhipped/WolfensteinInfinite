//Clean
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WolfensteinInfinite.Engine.Graphics
{
    public static partial class Quantization
    {
        public const int FastQuantizationDepth = 32;
        public static (byte[] pixels, byte[] pallet) Quantize8BitFast(byte[] pixels, byte[] pallet, int colourCount)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = new RGBA8[pixels.Length];
            ref byte buffer = ref MemoryMarshal.GetReference(memory);
            var off = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = Unsafe.Add(ref buffer, i) * 3;
                pixelsrgba[off++] = new RGBA8
                {
                    R = pallet[c],
                    G = pallet[c + 1],
                    B = pallet[c + 2],
                    A = 255
                };
            }
            return Quantize32BitFast(pixelsrgba.ToArray(), colourCount);
        }
        public static (byte[] pixels, byte[] pallet) Quantize32BitFast(byte[] pixels, int colourCount)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            return Quantize32BitFast(pixelsrgba.ToArray(), colourCount);
        }       
        public static (byte[] pixels, byte[] pallet) Quantize32BitFast(RGBA8[] pixels, int colourCount)
        {
            static int FindNearestColor(RGBA8 rgb, int index, int depth, List<(RGBA8 RGB, float Order, List<int> Indices)> palletList)  
            {
                int shortestDistance = int.MaxValue;
                int p = 0;
                var end = Math.Max(index - depth, 0);
                var list = CollectionsMarshal.AsSpan(palletList);
                for (int i = index - 1; i >= end; i--)
                {
                    var (RGB, _, _) = list[i];
                    int rd = rgb.R - RGB.R;
                    int gd = rgb.G - RGB.G;
                    int bd = rgb.B - RGB.B;
                    int distance = rd * rd + gd * gd + bd * bd;
                    if (distance < shortestDistance)
                    {
                        p = i;
                        shortestDistance = distance;
                    }
                }
                return p;
            }

            var table = new Dictionary<RGBA8, (RGBA8 RGB, float Order, List<int> Indices)>();
            Span<RGBA8> px = pixels;

            long sr = 0L;
            long sg = 0L;
            long sb = 0L;
            for (int i = 0; i < px.Length; i++)
            {
                var pix = px[i];
                sr += pix.R;
                sg += pix.G;
                sb += pix.B;
            }
            var center = new RGBA8 { R = (byte)(sr / px.Length), G = (byte)(sg / px.Length), B = (byte)(sb / px.Length), A = 255 };

            for (int i = 0; i < px.Length; i++)
            {
                var pix = px[i];
                if (!table.TryGetValue(pix, out var value)) value = new(pix, GraphicsHelpers.GetColorDistance(center.R, center.G, center.B, pix.R, pix.G, pix.B), []);
                value.Indices.Add(i);
                table[pix] = value;
            }

            var list = table.Values.OrderBy(x => x.Order).ToList();
            var index = list.Count;
            while (list.Count > colourCount)
            {
                index--;
                var c1 = list[index];
                var index2 = FindNearestColor(c1.RGB, index, FastQuantizationDepth, list);
                var c2 = list[index2];
                if (c1.Indices.Count < c2.Indices.Count) (c1, c2) = (c2, c1);
                var t = (float)c2.Indices.Count / (c2.Indices.Count + c1.Indices.Count);
                var rgb = new RGBA8
                {
                    R = (byte)(c1.RGB.R + (c2.RGB.R - c1.RGB.R) * t),
                    G = (byte)(c1.RGB.G + (c2.RGB.G - c1.RGB.G) * t),
                    B = (byte)(c1.RGB.B + (c2.RGB.B - c1.RGB.B) * t),
                    A = 255
                };
                c1.Indices.AddRange(c2.Indices);
                if (index > index2) (index, index2) = (index2, index);
                list.RemoveAt(index2);
                list[index] = new (rgb, c1.Order, c1.Indices);
                index--;
                if (index <= 1)
                {
                    index = list.Count;
                }
            }
            var ret = new byte[pixels.Length];
            var pallet = new byte[list.Count * 3];

            Parallel.For(0, list.Count, (i) =>
            {
                var (RGB, Order, Indices) = list[i];
                var indices = CollectionsMarshal.AsSpan(Indices);
                for (int j = 0; j < indices.Length; j++) ret[(int)indices[j]] = (byte)i;
                var x = i * 3;
                pallet[x] = RGB.R;
                pallet[x + 1] = RGB.G;
                pallet[x + 2] = RGB.B;
            });
            return (ret, pallet);
        }
    }
}
