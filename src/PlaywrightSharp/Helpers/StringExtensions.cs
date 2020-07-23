using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Esprima;
using Esprima.Ast;

namespace PlaywrightSharp.Helpers
{
    /// <summary>
    /// String extensions.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly char[] _escapeGlobChars = new[] { '/', '$', '^', '+', '.', '(', ')', '=', '!', '|' };

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

            if (query.StartsWith("?"))
            {
                query = query.Substring(1, query.Length - 1);
            }

            foreach (string keyValue in query.Split('&').Where(kv => kv.Contains("=")))
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

        internal static bool UrlMatches(this string url, string glob) => GlobToRegex(glob).Match(url).Success;

        private static Regex GlobToRegex(string glob)
        {
            List<string> tokens = new List<string> { "^" };
            bool inGroup = false;

            for (int i = 0; i < glob.Length; ++i)
            {
                char c = glob[i];
                if (_escapeGlobChars.Contains(c))
                {
                    tokens.Add("\\" + c);
                    continue;
                }

                if (c == '*')
                {
                    char? beforeDeep = i == 0 ? (char?)null : glob[i - 1];
                    int starCount = 1;

                    while (i < glob.Length - 1 && glob[i + 1] == '*')
                    {
                        starCount++;
                        i++;
                    }

                    char? afterDeep = i >= glob.Length - 1 ? (char?)null : glob[i + 1];
                    bool isDeep = starCount > 1 &&
                        (beforeDeep == '/' || beforeDeep == null) &&
                        (afterDeep == '/' || afterDeep == null);
                    if (isDeep)
                    {
                        tokens.Add("((?:[^/]*(?:\\/|$))*)");
                        i++;
                    }
                    else
                    {
                        tokens.Add("([^/]*)");
                    }

                    continue;
                }

                switch (c)
                {
                    case '?':
                        tokens.Add(".");
                        break;
                    case '{':
                        inGroup = true;
                        tokens.Add("(");
                        break;
                    case '}':
                        inGroup = false;
                        tokens.Add(")");
                        break;
                    case ',':
                        if (inGroup)
                        {
                            tokens.Add("|");
                            break;
                        }

                        tokens.Add("\\" + c);
                        break;
                    default:
                        tokens.Add(c.ToString());
                        break;
                }
            }

            tokens.Add("$");
            return new Regex(string.Concat(tokens.ToArray()));
        }

        private static bool IsQuoted(this string value)
            => value.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && value.EndsWith("\"", StringComparison.OrdinalIgnoreCase);
    }
}
