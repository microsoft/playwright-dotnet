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

using System.Text.RegularExpressions;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// FrameLocator represents a view to the <c>iframe</c> on the page. It captures the
/// logic sufficient to retrieve the <c>iframe</c> and locate elements in that iframe.
/// FrameLocator can be created with either <see cref="IPage.FrameLocator"/> or <see
/// cref="ILocator.FrameLocator"/> method.
/// </para>
/// <code>
/// var locator = page.FrameLocator("#my-frame").GetByText("Submit");<br/>
/// await locator.ClickAsync();
/// </code>
/// <para>**Strictness**</para>
/// <para>
/// Frame locators are strict. This means that all operations on frame locators will
/// throw if more than one element matches a given selector.
/// </para>
/// <code>
/// // Throws if there are several frames in DOM:<br/>
/// await page.FrameLocator(".result-frame").GetByRole(AriaRole.Button).ClickAsync();<br/>
/// <br/>
/// // Works because we explicitly tell locator to pick the first frame:<br/>
/// await page.FrameLocator(".result-frame").First.getByRole(AriaRole.Button).ClickAsync();
/// </code>
/// <para>**Converting Locator to FrameLocator**</para>
/// <para>
/// If you have a <see cref="ILocator"/> object pointing to an <c>iframe</c> it can
/// be converted to <see cref="IFrameLocator"/> using <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/:scope"><c>:scope</c></a>
/// CSS selector:
/// </para>
/// <code>var frameLocator = locator.FrameLocator(":scope");</code>
/// </summary>
public partial interface IFrameLocator
{
    /// <summary><para>Returns locator to the first matching frame.</para></summary>
    IFrameLocator First { get; }

    /// <summary>
    /// <para>
    /// When working with iframes, you can create a frame locator that will enter the iframe
    /// and allow selecting elements in that iframe.
    /// </para>
    /// </summary>
    /// <param name="selector">A selector to use when resolving DOM element.</param>
    IFrameLocator FrameLocator(string selector);

    /// <summary>
    /// <para>Allows locating elements by their alt text.</para>
    /// <para>**Usage**</para>
    /// <para>For example, this method will find the image by alt text "Playwright logo":</para>
    /// <code>await page.GetByAltText("Playwright logo").ClickAsync();</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(string text, FrameLocatorGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their alt text.</para>
    /// <para>**Usage**</para>
    /// <para>For example, this method will find the image by alt text "Playwright logo":</para>
    /// <code>await page.GetByAltText("Playwright logo").ClickAsync();</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(Regex text, FrameLocatorGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>Allows locating input elements by the text of the associated label.</para>
    /// <para>**Usage**</para>
    /// <para>
    /// For example, this method will find the input by label text "Password" in the following
    /// DOM:
    /// </para>
    /// <code>await page.GetByLabel("Password").FillAsync("secret");</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(string text, FrameLocatorGetByLabelOptions? options = default);

    /// <summary>
    /// <para>Allows locating input elements by the text of the associated label.</para>
    /// <para>**Usage**</para>
    /// <para>
    /// For example, this method will find the input by label text "Password" in the following
    /// DOM:
    /// </para>
    /// <code>await page.GetByLabel("Password").FillAsync("secret");</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(Regex text, FrameLocatorGetByLabelOptions? options = default);

    /// <summary>
    /// <para>Allows locating input elements by the placeholder text.</para>
    /// <para>**Usage**</para>
    /// <para>For example, consider the following DOM structure.</para>
    /// <para>You can fill the input after locating it by the placeholder text:</para>
    /// <code>
    /// await page<br/>
    ///     .GetByPlaceholder("name@example.com")<br/>
    ///     .FillAsync("playwright@microsoft.com");
    /// </code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByPlaceholder(string text, FrameLocatorGetByPlaceholderOptions? options = default);

