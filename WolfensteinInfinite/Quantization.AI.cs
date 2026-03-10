using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace WolfensteinInfinite
{
    public static partial class Quantization
    {
        public const int AIQuantizationDepth = 64;
        // Pre-allocated arrays to avoid repeated allocations
        private static readonly ThreadLocal<List<ColorEntry>> _colorListCache = new(() => new List<ColorEntry>(65536));
        private static readonly ThreadLocal<Dictionary<uint, int>> _colorMapCache = new(() => new Dictionary<uint, int>(65536));

        [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
        private struct ColorEntry(RGBA8 color, float distance, int firstIndex)
        {
            public RGBA8 Color = color;
            public float DistanceFromCenter = distance;
            public int PixelCount = 1;
            public int FirstPixelIndex = firstIndex; // Store only first index to save memory
        }
        public static (byte[] pixels, byte[] pallet) Quantize8BitAI(byte[] pixels, byte[] pallet, int colourCount)
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
            return Quantize32BitAI(pixelsrgba.ToArray(), colourCount);
        }
        public static (byte[] pixels, byte[] pallet) Quantize32BitAI(byte[] pixels, int colourCount)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            return Quantize32BitAI(pixelsrgba.ToArray(), colourCount);
        }

        public static (byte[] pixels, byte[] palette) Quantize32BitAI(byte[] pixels, int colourCount, RGBA8[] requiredColors)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            return Quantize32BitAI(pixelsrgba.ToArray(), colourCount, requiredColors);
        }
        public static (byte[] pixels, byte[] palette) Quantize32BitAI(RGBA8[] pixels, int colourCount, RGBA8[] requiredColors)
        {
            if (pixels.Length == 0 || colourCount == 0) return (Array.Empty<byte>(), Array.Empty<byte>());

            var pixelSpan = pixels.AsSpan();
            var colorList = _colorListCache.Value!;
            var colorMap = _colorMapCache.Value!;

            colorList.Clear();
            colorMap.Clear();

            // First, add required colors to the palette if provided
            var reservedSlots = 0;
            if (requiredColors != null && requiredColors.Length > 0)
            {
                reservedSlots = Math.Min(requiredColors.Length, colourCount);
                AddRequiredColors(requiredColors, colorList, colorMap, reservedSlots);
            }

            // Calculate remaining slots for quantization
            var remainingColorCount = colourCount - reservedSlots;
            if (remainingColorCount <= 0)
            {
                // If required colors fill all slots, just use them
                return GenerateFinalResult(pixelSpan, colorList);
            }

            // Calculate center color using SIMD when possible
            var center = CalculateCenterColor(pixelSpan);

            // Build unique color map, excluding already added required colors
            BuildColorMapWithExclusions(pixelSpan, center, colorList, colorMap);

            if (colorList.Count <= colourCount)
            {
                return CreateDirectPalette(pixelSpan, colorList, colorMap);
            }

            // Sort non-required colors by distance from center (skip the required colors at the start)
            if (reservedSlots < colorList.Count)
            {
                var nonRequiredColors = colorList.Skip(reservedSlots).ToList();
                nonRequiredColors.Sort((a, b) => a.DistanceFromCenter.CompareTo(b.DistanceFromCenter));

                // Replace the non-required portion of the list
                for (int i = 0; i < nonRequiredColors.Count; i++)
                {
                    colorList[reservedSlots + i] = nonRequiredColors[i];
                }
            }

            // Reduce colors using optimized merging, protecting required colors
            ReduceColorsWithProtection(colorList, colourCount, reservedSlots);

            // Generate final result
            return GenerateFinalResult(pixelSpan, colorList);
        }
        public static (byte[] pixels, byte[] palette) Quantize32BitAI(RGBA8[] pixels, int colourCount)
        {
            if (pixels.Length == 0 || colourCount == 0) return (Array.Empty<byte>(), Array.Empty<byte>());

            var pixelSpan = pixels.AsSpan();
            var colorList = _colorListCache.Value!;
            var colorMap = _colorMapCache.Value!;

            colorList.Clear();
            colorMap.Clear();

            // Calculate center color using SIMD when possible
            var center = CalculateCenterColor(pixelSpan);

            // Build unique color map with vectorized distance calculation
            BuildColorMap(pixelSpan, center, colorList, colorMap);

            if (colorList.Count <= colourCount)
            {
                return CreateDirectPalette(pixelSpan, colorList, colorMap);
            }

            // Sort by distance from center (closest first for better visual quality)
            colorList.Sort((a, b) => a.DistanceFromCenter.CompareTo(b.DistanceFromCenter));

            // Reduce colors using optimized merging
            ReduceColors(colorList, colourCount);

            // Generate final result
            return GenerateFinalResult(pixelSpan, colorList);
        }

        private static void AddRequiredColors(RGBA8[] requiredColors, List<ColorEntry> colorList, Dictionary<uint, int> colorMap, int maxSlots)
        {
            for (int i = 0; i < Math.Min(requiredColors.Length, maxSlots); i++)
            {
                var color = requiredColors[i];
                var key = color.ToUInt32();

                // Only add if not already present
                if (!colorMap.ContainsKey(key))
                {
                    var entry = new ColorEntry(color, 0f, -1); // Distance 0, no first pixel index
                    colorMap[key] = colorList.Count;
                    colorList.Add(entry);
                }
            }
        }
        private static void BuildColorMapWithExclusions(ReadOnlySpan<RGBA8> pixels, RGBA8 center, List<ColorEntry> colorList, Dictionary<uint, int> colorMap)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var key = pixel.ToUInt32();

                if (colorMap.TryGetValue(key, out int existingIndex))
                {
                    // Update pixel count for existing colors (including required ones)
                    var entry = colorList[existingIndex];
                    entry.PixelCount++;
                    if (entry.FirstPixelIndex == -1) // Update first pixel index for required colors
                        entry.FirstPixelIndex = i;
                    colorList[existingIndex] = entry;
                }
                else
                {
                    var distance = GetColorDistanceFast(center, pixel);
                    var newEntry = new ColorEntry(pixel, distance, i);
                    colorMap[key] = colorList.Count;
                    colorList.Add(newEntry);
                }
            }
        }
        private static void ReduceColorsWithProtection(List<ColorEntry> colorList, int targetCount, int protectedCount)
        {
            while (colorList.Count > targetCount)
            {
                // Find color to merge, but never touch the first 'protectedCount' colors
                var mergeIndex = FindColorToMergeWithProtection(colorList, protectedCount);
                var nearestIndex = FindNearestColorWithProtection(colorList, mergeIndex, protectedCount);

                MergeColors(colorList, mergeIndex, nearestIndex);
            }
        }
        private static int FindColorToMergeWithProtection(List<ColorEntry> colorList, int protectedCount)
        {
            // Only consider colors beyond the protected range
            int minPixels = int.MaxValue;
            int minIndex = Math.Max(protectedCount, colorList.Count - 1);

            var searchStart = Math.Max(protectedCount, colorList.Count - AIQuantizationDepth);
            var searchEnd = colorList.Count;

            for (int i = searchEnd - 1; i >= searchStart; i--)
            {
                if (colorList[i].PixelCount < minPixels)
                {
                    minPixels = colorList[i].PixelCount;
                    minIndex = i;
                }
            }

            return minIndex;
        }
        private static int FindNearestColorWithProtection(List<ColorEntry> colorList, int targetIndex, int protectedCount)
        {
            var targetColor = colorList[targetIndex].Color;
            float minDistance = float.MaxValue;
            int nearestIndex = targetIndex > protectedCount ? targetIndex - 1 : Math.Max(protectedCount, targetIndex + 1);

            var searchStart = Math.Max(protectedCount, targetIndex - AIQuantizationDepth / 2);
            var searchEnd = Math.Min(colorList.Count, targetIndex + AIQuantizationDepth / 2);

            for (int i = searchStart; i < searchEnd; i++)
            {
                if (i == targetIndex || i < protectedCount) continue; // Skip target and protected colors

                var distance = GetColorDistanceFast(targetColor, colorList[i].Color);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RGBA8 CalculateCenterColor(ReadOnlySpan<RGBA8> pixels)
        {
            if (Vector.IsHardwareAccelerated && pixels.Length >= Vector<uint>.Count)
            {
                return CalculateCenterColorVectorized(pixels);
            }

            long sr = 0, sg = 0, sb = 0;
            foreach (var pixel in pixels)
            {
                sr += pixel.R;
                sg += pixel.G;
                sb += pixel.B;
            }

            var len = pixels.Length;
            return new RGBA8 { R = (byte)(sr / len), G = (byte)(sg / len), B = (byte)(sb / len), A = 255 };
        }

        private static RGBA8 CalculateCenterColorVectorized(ReadOnlySpan<RGBA8> pixels)
        {
            var vectorSize = Vector<uint>.Count;
            var vectorCount = pixels.Length / vectorSize;

            var rSum = Vector<uint>.Zero;
            var gSum = Vector<uint>.Zero;
            var bSum = Vector<uint>.Zero;

            var pixelBytes = MemoryMarshal.AsBytes(pixels);

            for (int i = 0; i < vectorCount; i++)
            {
                var offset = i * vectorSize * 4;
                var rVector = new Vector<uint>();
                var gVector = new Vector<uint>();
                var bVector = new Vector<uint>();

                for (int j = 0; j < vectorSize; j++)
                {
                    var pixelOffset = offset + j * 4;
                    Unsafe.Add(ref Unsafe.As<Vector<uint>, uint>(ref rVector), j) = pixelBytes[pixelOffset + 1]; // R
                    Unsafe.Add(ref Unsafe.As<Vector<uint>, uint>(ref gVector), j) = pixelBytes[pixelOffset + 2]; // G
                    Unsafe.Add(ref Unsafe.As<Vector<uint>, uint>(ref bVector), j) = pixelBytes[pixelOffset + 3]; // B
                }

                rSum += rVector;
                gSum += gVector;
                bSum += bVector;
            }

            uint totalR = 0, totalG = 0, totalB = 0;
            for (int i = 0; i < vectorSize; i++)
            {
                totalR += Unsafe.Add(ref Unsafe.As<Vector<uint>, uint>(ref rSum), i);
                totalG += Unsafe.Add(ref Unsafe.As<Vector<uint>, uint>(ref gSum), i);
                totalB += Unsafe.Add(ref Unsafe.As<Vector<uint>, uint>(ref bSum), i);
            }

            // Handle remaining pixels
            for (int i = vectorCount * vectorSize; i < pixels.Length; i++)
            {
                totalR += pixels[i].R;
                totalG += pixels[i].G;
                totalB += pixels[i].B;
            }

            var len = (uint)pixels.Length;
            return new RGBA8 { R = (byte)(totalR / len), G = (byte)(totalG / len), B = (byte)(totalB / len), A = 255 };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BuildColorMap(ReadOnlySpan<RGBA8> pixels, RGBA8 center, List<ColorEntry> colorList, Dictionary<uint, int> colorMap)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var key = pixel.ToUInt32();

                if (colorMap.TryGetValue(key, out int existingIndex))
                {
                    var entry = colorList[existingIndex];
                    entry.PixelCount++;
                    colorList[existingIndex] = entry;
                }
                else
                {
                    var distance = GetColorDistanceFast(center, pixel);
                    var newEntry = new ColorEntry(pixel, distance, i);
                    colorMap[key] = colorList.Count;
                    colorList.Add(newEntry);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetColorDistanceFast(RGBA8 c1, RGBA8 c2)
        {
            // Use faster integer math and avoid sqrt
            int rd = c1.R - c2.R;
            int gd = c1.G - c2.G;
            int bd = c1.B - c2.B;
            return rd * rd + gd * gd + bd * bd;
        }

        private static (byte[] pixels, byte[] palette) CreateDirectPalette(ReadOnlySpan<RGBA8> pixels, List<ColorEntry> colorList, Dictionary<uint, int> colorMap)
        {
            var result = new byte[pixels.Length];
            var palette = new byte[colorList.Count * 3];

            // Fill palette
            for (int i = 0; i < colorList.Count; i++)
            {
                var color = colorList[i].Color;
                var offset = i * 3;
                palette[offset] = color.R;
                palette[offset + 1] = color.G;
                palette[offset + 2] = color.B;
            }

            // Map pixels to palette indices
            for (int i = 0; i < pixels.Length; i++)
            {
                result[i] = (byte)colorMap[pixels[i].ToUInt32()];
            }

            return (result, palette);
        }

        private static void ReduceColors(List<ColorEntry> colorList, int targetCount)
        {
            while (colorList.Count > targetCount)
            {
                var mergeIndex = FindColorToMerge(colorList);
                var nearestIndex = FindNearestColor(colorList, mergeIndex);

                MergeColors(colorList, mergeIndex, nearestIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindColorToMerge(List<ColorEntry> colorList)
        {
            // Find color with least pixels (more aggressive merging of rare colors)
            int minPixels = int.MaxValue;
            int minIndex = colorList.Count - 1;

            var searchEnd = Math.Max(colorList.Count - AIQuantizationDepth, colorList.Count / 2);
            for (int i = colorList.Count - 1; i >= searchEnd; i--)
            {
                if (colorList[i].PixelCount < minPixels)
                {
                    minPixels = colorList[i].PixelCount;
                    minIndex = i;
                }
            }

            return minIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindNearestColor(List<ColorEntry> colorList, int targetIndex)
        {
            var targetColor = colorList[targetIndex].Color;
            float minDistance = float.MaxValue;
            int nearestIndex = targetIndex > 0 ? targetIndex - 1 : targetIndex + 1;

            var searchStart = Math.Max(0, targetIndex - AIQuantizationDepth / 2);
            var searchEnd = Math.Min(colorList.Count, targetIndex + AIQuantizationDepth / 2);

            for (int i = searchStart; i < searchEnd; i++)
            {
                if (i == targetIndex) continue;

                var distance = GetColorDistanceFast(targetColor, colorList[i].Color);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MergeColors(List<ColorEntry> colorList, int index1, int index2)
        {
            if (index1 > index2) (index1, index2) = (index2, index1);

            var color1 = colorList[index1];
            var color2 = colorList[index2];

            var totalPixels = color1.PixelCount + color2.PixelCount;
            var weight1 = (float)color1.PixelCount / totalPixels;
            var weight2 = (float)color2.PixelCount / totalPixels;

            var mergedColor = new RGBA8
            {
                R = (byte)(color1.Color.R * weight1 + color2.Color.R * weight2),
                G = (byte)(color1.Color.G * weight1 + color2.Color.G * weight2),
                B = (byte)(color1.Color.B * weight1 + color2.Color.B * weight2),
                A = 255
            };

            colorList[index1] = new ColorEntry
            {
                Color = mergedColor,
                DistanceFromCenter = Math.Min(color1.DistanceFromCenter, color2.DistanceFromCenter),
                PixelCount = totalPixels,
                FirstPixelIndex = color1.FirstPixelIndex
            };

            colorList.RemoveAt(index2);
        }


        private static (byte[] pixels, byte[] palette) GenerateFinalResult(ReadOnlySpan<RGBA8> pixels, List<ColorEntry> colorList)
        {
            var result = new byte[pixels.Length];
            var palette = new byte[colorList.Count * 3];
            var colorToPaletteIndex = new Dictionary<uint, byte>(colorList.Count);

            // Build new color mapping and palette
            for (int i = 0; i < colorList.Count; i++)
            {
                var color = colorList[i].Color;
                var offset = i * 3;
                palette[offset] = color.R;
                palette[offset + 1] = color.G;
                palette[offset + 2] = color.B;
                colorToPaletteIndex[color.ToUInt32()] = (byte)i;
            }

            // Map each pixel to nearest palette color
            for (int i = 0; i < pixels.Length; i++)
            {
                var pixelColor = pixels[i];
                var pixelKey = pixelColor.ToUInt32();

                if (colorToPaletteIndex.TryGetValue(pixelKey, out byte directIndex))
                {
                    result[i] = directIndex;
                }
                else
                {
                    // Find nearest color in final palette
                    result[i] = FindNearestPaletteColor(pixelColor, colorList);
                }
            }

            return (result, palette);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte FindNearestPaletteColor(RGBA8 targetColor, List<ColorEntry> colorList)
        {
            float minDistance = float.MaxValue;
            byte nearestIndex = 0;

            for (int i = 0; i < colorList.Count; i++)
            {
                var distance = GetColorDistanceFast(targetColor, colorList[i].Color);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = (byte)i;
                }
            }

            return nearestIndex;
        }
    }
}