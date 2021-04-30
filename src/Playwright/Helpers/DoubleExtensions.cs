using System;

namespace Microsoft.Playwright.Helpers
{
    /// <summary>
    /// Extensions for <see cref="double"/>.
    /// </summary>
    public static class DoubleExtensions
    {
        /// <summary>
        /// Checks if the double is a negative zero.
        /// </summary>
        /// <param name="d">Number to check.</param>
        /// <returns>Whether the number is negative zero or not.</returns>
        public static bool IsNegativeZero(this double d) => d == 0.0 && BitConverter.GetBytes(0d) != BitConverter.GetBytes(d);
    }
}
