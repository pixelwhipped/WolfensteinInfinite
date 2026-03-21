//Clean
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace WolfensteinInfinite.Engine.Graphics
{
    public static class GraphicsHelpers
    {
        public static Texture32 TintRed(Texture32 source, float intensity)
        {
            var ret = new Texture32(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    source.GetPixel(x, y, out byte r, out byte g, out byte b, out byte a);
                    // Add red based on intensity (0-1)
                    byte newR = (byte)Math.Min(255, r + (int)(intensity * 128));
                    byte newG = (byte)Math.Max(0, g - (int)(intensity * g * 0.5f)); // Slightly desaturate green
                    byte newB = (byte)Math.Max(0, b - (int)(intensity * b * 0.5f)); // Slightly desaturate blue
                    ret.PutPixel(x, y, newR, newG, newB, a);
                }
            }
            return ret;
        }

        public static Texture32 Colorize(float v, Texture32 t)
        {
            var ret = new Texture32(t.Width, t.Height);
            for (int y = 0; y < t.Height; y++)
            {
                for (int x = 0; x < t.Width; x++)
                {
                    t.GetPixel(x, y, out byte r, out byte g, out byte b, out byte a);
                    var h = new RGBA8() { R = r, G = g, B = b, A = a }.RGBA8ToHSL();
                    if (h.S == 0)
                    {
                        ret.PutPixel(x, y, r, g, b, a);
                    }
                    else
                    {
                        h.H = v;
                        var rgb = ColorHelpers.RGBA8FromHSL((float)h.H, (float)h.S, (float)h.L, (float)h.A);
                        ret.PutPixel(x, y, rgb.R, rgb.G, rgb.B, a);
                    }
                }
            }
            return ret;
        }
        public static int GetColorDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            int redDifference = r1 - r2;
            int greenDifference = g1 - g2;
            int blueDifference = b1 - b2;
            return redDifference * redDifference + greenDifference * greenDifference + blueDifference * blueDifference;
        }
        // Fast approximation for perceptual distance (faster than sqrt)
        public static int GetPerceptualColorDistanceFast(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            int dr = r1 - r2;
            int dg = g1 - g2;
            int db = b1 - b2;

            // Weighted without sqrt for speed
            return 2 * dr * dr + 4 * dg * dg + 3 * db * db;
        }
        public static byte Lerp(byte a, byte b, float t)
        {
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;
            return (byte)(a + (b - a) * t);
        }
        // Fastest: Just return squared distance (no sqrt needed for comparisons!)
        public static int GetDistanceSquared(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            return dx * dx + dy * dy;
        }
        public static int GetDistance(int x1, int y1, int x2, int y2) => GetDistanceFast(x1, y1, x2, y2);
        public static int GetDistanceFast(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            return (int)MathF.Sqrt(dx * dx + dy * dy);
        }
        // SSE intrinsics version (requires x86/x64)
        public static int GetDistanceSSE(int x1, int y1, int x2, int y2)
        {
            if (Sse2.IsSupported)
            {
                var v1 = Vector128.Create(x1, y1, 0, 0);
                var v2 = Vector128.Create(x2, y2, 0, 0);
                var diff = Sse2.Subtract(v2, v1);
                var squared = Sse2.MultiplyLow(diff.AsInt16(), diff.AsInt16()).AsInt32();

                // Sum the first two elements
                var sum = Sse2.Add(squared, Sse2.Shuffle(squared, 0x01));
                return (int)Math.Sqrt(sum.GetElement(0));
            }

            // Fallback to fast version
            return GetDistanceFast(x1, y1, x2, y2);
        }

        // AVX2 version for even better performance
        public static int GetDistanceAVX(int x1, int y1, int x2, int y2)
        {
            if (Avx2.IsSupported)
            {
                var v1 = Vector256.Create(x1, y1, 0, 0, 0, 0, 0, 0);
                var v2 = Vector256.Create(x2, y2, 0, 0, 0, 0, 0, 0);
                var diff = Avx2.Subtract(v2, v1);
                var squared = Avx2.MultiplyLow(diff.AsInt16(), diff.AsInt16()).AsInt32();

                // Extract and sum
                int dx2 = squared.GetElement(0);
                int dy2 = squared.GetElement(1);
                return (int)Math.Sqrt(dx2 + dy2);
            }

            return GetDistanceFast(x1, y1, x2, y2);
        }
        public static int GetDistanceOld(int x1, int y1, int x2, int y2)
        {
            return (int)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }
        public static byte FindNearestColor(byte r, byte g, byte b, byte[] pallet)
        {
            int shortestDistance = int.MaxValue;
            byte p = 0;
            int index = 0;

            unsafe
            {
                fixed (byte* palettePtr = pallet)
                {
                    byte* currentColor = palettePtr;
                    for (int i = 0; i < pallet.Length; i += 3)
                    {
                        int rd = r - *currentColor++;
                        int gd = g - *currentColor++;
                        int bd = b - *currentColor++;

                        int distance = rd * rd + gd * gd + bd * bd;
                        if (distance < shortestDistance)
                        {
                            p = (byte)index;
                            shortestDistance = distance;
                        }
                        index++;
                    }
                }
            }

            return p;
        }        

        public static float MagnitudeIntrinsic(byte a, byte b, byte c)
        {
            // Using SIMD if available
            var vector = new System.Numerics.Vector3(a, b, c);
            return vector.Length();
        }

        public static int GetPosistionThroughIndex(int currentStep, int segmentIndex, int totalSegments, int iterations)
        {
            int firstStep = (int)Math.Ceiling((float)segmentIndex / totalSegments * iterations);
            int lastStep = (int)Math.Floor((float)(segmentIndex + 1) / totalSegments * iterations);

            // Handle edge cases
            if (segmentIndex == 0) firstStep = 0;
            if (segmentIndex == totalSegments - 1) lastStep = iterations;

            // For segments that don't include the boundary, the last step should reach 100%
            if (segmentIndex < totalSegments - 1 && currentStep == lastStep - 1)
            {
                return lastStep - firstStep; // Last step of non-final segment should be 100%
            }
            return currentStep - firstStep;
        }

        public static float GetProgressThroughIndex(int currentStep, int segmentIndex, int totalSegments, int iterations)
        {
            // Calculate first and last step of current segment
            int firstStep = (int)Math.Ceiling((float)segmentIndex / totalSegments * iterations);
            int lastStep = (int)Math.Floor((float)(segmentIndex + 1) / totalSegments * iterations);

            // Handle edge cases
            if (segmentIndex == 0) firstStep = 0;
            if (segmentIndex == totalSegments - 1) lastStep = iterations;

            // For segments that don't include the boundary, the last step should reach 100%
            if (segmentIndex < totalSegments - 1 && currentStep == lastStep - 1)
            {
                return 1f; // Last step of non-final segment should be 100%
            }

            // Steps into this segment
            int stepsInto = currentStep - firstStep;
            int totalSteps = lastStep - firstStep;  // Remove the +1 here

            // Prevent division by zero
            if (totalSteps == 0) return 1f;

            return (float)stepsInto / totalSteps;
        }

        public static int GetCumulativeFrameCountForIndex(int segmentIndex, int totalSegments, int iterations)
        {
            if (segmentIndex < 0) return 0;
            if (segmentIndex >= totalSegments - 1)
            {
                return iterations + 1;
            }
            float pEnd = (float)(segmentIndex + 1) / totalSegments;
            int lastStep = (int)Math.Floor(pEnd * iterations);
            return lastStep + 1;
        }
        public static int GetFrameCountForIndex(int segmentIndex, int totalSegments, int iterations)
        {
            float rangeStart = (float)segmentIndex / totalSegments;
            float rangeEnd = segmentIndex == totalSegments - 1 ? 1.0f : (float)(segmentIndex + 1) / totalSegments;
            int stepStart = (int)Math.Ceiling(rangeStart * iterations);
            int stepEnd = (int)Math.Floor(rangeEnd * iterations);
            if (segmentIndex == 0) stepStart = 0;
            if (segmentIndex == totalSegments - 1) stepEnd = iterations;
            return stepEnd - stepStart + 1;
        }
    }
}
