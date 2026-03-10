//https://softwarebydefault.com/2013/06/16/image-transform-rotate/
namespace WolfensteinInfinite
{
    public partial class GraphicsSurface : IGraphics
    {
        public static (int X, int Y) RotateXY(int x, int y, double degrees, int offsetX, int offsetY) =>
            new((int)Math.Round((x - offsetX) *
                Math.Cos(degrees) - (y - offsetY) *
                Math.Sin(degrees)) + offsetX, (int)(Math.Round((x - offsetX) *
                Math.Sin(degrees) + (y - offsetY) *
                Math.Cos(degrees))) + offsetY);

        public static byte[] RotateImage(byte[] pixelBuffer, int width, int height,
                                       double degrees)
        {


            byte[] resultBuffer = new byte[pixelBuffer.Length];

            //Convert to Radians 
            degrees = degrees * Math.PI / 180.0;


            //Calculate Offset in order to rotate on image middle 
            int xOffset = (int)(width / 2.0);
            int yOffset = (int)(height / 2.0);
            int sx, xy;
            float rx, ry;

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int sourceXY = row * width + col * 4;


                    sx = col;
                    xy = row;


                    if (sourceXY >= 0 && sourceXY + 3 < pixelBuffer.Length)
                    {
                        //Calculate Rotation
                        var (X, Y) = RotateXY(sx, xy, degrees, xOffset, yOffset);
                        rx = X;
                        ry = Y;

                        int resultXY = (int)(Math.Round(
                  (ry * width) +
                  (rx), 0));
                        if ((rx < 0 || rx >= width || ry < 0 || ry >= height) &&
                                              resultXY >= 0)
                        {
                            if (resultXY < resultBuffer.Length)
                            {
                                resultBuffer[resultXY] =
                                     pixelBuffer[sourceXY];
                            }
                        }
                    }
                }
            }
            return resultBuffer;
        }
    }
}
