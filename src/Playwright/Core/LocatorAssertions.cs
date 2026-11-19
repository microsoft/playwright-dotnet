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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class LocatorAssertions : AssertionsBase, ILocatorAssertions
{
    public LocatorAssertions(ILocator locator, bool isNot, string? customMessage = null) : base(isNot, customMessage)
    {
        ActualLocator = (Locator)locator;
    }

    private Locator ActualLocator { get; }

    public ILocatorAssertions Not => new LocatorAssertions(ActualLocator, !IsNot, CustomMessage);

    protected override Task<FrameExpectResult> CallExpectAsync(string expression, FrameExpectOptions expectOptions, string title)
    {
        return ActualLocator.ExpectAsync(expression, expectOptions, title);
    }

    protected override async Task<string?> LocatorStringAsync() => await ActualLocator.ToStringAsync().ConfigureAwait(false);

    public Task ToBeAttachedAsync(LocatorAssertionsToBeAttachedOptions? options = null)
    {
        var attached = options == null || options.Attached == null || options.Attached == true;
        var expected = attached ? "attached" : "detached";
        var arg = attached ? string.Empty : "new() { Attached = false }";
        return ToBeTruthyAsync("ToBeAttachedAsync", expected, arg, attached ? "to.be.attached" : "to.be.detached", ConvertToFrameExpectOptions(options));
    }

    public Task ToBeCheckedAsync(LocatorAssertionsToBeCheckedOptions? options = null)
    {
        var expectedValue = new Dictionary<string, object>();
        if (options?.Checked != null)
        {
            expectedValue["checked"] = options.Checked;
        }
        if (options?.Indeterminate != null)
        {
            expectedValue["indeterminate"] = options.Indeterminate;
        }
        var frameExpectedOptions = ConvertToFrameExpectOptions(options);
        frameExpectedOptions.ExpectedValue = ScriptsHelper.SerializedArgument(expectedValue);
        string expected;
        string arg;
        if (options?.Indeterminate == true)
        {
            expected = "indeterminate";
            arg = "new() { Indeterminate = true }";
        }
        else
        {
            expected = options?.Checked == false ? "unchecked" : "checked";
            arg = options?.Checked == false ? "new() { Checked = false }" : string.Empty;
        }
        return ToBeTruthyAsync("ToBeCheckedAsync", expected, arg, "to.be.checked", frameExpectedOptions);
    }

    public Task ToBeDisabledAsync(LocatorAssertionsToBeDisabledOptions? options = null)
        => ToBeTruthyAsync("ToBeDisabledAsync", "disabled", string.Empty, "to.be.disabled", ConvertToFrameExpectOptions(options));

    public Task ToBeEditableAsync(LocatorAssertionsToBeEditableOptions? options = null)
    {
        var editable = options == null || options.Editable == null || options.Editable == true;
        var expected = editable ? "editable" : "readOnly";
        var arg = editable ? string.Empty : "new() { Editable = false }";
        return ToBeTruthyAsync("ToBeEditableAsync", expected, arg, editable ? "to.be.editable" : "to.be.readonly", ConvertToFrameExpectOptions(options));
    }

    public Task ToBeEmptyAsync(LocatorAssertionsToBeEmptyOptions? options = null)
        => ToBeTruthyAsync("ToBeEmptyAsync", "empty", string.Empty, "to.be.empty", ConvertToFrameExpectOptions(options));

    public Task ToBeEnabledAsync(LocatorAssertionsToBeEnabledOptions? options = null)
    {
        var enabled = options == null || options.Enabled == null || options.Enabled == true;
        var expected = enabled ? "enabled" : "disabled";
        var arg = enabled ? string.Empty : "new() { Enabled = false }";
        return ToBeTruthyAsync("ToBeEnabledAsync", expected, arg, enabled ? "to.be.enabled" : "to.be.disabled", ConvertToFrameExpectOptions(options));
    }

    public Task ToBeFocusedAsync(LocatorAssertionsToBeFocusedOptions? options = null)
        => ToBeTruthyAsync("ToBeFocusedAsync", "focused", string.Empty, "to.be.focused", ConvertToFrameExpectOptions(options));

    public Task ToBeHiddenAsync(LocatorAssertionsToBeHiddenOptions? options = null)
        => ToBeTruthyAsync("ToBeHiddenAsync", "hidden", string.Empty, "to.be.hidden", ConvertToFrameExpectOptions(options));

    public Task ToBeInViewportAsync(LocatorAssertionsToBeInViewportOptions? options = null)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        if (options?.Ratio != null)
        {
            frameExpectOptions.ExpectedNumber = (float)options.Ratio;
        }
        return ToBeTruthyAsync("ToBeInViewportAsync", "in viewport", string.Empty, "to.be.in.viewport", frameExpectOptions);
    }

    public Task ToBeVisibleAsync(LocatorAssertionsToBeVisibleOptions? options = null)
    {
        var visible = options == null || options.Visible == null || options.Visible == true;
        var expected = visible ? "visible" : "hidden";
        var arg = visible ? string.Empty : "new() { Visible = false }";
        return ToBeTruthyAsync("ToBeVisibleAsync", expected, arg, visible ? "to.be.visible" : "to.be.hidden", ConvertToFrameExpectOptions(options));
    }

    public Task ToContainTextAsync(string expected, LocatorAssertionsToContainTextOptions? options = null)
        => ToContainTextAsync((object)expected, options);

    public Task ToContainTextAsync(Regex expected, LocatorAssertionsToContainTextOptions? options = null)
        => ToContainTextAsync((object)expected, options);

    public Task ToContainTextAsync(IEnumerable<string> expected, LocatorAssertionsToContainTextOptions? options = null)
        => ToContainTextArrayAsync(expected, options);

    public Task ToContainTextAsync(IEnumerable<Regex> expected, LocatorAssertionsToContainTextOptions? options = null)
        => ToContainTextArrayAsync(expected, options);

    private Task ToContainTextAsync(object expected, LocatorAssertionsToContainTextOptions? options)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpectedText = ExpectedText(expected, new() { MatchSubstring = true, NormalizeWhiteSpace = true, IgnoreCase = options?.IgnoreCase });
        return ToMatchTextAsync("ToContainTextAsync", "to.have.text", expected, frameExpectOptions, matchSubstring: true);
    }

    private Task ToContainTextArrayAsync<T>(IEnumerable<T> expected, LocatorAssertionsToContainTextOptions? options)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpectedText = ExpectedTextArray(expected, () => new() { MatchSubstring = true, NormalizeWhiteSpace = true, IgnoreCase = options?.IgnoreCase });
        return ToEqualAsync("ToContainTextAsync", "to.contain.text.array", expected, frameExpectOptions);
    }

    public Task ToHaveAttributeAsync(string name, string value, LocatorAssertionsToHaveAttributeOptions? options = null)
        => ToHaveAttributeAsync(name, (object)value, options);

    public Task ToHaveAttributeAsync(string name, Regex value, LocatorAssertionsToHaveAttributeOptions? options = null)
        => ToHaveAttributeAsync(name, (object)value, options);

    private Task ToHaveAttributeAsync(string name, object expected, LocatorAssertionsToHaveAttributeOptions? options)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpressionArg = name;
        frameExpectOptions.ExpectedText = ExpectedText(expected, new() { IgnoreCase = options?.IgnoreCase });
        return ToMatchTextAsync("ToHaveAttributeAsync", "to.have.attribute.value", expected, frameExpectOptions);
    }

    public Task ToHaveClassAsync(string expected, LocatorAssertionsToHaveClassOptions? options = null)
        => ToMatchTextAsync("ToHaveClassAsync", "to.have.class", expected, WithExpectedText(options, ExpectedText(expected)));

    public Task ToHaveClassAsync(Regex expected, LocatorAssertionsToHaveClassOptions? options = null)
        => ToMatchTextAsync("ToHaveClassAsync", "to.have.class", expected, WithExpectedText(options, ExpectedText(expected)));

    public Task ToHaveClassAsync(IEnumerable<string> expected, LocatorAssertionsToHaveClassOptions? options = null)
        => ToEqualAsync("ToHaveClassAsync", "to.have.class.array", expected, WithExpectedText(options, ExpectedTextArray(expected)));

    public Task ToHaveClassAsync(IEnumerable<Regex> expected, LocatorAssertionsToHaveClassOptions? options = null)
        => ToEqualAsync("ToHaveClassAsync", "to.have.class.array", expected, WithExpectedText(options, ExpectedTextArray(expected)));

    public Task ToContainClassAsync(string expected, LocatorAssertionsToContainClassOptions? options = null)
        => ToMatchTextAsync("ToContainClassAsync", "to.contain.class", expected, WithExpectedText(options, ExpectedText(expected)));

    public Task ToContainClassAsync(IEnumerable<string> expected, LocatorAssertionsToContainClassOptions? options = null)
        => ToEqualAsync("ToContainClassAsync", "to.contain.class.array", expected, WithExpectedText(options, ExpectedTextArray(expected)));

    public Task ToHaveCountAsync(int count, LocatorAssertionsToHaveCountOptions? options = null)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpectedNumber = count;
        return ToEqualAsync("ToHaveCountAsync", "to.have.count", count, frameExpectOptions);
    }

    public Task ToHaveCSSAsync(string name, string value, LocatorAssertionsToHaveCSSOptions? options = null)
        => ToHaveCSSAsync(name, (object)value, options);

    public Task ToHaveCSSAsync(string name, Regex value, LocatorAssertionsToHaveCSSOptions? options = null)
        => ToHaveCSSAsync(name, (object)value, options);

    private Task ToHaveCSSAsync(string name, object expected, LocatorAssertionsToHaveCSSOptions? options)
    {
        var frameExpectOptions = WithExpectedText(options, ExpectedText(expected));
        frameExpectOptions.ExpressionArg = name;
        return ToMatchTextAsync("ToHaveCSSAsync", "to.have.css", expected, frameExpectOptions);
    }

    public Task ToHaveIdAsync(string id, LocatorAssertionsToHaveIdOptions? options = null)
        => ToMatchTextAsync("ToHaveIdAsync", "to.have.id", id, WithExpectedText(options, ExpectedText(id)));

    public Task ToHaveIdAsync(Regex id, LocatorAssertionsToHaveIdOptions? options = null)
        => ToMatchTextAsync("ToHaveIdAsync", "to.have.id", id, WithExpectedText(options, ExpectedText(id)));

    public Task ToHaveJSPropertyAsync(string name, object value, LocatorAssertionsToHaveJSPropertyOptions? options = null)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpressionArg = name;
        frameExpectOptions.ExpectedValue = ScriptsHelper.SerializedArgument(value);
        return ToEqualAsync("ToHaveJSPropertyAsync", "to.have.property", value, frameExpectOptions);
    }

    public Task ToHaveTextAsync(string expected, LocatorAssertionsToHaveTextOptions? options = null)
        => ToHaveTextAsync((object)expected, options);

    public Task ToHaveTextAsync(Regex expected, LocatorAssertionsToHaveTextOptions? options = null)
        => ToHaveTextAsync((object)expected, options);

    public Task ToHaveTextAsync(IEnumerable<string> expected, LocatorAssertionsToHaveTextOptions? options = null)
        => ToHaveTextArrayAsync(expected, options);

    public Task ToHaveTextAsync(IEnumerable<Regex> expected, LocatorAssertionsToHaveTextOptions? options = null)
        => ToHaveTextArrayAsync(expected, options);

    private Task ToHaveTextAsync(object expected, LocatorAssertionsToHaveTextOptions? options)
        => ToMatchTextAsync("ToHaveTextAsync", "to.have.text", expected, WithExpectedText(options, ExpectedText(expected, new() { NormalizeWhiteSpace = true, IgnoreCase = options?.IgnoreCase })));

    private Task ToHaveTextArrayAsync<T>(IEnumerable<T> expected, LocatorAssertionsToHaveTextOptions? options)
        => ToEqualAsync("ToHaveTextAsync", "to.have.text.array", expected, WithExpectedText(options, ExpectedTextArray(expected, () => new() { NormalizeWhiteSpace = true, IgnoreCase = options?.IgnoreCase })));

    public Task ToHaveValueAsync(string value, LocatorAssertionsToHaveValueOptions? options = null)
        => ToMatchTextAsync("ToHaveValueAsync", "to.have.value", value, WithExpectedText(options, ExpectedText(value)));

    public Task ToHaveValueAsync(Regex value, LocatorAssertionsToHaveValueOptions? options = null)
        => ToMatchTextAsync("ToHaveValueAsync", "to.have.value", value, WithExpectedText(options, ExpectedText(value)));

    public Task ToHaveValuesAsync(IEnumerable<string> values, LocatorAssertionsToHaveValuesOptions? options = null)
        => ToEqualAsync("ToHaveValuesAsync", "to.have.values", values, WithExpectedText(options, ExpectedTextArray(values)));

    public Task ToHaveValuesAsync(IEnumerable<Regex> values, LocatorAssertionsToHaveValuesOptions? options = null)
        => ToEqualAsync("ToHaveValuesAsync", "to.have.values", values, WithExpectedText(options, ExpectedTextArray(values)));

    public Task ToHaveAccessibleDescriptionAsync(string expected, LocatorAssertionsToHaveAccessibleDescriptionOptions? options = null)
        => ToMatchTextAsync("ToHaveAccessibleDescriptionAsync", "to.have.accessible.description", expected, WithExpectedText(options, ExpectedText(expected, new() { IgnoreCase = options?.IgnoreCase, NormalizeWhiteSpace = true })));

    public Task ToHaveAccessibleDescriptionAsync(Regex expected, LocatorAssertionsToHaveAccessibleDescriptionOptions? options = null)
        => ToMatchTextAsync("ToHaveAccessibleDescriptionAsync", "to.have.accessible.description", expected, WithExpectedText(options, ExpectedText(expected, new() { IgnoreCase = options?.IgnoreCase, NormalizeWhiteSpace = true })));

    public Task ToHaveAccessibleErrorMessageAsync(string errorMessage, LocatorAssertionsToHaveAccessibleErrorMessageOptions? options = null)
        => ToMatchTextAsync("ToHaveAccessibleErrorMessageAsync", "to.have.accessible.error.message", errorMessage, WithExpectedText(options, ExpectedText(errorMessage, new() { IgnoreCase = options?.IgnoreCase })));

    public Task ToHaveAccessibleErrorMessageAsync(Regex errorMessage, LocatorAssertionsToHaveAccessibleErrorMessageOptions? options = null)
        => ToMatchTextAsync("ToHaveAccessibleErrorMessageAsync", "to.have.accessible.error.message", errorMessage, WithExpectedText(options, ExpectedText(errorMessage, new() { IgnoreCase = options?.IgnoreCase })));

    public Task ToHaveAccessibleNameAsync(string expected, LocatorAssertionsToHaveAccessibleNameOptions? options = null)
        => ToMatchTextAsync("ToHaveAccessibleNameAsync", "to.have.accessible.name", expected, WithExpectedText(options, ExpectedText(expected, new() { IgnoreCase = options?.IgnoreCase, NormalizeWhiteSpace = true })));

    public Task ToHaveAccessibleNameAsync(Regex expected, LocatorAssertionsToHaveAccessibleNameOptions? options = null)
        => ToMatchTextAsync("ToHaveAccessibleNameAsync", "to.have.accessible.name", expected, WithExpectedText(options, ExpectedText(expected, new() { IgnoreCase = options?.IgnoreCase, NormalizeWhiteSpace = true })));

    public Task ToHaveRoleAsync(AriaRole role, LocatorAssertionsToHaveRoleOptions? options = null)
    {
        var expected = role.ToString().ToLowerInvariant();
        return ToMatchTextAsync("ToHaveRoleAsync", "to.have.role", expected, WithExpectedText(options, ExpectedText(expected)));
    }

    public Task ToMatchAriaSnapshotAsync(string expected, LocatorAssertionsToMatchAriaSnapshotOptions? options = null)
        => ToMatchAriaSnapshotAsync(expected, ConvertToFrameExpectOptions(options));

    private FrameExpectOptions WithExpectedText(object? options, ExpectedTextValue[] expectedText)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpectedText = expectedText;
        return frameExpectOptions;
    }
}
