using System;
using System.Collections.Generic;
using Esprima;
using Esprima.Ast;

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

            foreach (string keyValue in query.Split('&'))
            {
                string[] pair = keyValue.Split('=');
                result[pair[0]] = pair[1];
            }

            return result;
        }

        /// <summary>
        /// Determine if the script is a javascript function and not an expression.
        /// </summary>
        /// <param name="script">Script to evaluate.</param>
        /// <returns>Whether the script is a function or not.</returns>
        public static bool IsJavascriptFunction(this string script)
        {
            try
            {
                var parser = new JavaScriptParser(script);
                var program = parser.ParseScript();

                if (program.Body.Count > 0)
                {
                    return
                        (program.Body[0] is ExpressionStatement expression && expression.Expression.Type == Nodes.ArrowFunctionExpression) ||
                        program.Body[0] is FunctionDeclaration;
                }

                return false;
            }
            catch (ParserException)
            {
                // Retry using parenthesis
                var parser = new JavaScriptParser($"({script})");
                var program = parser.ParseScript();

                if (program.Body.Count > 0)
                {
                    return
                        program.Body.Count > 0 &&
                        program.Body[0] is ExpressionStatement expression &&
                        expression.Expression.Type == Nodes.FunctionExpression;
                }

                return false;
            }
        }

        private static bool IsQuoted(this string value)
            => value.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && value.EndsWith("\"", StringComparison.OrdinalIgnoreCase);
    }
}
