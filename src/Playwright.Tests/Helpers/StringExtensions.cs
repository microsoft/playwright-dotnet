/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright
{
    /// <summary>
    /// String extensions.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly char[] _escapeGlobChars = new[] { '/', '$', '^', '+', '.', '(', ')', '=', '!', '|' };

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

            if (query.StartsWith("?", StringComparison.InvariantCultureIgnoreCase))
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
        /// Converts an url glob expression to a regex.
        /// </summary>
        /// <param name="glob">Input url.</param>
        /// <returns>A Regex with the glob expression.</returns>
        public static Regex GlobToRegex(this string glob)
        {
            if (string.IsNullOrEmpty(glob))
            {
                return null;
            }

            List<string> tokens = new() { "^" };
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
    }
}
