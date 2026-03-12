//Clean
using System.Runtime.InteropServices;

namespace WolfensteinInfinite.Engine.Graphics
{
    public static class Dithering
    {
        public enum DitheringMethod
        {
            None,
            FloydSteinberg,
            Atkinson,
            Burkes,
            Sierra,
            SierraLite,
            Stucki,
            JarvisJudiceNinke
        }

        public static void Dither(byte[] pixelsHi, ref byte[] pixelsLo, byte[] pal, int width, DitheringMethod mode = DitheringMethod.FloydSteinberg) => Dither(MemoryMarshal.Cast<byte, RGBA8>(pixelsHi).ToArray(), ref pixelsLo, pal, width, mode);
        public static void Dither(byte[] pixelsHi, ref byte[] pixelsLo, RGBA8[] pal, int width, DitheringMethod mode = DitheringMethod.FloydSteinberg) => Dither(MemoryMarshal.Cast<byte, RGBA8>(pixelsHi).ToArray(), ref pixelsLo, MemoryMarshal.Cast<RGBA8, byte>(pal).ToArray(), width, mode);
        public static void Dither(RGBA8[] pixelsHi, ref byte[] pixelsLo, RGBA8[] pal, int width, DitheringMethod mode = DitheringMethod.FloydSteinberg) => Dither(pixelsHi, ref pixelsLo, MemoryMarshal.Cast<RGBA8, byte>(pal).ToArray(), width, mode);
        public static void Dither(RGBA8[] pixelsHi, ref byte[] pixelsLo, byte[] pal, int width, DitheringMethod mode = DitheringMethod.FloydSteinberg)
        {
            static RGBA8 ClampColor(int r, int g, int b)
            {
                return new RGBA8
                {
                    R = (byte)Math.Clamp(r, 0, 255),
                    G = (byte)Math.Clamp(g, 0, 255),
                    B = (byte)Math.Clamp(b, 0, 255),
                    A = 255
                };
            }
            // For dithering, we need to work with the original image data
            var height = pixelsHi.Length / width;
            var working = new RGBA8[pixelsHi.Length];
            Array.Copy(pixelsHi, working, pixelsHi.Length);
            Span<RGBA8> workingImage = working;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentIndex = y * width + x;
                    var oldColor = workingImage[currentIndex];

                    // Find closest palette color
                    int paletteIndex = GraphicsHelpers.FindNearestColor(oldColor.R, oldColor.G, oldColor.B, pal);
                    var off = paletteIndex * 3;
                    pixelsLo[currentIndex] = (byte)paletteIndex;

                    // Calculate error
                    int errorR = oldColor.R - pal[off];
                    int errorG = oldColor.G - pal[off + 1];
                    int errorB = oldColor.B - pal[off + 2];

                    ApplyDithering(workingImage, width, height, x, y, errorR, errorG, errorB, mode);
                }
                static void ApplyDithering(Span<RGBA8> workingImage, int width, int height, int x, int y, int errorR, int errorG, int errorB, DitheringMethod method)
                {
                    switch (method)
                    {
                        case DitheringMethod.FloydSteinberg:
                            // Floyd-Steinberg: [ ] [*] [7]
                            //                  [3] [5] [1]  (out of 16)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 7, 16);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 3, 16);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 5, 16);
                            DistributeError(workingImage, width, height, x + 1, y + 1, errorR, errorG, errorB, 1, 16);
                            break;

                        case DitheringMethod.Atkinson:
                            // Atkinson: [ ] [*] [1] [1]
                            //           [1] [1] [1] [ ]
                            //           [ ] [1] [ ] [ ]  (out of 8)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 1, 8);
                            DistributeError(workingImage, width, height, x + 2, y, errorR, errorG, errorB, 1, 8);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 1, 8);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 1, 8);
                            DistributeError(workingImage, width, height, x + 1, y + 1, errorR, errorG, errorB, 1, 8);
                            DistributeError(workingImage, width, height, x, y + 2, errorR, errorG, errorB, 1, 8);
                            break;

