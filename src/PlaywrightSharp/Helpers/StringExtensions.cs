using System;

namespace PlaywrightSharp.Helpers
{
    /// <summary>
    /// String extensions.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Quotes the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">The string to quote.</param>
        /// <returns>A quoted string.</returns>
        public static string Quote(this string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!IsQuoted(value))
            {
                value = string.Concat("\"", value, "\"");
            }

            return value;
        }

        /// <summary>
        /// Unquote the specified <see cref="string"/>.
        /// </summary>
        /// <param name="value">The string to unquote.</param>
        /// <returns>An unquoted string.</returns>
        public static string UnQuote(this string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (IsQuoted(value))
            {
                value = value.Trim('"');
            }

            return value;
        }

        private static bool IsQuoted(this string value)
            => value.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && value.EndsWith("\"", StringComparison.OrdinalIgnoreCase);
    }
}