    /// <summary>
    /// <para>Allows locating input elements by the placeholder text.</para>
    /// <para>**Usage**</para>
    /// <para>For example, consider the following DOM structure.</para>
    /// <para>You can fill the input after locating it by the placeholder text:</para>
    /// <code>
    /// await page<br/>
    ///     .GetByPlaceholder("name@example.com")<br/>
    ///     .FillAsync("playwright@microsoft.com");
    /// </code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByPlaceholder(Regex text, FrameLocatorGetByPlaceholderOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their <a href="https://www.w3.org/TR/wai-aria-1.2/#roles">ARIA
    /// role</a>, <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-attributes">ARIA attributes</a>
    /// and <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible name</a>.
    /// </para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can locate each element by it's implicit role:</para>
    /// <code>
    /// await Expect(page<br/>
    ///     .GetByRole(AriaRole.Heading, new() { Name = "Sign up" }))<br/>
    ///     .ToBeVisibleAsync();<br/>
    /// <br/>
    /// await page<br/>
    ///     .GetByRole(AriaRole.Checkbox, new() { Name = "Subscribe" })<br/>
    ///     .CheckAsync();<br/>
    /// <br/>
    /// await page<br/>
    ///     .GetByRole(AriaRole.Button, new() {<br/>
    ///         NameRegex = new Regex("submit", RegexOptions.IgnoreCase)<br/>
    ///     })<br/>
    ///     .ClickAsync();
    /// </code>
    /// <para>**Details**</para>
    /// <para>
    /// Role selector **does not replace** accessibility audits and conformance tests, but
    /// rather gives early feedback about the ARIA guidelines.
    /// </para>
    /// <para>
    /// Many html elements have an implicitly <a href="https://w3c.github.io/html-aam/#html-element-role-mappings">defined
    /// role</a> that is recognized by the role selector. You can find all the <a href="https://www.w3.org/TR/wai-aria-1.2/#role_definitions">supported
    /// roles here</a>. ARIA guidelines **do not recommend** duplicating implicit roles
    /// and attributes by setting <c>role</c> and/or <c>aria-*</c> attributes to default
    /// values.
    /// </para>
    /// </summary>
    /// <param name="role">Required aria role.</param>
    /// <param name="options">Call options</param>
    ILocator GetByRole(AriaRole role, FrameLocatorGetByRoleOptions? options = default);

    /// <summary>
    /// <para>Locate element by the test id.</para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can locate the element by it's test id:</para>
    /// <code>await page.GetByTestId("directions").ClickAsync();</code>
    /// <para>**Details**</para>
    /// <para>
    /// By default, the <c>data-testid</c> attribute is used as a test id. Use <see cref="ISelectors.SetTestIdAttribute"/>
    /// to configure a different test id attribute if necessary.
    /// </para>
    /// </summary>
    /// <param name="testId">Id to locate the element by.</param>
    ILocator GetByTestId(string testId);

    /// <summary>
    /// <para>Locate element by the test id.</para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can locate the element by it's test id:</para>
    /// <code>await page.GetByTestId("directions").ClickAsync();</code>
    /// <para>**Details**</para>
    /// <para>
    /// By default, the <c>data-testid</c> attribute is used as a test id. Use <see cref="ISelectors.SetTestIdAttribute"/>
    /// to configure a different test id attribute if necessary.
    /// </para>
    /// </summary>
    /// <param name="testId">Id to locate the element by.</param>
    ILocator GetByTestId(Regex testId);

