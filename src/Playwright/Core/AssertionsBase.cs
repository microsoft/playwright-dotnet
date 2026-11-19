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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

[assembly: InternalsVisibleTo("Microsoft.Playwright.MSTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleTo("Microsoft.Playwright.MSTest.v4, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleTo("Microsoft.Playwright.NUnit, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleTo("Microsoft.Playwright.Xunit, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleTo("Microsoft.Playwright.Xunit.v3, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]

namespace Microsoft.Playwright.Core;

internal abstract class AssertionsBase
{
    private static float _defaultTimeout = 5_000;

    public AssertionsBase(bool isNot, string? customMessage = null)
    {
        IsNot = isNot;
        CustomMessage = customMessage;
    }

    protected bool IsNot { get; }

    protected string? CustomMessage { get; }

    public static void SetDefaultTimeout(float timeout)
        => _defaultTimeout = timeout;

    protected abstract Task<FrameExpectResult> CallExpectAsync(string expression, FrameExpectOptions expectOptions, string title);

    // locator.toString(), or null when the receiver is a page.
    protected virtual Task<string?> LocatorStringAsync() => Task.FromResult<string?>(null);

    // toBeTruthy.ts
    protected async Task ToBeTruthyAsync(string matcherName, string expected, string arg, string expression, FrameExpectOptions options)
    {
        var result = await QueryAsync(matcherName, expression, options).ConfigureAwait(false);
        var pass = result.Matches;
        if (pass == !IsNot)
        {
            return;
        }

        var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
        string printedExpected;
        string printedReceived;
        if (pass)
        {
            printedExpected = $"Expected: not {expected}";
            printedReceived = hasErrorMessage ? string.Empty : $"Received: {expected}";
        }
        else
        {
            printedExpected = $"Expected: {expected}";
            printedReceived = hasErrorMessage ? string.Empty : $"Received: {result.Received as string ?? MatcherHint.PrintValue(result.Received)}";
        }
        throw await CreateExceptionAsync(result, options, new()
        {
            MatcherName = matcherName,
            Expectation = arg,
            PrintedExpected = printedExpected,
            PrintedReceived = printedReceived,
        }).ConfigureAwait(false);
    }

    // toEqual.ts
    protected async Task ToEqualAsync(string matcherName, string expression, object? expected, FrameExpectOptions options)
    {
        var result = await QueryAsync(matcherName, expression, options).ConfigureAwait(false);
        var pass = result.Matches;
        if (pass == !IsNot)
        {
            return;
        }

        var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
        string? printedExpected;
        string? printedReceived = null;
        if (pass)
        {
            printedExpected = $"Expected: not {MatcherHint.PrintValue(expected)}";
            printedReceived = hasErrorMessage ? string.Empty : $"Received: {MatcherHint.PrintValue(result.Received)}";
        }
        else if (hasErrorMessage)
        {
            printedExpected = $"Expected: {MatcherHint.PrintValue(expected)}";
        }
        else
        {
            (printedExpected, printedReceived) = MatcherHint.PrintExpectedAndReceived(expected, result.Received, "Expected", "Received");
        }
        throw await CreateExceptionAsync(result, options, new()
        {
            MatcherName = matcherName,
            Expectation = "expected",
            PrintedExpected = printedExpected,
            PrintedReceived = printedReceived,
        }).ConfigureAwait(false);
    }

    // toMatchText.ts, where expected is a string or a regular expression.
    protected async Task ToMatchTextAsync(string matcherName, string expression, object expected, FrameExpectOptions options, bool matchSubstring = false)
    {
        var result = await QueryAsync(matcherName, expression, options).ConfigureAwait(false);
        var pass = result.Matches;
        if (pass == !IsNot)
        {
            return;
        }

        var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
        var expectedSuffix = expected is Regex ? " pattern" : (matchSubstring ? " substring" : string.Empty);
        var receivedSuffix = expected is Regex || matchSubstring ? " string" : string.Empty;
        var receivedString = result.Received as string ?? string.Empty;
        string? printedExpected;
        string? printedReceived = null;
        if (pass)
        {
            printedExpected = $"Expected{expectedSuffix}: not {MatcherHint.PrintValue(expected)}";
            if (!hasErrorMessage)
            {
                printedReceived = $"Received{receivedSuffix}: {MatcherHint.PrintValue(receivedString)}";
            }
        }
        else if (hasErrorMessage)
        {
            printedExpected = $"Expected{expectedSuffix}: {MatcherHint.PrintValue(expected)}";
        }
        else
        {
            (printedExpected, printedReceived) = MatcherHint.PrintExpectedAndReceived(expected, receivedString, $"Expected{expectedSuffix}", $"Received{receivedSuffix}");
        }
        throw await CreateExceptionAsync(result, options, new()
        {
            MatcherName = matcherName,
            Expectation = "expected",
            PrintedExpected = printedExpected,
            PrintedReceived = printedReceived,
        }).ConfigureAwait(false);
    }

    // toMatchAriaSnapshot.ts
    protected async Task ToMatchAriaSnapshotAsync(string expected, FrameExpectOptions options)
    {
        const string MatcherName = "ToMatchAriaSnapshotAsync";
        expected = Unshift(expected);
        options.ExpectedValue = ScriptsHelper.SerializedArgument(expected);
        var result = await QueryAsync(MatcherName, "to.match.aria", options).ConfigureAwait(false);
        var pass = result.Matches;
        var hasErrorMessage = !string.IsNullOrEmpty(result.ErrorMessage);
        if (!hasErrorMessage && pass == !IsNot)
        {
            return;
        }

        var receivedRaw = result.Received is IDictionary<string, object?> typedReceived && typedReceived.TryGetValue("raw", out var raw) ? raw as string : null;
        string? printedExpected;
        string? printedReceived = null;
        if (hasErrorMessage)
        {
            printedExpected = $"Expected: {(IsNot ? "not " : string.Empty)}{MatcherHint.PrintValue(expected)}";
        }
        else if (pass)
        {
            printedExpected = $"Expected: not {MatcherHint.PrintValue(expected)}";
            printedReceived = $"Received: {MatcherHint.PrintValue(receivedRaw ?? string.Empty)}";
        }
        else
        {
            (printedExpected, printedReceived) = MatcherHint.PrintExpectedAndReceived(expected, receivedRaw ?? string.Empty, "Expected", "Received");
        }
        throw await CreateExceptionAsync(result, options, new()
        {
            MatcherName = MatcherName,
            Expectation = "expected",
            PrintedExpected = printedExpected,
            PrintedReceived = printedReceived,
        }).ConfigureAwait(false);
    }

    protected ExpectedTextValue[] ExpectedText(object expected, ExpectedTextValue? options = null)
        => new[] { ExpectedTextItem(expected, options) };

    protected ExpectedTextValue[] ExpectedTextArray<T>(IEnumerable<T> expected, Func<ExpectedTextValue>? options = null)
        => expected.Select(item => ExpectedTextItem(item!, options?.Invoke())).ToArray();

    protected FrameExpectOptions ConvertToFrameExpectOptions(object? source) => ClassUtils.Clone<FrameExpectOptions>(source);

    // unshift() of toMatchAriaSnapshot.ts.
    private static string Unshift(string snapshot)
    {
        var lines = snapshot.Split('\n');
        var whitespacePrefixLength = 100;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var prefixLength = line.Length - line.TrimStart().Length;
            if (prefixLength < whitespacePrefixLength)
            {
                whitespacePrefixLength = prefixLength;
            }
        }
        return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Substring(Math.Min(whitespacePrefixLength, line.Length))));
    }

    private static ExpectedTextValue ExpectedRegex(Regex pattern, ExpectedTextValue? options)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        ExpectedTextValue textValue = options ?? new() { };
        var (source, flags) = pattern.GetSourceAndFlags();
        textValue.RegexSource = source;
        textValue.RegexFlags = flags;
        return textValue;
    }

    private static ExpectedTextValue ExpectedTextItem(object expected, ExpectedTextValue? options)
    {
        if (expected is Regex regex)
        {
            return ExpectedRegex(regex, options);
        }
        options ??= new();
        options.String = (string)expected;
        return options;
    }

    private Task<FrameExpectResult> QueryAsync(string matcherName, string expression, FrameExpectOptions options)
    {
        options.Timeout ??= _defaultTimeout;
        options.IsNot = IsNot;
        var title = !string.IsNullOrEmpty(CustomMessage) ? CustomMessage! : $"Expect \"{(IsNot ? "Not." : string.Empty)}{matcherName}\"";
        return CallExpectAsync(expression, options, title);
    }

    private async Task<PlaywrightAssertionException> CreateExceptionAsync(FrameExpectResult result, FrameExpectOptions options, MatcherHint.Details details)
    {
        details.IsNot = IsNot;
        details.Locator = await LocatorStringAsync().ConfigureAwait(false);
        details.Timeout = options.Timeout ?? 0;
        details.TimedOut = result.TimedOut;
        details.ErrorMessage = result.ErrorMessage;
        details.Log = result.Log;
        var message = MatcherHint.FormatMatcherMessage(details);
        if (!string.IsNullOrEmpty(CustomMessage))
        {
            message = CustomMessage + "\n\n" + message;
        }
        if (!string.IsNullOrEmpty(result.ReceivedAriaSnapshot))
        {
            message += $"\nAria snapshot:\n{result.ReceivedAriaSnapshot}";
        }
        return new PlaywrightAssertionException(message);
    }
}
