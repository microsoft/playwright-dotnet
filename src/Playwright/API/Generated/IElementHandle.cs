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
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// ElementHandle represents an in-page DOM element. ElementHandles can be created with
/// the <see cref="IPage.QuerySelectorAsync"/> method.
/// </para>
/// <code>
/// var handle = await page.QuerySelectorAsync("a");<br/>
/// await handle.ClickAsync();
/// </code>
/// <para>
/// ElementHandle prevents DOM element from garbage collection unless the handle is
/// disposed with <see cref="IJSHandle.DisposeAsync"/>. ElementHandles are auto-disposed
/// when their origin frame gets navigated.
/// </para>
/// <para>
/// ElementHandle instances can be used as an argument in <see cref="IPage.EvalOnSelectorAsync"/>
/// and <see cref="IPage.EvaluateAsync"/> methods.
/// </para>
/// <para>
/// The difference between the <see cref="ILocator"/> and ElementHandle is that the
/// ElementHandle points to a particular element, while <see cref="ILocator"/> captures
/// the logic of how to retrieve an element.
/// </para>
/// <para>
/// In the example below, handle points to a particular DOM element on page. If that
/// element changes text or is used by React to render an entirely different component,
/// handle is still pointing to that very DOM element. This can lead to unexpected behaviors.
/// </para>
/// <code>
/// var handle = await page.QuerySelectorAsync("text=Submit");<br/>
/// await handle.HoverAsync();<br/>
/// await handle.ClickAsync();
/// </code>
/// <para>
/// With the locator, every time the <c>element</c> is used, up-to-date DOM element
/// is located in the page using the selector. So in the snippet below, underlying DOM
/// element is going to be located twice.
/// </para>
/// <code>
/// var locator = page.GetByText("Submit");<br/>
/// await locator.HoverAsync();<br/>
/// await locator.ClickAsync();
/// </code>
/// </summary>
/// <remarks>
/// Inherits from <see cref="IJSHandle"/>
/// <para>
/// The use of ElementHandle is discouraged, use <see cref="ILocator"/> objects and
/// web-first assertions instead.
/// </para>
/// </remarks>
public partial interface IElementHandle : IJSHandle
{
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
    /// var box = await elementHandle.BoundingBoxAsync();<br/>
    /// await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
    /// </code>
    /// </summary>
    Task<ElementHandleBoundingBoxResult?> BoundingBoxAsync();

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
    Task CheckAsync(ElementHandleCheckOptions? options = default);

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
    Task ClickAsync(ElementHandleClickOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the content frame for element handles referencing iframe nodes, or <c>null</c>
    /// otherwise
    /// </para>
    /// </summary>
    Task<IFrame?> ContentFrameAsync();

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
    /// <c>elementHandle.dblclick()</c> dispatches two <c>click</c> events and a single
    /// <c>dblclick</c> event.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task DblClickAsync(ElementHandleDblClickOptions? options = default);

    /// <summary>
    /// <para>
    /// The snippet below dispatches the <c>click</c> event on the element. Regardless of
    /// the visibility state of the element, <c>click</c> is dispatched. This is equivalent
    /// to calling <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click">element.click()</a>.
    /// </para>
    /// <code>await elementHandle.DispatchEventAsync("click");</code>
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
    /// await elementHandle.DispatchEventAsync("dragstart", new Dictionary&lt;string, object&gt;<br/>
    /// {<br/>
    ///     { "dataTransfer", dataTransfer }<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="type">DOM event type: <c>"click"</c>, <c>"dragstart"</c>, etc.</param>
    /// <param name="eventInit">Optional event-specific initialization properties.</param>
    Task DispatchEventAsync(string type, object? eventInit = default);

    /// <summary>
    /// <para>Returns the return value of <paramref name="expression"/>.</para>
    /// <para>
    /// The method finds an element matching the specified selector in the <c>ElementHandle</c>s
    /// subtree and passes it as a first argument to <paramref name="expression"/>. See
    /// <a href="https://playwright.dev/dotnet/docs/selectors">Working with selectors</a>
    /// for more details. If no elements match the selector, the method throws an error.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, then <see cref="IElementHandle.EvalOnSelectorAsync"/>
    /// would wait for the promise to resolve and return its value.
    /// </para>
    /// <para>Examples:</para>
    /// <code>
    /// var tweetHandle = await page.QuerySelectorAsync(".tweet");<br/>
    /// Assert.AreEqual("100", await tweetHandle.EvalOnSelectorAsync(".like", "node =&gt; node.innerText"));<br/>
    /// Assert.AreEqual("10", await tweetHandle.EvalOnSelectorAsync(".retweets", "node =&gt; node.innerText"));
    /// </code>
    /// </summary>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object? arg = default);

    /// <summary>
    /// <para>Returns the return value of <paramref name="expression"/>.</para>
    /// <para>
    /// The method finds all elements matching the specified selector in the <c>ElementHandle</c>'s
    /// subtree and passes an array of matched elements as a first argument to <paramref
    /// name="expression"/>. See <a href="https://playwright.dev/dotnet/docs/selectors">Working
    /// with selectors</a> for more details.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, then <see cref="IElementHandle.EvalOnSelectorAllAsync"/>
    /// would wait for the promise to resolve and return its value.
    /// </para>
    /// <para>Examples:</para>
    /// <code>
    /// var feedHandle = await page.QuerySelectorAsync(".feed");<br/>
    /// Assert.AreEqual(new [] { "Hello!", "Hi!" }, await feedHandle.EvalOnSelectorAllAsync&lt;string[]&gt;(".tweet", "nodes =&gt; nodes.map(n =&gt; n.innerText)"));
    /// </code>
    /// </summary>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object? arg = default);

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
    /// <para>To send fine-grained keyboard events, use <see cref="IElementHandle.TypeAsync"/>.</para>
    /// </summary>
    /// <param name="value">
    /// Value to set for the <c>&lt;input&gt;</c>, <c>&lt;textarea&gt;</c> or <c>[contenteditable]</c>
    /// element.
    /// </param>
    /// <param name="options">Call options</param>
    Task FillAsync(string value, ElementHandleFillOptions? options = default);

    /// <summary>
    /// <para>
    /// Calls <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/focus">focus</a>
    /// on the element.
    /// </para>
    /// </summary>
    Task FocusAsync();

    /// <summary><para>Returns element attribute value.</para></summary>
    /// <param name="name">Attribute name to get the value for.</param>
    Task<string?> GetAttributeAsync(string name);

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
    Task HoverAsync(ElementHandleHoverOptions? options = default);

    /// <summary><para>Returns the <c>element.innerHTML</c>.</para></summary>
    Task<string> InnerHTMLAsync();

    /// <summary><para>Returns the <c>element.innerText</c>.</para></summary>
    Task<string> InnerTextAsync();

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
    Task<string> InputValueAsync(ElementHandleInputValueOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns whether the element is checked. Throws if the element is not a checkbox
    /// or radio input.
    /// </para>
    /// </summary>
    Task<bool> IsCheckedAsync();

    /// <summary><para>Returns whether the element is disabled, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para></summary>
    Task<bool> IsDisabledAsync();

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#editable">editable</a>.</para></summary>
    Task<bool> IsEditableAsync();

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para></summary>
    Task<bool> IsEnabledAsync();

    /// <summary><para>Returns whether the element is hidden, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</para></summary>
    Task<bool> IsHiddenAsync();

    /// <summary><para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</para></summary>
    Task<bool> IsVisibleAsync();

    /// <summary><para>Returns the frame containing the given element.</para></summary>
    Task<IFrame?> OwnerFrameAsync();

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
    Task PressAsync(string key, ElementHandlePressOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector in the <c>ElementHandle</c>'s
    /// subtree. See <a href="https://playwright.dev/dotnet/docs/selectors">Working with
    /// selectors</a> for more details. If no elements match the selector, returns <c>null</c>.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    Task<IElementHandle?> QuerySelectorAsync(string selector);

    /// <summary>
    /// <para>
    /// The method finds all elements matching the specified selector in the <c>ElementHandle</c>s
    /// subtree. See <a href="https://playwright.dev/dotnet/docs/selectors">Working with
    /// selectors</a> for more details. If no elements match the selector, returns empty
    /// array.
    /// </para>
    /// </summary>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector);

    /// <summary>
    /// <para>
    /// This method captures a screenshot of the page, clipped to the size and position
    /// of this particular element. If the element is covered by other elements, it will
    /// not be actually visible on the screenshot. If the element is a scrollable container,
    /// only the currently scrolled content will be visible on the screenshot.
    /// </para>
    /// <para>
    /// This method waits for the <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, then scrolls element into view before taking a screenshot. If the element
    /// is detached from DOM, the method throws an error.
    /// </para>
    /// <para>Returns the buffer with the captured screenshot.</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<byte[]> ScreenshotAsync(ElementHandleScreenshotOptions? options = default);

    /// <summary>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, then tries to scroll element into view, unless it is completely visible
    /// as defined by <a href="https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API">IntersectionObserver</a>'s
    /// <c>ratio</c>.
    /// </para>
    /// <para>
    /// Throws when <c>elementHandle</c> does not point to an element <a href="https://developer.mozilla.org/en-US/docs/Web/API/Node/isConnected">connected</a>
    /// to a Document or a ShadowRoot.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ScrollIntoViewIfNeededAsync(ElementHandleScrollIntoViewIfNeededOptions? options = default);

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
    /// await handle.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await handle.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await handle.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await handle.SelectOptionAsync(new[] {<br/>
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
    Task<IReadOnlyList<string>> SelectOptionAsync(string values, ElementHandleSelectOptionOptions? options = default);

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
    /// await handle.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await handle.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await handle.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await handle.SelectOptionAsync(new[] {<br/>
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
    Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, ElementHandleSelectOptionOptions? options = default);

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
    /// await handle.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await handle.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await handle.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await handle.SelectOptionAsync(new[] {<br/>
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
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, ElementHandleSelectOptionOptions? options = default);

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
    /// await handle.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await handle.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await handle.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await handle.SelectOptionAsync(new[] {<br/>
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
    Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, ElementHandleSelectOptionOptions? options = default);

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
    /// await handle.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await handle.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await handle.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await handle.SelectOptionAsync(new[] {<br/>
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
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, ElementHandleSelectOptionOptions? options = default);

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
    /// await handle.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await handle.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection<br/>
    /// await handle.SelectOptionAsync(new[] { "red", "green", "blue" });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await handle.SelectOptionAsync(new[] {<br/>
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
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, ElementHandleSelectOptionOptions? options = default);

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
    Task SelectTextAsync(ElementHandleSelectTextOptions? options = default);

    /// <summary>
    /// <para>This method checks or unchecks an element by performing the following steps:</para>
    /// <list type="ordinal">
    /// <item><description>Ensure that element is a checkbox or a radio input. If not, this method throws.</description></item>
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
    Task SetCheckedAsync(bool checkedState, ElementHandleSetCheckedOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="IElementHandle"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(string files, ElementHandleSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="IElementHandle"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(IEnumerable<string> files, ElementHandleSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="IElementHandle"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(FilePayload files, ElementHandleSetInputFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input to these file paths or files. If some of the <c>filePaths</c>
    /// are relative paths, then they are resolved relative to the current working directory.
    /// For empty array, clears the selected files.
    /// </para>
    /// <para>
    /// This method expects <see cref="IElementHandle"/> to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input
    /// element</a>. However, if the element is inside the <c>&lt;label&gt;</c> element
    /// that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// targets the control instead.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetInputFilesAsync(IEnumerable<FilePayload> files, ElementHandleSetInputFilesOptions? options = default);

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
    /// <c>elementHandle.tap()</c> requires that the <c>hasTouch</c> option of the browser
    /// context be set to true.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task TapAsync(ElementHandleTapOptions? options = default);

    /// <summary><para>Returns the <c>node.textContent</c>.</para></summary>
    Task<string?> TextContentAsync();

    /// <summary>
    /// <para>
    /// Focuses the element, and then sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>,
    /// and <c>keyup</c> event for each character in the text.
    /// </para>
    /// <para>To press a special key, like <c>Control</c> or <c>ArrowDown</c>, use <see cref="IElementHandle.PressAsync"/>.</para>
    /// <code>
    /// await elementHandle.TypeAsync("Hello"); // Types instantly<br/>
    /// await elementHandle.TypeAsync("World", new() { Delay = 100 }); // Types slower, like a user
    /// </code>
    /// <para>An example of typing into a text field and then submitting the form:</para>
    /// <code>
    /// var elementHandle = await page.QuerySelectorAsync("input");<br/>
    /// await elementHandle.TypeAsync("some text");<br/>
    /// await elementHandle.PressAsync("Enter");
    /// </code>
    /// </summary>
    /// <param name="text">A text to type into a focused element.</param>
    /// <param name="options">Call options</param>
    Task TypeAsync(string text, ElementHandleTypeOptions? options = default);

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
    Task UncheckAsync(ElementHandleUncheckOptions? options = default);

    /// <summary>
    /// <para>Returns when the element satisfies the <paramref name="state"/>.</para>
    /// <para>
    /// Depending on the <paramref name="state"/> parameter, this method waits for one of
    /// the <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks to pass. This method throws when the element is detached while waiting, unless
    /// waiting for the <c>"hidden"</c> state.
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>"visible"</c> Wait until the element is <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</description></item>
    /// <item><description>
    /// <c>"hidden"</c> Wait until the element is <a href="https://playwright.dev/dotnet/docs/actionability#visible">not
    /// visible</a> or <a href="https://playwright.dev/dotnet/docs/actionability#attached">not
    /// attached</a>. Note that waiting for hidden does not throw when the element detaches.
    /// </description></item>
    /// <item><description>
    /// <c>"stable"</c> Wait until the element is both <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>
    /// and <a href="https://playwright.dev/dotnet/docs/actionability#stable">stable</a>.
    /// </description></item>
    /// <item><description><c>"enabled"</c> Wait until the element is <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</description></item>
    /// <item><description>
    /// <c>"disabled"</c> Wait until the element is <a href="https://playwright.dev/dotnet/docs/actionability#enabled">not
    /// enabled</a>.
    /// </description></item>
    /// <item><description><c>"editable"</c> Wait until the element is <a href="https://playwright.dev/dotnet/docs/actionability#editable">editable</a>.</description></item>
    /// </list>
    /// <para>
    /// If the element does not satisfy the condition for the <paramref name="timeout"/>
    /// milliseconds, this method will throw.
    /// </para>
    /// </summary>
    /// <param name="state">A state to wait for, see below for more details.</param>
    /// <param name="options">Call options</param>
    Task WaitForElementStateAsync(ElementState state, ElementHandleWaitForElementStateOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns element specified by selector when it satisfies <paramref name="state"/>
    /// option. Returns <c>null</c> if waiting for <c>hidden</c> or <c>detached</c>.
    /// </para>
    /// <para>
    /// Wait for the <paramref name="selector"/> relative to the element handle to satisfy
    /// <paramref name="state"/> option (either appear/disappear from dom, or become visible/hidden).
    /// If at the moment of calling the method <paramref name="selector"/> already satisfies
    /// the condition, the method will return immediately. If the selector doesn't satisfy
    /// the condition for the <paramref name="timeout"/> milliseconds, the function will
    /// throw.
    /// </para>
    /// <code>
    /// await page.SetContentAsync("&lt;div&gt;&lt;span&gt;&lt;/span&gt;&lt;/div&gt;");<br/>
    /// var div = await page.QuerySelectorAsync("div");<br/>
    /// // Waiting for the "span" selector relative to the div.<br/>
    /// var span = await page.WaitForSelectorAsync("span", WaitForSelectorState.Attached);
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method does not work across navigations, use <see cref="IPage.WaitForSelectorAsync"/>
    /// instead.
    /// </para>
    /// </remarks>
    /// <param name="selector">
    /// A selector to query for. See <a href="https://playwright.dev/dotnet/docs/selectors">working
    /// with selectors</a> for more details.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IElementHandle?> WaitForSelectorAsync(string selector, ElementHandleWaitForSelectorOptions? options = default);
}

#nullable disable
