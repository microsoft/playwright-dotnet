/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Core;

/// <summary>
/// Port of <c>packages/playwright/src/matchers/matcherHint.ts</c>: the message of a failed matcher.
/// It has the layout of upstream and names the .NET call. The values are printed on one line each,
/// where upstream prints a diff.
/// </summary>
internal static class MatcherHint
{
    public static string FormatMatcherMessage(Details details)
    {
        var receiver = details.Receiver ?? (string.IsNullOrEmpty(details.Locator) ? "page" : "locator");
        var message = new StringBuilder($"Expect({receiver}){(details.IsNot ? ".Not" : string.Empty)}.{details.MatcherName}({details.Expectation}) failed\n\n");

        var align = string.IsNullOrEmpty(details.ErrorMessage) &&
            details.PrintedExpected?.StartsWith("Expected:", StringComparison.Ordinal) == true &&
            (string.IsNullOrEmpty(details.PrintedReceived) || details.PrintedReceived!.StartsWith("Received:", StringComparison.Ordinal));
        if (!string.IsNullOrEmpty(details.Locator))
        {
            message.Append($"Locator: {(align ? " " : string.Empty)}{details.Locator}\n");
        }
        if (!string.IsNullOrEmpty(details.PrintedExpected))
        {
            message.Append(details.PrintedExpected).Append('\n');
        }
        if (!string.IsNullOrEmpty(details.PrintedReceived))
        {
            message.Append(details.PrintedReceived).Append('\n');
        }
        if (details.TimedOut && details.Timeout != 0)
        {
            message.Append($"Timeout: {(align ? " " : string.Empty)}{details.Timeout.ToString(CultureInfo.InvariantCulture)}ms\n");
        }
        if (!string.IsNullOrEmpty(details.ErrorMessage))
        {
            message.Append(details.ErrorMessage);
            if (!details.ErrorMessage!.EndsWith("\n", StringComparison.Ordinal))
            {
                message.Append('\n');
            }
        }
        message.Append(CallLogText(details.Log));
        return message.ToString();
    }

    public static string CallLogText(string[]? log)
    {
        if (log == null || !log.Any(l => !string.IsNullOrEmpty(l)))
        {
            return string.Empty;
        }
        return $"\nCall log:\n{string.Join("\n", log)}\n";
    }

    // The two lines of a mismatch, with the values in one column.
    public static (string Expected, string Received) PrintExpectedAndReceived(object? expected, object? received, string expectedLabel, string receivedLabel)
    {
        var width = Math.Max(expectedLabel.Length, receivedLabel.Length);
        var printedExpected = PrintValue(expected);
        var printedReceived = PrintValue(received);
        return (
            $"{expectedLabel}: {new string(' ', width - expectedLabel.Length)}{printedExpected}",
            $"{receivedLabel}: {new string(' ', width - receivedLabel.Length)}{(printedExpected == printedReceived ? "serializes to the same string" : printedReceived)}");
    }

    // A scalar is printed as upstream prints it, an array and an object on one line.
    public static string PrintValue(object? value, int depth = 0)
    {
        switch (value)
        {
            case null:
                return "null";
            case bool b:
                return b ? "true" : "false";
            case string or char or Guid or Uri:
                return "\"" + Convert.ToString(value, CultureInfo.InvariantCulture)!.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            case Regex regex:
                var (source, flags) = regex.GetSourceAndFlags();
                return $"/{source}/{flags}";
            case DateTime date:
                return date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            case ChannelOwner:
                return value.ToString()!;
        }
        if (depth > 10)
        {
            return "…";
        }

        IEnumerable<KeyValuePair<string, object?>> properties;
        if (value is IDictionary<string, object?> expando)
        {
            properties = expando;
        }
        else if (value is IDictionary dictionary)
        {
            properties = dictionary.Keys.Cast<object>().Select(key => new KeyValuePair<string, object?>(Convert.ToString(key, CultureInfo.InvariantCulture)!, dictionary[key]));
        }
        else if (value is IEnumerable enumerable)
        {
            return "[" + string.Join(", ", enumerable.Cast<object?>().Select(item => PrintValue(item, depth + 1))) + "]";
        }
        else
        {
            properties = TypeDescriptor.GetProperties(value).Cast<PropertyDescriptor>().Select(property => new KeyValuePair<string, object?>(property.Name, property.GetValue(value)));
        }
        return "{" + string.Join(", ", properties.Select(property => $"{PrintValue(property.Key)}: {PrintValue(property.Value, depth + 1)}")) + "}";
    }

    internal sealed class Details
    {
        public bool IsNot { get; set; }

        public string? Receiver { get; set; }

        public string MatcherName { get; set; } = null!;

        public string Expectation { get; set; } = null!;

        public string? Locator { get; set; }

        public string? PrintedExpected { get; set; }

        public string? PrintedReceived { get; set; }

        public bool TimedOut { get; set; }

        public float Timeout { get; set; }

        public string? ErrorMessage { get; set; }

        public string[]? Log { get; set; }
    }
}