                        case DitheringMethod.Burkes:
                            // Burkes: [ ] [ ] [*] [8] [4]
                            //         [2] [4] [8] [4] [2]  (out of 32)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 8, 32);
                            DistributeError(workingImage, width, height, x + 2, y, errorR, errorG, errorB, 4, 32);
                            DistributeError(workingImage, width, height, x - 2, y + 1, errorR, errorG, errorB, 2, 32);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 4, 32);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 8, 32);
                            DistributeError(workingImage, width, height, x + 1, y + 1, errorR, errorG, errorB, 4, 32);
                            DistributeError(workingImage, width, height, x + 2, y + 1, errorR, errorG, errorB, 2, 32);
                            break;

                        case DitheringMethod.Sierra:
                            // Sierra: [ ] [ ] [*] [5] [3]
                            //         [2] [4] [5] [4] [2]
                            //         [ ] [2] [3] [2] [ ]  (out of 32)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 5, 32);
                            DistributeError(workingImage, width, height, x + 2, y, errorR, errorG, errorB, 3, 32);
                            DistributeError(workingImage, width, height, x - 2, y + 1, errorR, errorG, errorB, 2, 32);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 4, 32);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 5, 32);
                            DistributeError(workingImage, width, height, x + 1, y + 1, errorR, errorG, errorB, 4, 32);
                            DistributeError(workingImage, width, height, x + 2, y + 1, errorR, errorG, errorB, 2, 32);
                            DistributeError(workingImage, width, height, x - 1, y + 2, errorR, errorG, errorB, 2, 32);
                            DistributeError(workingImage, width, height, x, y + 2, errorR, errorG, errorB, 3, 32);
                            DistributeError(workingImage, width, height, x + 1, y + 2, errorR, errorG, errorB, 2, 32);
                            break;

                        case DitheringMethod.SierraLite:
                            // Sierra Lite: [ ] [*] [2]
                            //              [1] [1] [ ]  (out of 4)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 2, 4);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 1, 4);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 1, 4);
                            break;

                        case DitheringMethod.Stucki:
                            // Stucki: [ ] [ ] [*] [8] [4]
                            //         [2] [4] [8] [4] [2]
                            //         [1] [2] [4] [2] [1]  (out of 42)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 8, 42);
                            DistributeError(workingImage, width, height, x + 2, y, errorR, errorG, errorB, 4, 42);
                            DistributeError(workingImage, width, height, x - 2, y + 1, errorR, errorG, errorB, 2, 42);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 4, 42);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 8, 42);
                            DistributeError(workingImage, width, height, x + 1, y + 1, errorR, errorG, errorB, 4, 42);
                            DistributeError(workingImage, width, height, x + 2, y + 1, errorR, errorG, errorB, 2, 42);
                            DistributeError(workingImage, width, height, x - 2, y + 2, errorR, errorG, errorB, 1, 42);
                            DistributeError(workingImage, width, height, x - 1, y + 2, errorR, errorG, errorB, 2, 42);
                            DistributeError(workingImage, width, height, x, y + 2, errorR, errorG, errorB, 4, 42);
                            DistributeError(workingImage, width, height, x + 1, y + 2, errorR, errorG, errorB, 2, 42);
                            DistributeError(workingImage, width, height, x + 2, y + 2, errorR, errorG, errorB, 1, 42);
                            break;

                        case DitheringMethod.JarvisJudiceNinke:
                            // Jarvis-Judice-Ninke: [ ] [ ] [*] [7] [5]
                            //                      [3] [5] [7] [5] [3]
                            //                      [1] [3] [5] [3] [1]  (out of 48)
                            DistributeError(workingImage, width, height, x + 1, y, errorR, errorG, errorB, 7, 48);
                            DistributeError(workingImage, width, height, x + 2, y, errorR, errorG, errorB, 5, 48);
                            DistributeError(workingImage, width, height, x - 2, y + 1, errorR, errorG, errorB, 3, 48);
                            DistributeError(workingImage, width, height, x - 1, y + 1, errorR, errorG, errorB, 5, 48);
                            DistributeError(workingImage, width, height, x, y + 1, errorR, errorG, errorB, 7, 48);
                            DistributeError(workingImage, width, height, x + 1, y + 1, errorR, errorG, errorB, 5, 48);
                            DistributeError(workingImage, width, height, x + 2, y + 1, errorR, errorG, errorB, 3, 48);
                            DistributeError(workingImage, width, height, x - 2, y + 2, errorR, errorG, errorB, 1, 48);
                            DistributeError(workingImage, width, height, x - 1, y + 2, errorR, errorG, errorB, 3, 48);
                            DistributeError(workingImage, width, height, x, y + 2, errorR, errorG, errorB, 5, 48);
                            DistributeError(workingImage, width, height, x + 1, y + 2, errorR, errorG, errorB, 3, 48);
                            DistributeError(workingImage, width, height, x + 2, y + 2, errorR, errorG, errorB, 1, 48);
                            break;
                    }
                }
                static void DistributeError(Span<RGBA8> workingImage, int width, int height, int x, int y, int errorR, int errorG, int errorB, int numerator, int denominator)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        int index = y * width + x;
                        var color = workingImage[index];
                        workingImage[index] = ClampColor(
                            color.R + errorR * numerator / denominator,
                            color.G + errorG * numerator / denominator,
                            color.B + errorB * numerator / denominator
                        );
                    }
                }
            }
        }
    }
}
