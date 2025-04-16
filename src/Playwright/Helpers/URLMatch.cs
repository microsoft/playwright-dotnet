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
    public Regex re { get; set; }

    public Func<string, bool> func { get; set; }

    public bool Match(string url)
    {
        return MatchImpl(url, re, func);
    }

    private static bool MatchImpl(string url, Regex re, Func<string, bool> func)
    {
        if (re != null)
        {
            return re.IsMatch(url);
        }

        if (func != null)
        {
            return func(url);
        }

        return true;
    }

    internal static string ConstructURLBasedOnBaseURL(string baseUrl, string url)
    {
        try
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                return new Uri(url, UriKind.Absolute).ToString();
            }
            return new Uri(new Uri(baseUrl), new Uri(url, UriKind.RelativeOrAbsolute)).ToString();
        }
        catch
        {
            return url;
        }
    }

    public bool Equals(URLMatch matcher)
    {
        return this.re?.ToString() == matcher.re?.ToString() && this.re?.Options == matcher.re?.Options
            && this.func == matcher.func;
    }
}
