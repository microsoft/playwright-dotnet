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
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Helpers;

public class URLMatch
{
    public Regex reMatch { get; set; }

    public Func<string, bool> funcMatch { get; set; }

    public string globMatch { get; set; }

    public string BaseURL { get; set; }

    public bool Match(string url)
    {
        if (reMatch != null)
        {
            return reMatch.IsMatch(url);
        }

        if (funcMatch != null)
        {
            return funcMatch(url);
        }

        if (globMatch != null)
        {
            var globWithBaseURL = JoinWithBaseURL(BaseURL, globMatch);
            // Allow http(s) baseURL to match ws(s) urls.
            if (new Regex("^https?://").IsMatch(globWithBaseURL) && new Regex("^wss?://").IsMatch(url))
            {
                globWithBaseURL = new Regex("^http").Replace(globWithBaseURL, "ws");
            }
            return new Regex(globWithBaseURL.GlobToRegex()).IsMatch(url);
        }
        return false;
    }

    internal static string JoinWithBaseURL(string baseUrl, string url)
    {
        if (string.IsNullOrEmpty(baseUrl)
            || (url?.StartsWith("*", StringComparison.InvariantCultureIgnoreCase) ?? false)
            || !Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
        {
            return url;
        }

        var mUri = new Uri(url, UriKind.RelativeOrAbsolute);
        if (!mUri.IsAbsoluteUri)
        {
            return new Uri(new Uri(baseUrl), mUri).ToString();
        }

        return url;
    }

    public bool Equals(string globMatch, Regex reMatch, Func<string, bool> funcMatch, string baseURL)
    {
        return this.reMatch?.ToString() == reMatch?.ToString() && this.reMatch?.Options == reMatch?.Options
            && this.funcMatch == funcMatch
            && this.globMatch == globMatch
            && BaseURL == baseURL;
    }
}
