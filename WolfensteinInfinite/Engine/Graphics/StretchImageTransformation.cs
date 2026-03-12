//Clean
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.Engine.Graphics
{
    public class StretchImageTransformation : IImageTransformation
    {
        public float HorizontalStretch { get; set; }
        public float VerticalStretch { get; set; }

        public float[,] CreateTransformationMatrix()
        {
            float[,] matrix = Matrices.CreateIdentityMatrix(2);

            matrix[0, 0] += HorizontalStretch;
            matrix[1, 1] += VerticalStretch;

            return matrix;
        }

        public StretchImageTransformation() { }
        public StretchImageTransformation(float horizStretch, float vertStretch)
        {
            HorizontalStretch = horizStretch;
            VerticalStretch = vertStretch;
        }
    }
}
