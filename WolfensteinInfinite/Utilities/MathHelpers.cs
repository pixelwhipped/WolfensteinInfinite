//Clean
namespace WolfensteinInfinite.Utilities
{
    public static class MathHelpers
    {
        public static bool IsClose(double value1, double value2)
        {            
            // In case they are Infinities (then epsilon check does not work)
            if (value1 == value2)
                return true;            
            // This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < Epsilon            
            var epsilon = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * double.Epsilon;
            var delta = value1 - value2;

            return -epsilon < delta && epsilon > delta;
        }
    }
}
