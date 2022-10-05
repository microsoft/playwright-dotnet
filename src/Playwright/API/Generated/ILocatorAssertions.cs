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

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// The <see cref="ILocatorAssertions"/> class provides assertion methods that can be
/// used to make assertions about the <see cref="ILocator"/> state in the tests. A new
/// instance of <see cref="ILocatorAssertions"/> is created by calling <see cref="IPlaywrightAssertions.Expect"/>:
/// </para>
/// <code>
/// using System.Text.RegularExpressions;<br/>
/// using System.Threading.Tasks;<br/>
/// using Microsoft.Playwright.NUnit;<br/>
/// using NUnit.Framework;<br/>
/// <br/>
/// namespace PlaywrightTests;<br/>
/// <br/>
/// [TestFixture]<br/>
/// public class ExampleTests : PageTest<br/>
/// {<br/>
///     [Test]<br/>
///     public async Task StatusBecomesSubmitted()<br/>
///     {<br/>
///         // ..<br/>
///         await Page.GetByRole("button").ClickAsync();<br/>
///         await Expect(Page.Locator(".status")).ToHaveTextAsync("Submitted");<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
public partial interface ILocatorAssertions
{
    /// <summary>
    /// <para>
    /// Makes the assertion check for the opposite condition. For example, this code tests
    /// that the Locator doesn't contain text <c>"error"</c>:
    /// </para>
    /// <code>await Expect(locator).Not.ToContainTextAsync("error");</code>
    /// </summary>
    public ILocatorAssertions Not { get; }

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> points to a checked input.</para>
    /// <code>
    /// var locator = Page.GetByLabel("Subscribe to newsletter");<br/>
    /// await Expect(locator).ToBeCheckedAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeCheckedAsync(LocatorAssertionsToBeCheckedOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to a disabled element. Element is disabled
    /// if it has "disabled" attribute or is disabled via <a href="https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Attributes/aria-disabled">'aria-disabled'</a>.
    /// Note that only native control elements such as HTML <c>button</c>, <c>input</c>,
    /// <c>select</c>, <c>textarea</c>, <c>option</c>, <c>optgroup</c> can be disabled by
    /// setting "disabled" attribute. "disabled" attribute on other elements is ignored
    /// by the browser.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("button.submit");<br/>
    /// await Expect(locator).ToBeDisabledAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeDisabledAsync(LocatorAssertionsToBeDisabledOptions? options = default);

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> points to an editable element.</para>
    /// <code>
    /// var locator = Page.GetByRole("textbox");<br/>
    /// await Expect(locator).ToBeEditableAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeEditableAsync(LocatorAssertionsToBeEditableOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an empty editable element or to a DOM
    /// node that has no text.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("div.warning");<br/>
    /// await Expect(locator).ToBeEmptyAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeEmptyAsync(LocatorAssertionsToBeEmptyOptions? options = default);

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> points to an enabled element.</para>
    /// <code>
    /// var locator = Page.Locator("button.submit");<br/>
    /// await Expect(locator).toBeEnabledAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeEnabledAsync(LocatorAssertionsToBeEnabledOptions? options = default);

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> points to a focused DOM node.</para>
    /// <code>
    /// var locator = Page.GetByRole("textbox");<br/>
    /// await Expect(locator).ToBeFocusedAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeFocusedAsync(LocatorAssertionsToBeFocusedOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures that <see cref="ILocator"/> either does not resolve to any DOM node, or
    /// resolves to a <a href="https://playwright.dev/dotnet/docs/api/actionability#visible">non-visible</a>
    /// one.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".my-element");<br/>
    /// await Expect(locator).ToBeHiddenAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeHiddenAsync(LocatorAssertionsToBeHiddenOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures that <see cref="ILocator"/> points to an <a href="https://playwright.dev/dotnet/docs/api/actionability#attached">attached</a>
    /// and <a href="https://playwright.dev/dotnet/docs/api/actionability#visible">visible</a>
    /// DOM node.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".my-element");<br/>
    /// await Expect(locator).ToBeVisibleAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToBeVisibleAsync(LocatorAssertionsToBeVisibleOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element that contains the given
    /// text. You can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToContainTextAsync("substring");<br/>
    /// await Expect(locator).ToContainTextAsync(new Regex("\\d messages"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>Elements from a **subset** of this list contain text from the expected array, respectively.</description></item>
    /// <item><description>The matching subset of elements has the same order as the expected array.</description></item>
    /// <item><description>Each text value from the expected array is matched by some element from the list.</description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Contains the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 1", "Text 3", "Text 4"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 3", "Text 2"});<br/>
    /// <br/>
    /// // ✖ No item contains this text<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Some 33"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToContainTextAsync(new string[] {"Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToContainTextAsync(string expected, LocatorAssertionsToContainTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element that contains the given
    /// text. You can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToContainTextAsync("substring");<br/>
    /// await Expect(locator).ToContainTextAsync(new Regex("\\d messages"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>Elements from a **subset** of this list contain text from the expected array, respectively.</description></item>
    /// <item><description>The matching subset of elements has the same order as the expected array.</description></item>
    /// <item><description>Each text value from the expected array is matched by some element from the list.</description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Contains the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 1", "Text 3", "Text 4"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 3", "Text 2"});<br/>
    /// <br/>
    /// // ✖ No item contains this text<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Some 33"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToContainTextAsync(new string[] {"Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToContainTextAsync(Regex expected, LocatorAssertionsToContainTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element that contains the given
    /// text. You can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToContainTextAsync("substring");<br/>
    /// await Expect(locator).ToContainTextAsync(new Regex("\\d messages"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>Elements from a **subset** of this list contain text from the expected array, respectively.</description></item>
    /// <item><description>The matching subset of elements has the same order as the expected array.</description></item>
    /// <item><description>Each text value from the expected array is matched by some element from the list.</description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Contains the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 1", "Text 3", "Text 4"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 3", "Text 2"});<br/>
    /// <br/>
    /// // ✖ No item contains this text<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Some 33"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToContainTextAsync(new string[] {"Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToContainTextAsync(IEnumerable<string> expected, LocatorAssertionsToContainTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element that contains the given
    /// text. You can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToContainTextAsync("substring");<br/>
    /// await Expect(locator).ToContainTextAsync(new Regex("\\d messages"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>Elements from a **subset** of this list contain text from the expected array, respectively.</description></item>
    /// <item><description>The matching subset of elements has the same order as the expected array.</description></item>
    /// <item><description>Each text value from the expected array is matched by some element from the list.</description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Contains the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 1", "Text 3", "Text 4"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Text 3", "Text 2"});<br/>
    /// <br/>
    /// // ✖ No item contains this text<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToContainTextAsync(new string[] {"Some 33"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToContainTextAsync(new string[] {"Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToContainTextAsync(IEnumerable<Regex> expected, LocatorAssertionsToContainTextOptions? options = default);

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> points to an element with given attribute.</para>
    /// <code>
    /// var locator = Page.Locator("input");<br/>
    /// await Expect(locator).ToHaveAttributeAsync("type", "text");
    /// </code>
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="value">Expected attribute value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveAttributeAsync(string name, string value, LocatorAssertionsToHaveAttributeOptions? options = default);

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> points to an element with given attribute.</para>
    /// <code>
    /// var locator = Page.Locator("input");<br/>
    /// await Expect(locator).ToHaveAttributeAsync("type", "text");
    /// </code>
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="value">Expected attribute value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveAttributeAsync(string name, Regex value, LocatorAssertionsToHaveAttributeOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with given CSS classes.
    /// This needs to be a full match or using a relaxed regular expression.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("#component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new Regex("selected"));<br/>
    /// await Expect(locator).ToHaveClassAsync("selected row");
    /// </code>
    /// <para>
    /// Note that if array is passed as an expected value, entire lists of elements can
    /// be asserted:
    /// </para>
    /// <code>
    /// var locator = Page.Locator("list &gt; .component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new string[]{"component", "component selected", "component"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected class or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveClassAsync(string expected, LocatorAssertionsToHaveClassOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with given CSS classes.
    /// This needs to be a full match or using a relaxed regular expression.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("#component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new Regex("selected"));<br/>
    /// await Expect(locator).ToHaveClassAsync("selected row");
    /// </code>
    /// <para>
    /// Note that if array is passed as an expected value, entire lists of elements can
    /// be asserted:
    /// </para>
    /// <code>
    /// var locator = Page.Locator("list &gt; .component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new string[]{"component", "component selected", "component"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected class or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveClassAsync(Regex expected, LocatorAssertionsToHaveClassOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with given CSS classes.
    /// This needs to be a full match or using a relaxed regular expression.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("#component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new Regex("selected"));<br/>
    /// await Expect(locator).ToHaveClassAsync("selected row");
    /// </code>
    /// <para>
    /// Note that if array is passed as an expected value, entire lists of elements can
    /// be asserted:
    /// </para>
    /// <code>
    /// var locator = Page.Locator("list &gt; .component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new string[]{"component", "component selected", "component"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected class or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveClassAsync(IEnumerable<string> expected, LocatorAssertionsToHaveClassOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with given CSS classes.
    /// This needs to be a full match or using a relaxed regular expression.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("#component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new Regex("selected"));<br/>
    /// await Expect(locator).ToHaveClassAsync("selected row");
    /// </code>
    /// <para>
    /// Note that if array is passed as an expected value, entire lists of elements can
    /// be asserted:
    /// </para>
    /// <code>
    /// var locator = Page.Locator("list &gt; .component");<br/>
    /// await Expect(locator).ToHaveClassAsync(new string[]{"component", "component selected", "component"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected class or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveClassAsync(IEnumerable<Regex> expected, LocatorAssertionsToHaveClassOptions? options = default);

    /// <summary>
    /// <para>Ensures the <see cref="ILocator"/> resolves to an exact number of DOM nodes.</para>
    /// <code>
    /// var locator = Page.Locator("list &gt; .component");<br/>
    /// await Expect(locator).ToHaveCountAsync(3);
    /// </code>
    /// </summary>
    /// <param name="count">Expected count.</param>
    /// <param name="options">Call options</param>
    Task ToHaveCountAsync(int count, LocatorAssertionsToHaveCountOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> resolves to an element with the given computed
    /// CSS style.
    /// </para>
    /// <code>
    /// var locator = Page.GetByRole("button");<br/>
    /// await Expect(locator).ToHaveCSSAsync("display", "flex");
    /// </code>
    /// </summary>
    /// <param name="name">CSS property name.</param>
    /// <param name="value">CSS property value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveCSSAsync(string name, string value, LocatorAssertionsToHaveCSSOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> resolves to an element with the given computed
    /// CSS style.
    /// </para>
    /// <code>
    /// var locator = Page.GetByRole("button");<br/>
    /// await Expect(locator).ToHaveCSSAsync("display", "flex");
    /// </code>
    /// </summary>
    /// <param name="name">CSS property name.</param>
    /// <param name="value">CSS property value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveCSSAsync(string name, Regex value, LocatorAssertionsToHaveCSSOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given DOM Node
    /// ID.
    /// </para>
    /// <code>
    /// var locator = Page.GetByRole("textbox");<br/>
    /// await Expect(locator).ToHaveIdAsync("lastname");
    /// </code>
    /// </summary>
    /// <param name="id">Element id.</param>
    /// <param name="options">Call options</param>
    Task ToHaveIdAsync(string id, LocatorAssertionsToHaveIdOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given DOM Node
    /// ID.
    /// </para>
    /// <code>
    /// var locator = Page.GetByRole("textbox");<br/>
    /// await Expect(locator).ToHaveIdAsync("lastname");
    /// </code>
    /// </summary>
    /// <param name="id">Element id.</param>
    /// <param name="options">Call options</param>
    Task ToHaveIdAsync(Regex id, LocatorAssertionsToHaveIdOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with given JavaScript property.
    /// Note that this property can be of a primitive type as well as a plain serializable
    /// JavaScript object.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".component");<br/>
    /// await Expect(locator).ToHaveJSPropertyAsync("loaded", true);
    /// </code>
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveJSPropertyAsync(string name, object value, LocatorAssertionsToHaveJSPropertyOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
    /// can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, Test User"));<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, .*"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>The number of elements equals the number of expected values in the array.</description></item>
    /// <item><description>
    /// Elements from the list have text matching expected array values, one by one, in
    /// order.
    /// </description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Has the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 3", "Text 2", "Text 1"});<br/>
    /// <br/>
    /// // ✖ Last item does not match<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveTextAsync(string expected, LocatorAssertionsToHaveTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
    /// can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, Test User"));<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, .*"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>The number of elements equals the number of expected values in the array.</description></item>
    /// <item><description>
    /// Elements from the list have text matching expected array values, one by one, in
    /// order.
    /// </description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Has the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 3", "Text 2", "Text 1"});<br/>
    /// <br/>
    /// // ✖ Last item does not match<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveTextAsync(Regex expected, LocatorAssertionsToHaveTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
    /// can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, Test User"));<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, .*"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>The number of elements equals the number of expected values in the array.</description></item>
    /// <item><description>
    /// Elements from the list have text matching expected array values, one by one, in
    /// order.
    /// </description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Has the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 3", "Text 2", "Text 1"});<br/>
    /// <br/>
    /// // ✖ Last item does not match<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveTextAsync(IEnumerable<string> expected, LocatorAssertionsToHaveTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
    /// can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator(".title");<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, Test User"));<br/>
    /// await Expect(locator).ToHaveTextAsync(new Regex("Welcome, .*"));
    /// </code>
    /// <para>If you pass an array as an expected value, the expectations are:</para>
    /// <list type="ordinal">
    /// <item><description>Locator resolves to a list of elements.</description></item>
    /// <item><description>The number of elements equals the number of expected values in the array.</description></item>
    /// <item><description>
    /// Elements from the list have text matching expected array values, one by one, in
    /// order.
    /// </description></item>
    /// </list>
    /// <para>For example, consider the following list:</para>
    /// <para>Let's see how we can use the assertion:</para>
    /// <code>
    /// // ✓ Has the right items in the right order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});<br/>
    /// <br/>
    /// // ✖ Wrong order<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 3", "Text 2", "Text 1"});<br/>
    /// <br/>
    /// // ✖ Last item does not match<br/>
    /// await Expect(Page.Locator("ul &gt; li")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text"});<br/>
    /// <br/>
    /// // ✖ Locator points to the outer list element, not to the list items<br/>
    /// await Expect(Page.Locator("ul")).ToHaveTextAsync(new string[] {"Text 1", "Text 2", "Text 3"});
    /// </code>
    /// </summary>
    /// <param name="expected">Expected substring or RegExp or a list of those.</param>
    /// <param name="options">Call options</param>
    Task ToHaveTextAsync(IEnumerable<Regex> expected, LocatorAssertionsToHaveTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given input value.
    /// You can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("input[type=number]");<br/>
    /// await Expect(locator).ToHaveValueAsync(new Regex("[0-9]"));
    /// </code>
    /// </summary>
    /// <param name="value">Expected value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveValueAsync(string value, LocatorAssertionsToHaveValueOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to an element with the given input value.
    /// You can use regular expressions for the value as well.
    /// </para>
    /// <code>
    /// var locator = Page.Locator("input[type=number]");<br/>
    /// await Expect(locator).ToHaveValueAsync(new Regex("[0-9]"));
    /// </code>
    /// </summary>
    /// <param name="value">Expected value.</param>
    /// <param name="options">Call options</param>
    Task ToHaveValueAsync(Regex value, LocatorAssertionsToHaveValueOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to multi-select/combobox (i.e. a <c>select</c>
    /// with the <c>multiple</c> attribute) and the specified values are selected.
    /// </para>
    /// <para>For example, given the following element:</para>
    /// <code>
    /// var locator = Page.Locator("id=favorite-colors");<br/>
    /// await locator.SelectOptionAsync(new string[] { "R", "G" })<br/>
    /// await Expect(locator).ToHaveValuesAsync(new Regex[] { new Regex("R"), new Regex("G") });
    /// </code>
    /// </summary>
    /// <param name="values">Expected options currently selected.</param>
    /// <param name="options">Call options</param>
    Task ToHaveValuesAsync(IEnumerable<string> values, LocatorAssertionsToHaveValuesOptions? options = default);

    /// <summary>
    /// <para>
    /// Ensures the <see cref="ILocator"/> points to multi-select/combobox (i.e. a <c>select</c>
    /// with the <c>multiple</c> attribute) and the specified values are selected.
    /// </para>
    /// <para>For example, given the following element:</para>
    /// <code>
    /// var locator = Page.Locator("id=favorite-colors");<br/>
    /// await locator.SelectOptionAsync(new string[] { "R", "G" })<br/>
    /// await Expect(locator).ToHaveValuesAsync(new Regex[] { new Regex("R"), new Regex("G") });
    /// </code>
    /// </summary>
    /// <param name="values">Expected options currently selected.</param>
    /// <param name="options">Call options</param>
    Task ToHaveValuesAsync(IEnumerable<Regex> values, LocatorAssertionsToHaveValuesOptions? options = default);
}

#nullable disable