    /// <summary>
    /// <para>Allows locating elements that contain given text.</para>
    /// <para>
    /// See also <see cref="ILocator.Filter"/> that allows to match by another criteria,
    /// like an accessible role, and then filter by the text content.
    /// </para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure:</para>
    /// <para>You can locate by text substring, exact string, or a regular expression:</para>
    /// <code>
    /// // Matches &lt;span&gt;<br/>
    /// page.GetByText("world")<br/>
    /// <br/>
    /// // Matches first &lt;div&gt;<br/>
    /// page.GetByText("Hello world")<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText("Hello", new() { Exact: true })<br/>
    /// <br/>
    /// // Matches both &lt;div&gt;s<br/>
    /// page.GetByText(new Regex("Hello"))<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText(new Regex("^hello$", RegexOptions.IgnoreCase))
    /// </code>
    /// <para>**Details**</para>
    /// <para>
    /// Matching by text always normalizes whitespace, even with exact match. For example,
    /// it turns multiple spaces into one, turns line breaks into spaces and ignores leading
    /// and trailing whitespace.
    /// </para>
    /// <para>
    /// Input elements of the type <c>button</c> and <c>submit</c> are matched by their
    /// <c>value</c> instead of the text content. For example, locating by text <c>"Log
    /// in"</c> matches <c>&lt;input type=button value="Log in"&gt;</c>.
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByText(string text, FrameLocatorGetByTextOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements that contain given text.</para>
    /// <para>
    /// See also <see cref="ILocator.Filter"/> that allows to match by another criteria,
    /// like an accessible role, and then filter by the text content.
    /// </para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure:</para>
    /// <para>You can locate by text substring, exact string, or a regular expression:</para>
    /// <code>
    /// // Matches &lt;span&gt;<br/>
    /// page.GetByText("world")<br/>
    /// <br/>
    /// // Matches first &lt;div&gt;<br/>
    /// page.GetByText("Hello world")<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText("Hello", new() { Exact: true })<br/>
    /// <br/>
    /// // Matches both &lt;div&gt;s<br/>
    /// page.GetByText(new Regex("Hello"))<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText(new Regex("^hello$", RegexOptions.IgnoreCase))
    /// </code>
    /// <para>**Details**</para>
    /// <para>
    /// Matching by text always normalizes whitespace, even with exact match. For example,
    /// it turns multiple spaces into one, turns line breaks into spaces and ignores leading
    /// and trailing whitespace.
    /// </para>
    /// <para>
    /// Input elements of the type <c>button</c> and <c>submit</c> are matched by their
    /// <c>value</c> instead of the text content. For example, locating by text <c>"Log
    /// in"</c> matches <c>&lt;input type=button value="Log in"&gt;</c>.
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByText(Regex text, FrameLocatorGetByTextOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their title attribute.</para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can check the issues count after locating it by the title text:</para>
    /// <code>await Expect(page.GetByTitle("Issues count")).toHaveText("25 issues");</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(string text, FrameLocatorGetByTitleOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their title attribute.</para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can check the issues count after locating it by the title text:</para>
    /// <code>await Expect(page.GetByTitle("Issues count")).toHaveText("25 issues");</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(Regex text, FrameLocatorGetByTitleOptions? options = default);

    /// <summary><para>Returns locator to the last matching frame.</para></summary>
    IFrameLocator Last { get; }

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector in the locator's subtree.
    /// It also accepts filter options, similar to <see cref="ILocator.Filter"/> method.
    /// </para>
    /// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
    /// </summary>
    /// <param name="selectorOrLocator">A selector or locator to use when resolving DOM element.</param>
    /// <param name="options">Call options</param>
    ILocator Locator(string selectorOrLocator, FrameLocatorLocatorOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector in the locator's subtree.
    /// It also accepts filter options, similar to <see cref="ILocator.Filter"/> method.
    /// </para>
    /// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
    /// </summary>
    /// <param name="selectorOrLocator">A selector or locator to use when resolving DOM element.</param>
    /// <param name="options">Call options</param>
    ILocator Locator(ILocator selectorOrLocator, FrameLocatorLocatorOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns locator to the n-th matching frame. It's zero based, <c>nth(0)</c> selects
    /// the first frame.
    /// </para>
    /// </summary>
    /// <param name="index">
    /// </param>
    IFrameLocator Nth(int index);
}

#nullable disable
