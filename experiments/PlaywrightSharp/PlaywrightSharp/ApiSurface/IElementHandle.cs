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
 *
 *
 * ------------------------------------------------------------------------------ 
 * <auto-generated> 
 * This code was generated by a tool at:
 * /utils/doclint/generateDotnetApi.js
 * 
 * Changes to this file may cause incorrect behavior and will be lost if 
 * the code is regenerated. 
 * </auto-generated> 
 * ------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// <seealso cref="<see cref="IJSHandle"/>"/>
	/// ElementHandle represents an in-page DOM element. ElementHandles can be created with the <see cref="IPage.QuerySelectorAsync"/> method.
	/// ElementHandle prevents DOM element from garbage collection unless the handle is disposed with <see cref="IJSHandle.DisposeAsync"/>.
	/// ElementHandles are auto-disposed when their origin frame gets navigated.
	/// ElementHandle instances can be used as an argument in <see cref="IPage.EvalOnSelectorAsync"/> and <see cref="IPage.EvaluateAsync"/> methods.
	/// </summary>
	public interface IElementHandle : IJSHandle
	{
		/// <summary>
		/// The method finds an element matching the specified selector in the `ElementHandle`'s subtree. See <a href="./selectors.md">Working with selectors</a> for
		/// more details. If no elements match the selector, returns `null`.
		/// </summary>
		Task<IElementHandle> QuerySelectorAsync(string selector);
		/// <summary>
		/// The method finds all elements matching the specified selector in the `ElementHandle`s subtree. See <a href="./selectors.md">Working with selectors</a> for
		/// more details. If no elements match the selector, returns empty array.
		/// </summary>
		Task<dynamic> QuerySelectorAllAsync(string selector);
		/// <summary>
		/// Returns the return value of {PARAM}
		/// The method finds an element matching the specified selector in the `ElementHandle`s subtree and passes it as a first argument
		/// to {PARAM}. See <a href="./selectors.md">Working with selectors</a> for more details. If no elements match the selector,
		/// the method throws an error.
		/// If {PARAM} returns a [Promise], then `frame.evalOnSelector` would wait for the promise to resolve and return its value.
		/// Examples:
		/// </summary>
		Task<T> EvalOnSelectorAsync<T>(string selector, EvaluationArgument arg);
		/// <summary>
		/// Returns the return value of {PARAM}
		/// The method finds all elements matching the specified selector in the `ElementHandle`'s subtree and passes an array of matched
		/// elements as a first argument to {PARAM}. See <a href="./selectors.md">Working with selectors</a> for more details.
		/// If {PARAM} returns a [Promise], then `frame.evalOnSelectorAll` would wait for the promise to resolve and return its value.
		/// Examples:
		/// </summary>
		Task<T> EvalOnSelectorAllAsync<T>(string selector, EvaluationArgument arg);
		/// <summary>
		/// This method returns the bounding box of the element, or `null` if the element is not visible. The bounding box is calculated
		/// relative to the main frame viewport - which is usually the same as the browser window.
		/// Scrolling affects the returned bonding box, similarly to <a href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect">Element.getBoundingClientRect</a>.
		/// That means `x` and/or `y` may be negative.
		/// Elements from child frames return the bounding box relative to the main frame, unlike the <a href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getBoundingClientRect">Element.getBoundingClientRect</a>.
		/// Assuming the page is static, it is safe to use bounding box coordinates to perform input. For example, the following snippet
		/// should click the center of the element.
		/// </summary>
		Task<ElementHandleBoundingBoxResult> GetBoundingBoxAsync();
		/// <summary>
		/// This method checks the element by performing the following steps:
		/// <list>
		/// <item><description>Ensure that element is a checkbox or a radio input. If not, this method rejects. If the element is already checked, this method returns immediately.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the element, unless {OPTION} option
		/// is set.</description></item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// <item><description>Ensure that the element is now checked. If not, this method rejects.</description></item>
		/// </list>
		/// If the element is detached from the DOM at any moment during the action, this method rejects.
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task CheckAsync(bool force, bool noWaitAfter, float timeout);
		/// <summary>
		/// This method clicks the element by performing the following steps:
		/// <list>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the element, unless {OPTION} option
		/// is set.</description></item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// </list>
		/// If the element is detached from the DOM at any moment during the action, this method rejects.
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task ClickAsync(Button button, int clickCount, float delay, bool force, Modifiers[] modifiers, bool noWaitAfter, ElementHandlePosition position, float timeout);
		/// <summary>
		/// Returns the content frame for element handles referencing iframe nodes, or `null` otherwise
		/// </summary>
		Task<IFrame> GetContentFrameAsync();
		/// <summary>
		/// This method double clicks the element by performing the following steps:
		/// <list>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the element, unless {OPTION} option
		/// is set.</description></item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to double click in the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set. Note that if the first click of the `dblclick()` triggers a navigation event, this method will reject.</description>
		/// </item>
		/// </list>
		/// If the element is detached from the DOM at any moment during the action, this method rejects.
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task DblclickAsync(Button button, float delay, bool force, Modifiers[] modifiers, bool noWaitAfter, ElementHandlePosition position, float timeout);
		/// <summary>
		/// The snippet below dispatches the `click` event on the element. Regardless of the visibility state of the elment, `click`
		/// is dispatched. This is equivalend to calling <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/click">element.click()</a>.
		/// Under the hood, it creates an instance of an event based on the given {PARAM}, initializes it with {PARAM} properties and
		/// dispatches it on the element. Events are `composed`, `cancelable` and bubble by default.
		/// Since {PARAM} is event-specific, please refer to the events documentation for the lists of initial properties:
		/// <list>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/DragEvent/DragEvent">DragEvent</a>
		/// </description></item>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/FocusEvent/FocusEvent">FocusEvent</a>
		/// </description></item>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/KeyboardEvent">KeyboardEvent</a>
		/// </description></item>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/MouseEvent/MouseEvent">MouseEvent</a>
		/// </description></item>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent/PointerEvent">PointerEvent</a>
		/// </description></item>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/TouchEvent/TouchEvent">TouchEvent</a>
		/// </description></item>
		/// <item><description><a href="https://developer.mozilla.org/en-US/docs/Web/API/Event/Event">Event</a></description>
		/// </item>
		/// </list>
		/// You can also specify `JSHandle` as the property value if you want live objects to be passed into the event:
		/// </summary>
		Task DispatchEventAsync(string type, EvaluationArgument eventInit);
		/// <summary>
		/// This method waits for <a href="./actionability.md">actionability</a> checks, focuses the element, fills it and triggers an
		/// `input` event after filling. If the element is not an `<input>`, `<textarea>` or `[contenteditable]` element, this method throws an error. Note that you can pass an empty string to clear the input field.
		/// </summary>
		Task FillAsync(string value, bool noWaitAfter, float timeout);
		/// <summary>
		/// Calls <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/focus">focus</a> on the element.
		/// </summary>
		Task FocusAsync();
		/// <summary>
		/// Returns element attribute value.
		/// </summary>
		Task<string> GetAttributeAsync(string name);
		/// <summary>
		/// This method hovers over the element by performing the following steps:
		/// <list>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the element, unless {OPTION} option
		/// is set.</description></item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to hover over the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless `noWaitAfter` option is set.</description>
		/// </item>
		/// </list>
		/// If the element is detached from the DOM at any moment during the action, this method rejects.
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task HoverAsync(bool force, Modifiers[] modifiers, ElementHandlePosition position, float timeout);
		/// <summary>
		/// Returns the `element.innerHTML`.
		/// </summary>
		Task<string> GetInnerHTMLAsync();
		/// <summary>
		/// Returns the `element.innerText`.
		/// </summary>
		Task<string> GetInnerTextAsync();
		/// <summary>
		/// Returns whether the element is checked. Throws if the element is not a checkbox or radio input.
		/// </summary>
		Task<bool> IsCheckedAsync();
		/// <summary>
		/// Returns whether the element is disabled, the opposite of <a href="./actionability.md#enabled">enabled</a>.
		/// </summary>
		Task<bool> IsDisabledAsync();
		/// <summary>
		/// Returns whether the element is <a href="./actionability.md#editable">editable</a>.
		/// </summary>
		Task<bool> IsEditableAsync();
		/// <summary>
		/// Returns whether the element is <a href="./actionability.md#enabled">enabled</a>.
		/// </summary>
		Task<bool> IsEnabledAsync();
		/// <summary>
		/// Returns whether the element is hidden, the opposite of <a href="./actionability.md#visible">visible</a>.
		/// </summary>
		Task<bool> IsHiddenAsync();
		/// <summary>
		/// Returns whether the element is <a href="./actionability.md#visible">visible</a>.
		/// </summary>
		Task<bool> IsVisibleAsync();
		/// <summary>
		/// Returns the frame containing the given element.
		/// </summary>
		Task<IFrame> GetOwnerFrameAsync();
		/// <summary>
		/// Focuses the element, and then uses <see cref="IKeyboard.DownAsync"/> and <see cref="IKeyboard.UpAsync"/>.
		/// {PARAM} can specify the intended <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key">keyboardEvent.key</a> value
		/// or a single character to generate the text for. A superset of the {PARAM} values can be found <a href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/key/Key_Values">here</a>.
		/// Examples of the keys are:
		/// `F1` - `F12`, `Digit0`- `Digit9`, `KeyA`- `KeyZ`, `Backquote`, `Minus`, `Equal`, `Backslash`, `Backspace`, `Tab`, `Delete`,
		/// `Escape`, `ArrowDown`, `End`, `Enter`, `Home`, `Insert`, `PageDown`, `PageUp`, `ArrowRight`, `ArrowUp`, etc.
		/// Following modification shortcuts are also supported: `Shift`, `Control`, `Alt`, `Meta`, `ShiftLeft`.
		/// Holding down `Shift` will type the text that corresponds to the {PARAM} in the upper case.
		/// If {PARAM} is a single character, it is case-sensitive, so the values `a` and `A` will generate different respective texts.
		/// Shortcuts such as `key: "Control+o"` or `key: "Control+Shift+T"` are supported as well. When speficied with the modifier,
		/// modifier is pressed and being held while the subsequent key is being pressed.
		/// </summary>
		Task PressAsync(string key, float delay, bool noWaitAfter, float timeout);
		/// <summary>
		/// Returns the buffer with the captured screenshot.
		/// This method waits for the <a href="./actionability.md">actionability</a> checks, then scrolls element into view before taking
		/// a screenshot. If the element is detached from DOM, the method throws an error.
		/// </summary>
		Task<byte[]> ScreenshotAsync(bool omitBackground, string path, int quality, float timeout, Type type);
		/// <summary>
		/// This method waits for <a href="./actionability.md">actionability</a> checks, then tries to scroll element into view, unless
		/// it is completely visible as defined by <a href="https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API">IntersectionObserver</a>'s
		/// `ratio`.
		/// Throws when `elementHandle` does not point to an element <a href="https://developer.mozilla.org/en-US/docs/Web/API/Node/isConnected">connected</a> to
		/// a Document or a ShadowRoot.
		/// </summary>
		Task ScrollIntoViewIfNeededAsync(float timeout);
		/// <summary>
		/// Returns the array of option values that have been successfully selected.
		/// Triggers a `change` and `input` event once all the provided options have been selected. If element is not a `
		/// <select>` element, the method throws an error.
		/// Will wait until all specified options are present in the `<select>` element.
		/// </summary>
		Task<dynamic> SelectOptionAsync(bool noWaitAfter, float timeout);
		/// <summary>
		/// This method waits for <a href="./actionability.md">actionability</a> checks, then focuses the element and selects all its
		/// text content.
		/// </summary>
		Task SelectTextAsync(float timeout);
		/// <summary>
		/// This method expects `elementHandle` to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input element</a>.
		/// Sets the value of the file input to these file paths or files. If some of the `filePaths` are relative paths, then they are
		/// resolved relative to the the current working directory. For empty array, clears the selected files.
		/// </summary>
		Task SetInputFilesAsync(string[] files, bool noWaitAfter, float timeout);
		/// <summary>
		/// This method taps the element by performing the following steps:
		/// <list>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the element, unless {OPTION} option
		/// is set.</description></item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Touchscreen"/> to tap the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// </list>
		/// If the element is detached from the DOM at any moment during the action, this method rejects.
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task TapAsync(bool force, Modifiers[] modifiers, bool noWaitAfter, ElementHandlePosition position, float timeout);
		/// <summary>
		/// Returns the `node.textContent`.
		/// </summary>
		Task<string> GetTextContentAsync();
		/// <summary>
		/// Focuses the element, and then sends a `keydown`, `keypress`/`input`, and `keyup` event for each character in the text.
		/// To press a special key, like `Control` or `ArrowDown`, use <see cref="IElementHandle.PressAsync"/>.
		/// An example of typing into a text field and then submitting the form:
		/// </summary>
		Task TypeAsync(string text, float delay, bool noWaitAfter, float timeout);
		/// <summary>
		/// This method checks the element by performing the following steps:
		/// <list>
		/// <item><description>Ensure that element is a checkbox or a radio input. If not, this method rejects. If the element is already unchecked, this method returns immediately.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the element, unless {OPTION} option
		/// is set.</description></item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// <item><description>Ensure that the element is now unchecked. If not, this method rejects.</description>
		/// </item>
		/// </list>
		/// If the element is detached from the DOM at any moment during the action, this method rejects.
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task UncheckAsync(bool force, bool noWaitAfter, float timeout);
		/// <summary>
		/// Returns when the element satisfies the {PARAM}.
		/// Depending on the {PARAM} parameter, this method waits for one of the <a href="./actionability.md">actionability</a> checks
		/// to pass. This method throws when the element is detached while waiting, unless waiting for the `"hidden"` state.
		/// <list>
		/// <item><description>`"visible"` Wait until the element is <a href="./actionability.md#visible">visible</a>.
		/// </description></item>
		/// <item><description>`"hidden"` Wait until the element is <a href="./actionability.md#visible">not visible</a> or 
		/// <a href="./actionability.md#attached">not attached</a>. Note that waiting for hidden does not throw when the element detaches.
		/// </description></item>
		/// <item><description>`"stable"` Wait until the element is both <a href="./actionability.md#visible">visible</a> and 
		/// <a href="./actionability.md#stable">stable</a>.</description></item>
		/// <item><description>`"enabled"` Wait until the element is <a href="./actionability.md#enabled">enabled</a>.
		/// </description></item>
		/// <item><description>`"disabled"` Wait until the element is <a href="./actionability.md#enabled">not enabled</a>.
		/// </description></item>
		/// <item><description>`"editable"` Wait until the element is <a href="./actionability.md#editable">editable</a>.
		/// </description></item>
		/// </list>
		/// If the element does not satisfy the condition for the {OPTION} milliseconds, this method will throw.
		/// </summary>
		Task WaitForElementStateAsync(State state, float timeout);
		/// <summary>
		/// Returns element specified by selector when it satisfies {OPTION} option. Returns `null` if waiting for `hidden` or `detached`.
		/// Wait for the {PARAM} relative to the element handle to satisfy {OPTION} option (either appear/disappear from dom, or become
		/// visible/hidden). If at the moment of calling the method {PARAM} already satisfies the condition, the method will return immediately.
		/// If the selector doesn't satisfy the condition for the {OPTION} milliseconds, the function will throw.
		/// </summary>
		Task<IElementHandle> WaitForSelectorAsync(string selector, State state, float timeout);
	}
}