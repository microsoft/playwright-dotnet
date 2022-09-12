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

/// <summary>
/// Extensions for <see cref="System.Text.RegularExpressions.RegexOptions"/>.
/// </summary>
internal static class RegexOptionsExtensions
{
    public static string GetInlineFlags(this System.Text.RegularExpressions.RegexOptions options)
    {
        string flags = string.Empty;
        if (options.HasFlag(RegexOptions.IgnoreCase))
        {
            flags += "i";
        }
        if (options.HasFlag(RegexOptions.Singleline))
        {
            flags += "s";
        }
        if (options.HasFlag(RegexOptions.Multiline))
        {
            flags += "m";
        }
        if ((options & ~(RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline)) != 0)
        {
            throw new ArgumentException("Unsupported RegularExpression flags");
        }
        return flags;
    }

    public static RegexOptions FromInlineFlags(string flags)
    {
        var options = RegexOptions.None;
        for (int i = 0; i < flags.Length; i++)
        {
            switch (flags[i])
            {
                case 'i':
                    options |= RegexOptions.IgnoreCase;
                    break;
                case 's':
                    options |= RegexOptions.Singleline;
                    break;
                case 'm':
                    options |= RegexOptions.Multiline;
                    break;
                default:
                    throw new ArgumentException("Unsupported RegularExpression flags");
            }
        }
        return options;
    }
}
