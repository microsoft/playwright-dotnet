/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/expect-to-have-accessible.spec.ts.</summary>
public class ExpectToHaveAccessibleTests
{
    [UpstreamTest("EX-259 toHaveAccessibleName")]
    public async Task ToHaveAccessibleName(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=""button"" aria-label=""Hello""></div>
  ");
        await Expect(page.Locator("div")).ToHaveAccessibleNameAsync("Hello");
        await Expect(page.Locator("div")).Not.ToHaveAccessibleNameAsync("hello");
        await Expect(page.Locator("div")).ToHaveAccessibleNameAsync("hello", new() { IgnoreCase = true });
        await Expect(page.Locator("div")).ToHaveAccessibleNameAsync(new Regex(@"ell\w"));
        await Expect(page.Locator("div")).Not.ToHaveAccessibleNameAsync(new Regex("hello"));
        await Expect(page.Locator("div")).ToHaveAccessibleNameAsync(new Regex("hello"), new() { IgnoreCase = true });

        await page.SetContentAsync("<button>foo&nbsp;bar\nbaz</button>");
        await Expect(page.Locator("button")).ToHaveAccessibleNameAsync("foo bar baz");

        await page.SetContentAsync(@"
    <select>
      <option>&nbsp;HTML</option>
    </select>
  ");
        await Expect(page.Locator("option")).ToHaveAccessibleNameAsync("HTML");
    }

    [UpstreamTest("EX-260 toHaveAccessibleDescription")]
    public async Task ToHaveAccessibleDescription(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=""button"" aria-description=""Hello""></div>
  ");
        await Expect(page.Locator("div")).ToHaveAccessibleDescriptionAsync("Hello");
        await Expect(page.Locator("div")).Not.ToHaveAccessibleDescriptionAsync("hello");
        await Expect(page.Locator("div")).ToHaveAccessibleDescriptionAsync("hello", new() { IgnoreCase = true });
        await Expect(page.Locator("div")).ToHaveAccessibleDescriptionAsync(new Regex(@"ell\w"));
        await Expect(page.Locator("div")).Not.ToHaveAccessibleDescriptionAsync(new Regex("hello"));
        await Expect(page.Locator("div")).ToHaveAccessibleDescriptionAsync(new Regex("hello"), new() { IgnoreCase = true });

        // .NET: a verbatim string has no escapes: the \n of upstream is a string of its own.
        await page.SetContentAsync(@"
    <div role=""button"" aria-describedby=""desc""></div>
    <span id=""desc"">foo&nbsp;bar" + "\n" + @"baz</span>
  ");
        await Expect(page.Locator("div")).ToHaveAccessibleDescriptionAsync("foo bar baz");
    }

    [UpstreamTest("EX-261 toHaveAccessibleErrorMessage")]
    public async Task ToHaveAccessibleErrorMessage(IPage page)
    {
        await page.SetContentAsync(@"
    <form>
      <input role=""textbox"" aria-invalid=""true"" aria-errormessage=""error-message"" />
      <div id=""error-message"">Hello</div>
      <div id=""irrelevant-error"">This should not be considered.</div>
    </form>
  ");

        var locator = page.Locator("input[role=\"textbox\"]");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync("Hello");
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync("hello");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync("hello", new() { IgnoreCase = true });
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex(@"ell\w"));
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(new Regex("hello"));
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex("hello"), new() { IgnoreCase = true });
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync("This should not be considered.");
    }

    [UpstreamTest("EX-262 toHaveAccessibleErrorMessage should handle multiple aria-errormessage references")]
    public async Task ToHaveAccessibleErrorMessageShouldHandleMultipleAriaErrormessageReferences(IPage page)
    {
        await page.SetContentAsync(@"
    <form>
      <input role=""textbox"" aria-invalid=""true"" aria-errormessage=""error1 error2"" />
      <div id=""error1"">First error message.</div>
      <div id=""error2"">Second error message.</div>
      <div id=""irrelevant-error"">This should not be considered.</div>
    </form>
  ");

        var locator = page.Locator("input[role=\"textbox\"]");

        await Expect(locator).ToHaveAccessibleErrorMessageAsync("First error message. Second error message.");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex("first error message.", RegexOptions.IgnoreCase));
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex("second error message.", RegexOptions.IgnoreCase));
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(new Regex("This should not be considered.", RegexOptions.IgnoreCase));
    }

