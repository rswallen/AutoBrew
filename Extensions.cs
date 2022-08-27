using System;

namespace AutoBrew
{
    internal static class Extensions
    {
        public static bool Is(this double value, double target)
        {
            return value.Is(target, double.Epsilon);
        }

        public static bool Is(this double value, double target, double epsilon)
        {
            return Math.Abs(value - target) < epsilon;
        }
    }
}
