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

namespace Microsoft.Playwright.Helpers;

public class URLMatch
{
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_expressions#escaping
    private static readonly char[] _escapeGlobChars = new[] { '$', '^', '+', '.', '*', '(', ')', '|', '\\', '?', '{', '}', '[', ']' };

    public Regex? re { get; set; }

    public Func<string, bool>? func { get; set; }

    public string? glob { get; set; }

    public string? baseURL { get; set; }

    public bool isWebSocketUrl { get; set; }

    public bool Match(string url)
    {
        return MatchImpl(url, re, func, glob, baseURL, isWebSocketUrl);
    }

    private static bool MatchImpl(string url, Regex? re, Func<string, bool>? func, string? glob, string? baseURL, bool isWebSocketUrl)
    {
        if (re != null)
        {
            return re.IsMatch(url);
        }

        if (func != null)
        {
            return func(url);
        }

        if (glob != null)
        {
            if (string.IsNullOrEmpty(glob))
            {
                return true;
            }
            var match = ResolveGlobToRegexPattern(baseURL, glob, isWebSocketUrl);
            return new Regex(match).IsMatch(url);
        }

        return true;
    }

    // In Node.js, new URL('http://localhost') returns 'http://localhost/'.
    // To ensure the same url matching behvaior, do the same.
    internal static Uri FixupTrailingSlash(Uri uri)
    {
        var builder = new UriBuilder(uri);
        if (string.IsNullOrEmpty(builder.Path))
        {
            builder.Path = "/";
        }
        return builder.Uri;
    }

    internal static string ConstructURLBasedOnBaseURL(string? baseUrl, string url)
    {
        try
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                return FixupTrailingSlash(new Uri(url, UriKind.Absolute)).ToString();
            }
            return FixupTrailingSlash(new Uri(new Uri(baseUrl), new Uri(url, UriKind.RelativeOrAbsolute))).ToString();
        }
        catch
        {
            return url;
        }
    }

    public static string? GlobToRegexPattern(string glob)
    {
        if (string.IsNullOrEmpty(glob))
        {
            return null;
        }

        List<string> tokens = new() { "^" };
        bool inGroup = false;

        for (int i = 0; i < glob.Length; ++i)
        {
            var c = glob[i];
            if (c == '\\' && i + 1 < glob.Length)
            {
                var @char = glob[++i];
                tokens.Add(_escapeGlobChars.Contains(@char) ? "\\" + @char : @char.ToString());
                continue;
            }
            if (c == '*')
            {
                char? beforeDeep = i == 0 ? null : glob[i - 1];
                int starCount = 1;
                while (i < glob.Length - 1 && glob[i + 1] == '*')
                {
                    starCount++;
                    i++;
                }

                char? afterDeep = i >= glob.Length - 1 ? null : glob[i + 1];
                var isDeep = starCount > 1 &&
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
                    tokens.Add(_escapeGlobChars.Contains(c) ? "\\" + c : c.ToString());
                    break;
            }
        }

        tokens.Add("$");
        return string.Concat(tokens.ToArray());
    }

    internal static string? ToWebSocketBaseURL(string? baseURL)
    {
        if (string.IsNullOrEmpty(baseURL) || baseURL == null)
        {
            return baseURL;
        }
        // Allow http(s) baseURL to match ws(s) urls.
        if (baseURL.StartsWith("http://"))
        {
            return baseURL.Replace("http://", "ws://");
        }
        if (baseURL.StartsWith("https://"))
        {
            return baseURL.Replace("https://", "wss://");
        }
        return baseURL;
    }

    internal static string? ResolveGlobToRegexPattern(string? baseURL, string glob, bool isWebSocketUrl)
    {
        if (isWebSocketUrl)
        {
            baseURL = ToWebSocketBaseURL(baseURL);
        }
        glob = ResolveGlobBase(baseURL, glob);
        return GlobToRegexPattern(glob);
    }

    internal static string ResolveGlobBase(string? baseURL, string match)
    {
        // NOTE: Node.js version uses "$" in mapped tokens, but C# cannot swallow that.
        // So we use "playwright-pw-" instead. It is also important that this string is lowercase.
        if (!match.StartsWith("*"))
        {
            var tokenMap = new Dictionary<string, string>();

            string MapToken(string original, string replacement)
            {
                if (string.IsNullOrEmpty(original))
                {
                    return string.Empty;
                }
                tokenMap[replacement] = original;
                return replacement;
            }

            // Escaped `\\?` behaves the same as `?` in our glob patterns.
            match = match.Replace("\\\\?", "?");
            // Glob symbols may be escaped in the URL and some of them such as ? affect resolution,
            // so we replace them with safe components first.
            var relativePath = string.Join("/", match.Split('/').Select((token, index) =>
            {
                if (token == "." || token == ".." || token == string.Empty)
                {
                    return token;
                }
                // Handle special case of http*://, note that the new schema has to be
                // a web schema so that slashes are properly inserted after domain.
                if (index == 0 && token.EndsWith(":"))
                {
                    return MapToken(token, "http:");
                }
                int questionIndex = token.IndexOf('?');
                if (questionIndex == -1)
                {
                    return MapToken(token, $"playwright-pw-{index}-pw-playwright");
                }
                string newPrefix = MapToken(token.Substring(0, questionIndex), $"playwright-pw-{index}-pw-playwright");
                string newSuffix = MapToken(token.Substring(questionIndex), $"?playwright-pw2-{index}-pw2-playwright");
                return newPrefix + newSuffix;
            }));

            var resolved = ConstructURLBasedOnBaseURL(baseURL, relativePath);
            foreach (var kvp in tokenMap)
            {
                resolved = resolved.Replace(kvp.Key, kvp.Value);
            }
            match = resolved;
        }
        return match;
    }

    public bool Equals(string? globMatch, Regex? reMatch, Func<string, bool>? funcMatch, string? baseURL, bool isWebSocketUrl)
    {
        return this.re?.ToString() == reMatch?.ToString() && this.re?.Options == reMatch?.Options
            && this.func == funcMatch
            && this.glob == globMatch
            && this.baseURL == baseURL
            && this.isWebSocketUrl == isWebSocketUrl;
    }
}