    public class ToHaveAccessibleErrorMessageShouldHandleAriaInvalidAttribute
    {
        private const string errorMessageText = "Error message";

        private static async Task<ILocator> SetupPage(IPage page, string ariaInvalidValue)
        {
            var ariaInvalidAttr = ariaInvalidValue == null ? "" : $"aria-invalid=\"{ariaInvalidValue}\"";
            await page.SetContentAsync($@"
        <form>
          <input id=""node"" role=""textbox"" {ariaInvalidAttr} aria-errormessage=""error-msg"" />
          <div id=""error-msg"">{errorMessageText}</div>
        </form>
      ");
            return page.Locator("#node");
        }

        public class EvaluatedInFalse
        {
            [UpstreamTest("EX-263 no aria-invalid attribute")]
            public async Task NoAriaInvalidAttribute(IPage page)
            {
                var locator = await SetupPage(page, null);
                await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }

            [UpstreamTest("EX-264 aria-invalid=\"false\"")]
            public async Task AriaInvalidFalse(IPage page)
            {
                var locator = await SetupPage(page, "false");
                await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }

            [UpstreamTest("EX-265 aria-invalid=\"\" (empty string)")]
            public async Task AriaInvalidEmptyString(IPage page)
            {
                var locator = await SetupPage(page, "");
                await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }
        }

        public class EvaluatedInTrue
        {
            [UpstreamTest("EX-266 aria-invalid=\"true\"")]
            public async Task AriaInvalidTrue(IPage page)
            {
                var locator = await SetupPage(page, "true");
                await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }

            [UpstreamTest("EX-267 aria-invalid=\"foo\" (unrecognized value)")]
            public async Task AriaInvalidFooUnrecognizedValue(IPage page)
            {
                var locator = await SetupPage(page, "foo");
                await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }
        }
    }

    private const string errorMessageText = "Error message";

    [UpstreamTest("EX-268 should show error message when validity is false and aria-invalid is true")]
    public async Task ShouldShowErrorMessageWhenValidityIsFalseAndAriaInvalidIsTrue(IPage page)
    {
        await page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""true"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = page.Locator("#node");
        await locator.FillAsync("101");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-269 should show error message when validity is true and aria-invalid is true")]
    public async Task ShouldShowErrorMessageWhenValidityIsTrueAndAriaInvalidIsTrue(IPage page)
    {
        await page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""true"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = page.Locator("#node");
        await locator.FillAsync("99");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-270 should show error message when validity is false and aria-invalid is false")]
    public async Task ShouldShowErrorMessageWhenValidityIsFalseAndAriaInvalidIsFalse(IPage page)
    {
        await page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""false"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = page.Locator("#node");
        await locator.FillAsync("101");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-271 should not show error message when validity is true and aria-invalid is false")]
    public async Task ShouldNotShowErrorMessageWhenValidityIsTrueAndAriaInvalidIsFalse(IPage page)
    {
        await page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""false"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = page.Locator("#node");
        await locator.FillAsync("99");
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-272 should show error message for all roles")]
    public async Task ShouldShowErrorMessageForAllRoles(IPage page)
    {
        await page.SetContentAsync(@"
    <label for=""file"">File input</label>
    <input id=""file"" aria-invalid=""true"" aria-errormessage=""file-error-id"" type=""file"" />
    <p id=""file-error-id"" role=""alert"">File is incorrect</p>
  ");
        var locator = page.GetByLabel("File input");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync("File is incorrect");
    }

    [UpstreamTest("EX-273 toHaveRole")]
    public async Task ToHaveRole(IPage page)
    {
        await page.SetContentAsync("<div role=\"button\">Button!</div>");
        await Expect(page.Locator("div")).ToHaveRoleAsync(AriaRole.Button);
        await Expect(page.Locator("div")).Not.ToHaveRoleAsync(AriaRole.Checkbox);
        // .NET: try { await expect(page.locator('div')).toHaveRole(/button|checkbox/); ... } catch (error) { ... } is left out:
        // ToHaveRoleAsync() takes an AriaRole, a regular expression does not compile.
    }
}
