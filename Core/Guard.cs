using System;

namespace OberbekRotation.Core
{
    internal static class Guard
    {
        internal static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "A finite value is required.");
            return value;
        }

        internal static double NonNegative(double value, string name)
        {
            if (Finite(value, name) < 0)
                throw new ArgumentOutOfRangeException(name, "A non-negative value is required.");
            return value;
        }

        internal static double Positive(double value, string name)
        {
            if (Finite(value, name) <= 0)
                throw new ArgumentOutOfRangeException(name, "A positive value is required.");
            return value;
        }
    }
}
