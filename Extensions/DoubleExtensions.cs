using System;

namespace AutoBrew.Extensions
{
    internal static class DoubleExtensions
    {
        public static bool Is(this double value, double target)
        {
            return value.Is(target, double.Epsilon);
        }

        public static bool Is(this double value, double target, double epsilon)
        {
            return Math.Abs(value - target) < epsilon;
        }

        public static double Clamp01(this double value)
        {
            return Clamp(value, 0.0, 1.0);
        }

        public static double Clamp(this double value, double min, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
