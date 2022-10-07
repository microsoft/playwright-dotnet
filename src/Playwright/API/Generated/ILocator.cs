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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Locators are the central piece of Playwright's auto-waiting and retry-ability. In
/// a nutshell, locators represent a way to find element(s) on the page at any moment.
/// Locator can be created with the <see cref="IPage.Locator"/> method.
/// </para>
/// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
/// </summary>
public partial interface ILocator
{
    /// <summary><para>Returns an array of <c>node.innerText</c> values for all matching nodes.</para></summary>
    Task<IReadOnlyList<string>> AllInnerTextsAsync();

    /// <summary><para>Returns an array of <c>node.textContent</c> values for all matching nodes.</para></summary>
    Task<IReadOnlyList<string>> AllTextContentsAsync();

    /// <summary>
    /// <para>
    /// This method returns the bounding box of the element, or <c>null</c> if the element
    /// is not visible. The bounding box is calculated relative to the main frame viewport
    /// - which is usually the same as the browser window.
    /// </para>
    /// <para>
    /// Scrolling affects the returned bounding box, similarly to <a href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect">Element.getBoundingClientRect</a>.
    /// That means <c>x</c> and/or <c>y</c> may be negative.
    /// </para>
    /// <para>
    /// Elements from child frames return the bounding box relative to the main frame, unlike
    /// the <a href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect">Element.getBoundingClientRect</a>.
    /// </para>
    /// <para>
    /// Assuming the page is static, it is safe to use bounding box coordinates to perform
    /// input. For example, the following snippet should click the center of the element.
    /// </para>
    /// <code>
    /// var box = await element.BoundingBoxAsync();<br/>
    /// await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<LocatorBoundingBoxResult?> BoundingBoxAsync(LocatorBoundingBoxOptions? options = default);

    /// <summary>
    /// <para>This method checks the element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Ensure that element is a checkbox or a radio input. If not, this method throws.
    /// If the element is already checked, this method returns immediately.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the element, unless <paramref name="force"/> option is set.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// <item><description>Ensure that the element is now checked. If not, this method throws.</description></item>
    /// </list>
    /// <para>
    /// If the element is detached from the DOM at any moment during the action, this method
    /// throws.
    /// </para>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task CheckAsync(LocatorCheckOptions? options = default);

    /// <summary>
    /// <para>This method clicks the element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the element, unless <paramref name="force"/> option is set.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Mouse"/> to click in the center of the element, or the specified
    /// <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// </list>
    /// <para>
    /// If the element is detached from the DOM at any moment during the action, this method
    /// throws.
    /// </para>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ClickAsync(LocatorClickOptions? options = default);

    /// <summary><para>Returns the number of elements matching given selector.</para></summary>
    Task<int> CountAsync();

    /// <summary>
    /// <para>This method double clicks the element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the element, unless <paramref name="force"/> option is set.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Mouse"/> to double click in the center of the element, or the
    /// specified <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set. Note that if the first click of the <c>dblclick()</c> triggers a
    /// navigation event, this method will throw.
    /// </description></item>
    /// </list>
    /// <para>
    /// If the element is detached from the DOM at any moment during the action, this method
    /// throws.
    /// </para>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>element.dblclick()</c> dispatches two <c>click</c> events and a single <c>dblclick</c>
    /// event.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task DblClickAsync(LocatorDblClickOptions? options = default);

    /// <summary>
    /// <para>
    /// The snippet below dispatches the <c>click</c> event on the element. Regardless of
    /// the visibility state of the element, <c>click</c> is dispatched. This is equivalent
    /// to calling <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click">element.click()</a>.
    /// </para>
    /// <code>await element.DispatchEventAsync("click");</code>
    /// <para>
    /// Under the hood, it creates an instance of an event based on the given <paramref
    /// name="type"/>, initializes it with <paramref name="eventInit"/> properties and dispatches
    /// it on the element. Events are <c>composed</c>, <c>cancelable</c> and bubble by default.
    /// </para>
    /// <para>
    /// Since <paramref name="eventInit"/> is event-specific, please refer to the events
    /// documentation for the lists of initial properties:
    /// </para>
    /// <list type="bullet">
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/DragEvent/DragEvent">DragEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent/FocusEvent">FocusEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/KeyboardEvent">KeyboardEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/MouseEvent/MouseEvent">MouseEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent/PointerEvent">PointerEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/TouchEvent/TouchEvent">TouchEvent</a></description></item>
    /// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/Event/Event">Event</a></description></item>
    /// </list>
    /// <para>
    /// You can also specify <c>JSHandle</c> as the property value if you want live objects
    /// to be passed into the event:
    /// </para>
    /// <code>
    /// var dataTransfer = await page.EvaluateHandleAsync("() =&gt; new DataTransfer()");<br/>
    /// await element.DispatchEventAsync("dragstart", new Dictionary&lt;string, object&gt;<br/>
    /// {<br/>
    ///     { "dataTransfer", dataTransfer }<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="type">DOM event type: <c>"click"</c>, <c>"dragstart"</c>, etc.</param>
    /// <param name="eventInit">Optional event-specific initialization properties.</param>
    /// <param name="options">Call options</param>
    Task DispatchEventAsync(string type, object? eventInit = default, LocatorDispatchEventOptions? options = default);

