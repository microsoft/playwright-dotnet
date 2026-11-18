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
public class ExpectToHaveAccessibleTests : UpstreamPageTest
{
    [UpstreamTest("EX-259 toHaveAccessibleName")]
    public async Task ToHaveAccessibleName()
    {
        await Page.SetContentAsync(@"
    <div role=""button"" aria-label=""Hello""></div>
  ");
        await Expect(Page.Locator("div")).ToHaveAccessibleNameAsync("Hello");
        await Expect(Page.Locator("div")).Not.ToHaveAccessibleNameAsync("hello");
        await Expect(Page.Locator("div")).ToHaveAccessibleNameAsync("hello", new() { IgnoreCase = true });
        await Expect(Page.Locator("div")).ToHaveAccessibleNameAsync(new Regex(@"ell\w"));
        await Expect(Page.Locator("div")).Not.ToHaveAccessibleNameAsync(new Regex("hello"));
        await Expect(Page.Locator("div")).ToHaveAccessibleNameAsync(new Regex("hello"), new() { IgnoreCase = true });

        await Page.SetContentAsync("<button>foo&nbsp;bar\nbaz</button>");
        await Expect(Page.Locator("button")).ToHaveAccessibleNameAsync("foo bar baz");

        await Page.SetContentAsync(@"
    <select>
      <option>&nbsp;HTML</option>
    </select>
  ");
        await Expect(Page.Locator("option")).ToHaveAccessibleNameAsync("HTML");
    }

    [UpstreamTest("EX-260 toHaveAccessibleDescription")]
    public async Task ToHaveAccessibleDescription()
    {
        await Page.SetContentAsync(@"
    <div role=""button"" aria-description=""Hello""></div>
  ");
        await Expect(Page.Locator("div")).ToHaveAccessibleDescriptionAsync("Hello");
        await Expect(Page.Locator("div")).Not.ToHaveAccessibleDescriptionAsync("hello");
        await Expect(Page.Locator("div")).ToHaveAccessibleDescriptionAsync("hello", new() { IgnoreCase = true });
        await Expect(Page.Locator("div")).ToHaveAccessibleDescriptionAsync(new Regex(@"ell\w"));
        await Expect(Page.Locator("div")).Not.ToHaveAccessibleDescriptionAsync(new Regex("hello"));
        await Expect(Page.Locator("div")).ToHaveAccessibleDescriptionAsync(new Regex("hello"), new() { IgnoreCase = true });

        // .NET: a verbatim string has no escapes: the \n of upstream is a string of its own.
        await Page.SetContentAsync(@"
    <div role=""button"" aria-describedby=""desc""></div>
    <span id=""desc"">foo&nbsp;bar" + "\n" + @"baz</span>
  ");
        await Expect(Page.Locator("div")).ToHaveAccessibleDescriptionAsync("foo bar baz");
    }

    [UpstreamTest("EX-261 toHaveAccessibleErrorMessage")]
    public async Task ToHaveAccessibleErrorMessage()
    {
        await Page.SetContentAsync(@"
    <form>
      <input role=""textbox"" aria-invalid=""true"" aria-errormessage=""error-message"" />
      <div id=""error-message"">Hello</div>
      <div id=""irrelevant-error"">This should not be considered.</div>
    </form>
  ");

        var locator = Page.Locator("input[role=\"textbox\"]");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync("Hello");
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync("hello");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync("hello", new() { IgnoreCase = true });
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex(@"ell\w"));
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(new Regex("hello"));
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex("hello"), new() { IgnoreCase = true });
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync("This should not be considered.");
    }

    [UpstreamTest("EX-262 toHaveAccessibleErrorMessage should handle multiple aria-errormessage references")]
    public async Task ToHaveAccessibleErrorMessageShouldHandleMultipleAriaErrormessageReferences()
    {
        await Page.SetContentAsync(@"
    <form>
      <input role=""textbox"" aria-invalid=""true"" aria-errormessage=""error1 error2"" />
      <div id=""error1"">First error message.</div>
      <div id=""error2"">Second error message.</div>
      <div id=""irrelevant-error"">This should not be considered.</div>
    </form>
  ");

        var locator = Page.Locator("input[role=\"textbox\"]");

        await Expect(locator).ToHaveAccessibleErrorMessageAsync("First error message. Second error message.");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex("first error message.", RegexOptions.IgnoreCase));
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(new Regex("second error message.", RegexOptions.IgnoreCase));
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(new Regex("This should not be considered.", RegexOptions.IgnoreCase));
    }

    public class ToHaveAccessibleErrorMessageShouldHandleAriaInvalidAttribute : UpstreamPageTest
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

        public class EvaluatedInFalse : UpstreamPageTest
        {
            [UpstreamTest("EX-263 no aria-invalid attribute")]
            public async Task NoAriaInvalidAttribute()
            {
                var locator = await SetupPage(Page, null);
                await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }

            [UpstreamTest("EX-264 aria-invalid=\"false\"")]
            public async Task AriaInvalidFalse()
            {
                var locator = await SetupPage(Page, "false");
                await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }

            [UpstreamTest("EX-265 aria-invalid=\"\" (empty string)")]
            public async Task AriaInvalidEmptyString()
            {
                var locator = await SetupPage(Page, "");
                await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }
        }

        public class EvaluatedInTrue : UpstreamPageTest
        {
            [UpstreamTest("EX-266 aria-invalid=\"true\"")]
            public async Task AriaInvalidTrue()
            {
                var locator = await SetupPage(Page, "true");
                await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }

            [UpstreamTest("EX-267 aria-invalid=\"foo\" (unrecognized value)")]
            public async Task AriaInvalidFooUnrecognizedValue()
            {
                var locator = await SetupPage(Page, "foo");
                await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
            }
        }
    }

    private const string errorMessageText = "Error message";

    [UpstreamTest("EX-268 should show error message when validity is false and aria-invalid is true")]
    public async Task ShouldShowErrorMessageWhenValidityIsFalseAndAriaInvalidIsTrue()
    {
        await Page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""true"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = Page.Locator("#node");
        await locator.FillAsync("101");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-269 should show error message when validity is true and aria-invalid is true")]
    public async Task ShouldShowErrorMessageWhenValidityIsTrueAndAriaInvalidIsTrue()
    {
        await Page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""true"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = Page.Locator("#node");
        await locator.FillAsync("99");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-270 should show error message when validity is false and aria-invalid is false")]
    public async Task ShouldShowErrorMessageWhenValidityIsFalseAndAriaInvalidIsFalse()
    {
        await Page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""false"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = Page.Locator("#node");
        await locator.FillAsync("101");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-271 should not show error message when validity is true and aria-invalid is false")]
    public async Task ShouldNotShowErrorMessageWhenValidityIsTrueAndAriaInvalidIsFalse()
    {
        await Page.SetContentAsync($@"
    <form>
      <input id=""node"" role=""textbox"" type=""number"" min=""1"" max=""100"" aria-invalid=""false"" aria-errormessage=""error-msg"" />
      <div id=""error-msg"">{errorMessageText}</div>
    </form>
  ");
        var locator = Page.Locator("#node");
        await locator.FillAsync("99");
        await Expect(locator).Not.ToHaveAccessibleErrorMessageAsync(errorMessageText);
    }

    [UpstreamTest("EX-272 should show error message for all roles")]
    public async Task ShouldShowErrorMessageForAllRoles()
    {
        await Page.SetContentAsync(@"
    <label for=""file"">File input</label>
    <input id=""file"" aria-invalid=""true"" aria-errormessage=""file-error-id"" type=""file"" />
    <p id=""file-error-id"" role=""alert"">File is incorrect</p>
  ");
        var locator = Page.GetByLabel("File input");
        await Expect(locator).ToHaveAccessibleErrorMessageAsync("File is incorrect");
    }

    [UpstreamTest("EX-273 toHaveRole")]
    public async Task ToHaveRole()
    {
        await Page.SetContentAsync("<div role=\"button\">Button!</div>");
        await Expect(Page.Locator("div")).ToHaveRoleAsync(AriaRole.Button);
        await Expect(Page.Locator("div")).Not.ToHaveRoleAsync(AriaRole.Checkbox);
        // .NET: try { await expect(page.locator('div')).toHaveRole(/button|checkbox/); ... } catch (error) { ... } is left out:
        // ToHaveRoleAsync() takes an AriaRole, a regular expression does not compile.
    }
}
