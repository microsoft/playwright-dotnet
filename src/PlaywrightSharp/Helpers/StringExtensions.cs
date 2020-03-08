using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Parse the query string.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> containing the parsed QueryString.</returns>
        public static Dictionary<string, string> ParseQueryString(this string query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var result = new Dictionary<string, string>();

            foreach (string keyvalue in query.Split('&'))
            {
                string[] pair = keyvalue.Split('=');
                result[pair[0]] = pair[1];
            }

            return result;
        }

        private static bool IsQuoted(this string value)
            => value.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && value.EndsWith("\"", StringComparison.OrdinalIgnoreCase);
    }
}