    /// <summary>
    /// <para>
    /// This method drags the locator to another target locator or target position. It will
    /// first move to the source element, perform a <c>mousedown</c>, then move to the target
    /// element or position and perform a <c>mouseup</c>.
    /// </para>
    /// <code>
    /// var source = Page.Locator("#source");<br/>
    /// var target = Page.Locator("#target");<br/>
    /// <br/>
    /// await source.DragToAsync(target);<br/>
    /// // or specify exact positions relative to the top-left corners of the elements:<br/>
    /// await source.DragToAsync(target, new()<br/>
    /// {<br/>
    ///     SourcePosition = new() { X = 34, Y = 7 },<br/>
    ///     TargetPosition = new() { X = 10, Y = 20 },<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="target">Locator of the element to drag to.</param>
    /// <param name="options">Call options</param>
    Task DragToAsync(ILocator target, LocatorDragToOptions? options = default);

    /// <summary>
    /// <para>
    /// Resolves given locator to the first matching DOM element. If no elements matching
    /// the query are visible, waits for them up to a given timeout. If multiple elements
    /// match the selector, throws.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IElementHandle> ElementHandleAsync(LocatorElementHandleOptions? options = default);

    /// <summary><para>Resolves given locator to all matching DOM elements.</para></summary>
    Task<IReadOnlyList<IElementHandle>> ElementHandlesAsync();

