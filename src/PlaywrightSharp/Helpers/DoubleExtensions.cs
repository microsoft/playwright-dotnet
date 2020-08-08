using System;

namespace PlaywrightSharp.Helpers
{
    internal static class DoubleExtensions
    {
        public static bool IsNegativeZero(this double d) => d == 0.0 && BitConverter.GetBytes(0d) != BitConverter.GetBytes(d);
    }
}
