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
    /// <summary>
    /// <para>
    /// When locator points to a list of elements, returns array of locators, pointing to
    /// respective elements.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// foreach (var li in await page.GetByRole("listitem").AllAsync())<br/>
    ///   await li.ClickAsync();
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ILocator.AllAsync"/> does not wait for elements to match the locator,
    /// and instead immediately returns whatever is present in the page.  When the list
    /// of elements changes dynamically, <see cref="ILocator.AllAsync"/> will produce unpredictable
    /// and flaky results.  When the list of elements is stable, but loaded dynamically,
    /// wait for the full list to finish loading before calling <see cref="ILocator.AllAsync"/>.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ILocator>> AllAsync();

    /// <summary>
    /// <para>Returns an array of <c>node.innerText</c> values for all matching nodes.</para>
    /// <para>**Usage**</para>
    /// <code>var texts = await page.GetByRole(AriaRole.Link).AllInnerTextsAsync();</code>
    /// </summary>
    Task<IReadOnlyList<string>> AllInnerTextsAsync();

    /// <summary>
    /// <para>Returns an array of <c>node.textContent</c> values for all matching nodes.</para>
    /// <para>**Usage**</para>
    /// <code>var texts = await page.GetByRole(AriaRole.Link).AllTextContentsAsync();</code>
    /// </summary>
    Task<IReadOnlyList<string>> AllTextContentsAsync();

    /// <summary>
    /// <para>Creates a locator that matches both this locator and the argument locator.</para>
    /// <para>**Usage**</para>
    /// <para>The following example finds a button with a specific title.</para>
    /// <code>var button = page.GetByRole(AriaRole.Button).And(page.GetByTitle("Subscribe"));</code>
    /// </summary>
    /// <param name="locator">Additional locator to match.</param>
    ILocator And(ILocator locator);

    /// <summary>
    /// <para>
    /// Calls <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/blur">blur</a>
    /// on the element.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task BlurAsync(LocatorBlurOptions? options = default);

    /// <summary>
    /// <para>
    /// This method returns the bounding box of the element matching the locator, or <c>null</c>
    /// if the element is not visible. The bounding box is calculated relative to the main
    /// frame viewport - which is usually the same as the browser window.
    /// </para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// var box = await page.GetByRole(AriaRole.Button).BoundingBoxAsync();<br/>
    /// await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<LocatorBoundingBoxResult?> BoundingBoxAsync(LocatorBoundingBoxOptions? options = default);

    /// <summary>
    /// <para>Ensure that checkbox or radio element is checked.</para>
    /// <para>**Details**</para>
    /// <para>Performs the following steps:</para>
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
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Checkbox).CheckAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task CheckAsync(LocatorCheckOptions? options = default);

    /// <summary>
    /// <para>Clear the input field.</para>
    /// <para>**Details**</para>
    /// <para>
    /// This method waits for <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks, focuses the element, clears it and triggers an <c>input</c> event after
    /// clearing.
    /// </para>
    /// <para>
    /// If the target element is not an <c>&lt;input&gt;</c>, <c>&lt;textarea&gt;</c> or
    /// <c>[contenteditable]</c> element, this method throws an error. However, if the element
    /// is inside the <c>&lt;label&gt;</c> element that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
    /// the control will be cleared instead.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Textbox).ClearAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ClearAsync(LocatorClearOptions? options = default);

    /// <summary>
    /// <para>Click an element.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <para>Click a button:</para>
    /// <code>await page.GetByRole(AriaRole.Button).ClickAsync();</code>
    /// <para>Shift-right-click at a specific position on a canvas:</para>
    /// <code>
    /// await page.Locator("canvas").ClickAsync(new() {<br/>
    ///   Button = MouseButton.Right,<br/>
    ///   Modifiers = new[] { KeyboardModifier.Shift },<br/>
    ///   Position = new Position { X = 0, Y = 0 }<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ClickAsync(LocatorClickOptions? options = default);

    /// <summary>
    /// <para>Returns the number of elements matching the locator.</para>
    /// <para>**Usage**</para>
    /// <code>int count = await page.GetByRole(AriaRole.Listitem).CountAsync();</code>
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// <para>Double-click an element.</para>
    /// <para>**Details**</para>
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
    /// <para>Programmatically dispatch an event on the matching element.</para>
    /// <para>**Usage**</para>
    /// <code>await locator.DispatchEventAsync("click");</code>
    /// <para>**Details**</para>
    /// <para>
    /// The snippet above dispatches the <c>click</c> event on the element. Regardless of
    /// the visibility state of the element, <c>click</c> is dispatched. This is equivalent
    /// to calling <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click">element.click()</a>.
    /// </para>
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
    /// You can also specify <see cref="IJSHandle"/> as the property value if you want live
    /// objects to be passed into the event:
    /// </para>
    /// <code>
    /// var dataTransfer = await page.EvaluateHandleAsync("() =&gt; new DataTransfer()");<br/>
    /// await locator.DispatchEventAsync("dragstart", new Dictionary&lt;string, object&gt;<br/>
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
    /// <para>Drag the source element towards the target element and drop it.</para>
    /// <para>**Details**</para>
    /// <para>
    /// This method drags the locator to another target locator or target position. It will
    /// first move to the source element, perform a <c>mousedown</c>, then move to the target
    /// element or position and perform a <c>mouseup</c>.
    /// </para>
    /// <para>**Usage**</para>
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
    /// Always prefer using <see cref="ILocator"/>s and web assertions over <see cref="IElementHandle"/>s
    /// because latter are inherently racy.
    /// </para>
    /// <para>
    /// Resolves given locator to the first matching DOM element. If there are no matching
    /// elements, waits for one. If multiple elements match the locator, throws.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IElementHandle> ElementHandleAsync(LocatorElementHandleOptions? options = default);

    /// <summary>
    /// <para>
    /// Always prefer using <see cref="ILocator"/>s and web assertions over <see cref="IElementHandle"/>s
    /// because latter are inherently racy.
    /// </para>
    /// <para>
    /// Resolves given locator to all matching DOM elements. If there are no matching elements,
    /// returns an empty list.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<IElementHandle>> ElementHandlesAsync();

    /// <summary>
    /// <para>Execute JavaScript code in the page, taking the matching element as an argument.</para>
    /// <para>**Details**</para>
    /// <para>
    /// Returns the return value of <paramref name="expression"/>, called with the matching
    /// element as a first argument, and <paramref name="arg"/> as a second argument.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, this method will
    /// wait for the promise to resolve and return its value.
    /// </para>
    /// <para>If <paramref name="expression"/> throws or rejects, this method throws.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// var tweets = page.Locator(".tweet .retweets");<br/>
    /// Assert.AreEqual("10 retweets", await tweets.EvaluateAsync("node =&gt; node.innerText"));
    /// </code>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expression
    /// evaluates to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    /// <param name="options">Call options</param>
    Task<T> EvaluateAsync<T>(string expression, object? arg = default, LocatorEvaluateOptions? options = default);

    /// <summary>
    /// <para>Execute JavaScript code in the page, taking all matching elements as an argument.</para>
    /// <para>**Details**</para>
    /// <para>
    /// Returns the return value of <paramref name="expression"/>, called with an array
    /// of all matching elements as a first argument, and <paramref name="arg"/> as a second
    /// argument.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, this method will
    /// wait for the promise to resolve and return its value.
    /// </para>
    /// <para>If <paramref name="expression"/> throws or rejects, this method throws.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// var locator = page.Locator("div");<br/>
    /// var moreThanTen = await locator.EvaluateAllAsync&lt;bool&gt;("(divs, min) =&gt; divs.length &gt; min", 10);
    /// </code>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expression
    /// evaluates to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvaluateAllAsync<T>(string expression, object? arg = default);

    /// <summary>
    /// <para>
    /// Execute JavaScript code in the page, taking the matching element as an argument,
    /// and return a <see cref="IJSHandle"/> with the result.
    /// </para>
    /// <para>**Details**</para>
    /// <para>
    /// Returns the return value of <paramref name="expression"/> as a<see cref="IJSHandle"/>,
    /// called with the matching element as a first argument, and <paramref name="arg"/>
    /// as a second argument.
    /// </para>
    /// <para>
    /// The only difference between <see cref="ILocator.EvaluateAsync"/> and <see cref="ILocator.EvaluateHandleAsync"/>
    /// is that <see cref="ILocator.EvaluateHandleAsync"/> returns <see cref="IJSHandle"/>.
    /// </para>
    /// <para>
    /// If <paramref name="expression"/> returns a <see cref="Task"/>, this method will
    /// wait for the promise to resolve and return its value.
    /// </para>
    /// <para>If <paramref name="expression"/> throws or rejects, this method throws.</para>
    /// <para>See <see cref="IPage.EvaluateHandleAsync"/> for more details.</para>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expression
    /// evaluates to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    /// <param name="options">Call options</param>
    Task<IJSHandle> EvaluateHandleAsync(string expression, object? arg = default, LocatorEvaluateHandleOptions? options = default);

    /// <summary>
    /// <para>Set a value to the input field.</para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Textbox).FillAsync("example value");</code>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// var rowLocator = page.Locator("tr");<br/>
    /// // ...<br/>
    /// await rowLocator<br/>
    ///     .Filter(new() { HasText = "text in column 1" })<br/>
    ///     .Filter(new() {<br/>
    ///         Has = page.GetByRole(AriaRole.Button, new() { Name = "column 2 button" } )<br/>
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
    /// on the matching element.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task FocusAsync(LocatorFocusOptions? options = default);

    /// <summary>
    /// <para>
    /// When working with iframes, you can create a frame locator that will enter the iframe
    /// and allow locating elements in that iframe:
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// var locator = page.FrameLocator("iframe").GetByText("Submit");<br/>
    /// await locator.ClickAsync();
    /// </code>
    /// </summary>
    /// <param name="selector">A selector to use when resolving DOM element.</param>
    IFrameLocator FrameLocator(string selector);

    /// <summary><para>Returns the matching element's attribute value.</para></summary>
    /// <param name="name">Attribute name to get the value for.</param>
    /// <param name="options">Call options</param>
    Task<string?> GetAttributeAsync(string name, LocatorGetAttributeOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their alt text.</para>
    /// <para>**Usage**</para>
    /// <para>For example, this method will find the image by alt text "Playwright logo":</para>
    /// <code>await page.GetByAltText("Playwright logo").ClickAsync();</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(string text, LocatorGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their alt text.</para>
    /// <para>**Usage**</para>
    /// <para>For example, this method will find the image by alt text "Playwright logo":</para>
    /// <code>await page.GetByAltText("Playwright logo").ClickAsync();</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByAltText(Regex text, LocatorGetByAltTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the text of the associated <c>&lt;label&gt;</c>
    /// or <c>aria-labelledby</c> element, or by the <c>aria-label</c> attribute.
    /// </para>
    /// <para>**Usage**</para>
    /// <para>
    /// For example, this method will find inputs by label "Username" and "Password" in
    /// the following DOM:
    /// </para>
    /// <code>
    /// await page.GetByLabel("Username").FillAsync("john");<br/>
    /// await page.GetByLabel("Password").FillAsync("secret");
    /// </code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(string text, LocatorGetByLabelOptions? options = default);

    /// <summary>
    /// <para>
    /// Allows locating input elements by the text of the associated <c>&lt;label&gt;</c>
    /// or <c>aria-labelledby</c> element, or by the <c>aria-label</c> attribute.
    /// </para>
    /// <para>**Usage**</para>
    /// <para>
    /// For example, this method will find inputs by label "Username" and "Password" in
    /// the following DOM:
    /// </para>
    /// <code>
    /// await page.GetByLabel("Username").FillAsync("john");<br/>
    /// await page.GetByLabel("Password").FillAsync("secret");
    /// </code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByLabel(Regex text, LocatorGetByLabelOptions? options = default);

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
    ILocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions? options = default);

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
    ILocator GetByPlaceholder(Regex text, LocatorGetByPlaceholderOptions? options = default);

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
    ILocator GetByRole(AriaRole role, LocatorGetByRoleOptions? options = default);

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
    /// page.GetByText("world");<br/>
    /// <br/>
    /// // Matches first &lt;div&gt;<br/>
    /// page.GetByText("Hello world");<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText("Hello", new() { Exact = true });<br/>
    /// <br/>
    /// // Matches both &lt;div&gt;s<br/>
    /// page.GetByText(new Regex("Hello"));<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText(new Regex("^hello$", RegexOptions.IgnoreCase));
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
    ILocator GetByText(string text, LocatorGetByTextOptions? options = default);

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
    /// page.GetByText("world");<br/>
    /// <br/>
    /// // Matches first &lt;div&gt;<br/>
    /// page.GetByText("Hello world");<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText("Hello", new() { Exact = true });<br/>
    /// <br/>
    /// // Matches both &lt;div&gt;s<br/>
    /// page.GetByText(new Regex("Hello"));<br/>
    /// <br/>
    /// // Matches second &lt;div&gt;<br/>
    /// page.GetByText(new Regex("^hello$", RegexOptions.IgnoreCase));
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
    ILocator GetByText(Regex text, LocatorGetByTextOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their title attribute.</para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can check the issues count after locating it by the title text:</para>
    /// <code>await Expect(page.GetByTitle("Issues count")).toHaveText("25 issues");</code>
    /// </summary>
    /// <param name="text">Text to locate the element for.</param>
    /// <param name="options">Call options</param>
    ILocator GetByTitle(string text, LocatorGetByTitleOptions? options = default);

    /// <summary>
    /// <para>Allows locating elements by their title attribute.</para>
    /// <para>**Usage**</para>
    /// <para>Consider the following DOM structure.</para>
    /// <para>You can check the issues count after locating it by the title text:</para>
    /// <code>await Expect(page.GetByTitle("Issues count")).toHaveText("25 issues");</code>
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
    /// <para>Hover over the matching element.</para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Link).HoverAsync();</code>
    /// <para>**Details**</para>
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

    /// <summary><para>Returns the <a href="https://developer.mozilla.org/en-US/docs/Web/API/Element/innerHTML"><c>element.innerHTML</c></a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<string> InnerHTMLAsync(LocatorInnerHTMLOptions? options = default);

    /// <summary><para>Returns the <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/innerText"><c>element.innerText</c></a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<string> InnerTextAsync(LocatorInnerTextOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the value for the matching <c>&lt;input&gt;</c> or <c>&lt;textarea&gt;</c>
    /// or <c>&lt;select&gt;</c> element.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>String value = await page.GetByRole(AriaRole.Textbox).InputValueAsync();</code>
    /// <para>**Details**</para>
    /// <para>
    /// Throws elements that are not an input, textarea or a select. However, if the element
    /// is inside the <c>&lt;label&gt;</c> element that has an associated <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLLabelElement/control">control</a>,
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
    /// <para>**Usage**</para>
    /// <code>var isChecked = await page.GetByRole(AriaRole.Checkbox).IsCheckedAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsCheckedAsync(LocatorIsCheckedOptions? options = default);

    /// <summary>
    /// <para>Returns whether the element is disabled, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para>
    /// <para>**Usage**</para>
    /// <code>Boolean disabled = await page.GetByRole(AriaRole.Button).IsDisabledAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsDisabledAsync(LocatorIsDisabledOptions? options = default);

    /// <summary>
    /// <para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#editable">editable</a>.</para>
    /// <para>**Usage**</para>
    /// <code>Boolean editable = await page.GetByRole(AriaRole.Textbox).IsEditableAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsEditableAsync(LocatorIsEditableOptions? options = default);

    /// <summary>
    /// <para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#enabled">enabled</a>.</para>
    /// <para>**Usage**</para>
    /// <code>Boolean enabled = await page.GetByRole(AriaRole.Button).IsEnabledAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsEnabledAsync(LocatorIsEnabledOptions? options = default);

    /// <summary>
    /// <para>Returns whether the element is hidden, the opposite of <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</para>
    /// <para>**Usage**</para>
    /// <code>Boolean hidden = await page.GetByRole(AriaRole.Button).IsHiddenAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsHiddenAsync(LocatorIsHiddenOptions? options = default);

    /// <summary>
    /// <para>Returns whether the element is <a href="https://playwright.dev/dotnet/docs/actionability#visible">visible</a>.</para>
    /// <para>**Usage**</para>
    /// <code>Boolean visible = await page.GetByRole(AriaRole.Button).IsVisibleAsync();</code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<bool> IsVisibleAsync(LocatorIsVisibleOptions? options = default);

    /// <summary>
    /// <para>Returns locator to the last matching element.</para>
    /// <para>**Usage**</para>
    /// <code>var banana = await page.GetByRole(AriaRole.Listitem).Last(1);</code>
    /// </summary>
    ILocator Last { get; }

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector in the locator's subtree.
    /// It also accepts filter options, similar to <see cref="ILocator.Filter"/> method.
    /// </para>
    /// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
    /// </summary>
    /// <param name="selectorOrLocator">A selector or locator to use when resolving DOM element.</param>
    /// <param name="options">Call options</param>
    ILocator Locator(string selectorOrLocator, LocatorLocatorOptions? options = default);

    /// <summary>
    /// <para>
    /// The method finds an element matching the specified selector in the locator's subtree.
    /// It also accepts filter options, similar to <see cref="ILocator.Filter"/> method.
    /// </para>
    /// <para><a href="https://playwright.dev/dotnet/docs/locators">Learn more about locators</a>.</para>
    /// </summary>
    /// <param name="selectorOrLocator">A selector or locator to use when resolving DOM element.</param>
    /// <param name="options">Call options</param>
    ILocator Locator(ILocator selectorOrLocator, LocatorLocatorOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns locator to the n-th matching element. It's zero based, <c>nth(0)</c> selects
    /// the first element.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>var banana = await page.GetByRole(AriaRole.Listitem).Nth(2);</code>
    /// </summary>
    /// <param name="index">
    /// </param>
    ILocator Nth(int index);

    /// <summary>
    /// <para>Creates a locator that matches either of the two locators.</para>
    /// <para>**Usage**</para>
    /// <para>
    /// Consider a scenario where you'd like to click on a "New email" button, but sometimes
    /// a security settings dialog shows up instead. In this case, you can wait for either
    /// a "New email" button, or a dialog and act accordingly.
    /// </para>
    /// <code>
    /// var newEmail = page.GetByRole(AriaRole.Button, new() { Name = "New" });<br/>
    /// var dialog = page.GetByText("Confirm security settings");<br/>
    /// await Expect(newEmail.Or(dialog)).ToBeVisibleAsync();<br/>
    /// if (await dialog.IsVisibleAsync())<br/>
    ///   await page.GetByRole(AriaRole.Button, new() { Name = "Dismiss" }).ClickAsync();<br/>
    /// await newEmail.ClickAsync();
    /// </code>
    /// </summary>
    /// <param name="locator">Alternative locator to match.</param>
    ILocator Or(ILocator locator);

    /// <summary><para>A page this locator belongs to.</para></summary>
    IPage Page { get; }

    /// <summary>
    /// <para>Focuses the matching element and presses a combination of the keys.</para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Textbox).PressAsync("Backspace");</code>
    /// <para>**Details**</para>
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
    /// <para>Take a screenshot of the element matching the locator.</para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Link).ScreenshotAsync();</code>
    /// <para>Disable animations and save screenshot to a file:</para>
    /// <code>
    /// await page.GetByRole(AriaRole.Link).ScreenshotAsync(new() {<br/>
    ///   Animations = ScreenshotAnimations.Disabled,<br/>
    ///   Path = "link.png"<br/>
    /// });
    /// </code>
    /// <para>**Details**</para>
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
    /// <para>Selects option or options in <c>&lt;select&gt;</c>.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// // single selection matching the value or label<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are matching both values and labels.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(string values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>Selects option or options in <c>&lt;select&gt;</c>.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// // single selection matching the value or label<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are matching both values and labels.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>Selects option or options in <c>&lt;select&gt;</c>.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// // single selection matching the value or label<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are matching both values and labels.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>Selects option or options in <c>&lt;select&gt;</c>.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// // single selection matching the value or label<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are matching both values and labels.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>Selects option or options in <c>&lt;select&gt;</c>.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// // single selection matching the value or label<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are matching both values and labels.
    /// Option is considered matching if all specified properties match.
    /// </param>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, LocatorSelectOptionOptions? options = default);

    /// <summary>
    /// <para>Selects option or options in <c>&lt;select&gt;</c>.</para>
    /// <para>**Details**</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// // single selection matching the value or label<br/>
    /// await element.SelectOptionAsync(new[] { "blue" });<br/>
    /// // single selection matching the label<br/>
    /// await element.SelectOptionAsync(new[] { new SelectOptionValue() { Label = "blue" } });<br/>
    /// // multiple selection for blue, red and second option<br/>
    /// await element.SelectOptionAsync(new[] { "red", "green", "blue" });
    /// </code>
    /// </summary>
    /// <param name="values">
    /// Options to select. If the <c>&lt;select&gt;</c> has the <c>multiple</c> attribute,
    /// all matching options are selected, otherwise only the first option matching one
    /// of the passed options is selected. String values are matching both values and labels.
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
    /// <para>Set the state of a checkbox or a radio element.</para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Checkbox).SetCheckedAsync(true);</code>
    /// <para>**Details**</para>
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
    /// <para>Upload file or multiple files into <c>&lt;input type=file&gt;</c>.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// // Select one file<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync("myfile.pdf");<br/>
    /// <br/>
    /// // Select multiple files<br/>
    /// await page.GetByLabel("Upload files").SetInputFilesAsync(new[] { "file1.txt", "file12.txt" });<br/>
    /// <br/>
    /// // Remove all the selected files<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new[] {});<br/>
    /// <br/>
    /// // Upload buffer from memory<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new FilePayload<br/>
    /// {<br/>
    ///     Name = "file.txt",<br/>
    ///     MimeType = "text/plain",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("this is a test"),<br/>
    /// });
    /// </code>
    /// <para>**Details**</para>
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
    /// <para>Upload file or multiple files into <c>&lt;input type=file&gt;</c>.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// // Select one file<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync("myfile.pdf");<br/>
    /// <br/>
    /// // Select multiple files<br/>
    /// await page.GetByLabel("Upload files").SetInputFilesAsync(new[] { "file1.txt", "file12.txt" });<br/>
    /// <br/>
    /// // Remove all the selected files<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new[] {});<br/>
    /// <br/>
    /// // Upload buffer from memory<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new FilePayload<br/>
    /// {<br/>
    ///     Name = "file.txt",<br/>
    ///     MimeType = "text/plain",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("this is a test"),<br/>
    /// });
    /// </code>
    /// <para>**Details**</para>
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
    /// <para>Upload file or multiple files into <c>&lt;input type=file&gt;</c>.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// // Select one file<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync("myfile.pdf");<br/>
    /// <br/>
    /// // Select multiple files<br/>
    /// await page.GetByLabel("Upload files").SetInputFilesAsync(new[] { "file1.txt", "file12.txt" });<br/>
    /// <br/>
    /// // Remove all the selected files<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new[] {});<br/>
    /// <br/>
    /// // Upload buffer from memory<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new FilePayload<br/>
    /// {<br/>
    ///     Name = "file.txt",<br/>
    ///     MimeType = "text/plain",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("this is a test"),<br/>
    /// });
    /// </code>
    /// <para>**Details**</para>
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
    /// <para>Upload file or multiple files into <c>&lt;input type=file&gt;</c>.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// // Select one file<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync("myfile.pdf");<br/>
    /// <br/>
    /// // Select multiple files<br/>
    /// await page.GetByLabel("Upload files").SetInputFilesAsync(new[] { "file1.txt", "file12.txt" });<br/>
    /// <br/>
    /// // Remove all the selected files<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new[] {});<br/>
    /// <br/>
    /// // Upload buffer from memory<br/>
    /// await page.GetByLabel("Upload file").SetInputFilesAsync(new FilePayload<br/>
    /// {<br/>
    ///     Name = "file.txt",<br/>
    ///     MimeType = "text/plain",<br/>
    ///     Buffer = System.Text.Encoding.UTF8.GetBytes("this is a test"),<br/>
    /// });
    /// </code>
    /// <para>**Details**</para>
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
    /// <para>Perform a tap gesture on the element matching the locator.</para>
    /// <para>**Details**</para>
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

    /// <summary><para>Returns the <a href="https://developer.mozilla.org/en-US/docs/Web/API/Node/textContent"><c>node.textContent</c></a>.</para></summary>
    /// <param name="options">Call options</param>
    Task<string?> TextContentAsync(LocatorTextContentOptions? options = default);

    /// <summary>
    /// <para>
    /// Focuses the element, and then sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>,
    /// and <c>keyup</c> event for each character in the text.
    /// </para>
    /// <para>To press a special key, like <c>Control</c> or <c>ArrowDown</c>, use <see cref="ILocator.PressAsync"/>.</para>
    /// <para>**Usage**</para>
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
    /// <para>Ensure that checkbox or radio element is unchecked.</para>
    /// <para>**Usage**</para>
    /// <code>await page.GetByRole(AriaRole.Checkbox).UncheckAsync();</code>
    /// <para>**Details**</para>
    /// <para>This method unchecks the element by performing the following steps:</para>
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
    /// <para>**Usage**</para>
    /// <code>
    /// var orderSent = page.Locator("#order-sent");<br/>
    /// orderSent.WaitForAsync();
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task WaitForAsync(LocatorWaitForOptions? options = default);
}

#nullable disable
