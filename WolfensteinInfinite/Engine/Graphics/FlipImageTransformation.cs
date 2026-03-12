//Clean
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.Engine.Graphics
{
    public class FlipImageTransformation : IImageTransformation
    {
        public bool FlipHorizontally { get; set; }
        public bool FlipVertically { get; set; }
        public float[,] CreateTransformationMatrix()
        {
            // identity matrix  
            float[,] matrix = Matrices.CreateIdentityMatrix(2);

            if (FlipHorizontally)
                matrix[0, 0] *= -1;
            if (FlipVertically)
                matrix[1, 1] *= -1;

            return matrix;
        }

        public FlipImageTransformation() { }
        public FlipImageTransformation(bool flipHoriz, bool flipVert)
        {
            FlipHorizontally = flipHoriz;
            FlipVertically = flipVert;
        }
    }
}
