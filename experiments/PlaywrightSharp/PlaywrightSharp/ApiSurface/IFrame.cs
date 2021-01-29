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
	/// At every point of time, page exposes its current frame tree via the <see cref="IPage.MainFrame"/> and 
	/// <see cref="IFrame.ChildFrames"/> methods.
	/// <see cref="IFrame"/> object's lifecycle is controlled by three events, dispatched on the page object:
	/// <list>
	/// <item><description><see cref="IPage.OnFrameattached"/> - fired when the frame gets attached to the page. A Frame can be attached
	/// to the page only once.</description></item>
	/// <item><description><see cref="IPage.OnFramenavigated"/> - fired when the frame commits navigation to a different URL.
	/// </description></item>
	/// <item><description><see cref="IPage.OnFramedetached"/> - fired when the frame gets detached from the page.  A Frame can be
	/// detached from the page only once.</description></item>
	/// </list>
	/// An example of dumping frame tree:
	/// </summary>
	public interface IFrame
	{
		/// <summary>
		/// Returns the ElementHandle pointing to the frame element.
		/// The method finds an element matching the specified selector within the frame. See <a href="./selectors.md">Working with selectors</a> for
		/// more details. If no elements match the selector, returns `null`.
		/// </summary>
		Task<IElementHandle> QuerySelectorAsync(string selector);
		/// <summary>
		/// Returns the ElementHandles pointing to the frame elements.
		/// The method finds all elements matching the specified selector within the frame. See <a href="./selectors.md">Working with selectors</a> for
		/// more details. If no elements match the selector, returns empty array.
		/// </summary>
		Task<dynamic> QuerySelectorAllAsync(string selector);
		/// <summary>
		/// Returns the return value of {PARAM}
		/// The method finds an element matching the specified selector within the frame and passes it as a first argument to {PARAM}.
		/// See <a href="./selectors.md">Working with selectors</a> for more details. If no elements match the selector, the method throws
		/// an error.
		/// If {PARAM} returns a [Promise], then `frame.$eval` would wait for the promise to resolve and return its value.
		/// Examples:
		/// </summary>
		Task<T> EvalOnSelectorAsync<T>(string selector, EvaluationArgument arg);
		/// <summary>
		/// Returns the return value of {PARAM}
		/// The method finds all elements matching the specified selector within the frame and passes an array of matched elements as
		/// a first argument to {PARAM}. See <a href="./selectors.md">Working with selectors</a> for more details.
		/// If {PARAM} returns a [Promise], then `frame.$eval` would wait for the promise to resolve and return its value.
		/// Examples:
		/// </summary>
		Task<T> EvalOnSelectorAllAsync<T>(string selector, EvaluationArgument arg);
		/// <summary>
		/// Returns the added tag when the script's onload fires or when the script content was injected into frame.
		/// Adds a `<script>` tag into the page with the desired url or content.
		/// </summary>
		Task<IElementHandle> AddScriptTagAsync(string content, path path, string type, string url);
		/// <summary>
		/// Returns the added tag when the stylesheet's onload fires or when the CSS content was injected into frame.
		/// Adds a `<link rel="stylesheet">` tag into the page with the desired url or a `<style type="text/css">` tag with the content.
		/// </summary>
		Task<IElementHandle> AddStyleTagAsync(string content, path path, string url);
		/// <summary>
		/// This method checks an element matching {PARAM} by performing the following steps:
		/// <list>
		/// <item><description>Find an element match matching {PARAM}. If there is none, wait until a matching element is attached to the DOM.</description>
		/// </item>
		/// <item><description>Ensure that matched element is a checkbox or a radio input. If not, this method rejects. If the element is already checked, this method returns immediately.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the matched element, unless {OPTION}
		/// option is set. If the element is detached during the checks, the whole action is retried.</description>
		/// </item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// <item><description>Ensure that the element is now checked. If not, this method rejects.</description></item>
		/// </list>
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task CheckAsync(string selector, bool force, bool noWaitAfter, float timeout);
		dynamic GetChildFrames();
		/// <summary>
		/// This method clicks an element matching {PARAM} by performing the following steps:
		/// <list>
		/// <item><description>Find an element match matching {PARAM}. If there is none, wait until a matching element is attached to the DOM.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the matched element, unless {OPTION}
		/// option is set. If the element is detached during the checks, the whole action is retried.</description>
		/// </item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// </list>
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task ClickAsync(string selector, Button button, int clickCount, float delay, bool force, Modifiers[] modifiers, bool noWaitAfter, object position, float timeout);
		/// <summary>
		/// Gets the full HTML contents of the frame, including the doctype.
		/// </summary>
		Task<string> GetContentAsync();
		/// <summary>
		/// This method double clicks an element matching {PARAM} by performing the following steps:
		/// <list>
		/// <item><description>Find an element match matching {PARAM}. If there is none, wait until a matching element is attached to the DOM.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the matched element, unless {OPTION}
		/// option is set. If the element is detached during the checks, the whole action is retried.</description>
		/// </item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to double click in the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set. Note that if the first click of the `dblclick()` triggers a navigation event, this method will reject.</description>
		/// </item>
		/// </list>
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task DblclickAsync(string selector, Button button, float delay, bool force, Modifiers[] modifiers, bool noWaitAfter, object position, float timeout);
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
		Task DispatchEventAsync(string selector, string type, EvaluationArgument eventInit, float timeout);
		/// <summary>
		/// Returns the return value of {PARAM}
		/// If the function passed to the <see cref="IFrame.EvaluateAsync"/> returns a [Promise], then <see cref="IFrame.EvaluateAsync"/> would
		/// wait for the promise to resolve and return its value.
		/// If the function passed to the <see cref="IFrame.EvaluateAsync"/> returns a non-[Serializable] value, then <see cref="IFrame.EvaluateAsync"/> returns
		/// `undefined`. DevTools Protocol also supports transferring some additional values that are not serializable by `JSON`: `-0`,
		/// `NaN`, `Infinity`, `-Infinity`, and bigint literals.
		/// A string can also be passed in instead of a function.
		/// <see cref="IElementHandle"/> instances can be passed as an argument to the <see cref="IFrame.EvaluateAsync"/>:
		/// </summary>
		Task<T> EvaluateAsync<T>(EvaluationArgument arg);
		/// <summary>
		/// Returns the return value of {PARAM} as in-page object (JSHandle).
		/// The only difference between <see cref="IFrame.EvaluateAsync"/> and <see cref="IFrame.EvaluateHandleAsync"/> is that 
		/// [method: Frame.evaluateHandle`] returns in-page object (JSHandle).
		/// If the function, passed to the <see cref="IFrame.EvaluateHandleAsync"/>, returns a [Promise], then <see cref="IFrame.EvaluateHandleAsync"/> would
		/// wait for the promise to resolve and return its value.
		/// A string can also be passed in instead of a function.
		/// <see cref="IJSHandle"/> instances can be passed as an argument to the <see cref="IFrame.EvaluateHandleAsync"/>:
		/// </summary>
		Task<IJSHandle> EvaluateHandleAsync(EvaluationArgument arg);
		/// <summary>
		/// This method waits for an element matching {PARAM}, waits for <a href="./actionability.md">actionability</a> checks, focuses
		/// the element, fills it and triggers an `input` event after filling. If the element matching {PARAM} is not an `
		/// <input>`, `<textarea>` or `[contenteditable]` element, this method throws an error. Note that you can pass an empty string to clear the input field.
		/// To send fine-grained keyboard events, use <see cref="IFrame.TypeAsync"/>.
		/// </summary>
		Task FillAsync(string selector, string value, bool noWaitAfter, float timeout);
		/// <summary>
		/// This method fetches an element with {PARAM} and focuses it. If there's no element matching {PARAM}, the method waits until
		/// a matching element appears in the DOM.
		/// </summary>
		Task FocusAsync(string selector, float timeout);
		/// <summary>
		/// Returns the `frame` or `iframe` element handle which corresponds to this frame.
		/// This is an inverse of <see cref="IElementHandle.ContentFrameAsync"/>. Note that returned handle actually belongs to the parent
		/// frame.
		/// This method throws an error if the frame has been detached before `frameElement()` returns.
		/// </summary>
		Task<IElementHandle> GetFrameElementAsync();
		/// <summary>
		/// Returns element attribute value.
		/// </summary>
		Task<string> GetAttributeAsync(string selector, string name, float timeout);
		/// <summary>
		/// Returns the main resource response. In case of multiple redirects, the navigation will resolve with the response of the last
		/// redirect.
		/// `frame.goto` will throw an error if:
		/// <list>
		/// <item><description>there's an SSL error (e.g. in case of self-signed certificates).</description></item>
		/// <item><description>target URL is invalid.</description></item>
		/// <item><description>the {OPTION} is exceeded during navigation.</description></item>
		/// <item><description>the remote server does not respond or is unreachable.</description></item>
		/// <item><description>the main resource failed to load.</description></item>
		/// </list>
		/// `frame.goto` will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not
		/// Found" and 500 "Internal Server Error".  The status code for such responses can be retrieved by calling 
		/// <see cref="IResponse.Status"/>.
		/// </summary>
		Task<IResponse> GotoAsync(string url, string referer, float timeout, WaitUntil waitUntil);
		/// <summary>
		/// This method hovers over an element matching {PARAM} by performing the following steps:
		/// <list>
		/// <item><description>Find an element match matching {PARAM}. If there is none, wait until a matching element is attached to the DOM.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the matched element, unless {OPTION}
		/// option is set. If the element is detached during the checks, the whole action is retried.</description>
		/// </item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to hover over the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless `noWaitAfter` option is set.</description>
		/// </item>
		/// </list>
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task HoverAsync(string selector, bool force, Modifiers[] modifiers, object position, float timeout);
		/// <summary>
		/// Returns `element.innerHTML`.
		/// </summary>
		Task<string> InnerHTMLAsync(string selector, float timeout);
		/// <summary>
		/// Returns `element.innerText`.
		/// </summary>
		Task<string> InnerTextAsync(string selector, float timeout);
		/// <summary>
		/// Returns whether the element is checked. Throws if the element is not a checkbox or radio input.
		/// </summary>
		Task<bool> IsCheckedAsync(string selector, float timeout);
		/// <summary>
		/// Returns `true` if the frame has been detached, or `false` otherwise.
		/// </summary>
		bool IsDetached();
		/// <summary>
		/// Returns whether the element is disabled, the opposite of <a href="./actionability.md#enabled">enabled</a>.
		/// </summary>
		Task<bool> IsDisabledAsync(string selector, float timeout);
		/// <summary>
		/// Returns whether the element is <a href="./actionability.md#editable">editable</a>.
		/// </summary>
		Task<bool> IsEditableAsync(string selector, float timeout);
		/// <summary>
		/// Returns whether the element is <a href="./actionability.md#enabled">enabled</a>.
		/// </summary>
		Task<bool> IsEnabledAsync(string selector, float timeout);
		/// <summary>
		/// Returns whether the element is hidden, the opposite of <a href="./actionability.md#visible">visible</a>.
		/// </summary>
		Task<bool> IsHiddenAsync(string selector, float timeout);
		/// <summary>
		/// Returns whether the element is <a href="./actionability.md#visible">visible</a>.
		/// </summary>
		Task<bool> IsVisibleAsync(string selector, float timeout);
		/// <summary>
		/// Returns frame's name attribute as specified in the tag.
		/// If the name is empty, returns the id attribute instead.
		/// </summary>
		string GetName();
		/// <summary>
		/// Returns the page containing this frame.
		/// </summary>
		IPage GetPage();
		/// <summary>
		/// Parent frame, if any. Detached frames and main frames return `null`.
		/// </summary>
		IFrame GetParentFrame();
		/// <summary>
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
		Task PressAsync(string selector, string key, float delay, bool noWaitAfter, float timeout);
		/// <summary>
		/// Returns the array of option values that have been successfully selected.
		/// Triggers a `change` and `input` event once all the provided options have been selected. If there's no `
		/// <select>` element matching {PARAM}, the method throws an error.
		/// Will wait until all specified options are present in the `<select>` element.
		/// </summary>
		Task<dynamic> SelectOptionAsync(string selector, bool noWaitAfter, float timeout);
		Task SetContentAsync(string html, float timeout, WaitUntil waitUntil);
		/// <summary>
		/// This method expects {PARAM} to point to an <a href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input">input element</a>.
		/// Sets the value of the file input to these file paths or files. If some of the `filePaths` are relative paths, then they are
		/// resolved relative to the the current working directory. For empty array, clears the selected files.
		/// </summary>
		Task SetInputFilesAsync(string selector, path[] files, bool noWaitAfter, float timeout);
		/// <summary>
		/// This method taps an element matching {PARAM} by performing the following steps:
		/// <list>
		/// <item><description>Find an element match matching {PARAM}. If there is none, wait until a matching element is attached to the DOM.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the matched element, unless {OPTION}
		/// option is set. If the element is detached during the checks, the whole action is retried.</description>
		/// </item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Touchscreen"/> to tap the center of the element, or the specified {OPTION}.
		/// </description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// </list>
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task TapAsync(string selector, bool force, Modifiers[] modifiers, bool noWaitAfter, object position, float timeout);
		/// <summary>
		/// Returns `element.textContent`.
		/// </summary>
		Task<string> TextContentAsync(string selector, float timeout);
		/// <summary>
		/// Returns the page title.
		/// </summary>
		Task<string> GetTitleAsync();
		/// <summary>
		/// Sends a `keydown`, `keypress`/`input`, and `keyup` event for each character in the text. `frame.type` can be used to send
		/// fine-grained keyboard events. To fill values in form fields, use <see cref="IFrame.FillAsync"/>.
		/// To press a special key, like `Control` or `ArrowDown`, use <see cref="IKeyboard.PressAsync"/>.
		/// </summary>
		Task TypeAsync(string selector, string text, float delay, bool noWaitAfter, float timeout);
		/// <summary>
		/// This method checks an element matching {PARAM} by performing the following steps:
		/// <list>
		/// <item><description>Find an element match matching {PARAM}. If there is none, wait until a matching element is attached to the DOM.</description>
		/// </item>
		/// <item><description>Ensure that matched element is a checkbox or a radio input. If not, this method rejects. If the element is already unchecked, this method returns immediately.</description>
		/// </item>
		/// <item><description>Wait for <a href="./actionability.md">actionability</a> checks on the matched element, unless {OPTION}
		/// option is set. If the element is detached during the checks, the whole action is retried.</description>
		/// </item>
		/// <item><description>Scroll the element into view if needed.</description></item>
		/// <item><description>Use <see cref="IPage.Mouse"/> to click in the center of the element.</description></item>
		/// <item><description>Wait for initiated navigations to either succeed or fail, unless {OPTION} option is set.</description>
		/// </item>
		/// <item><description>Ensure that the element is now unchecked. If not, this method rejects.</description>
		/// </item>
		/// </list>
		/// When all steps combined have not finished during the specified {OPTION}, this method rejects with a <see cref="ITimeoutError"/>.
		/// Passing zero timeout disables this.
		/// </summary>
		Task UncheckAsync(string selector, bool force, bool noWaitAfter, float timeout);
		/// <summary>
		/// Returns frame's url.
		/// </summary>
		string GetUrl();
		/// <summary>
		/// Returns when the {PARAM} returns a truthy value, returns that value.
		/// The <see cref="IFrame.WaitForFunctionAsync"/> can be used to observe viewport size change:
		/// To pass an argument to the predicate of `frame.waitForFunction` function:
		/// </summary>
		Task<IJSHandle> WaitForFunctionAsync(EvaluationArgument arg, Polling polling, float timeout);
		/// <summary>
		/// Waits for the required load state to be reached.
		/// This returns when the frame reaches a required load state, `load` by default. The navigation must have been committed when
		/// this method is called. If current document has already reached the required state, resolves immediately.
		/// </summary>
		Task WaitForLoadStateAsync(State state, float timeout);
		/// <summary>
		/// Waits for the frame navigation and returns the main resource response. In case of multiple redirects, the navigation will
		/// resolve with the response of the last redirect. In case of navigation to a different anchor or navigation due to History
		/// API usage, the navigation will resolve with `null`.
		/// This method waits for the frame to navigate to a new URL. It is useful for when you run code which will indirectly cause
		/// the frame to navigate. Consider this example:
		/// </summary>
		Task<IResponse> WaitForNavigationAsync(float timeout, Union url, WaitUntil waitUntil);
		/// <summary>
		/// Returns when element specified by selector satisfies {OPTION} option. Returns `null` if waiting for `hidden` or `detached`.
		/// Wait for the {PARAM} to satisfy {OPTION} option (either appear/disappear from dom, or become visible/hidden). If at the moment
		/// of calling the method {PARAM} already satisfies the condition, the method will return immediately. If the selector doesn't
		/// satisfy the condition for the {OPTION} milliseconds, the function will throw.
		/// This method works across navigations:
		/// </summary>
		Task<IElementHandle> WaitForSelectorAsync(string selector, State state, float timeout);
		/// <summary>
		/// Waits for the given {PARAM} in milliseconds.
		/// Note that `frame.waitForTimeout()` should only be used for debugging. Tests using the timer in production are going to be
		/// flaky. Use signals such as network events, selectors becoming visible and others instead.
		/// </summary>
		Task WaitForTimeoutAsync(float timeout);
	}
}