    /// <summary>
    /// <para>Returns the return value of <paramref name="expression"/>.</para>
    /// <para>This method passes this handle as the first argument to <paramref name="expression"/>.</para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, then <c>handle.evaluate</c>
    /// would wait for the promise to resolve and return its value.
    /// </para>
    /// <para>Examples:</para>
    /// <code>
    /// var tweets = page.Locator(".tweet .retweets");<br/>
    /// Assert.AreEqual("10 retweets", await tweets.EvaluateAsync("node =&gt; node.innerText"));
    /// </code>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    /// <param name="options">Call options</param>
    Task<T> EvaluateAsync<T>(string expression, object? arg = default, LocatorEvaluateOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds all elements matching the specified locator and passes an array
    /// of matched elements as a first argument to <paramref name="expression"/>. Returns
    /// the result of <paramref name="expression"/> invocation.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, then <see cref="ILocator.EvaluateAllAsync"/>
    /// would wait for the promise to resolve and return its value.
    /// </para>
    /// <para>Examples:</para>
    /// <code>
    /// var elements = page.Locator("div");<br/>
    /// var divsCount = await elements.EvaluateAll&lt;bool&gt;("(divs, min) =&gt; divs.length &gt;= min", 10);
    /// </code>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvaluateAllAsync<T>(string expression, object? arg = default);

    /// <summary>
    /// <para>Returns the return value of <paramref name="expression"/> as a <see cref="IJSHandle"/>.</para>
    /// <para>This method passes this handle as the first argument to <paramref name="expression"/>.</para>
    /// <para>
    /// The only difference between <see cref="ILocator.EvaluateAsync"/> and <see cref="ILocator.EvaluateHandleAsync"/>
    /// is that <see cref="ILocator.EvaluateHandleAsync"/> returns <see cref="IJSHandle"/>.
    /// </para>
    /// <para>
    /// If the function passed to the <see cref="ILocator.EvaluateHandleAsync"/> returns
    /// a <see cref="Task"/>, then <see cref="ILocator.EvaluateHandleAsync"/> would wait
    /// for the promise to resolve and return its value.
    /// </para>
    /// <para>See <see cref="IPage.EvaluateHandleAsync"/> for more details.</para>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    /// <param name="options">Call options</param>
    Task<IJSHandle> EvaluateHandleAsync(string expression, object? arg = default, LocatorEvaluateHandleOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, focuses the element, fills it and triggers an <c>input</c> event after filling.
    /// Note that you can pass an empty string to clear the input field.
    /// </para>
    /// <para>
    /// If the target element is not an <c>&lt;input&gt;</c>, <c>&lt;textarea&gt;</c> or
    /// <c>[contenteditable]</c> element, this method throws an error. However, if the element
    /// is inside the <c>&lt;label&gt;</c> element that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be filled instead.
    /// </para>
    /// <para>To send fine-grained keyboard events, use <see cref="ILocator.TypeAsync"/>.</para>
    /// </summary>
    /// <param name="value">
    /// Value to set for the <c>&lt;input&gt;</c>, <c>&lt;textarea&gt;</c> or <c>[contenteditable]</c>
    /// element.
    /// </param>
    /// <param name="options">Call options</param>
    Task FillAsync(string value, LocatorFillOptions? options = default);

    /// <summary>
    /// <para>
    /// This method narrows existing locator according to the options, for example filters
    /// by text. It can be chained to filter multiple times.
    /// </para>
    /// <code>
    /// var rowLocator = page.Locator("tr");<br/>
    /// // ...<br/>
    /// await rowLocator<br/>
    ///     .Filter(new LocatorFilterOptions { HasText = "text in column 1" })<br/>
    ///     .Filter(new LocatorFilterOptions {<br/>
    ///         Has = page.GetByRole("button", new() { Name = "column 2 button" } )<br/>
    ///     })<br/>
    ///     .ScreenshotAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    ILocator Filter(LocatorFilterOptions? options = default);

    /// <summary><para>Returns locator to the first matching element.</para></summary>
    ILocator First { get; }

    /// <summary>
    /// <para>
    /// Calls <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/focus">focus</a>
    /// on the element.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task FocusAsync(LocatorFocusOptions? options = default);

    /// <summary>
    /// <para>
    /// When working with iframes, you can create a frame locator that will enter the iframe
    /// and allow selecting elements in that iframe:
    /// </para>
    /// <code>
    /// var locator = page.FrameLocator("iframe").GetByText("Submit");<br/>
    /// await locator.ClickAsync();
    /// </code>
    /// </summary>
    /// <param name="selector">
    /// A selector to use when resolving DOM element. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    IFrameLocator FrameLocator(string selector);

    /// <summary><para>Returns element attribute value.</para></summary>
    /// <param name="name">Attribute name to get the value for.</param>
    /// <param name="options">Call options</param>
    Task<string?> GetAttributeAsync(string name, LocatorGetAttributeOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their alt text. For example, this method will find the
    /// image by alt text "Castle":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(string text, LocatorGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their alt text. For example, this method will find the
    /// image by alt text "Castle":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(Regex text, LocatorGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the text of the associated label. For example,
    /// this method will find the input by label text Password in the following DOM:
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(string text, LocatorGetByLabelOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the text of the associated label. For example,
    /// this method will find the input by label text Password in the following DOM:
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(Regex text, LocatorGetByLabelOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the placeholder text. For example, this method
    /// will find the input by placeholder "Country":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the placeholder text. For example, this method
    /// will find the input by placeholder "Country":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByPlaceholder(Regex text, LocatorGetByPlaceholderOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their <a href="https://www.w3.org/TR/wai-aria-1.2/#roles">ARIA
    /// role</a>, <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-attributes">ARIA attributes</a>
    /// and <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible name</a>.
    /// Note that role selector **does not replace** accessibility audits and conformance
    /// tests, but rather gives early feedback about the ARIA guidelines.
    /// </para>
    /// <para>
    /// Note that many html elements have an implicitly <a href="https://w3c.github.io/html-aam/#html-element-role-mappings">defined
    /// role</a> that is recognized by the role selector. You can find all the <a href="https://www.w3.org/TR/wai-aria-1.2/#role_definitions">supported
    /// roles here</a>. ARIA guidelines **do not recommend** duplicating implicit roles
    /// and attributes by setting <c>role</c> and/or <c>aria-*</c> attributes to default
    /// values.
    /// </para>
    /// </summary>
    /// <param name="role">Required aria role.</param>
    /// <param name="options">Call options</param>
    ILocator GetByRole(AriaRole role, LocatorGetByRoleOptions? options = default);

    /// <summary>
    /// <para>
    /// Locate element by the test id. By default, the <c>data-testid</c> attribute is used
    /// as a test id. Use <see cref="ISelectors.SetTestIdAttribute"/> to configure a different
    /// test id attribute if necessary.
    /// </para>
    /// </summary>
    /// <param name="testId">Id to locate the element by.</param>
    ILocator GetByTestId(string testId);

    /// <summary><para>Allows locating elements that contain given text.</para></summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByText(string text, LocatorGetByTextOptions? options = default);

    /// <summary><para>Allows locating elements that contain given text.</para></summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByText(Regex text, LocatorGetByTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their title. For example, this method will find the
    /// button by its title "Submit":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(string text, LocatorGetByTitleOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating elements by their title. For example, this method will find the
    /// button by its title "Submit":
    /// </para>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(Regex text, LocatorGetByTitleOptions? options = default);

    /// <summary>
    /// <para>
    /// Highlight the corresponding element(s) on the screen. Useful for debugging, don't
    /// commit the code that uses <see cref="ILocator.HighlightAsync"/>.
    /// </para>
    /// </summary>
    Task HighlightAsync();

    /// <summary>
    /// <para>This method hovers over the element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the element, unless <paramref name="force"/> option is set.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Mouse"/> to hover over the center of the element, or the specified
    /// <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <c>noWaitAfter</c>
    /// option is set.
    /// </description></item>
    /// </list>
    /// <para>
    /// If the element is detached from the DOM at any moment during the action, this method
    /// throws.
    /// </para>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task HoverAsync(LocatorHoverOptions? options = default);

    /// <summary><para>Returns the <c>element.innerHTML</c>.</para></summary>
    /// <param name="options">Call options</param>
    Task<string> InnerHTMLAsync(LocatorInnerHTMLOptions? options = default);

    /// <summary><para>Returns the <c>element.innerText</c>.</para></summary>
    /// <param name="options">Call options</param>
    Task<string> InnerTextAsync(LocatorInnerTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns <c>input.value</c> for the selected <c>&lt;input&gt;</c> or <c>&lt;textarea&gt;</c>
    /// or <c>&lt;select&gt;</c> element.
    /// </para>
    /// <para>
    /// Throws for non-input elements. However, if the element is inside the <c>&lt;label&gt;</c>
    /// element that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// returns the value of the control.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<string> InputValueAsync(LocatorInputValueOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns whether the element is checked. Throws if the element is not a checkbox
    /// or radio input.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsCheckedAsync(LocatorIsCheckedOptions? options = default);

    /// <summary><para>Returns whether the element is disabled, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<bool> IsDisabledAsync(LocatorIsDisabledOptions? options = default);

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#editable">editable</a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<bool> IsEditableAsync(LocatorIsEditableOptions? options = default);

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<bool> IsEnabledAsync(LocatorIsEnabledOptions? options = default);

    /// <summary><para>Returns whether the element is hidden, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<bool> IsHiddenAsync(LocatorIsHiddenOptions? options = default);

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<bool> IsVisibleAsync(LocatorIsVisibleOptions? options = default);

    /// <summary><para>Returns locator to the last matching element.</para></summary>
    ILocator Last { get; }

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector in the locator's subtree.
    /// It also accepts filter options, similar to <see cref="ILocator.Filter"/> method.
    /// </para>
    /// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
    /// </summary>
    /// <param name="selector">
    /// A selector to use when resolving DOM element. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    ILocator Locator(string selector, LocatorLocatorOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns locator to the n-th matching element. It's zero based, <c>nth(0)</c> selects
    /// the first element.
    /// </para>
    /// </summary>
    /// <param name="index">
    /// </param>
    ILocator Nth(int index);

    /// <summary><para>A page this locator belongs to.</para></summary>
    IPage Page { get; }

    /// <summary>
    /// <para>Focuses the element, and then uses <see cref="IKeyboard.DownAsync"/> and <see cref="IKeyboard.UpAsync"/>.</para>
    /// <para>
    /// <paramref name="key"/> can specify the intended <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key">keyboardEvent.key</a>
    /// value or a single character to generate the text for. A superset of the <paramref
    /// name="key"/> values can be found <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key/Key_Values">here</a>.
    /// Examples of the keys are:
    /// </para>
    /// <para>
    /// <c>F1</c> - <c>F12</c>, <c>Digit0</c>- <c>Digit9</c>, <c>KeyA</c>- <c>KeyZ</c>,
    /// <c>Backquote</c>, <c>Minus</c>, <c>Equal</c>, <c>Backslash</c>, <c>Backspace</c>,
    /// <c>Tab</c>, <c>Delete</c>, <c>Escape</c>, <c>ArrowDown</c>, <c>End</c>, <c>Enter</c>,
    /// <c>Home</c>, <c>Insert</c>, <c>PageDown</c>, <c>PageUp</c>, <c>ArrowRight</c>, <c>ArrowUp</c>,
    /// etc.
    /// </para>
    /// <para>
    /// Following modification shortcuts are also supported: <c>Shift</c>, <c>Control</c>,
    /// <c>Alt</c>, <c>Meta</c>, <c>ShiftLeft</c>.
    /// </para>
    /// <para>
    /// Holding down <c>Shift</c> will type the text that corresponds to the <paramref name="key"/>
    /// in the upper case.
    /// </para>
    /// <para>
    /// If <paramref name="key"/> is a single character, it is case-sensitive, so the values
    /// <c>a</c> and <c>A</c> will generate different respective texts.
    /// </para>
    /// <para>
    /// Shortcuts such as <c>key: "Control+o"</c> or <c>key: "Control+Shift+T"</c> are supported
    /// as well. When specified with the modifier, modifier is pressed and being held while
    /// the subsequent key is being pressed.
    /// </para>
    /// </summary>
    /// <param name="key">
    /// Name of the key to press or a character to generate, such as <c>ArrowLeft</c> or
    /// <c>a</c>.
    /// </param>
    /// <param name="options">Call options</param>
    Task PressAsync(string key, LocatorPressOptions? options = default);

    /// <summary>
    /// <para>
    /// This method captures a screenshot of the page, clipped to the size and position
    /// of a particular element matching the locator. If the element is covered by other
    /// elements, it will not be actually visible on the screenshot. If the element is a
    /// scrollable container, only the currently scrolled content will be visible on the
    /// screenshot.
    /// </para>
    /// <para>
    /// This method waits for the <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, then scrolls element into view before taking a screenshot. If the element
    /// is detached from DOM, the method throws an error.
    /// </para>
    /// <para>Returns the buffer with the captured screenshot.</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, then tries to scroll element into view, unless it is completely visible
    /// as defined by <a href="https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API">IntersectionObserver</a>'s
    /// <c>ratio</c>.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ScrollIntoViewIfNeededAsync(LocatorScrollIntoViewIfNeededOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, waits until all specified options are present in the <c>&lt;select&gt;</c>
    /// element and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] {<br/>
    ///     new SelectOptionValue() { Label = "blue" },<br/>
    ///     new SelectOptionValue() { Index = 2 },<br/>
    ///     new SelectOptionValue() { Value = "red" }});
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, waits until all specified options are present in the <c>&lt;select&gt;</c>
    /// element and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] {<br/>
    ///     new SelectOptionValue() { Label = "blue" },<br/>
    ///     new SelectOptionValue() { Index = 2 },<br/>
    ///     new SelectOptionValue() { Value = "red" }});
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, waits until all specified options are present in the <c>&lt;select&gt;</c>
    /// element and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] {<br/>
    ///     new SelectOptionValue() { Label = "blue" },<br/>
    ///     new SelectOptionValue() { Index = 2 },<br/>
    ///     new SelectOptionValue() { Value = "red" }});
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, waits until all specified options are present in the <c>&lt;select&gt;</c>
    /// element and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] {<br/>
    ///     new SelectOptionValue() { Label = "blue" },<br/>
    ///     new SelectOptionValue() { Index = 2 },<br/>
    ///     new SelectOptionValue() { Value = "red" }});
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, waits until all specified options are present in the <c>&lt;select&gt;</c>
    /// element and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] {<br/>
    ///     new SelectOptionValue() { Label = "blue" },<br/>
    ///     new SelectOptionValue() { Index = 2 },<br/>
    ///     new SelectOptionValue() { Value = "red" }});
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, waits until all specified options are present in the <c>&lt;select&gt;</c>
    /// element and selects these options.
    /// </para>
    /// <para>
    /// If the target element is not a <c>&lt;select&gt;</c> element, this method throws
    /// an error. However, if the element is inside the <c>&lt;label&gt;</c> element that
    /// has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be used instead.
    /// </para>
    /// <para>Returns the array of option values that have been successfully selected.</para>
    /// <para>
    /// Triggers a <c>change</c> and <c>input</c> event once all the provided options have
    /// been selected.
    /// </para>
    /// <code>
    /// // single selection matching the value<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] {<br/>
    ///     new SelectOptionValue() { Label = "blue" },<br/>
    ///     new SelectOptionValue() { Index = 2 },<br/>
    ///     new SelectOptionValue() { Value = "red" }});
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are equivalent to <c>{value:'string'}</c>.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, then focuses the element and selects all its text content.
    /// </para>
    /// <para>
    /// If the element is inside the <c>&lt;label&gt;</c> element that has an associated
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// focuses and selects text in the control instead.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task SelectTextAsync(LocatorSelectTextOptions? options = default);

    /// <summary>
    /// <para>This method checks or unchecks an element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Ensure that matched element is a checkbox or a radio input. If not, this method
    /// throws.
    /// </description></item>
    /// <item><description>If the element already has the right checked state, this method returns immediately.</description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the matched element, unless <paramref name="force"/> option is set. If
    /// the element is detached during the checks, the whole action is retried.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// <item><description>Ensure that the element is now checked or unchecked. If not, this method throws.</description></item>
    /// </list>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <param name="checkedState">Whether to check or uncheck the checkbox.</param>
    /// <param name="options">Call options</param>
    Task SetCheckedAsync(bool checkedState, LocatorSetCheckedOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="ILocator"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(string files, LocatorSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="ILocator"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(IEnumerable<string> files, LocatorSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="ILocator"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(FilePayload files, LocatorSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="ILocator"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(IEnumerable<FilePayload> files, LocatorSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>This method taps the element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the element, unless <paramref name="force"/> option is set.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>
    /// Use <see cref="IPage.Touchscreen"/> to tap the center of the element, or the specified
    /// <paramref name="position"/>.
    /// </description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// </list>
    /// <para>
    /// If the element is detached from the DOM at any moment during the action, this method
    /// throws.
    /// </para>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>element.tap()</c> requires that the <c>hasTouch</c> option of the browser context
    /// be set to true.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task TapAsync(LocatorTapOptions? options = default);

    /// <summary><para>Returns the <c>node.textContent</c>.</para></summary>
    /// <param name="options">Call options</param>
    Task<string?> TextContentAsync(LocatorTextContentOptions? options = default);

    /// <summary>
    /// <para>
    /// Focuses the element, and then sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>,
    /// and <c>keyup</c> event for each character in the text.
    /// </para>
    /// <para>To press a special key, like <c>Control</c> or <c>ArrowDown</c>, use <see cref="ILocator.PressAsync"/>.</para>
    /// <code>
    /// await element.TypeAsync("Hello"); // Types instantly<br/>
    /// await element.TypeAsync("World", new() { Delay = 100 }); // Types slower, like a user
    /// </code>
    /// <para>An example of typing into a text field and then submitting the form:</para>
    /// <code>
    /// var element = page.GetByLabel("Password");<br/>
    /// await element.TypeAsync("my password");<br/>
    /// await element.PressAsync("Enter");
    /// </code>
    /// </summary>
    /// <param name="text">A text to type into a focused element.</param>
    /// <param name="options">Call options</param>
    Task TypeAsync(string text, LocatorTypeOptions? options = default);

    /// <summary>
    /// <para>This method checks the element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Ensure that element is a checkbox or a radio input. If not, this method throws.
    /// If the element is already unchecked, this method returns immediately.
    /// </description></item>
    /// <item><description>
    /// Wait for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks on the element, unless <paramref name="force"/> option is set.
    /// </description></item>
    /// <item><description>Scroll the element into view if needed.</description></item>
    /// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
    /// <item><description>
    /// Wait for initiated navigations to either succeed or fail, unless <paramref name="noWaitAfter"/>
    /// option is set.
    /// </description></item>
    /// <item><description>Ensure that the element is now unchecked. If not, this method throws.</description></item>
    /// </list>
    /// <para>
    /// If the element is detached from the DOM at any moment during the action, this method
    /// throws.
    /// </para>
    /// <para>
    /// When all steps combined have not finished during the specified <paramref name="timeout"/>,
    /// this method throws a <see cref="TimeoutException"/>. Passing zero timeout disables
    /// this.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task UncheckAsync(LocatorUncheckOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns when element specified by locator satisfies the <paramref name="state"/>
    /// option.
    /// </para>
    /// <para>
    /// If target element already satisfies the condition, the method returns immediately.
    /// Otherwise, waits for up to <paramref name="timeout"/> milliseconds until the condition
    /// is met.
    /// </para>
    /// <code>
    /// var orderSent = page.Locator("#order-sent");<br/>
    /// orderSent.WaitForAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task WaitForAsync(LocatorWaitForOptions? options = default);
}

#nullable disable
