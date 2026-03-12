//Clean
namespace WolfensteinInfinite.Engine.Graphics
{
    public class RotationImageTransformation : IImageTransformation
    {
        public float AngleDegrees { get; set; }
        public float AngleRadians
        {
            get { return DegreesToRadians(AngleDegrees); }
            set { AngleDegrees = RadiansToDegrees(value); }
        }

        public static float DegreesToRadians(float degree)
        { return degree * MathF.PI / 180; }
        public static float RadiansToDegrees(float radians)
        { return radians / MathF.PI * 180; }

        public float[,] CreateTransformationMatrix()
        {
            float[,] matrix = new float[2, 2];

            matrix[0, 0] = MathF.Cos(AngleRadians);
            matrix[1, 0] = MathF.Sin(AngleRadians);
            matrix[0, 1] = -1 * MathF.Sin(AngleRadians);
            matrix[1, 1] = MathF.Cos(AngleRadians);

            return matrix;
        }

        public RotationImageTransformation() { }
        public RotationImageTransformation(float angleDegree)
        {
            AngleDegrees = angleDegree;
        }
    }
}
