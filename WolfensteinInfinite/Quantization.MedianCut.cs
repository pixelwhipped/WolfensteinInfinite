using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WolfensteinInfinite
{
    public static partial class Quantization
    {
        public static (byte[] pixels, byte[] pallet) Quantize8BitMedianCut(byte[] pixels, byte[] pallet, int colourCount)
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
            return Quantize32BitMedianCut(pixelsrgba.ToArray(),colourCount);
        }
        public static (byte[] pixels, byte[] pallet) Quantize32BitMedianCut(byte[] pixels, int colourCount)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            return Quantize32BitMedianCut(pixelsrgba.ToArray(), colourCount);
        }


        public static (byte[] pixels, byte[] pallet) Quantize32BitMedianCut(RGBA8[] pixels, int colourCount)
        {

            var colorCounts = new Dictionary<RGBA8, int>();

            Span<RGBA8> px = pixels;
            for (int i = 0; i < px.Length; i++)
            {
                RGBA8 pixel = px[i];
                if (colorCounts.TryGetValue(pixel, out var count))
                    colorCounts[pixel] = count + 1;
                else
                    colorCounts[pixel] = 1;
            }
            var buckets = new List<Bucket> { new(colorCounts) };
            while (buckets.Count < colourCount)
            {
                var newBuckets = new List<Bucket>();
                for (var i = 0; i < buckets.Count; i++)
                {
                    if (newBuckets.Count + (buckets.Count - i) < colourCount)
                    {
                        var split = buckets[i].Split();
                        newBuckets.Add(split.Item1);
                        newBuckets.Add(split.Item2);
                        continue;
                    }
                    newBuckets.AddRange(buckets.GetRange(i, buckets.Count - i));
                    break;
                }
                buckets = newBuckets;
            }

            var ret = new byte[pixels.Length];
            var pallet = new byte[buckets.Count * 3];
            Parallel.For(0, pixels.Length, (i) =>
            {
                var bi = (byte)buckets.FindIndex(b => b.HasColor(pixels[i]));
                var bucket = buckets[bi];
                ret[i] = bi;
                var x = bi * 3;
                pallet[x] = buckets[bi].Color.R;
                pallet[x + 1] = buckets[bi].Color.G;
                pallet[x + 2] = buckets[bi].Color.B;
            });
            return (ret, pallet);
        }
        private class Bucket
        {
            private readonly Dictionary<RGBA8, int> colors;
            public RGBA8 Color { get; }

            public Bucket(Dictionary<RGBA8, int> colorCounts)
            {
                colors = colorCounts;
                Color = CalculateAverageColor(colors);
            }

            public Bucket(IEnumerable<KeyValuePair<RGBA8, int>> colorCounts)
            {
                colors = new Dictionary<RGBA8, int>(colorCounts);
                Color = CalculateAverageColor(colors);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasColor(RGBA8 color) => colors.ContainsKey(color);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private (byte minR, byte maxR, byte minG, byte maxG, byte minB, byte maxB) GetColorRanges()
            {
                byte minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;

                Span<RGBA8> keys = colors.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    RGBA8 color = keys[i];
                    if (color.R < minR) minR = color.R;
                    if (color.R > maxR) maxR = color.R;
                    if (color.G < minG) minG = color.G;
                    if (color.G > maxG) maxG = color.G;
                    if (color.B < minB) minB = color.B;
                    if (color.B > maxB) maxB = color.B;
                }

                return (minR, maxR, minG, maxG, minB, maxB);
            }
            public (Bucket, Bucket) Split()
            {
                // Find the dimension with largest range (cached)
                var (minR, maxR, minG, maxG, minB, maxB) = GetColorRanges();

                // Determine the color channel with the largest range
                int redRange = maxR - minR;
                int greenRange = maxG - minG;
                int blueRange = maxB - minB;

                // Sort the colors and split them into two buckets
                KeyValuePair<RGBA8, int>[] sorted;
                if (redRange >= greenRange && redRange >= blueRange)
                {
                    sorted = [.. colors.OrderBy(c => c.Key.R)];
                }
                else if (greenRange >= blueRange)
                {
                    sorted = [.. colors.OrderBy(c => c.Key.G)];
                }
                else
                {
                    sorted = [.. colors.OrderBy(c => c.Key.B)];
                }

                int midIndex = sorted.Length / 2;
                Bucket bucket1 = new(sorted.Take(midIndex));
                Bucket bucket2 = new(sorted.Skip(midIndex));
                return (bucket1, bucket2);
            }
            private static RGBA8 CalculateAverageColor(Dictionary<RGBA8, int> colorCounts)
            {
                long totalR = 0, totalG = 0, totalB = 0, totalCount = 0;
                if(colorCounts.Count==0) return new RGBA8
                {
                    R = 0,
                    G = 0,
                    B = 0,
                    A = 255
                };
                foreach (var kvp in colorCounts)
                {
                    var color = kvp.Key;
                    var count = kvp.Value;

                    totalR += (long)color.R * count;
                    totalG += (long)color.G * count;
                    totalB += (long)color.B * count;
                    totalCount += count;
                }

                return new RGBA8
                {
                    R = (byte)(totalR / totalCount),
                    G = (byte)(totalG / totalCount),
                    B = (byte)(totalB / totalCount),
                    A = 255
                };
            }
        }
    }
}
