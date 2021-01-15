/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Copyright (c) 2020 Stafford Williams
 * Modifications copyright (c) Microsoft Corporation.
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
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// It represents an in-page DOM element.
    /// </summary>
    public interface IElementHandle : IJSHandle
    {
        /// <summary>
        /// Focuses the element, and then uses <see cref="IKeyboard.DownAsync(string)"/> and <see cref="IKeyboard.UpAsync(string)"/>.
        /// </summary>
        /// <param name="key">Name of key to press, such as <c>ArrowLeft</c>. See <see cref="KeyDefinitions"/> for a list of all key names.</param>
        /// <param name="delay">Time to wait between <c>keydown</c> and <c>keyup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <remarks>
        /// If <c>key</c> is a single character and no modifier keys besides <c>Shift</c> are being held down, a <c>keypress</c>/<c>input</c> event will also be generated.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task PressAsync(string key, int delay = 0, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// This method waits for actionability checks, then focuses the element and selects all its text content.
        /// </summary>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the text is selected or timeout.</returns>
        Task SelectTextAsync(int? timeout = null);

        /// <summary>
        /// Under the hood, it creates an instance of an event based on the given type, initializes it with eventInit properties and dispatches it on the element.
        /// Events are composed, cancelable and bubble by default.
        /// </summary>
        /// <param name="type">DOM event type: "click", "dragstart", etc.</param>
        /// <param name="eventInit">Event-specific initialization properties.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the event was dispatched.</returns>
        Task DispatchEventAsync(string type, object eventInit = null, int? timeout = null);

        /// <summary>
        /// Returns element attribute value.
        /// </summary>
        /// <param name="name">Attribute name to get the value for.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the value or the attribute.</returns>
        Task<string> GetAttributeAsync(string name, int? timeout = null);

        /// <summary>
        /// Resolves to the element.innerHTML.
        /// </summary>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the innerHTML of the element.</returns>
        Task<string> GetInnerHtmlAsync(int? timeout = null);

        /// <summary>
        /// Resolves to the element.innerText.
        /// </summary>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the innerText of the element.</returns>
        Task<string> GetInnerTextAsync(int? timeout = null);

        /// <summary>
        /// Resolves to the element.textContent.
        /// </summary>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the textContent of the element.</returns>
        Task<string> GetTextContentAsync(int? timeout = null);

        /// <summary>
        /// Focuses the element, and sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="text">A text to type into a focused element.</param>
        /// <param name="delay">Delay between key press.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <remarks>
        /// To press a special key, like <c>Control</c> or <c>ArrowDown</c> use <see cref="IElementHandle.PressAsync(string, int, int?, bool?)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// elementHandle.TypeAsync("#mytextarea", "Hello"); // Types instantly
        /// elementHandle.TypeAsync("#mytextarea", "World", new TypeOptions { Delay = 100 }); // Types slower, like a user
        /// </code>
        /// An example of typing into a text field and then submitting the form:
        /// <code>
        /// var elementHandle = await page.GetElementAsync("input");
        /// await elementHandle.TypeAsync("some text");
        /// await elementHandle.PressAsync("Enter");
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task TypeAsync(string text, int delay = 0, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Takes a screenshot of the element.
        /// </summary>
        /// <param name="path">The file path to save the image to.
        ///  The screenshot type will be inferred from file extension.
        /// If path is a relative path, then it is resolved relative to current working directory.
        /// If no path is provided, the image won't be saved to the disk.</param>
        /// <param name="omitBackground">Hides default white background and allows capturing screenshots with transparency. Defaults to <c>false</c>.</param>
        /// <param name="type">Specify screenshot type, can be either jpeg or png. Defaults to 'png'.</param>
        /// <param name="quality">The quality of the image, between 0-100. Not applicable to png images.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="t:byte[]"/>.
        /// </returns>
        Task<byte[]> ScreenshotAsync(
            string path = null,
            bool omitBackground = false,
            ScreenshotFormat? type = null,
            int? quality = null,
            int? timeout = null);

        /// <summary>
        /// Focuses the element and triggers an `input` event after filling.
        /// If element is not a text `&lt;input&gt;`, `&lt;textarea&gt;` or `[contenteditable]` element, the method throws an error.
        /// </summary>
        /// <param name="value">Value to set for the `&lt;input&gt;`, `&lt;textarea&gt;` or `[contenteditable]` element.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <returns>A <see cref="Task"/> that completes when the fill action is done.</returns>
        Task FillAsync(string value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Content frame for element handles referencing iframe nodes, or null otherwise.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the frame is resolved, yielding element's parent <see cref="IFrame" />.</returns>
        Task<IFrame> GetContentFrameAsync();

        /// <summary>
        /// Scrolls element into view if needed, and then uses <see cref="IPage.Mouse"/> to hover over the center of the element.
        /// </summary>
        /// <param name="modifiers">Modifier keys to press. Ensures that only these modifiers are pressed during the click, and then restores current modifiers back. If not specified, currently pressed modifiers are used.</param>
        /// <param name="position">A point to click relative to the top-left corner of element padding box. If not specified, clicks to some visible point of the element.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully hovered.</returns>
        Task HoverAsync(
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false);

        /// <summary>
        /// Tries to scroll element into view, unless it is completely visible as defined by <see href="https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API"/>'s <b>ratio</b>.
        /// </summary>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully scrolled into view.</returns>
        Task ScrollIntoViewIfNeededAsync(int? timeout = null);

        /// <summary>
        /// Returns the frame containing the given element.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the frame is resolved, yielding element's owner <see cref="IFrame" />.</returns>
        Task<IFrame> GetOwnerFrameAsync();

        /// <summary>
        /// Gets the bounding box of the element (relative to the main frame), or null if the element is not visible.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the <see cref="Rect"/> is resolved, yielding element's <see cref="Rect"/>.</returns>
        Task<Rect> GetBoundingBoxAsync();

        /// <summary>
        /// Scrolls element into view if needed, and then uses <see cref="IPage.Mouse"/> to click in the center of the element.
        /// </summary>
        /// <param name="delay">Time to wait between <c>mousedown</c> and <c>mouseup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="button">Button to click. Details to <see cref="MouseButton.Left"/>.</param>
        /// <param name="clickCount">Click count. Defaults to 1.</param>
        /// <param name="modifiers">Modifier keys to press. Ensures that only these modifiers are pressed during the click, and then restores current modifiers back. If not specified, currently pressed modifiers are used.</param>
        /// <param name="position">A point to click relative to the top-left corner of element padding box. If not specified, clicks to some visible point of the element.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task ClickAsync(
            int delay = 0,
            MouseButton button = MouseButton.Left,
            int clickCount = 1,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null);

        /// <summary>
        /// Scrolls element into view if needed, and then uses <see cref="IPage.Mouse"/> to double click in the center of the element.
        /// </summary>
        /// <param name="delay">Time to wait between <c>mousedown</c> and <c>mouseup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="button">Button to click. Details to <see cref="MouseButton.Left"/>.</param>
        /// <param name="modifiers">Modifier keys to press. Ensures that only these modifiers are pressed during the click, and then restores current modifiers back. If not specified, currently pressed modifiers are used.</param>
        /// <param name="position">A point to click relative to the top-left corner of element padding box. If not specified, clicks to some visible point of the element.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully double clicked.</returns>
        Task DblClickAsync(
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="file"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="file">The file path.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string file, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="files">File paths.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string[] files, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="file"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="file">The file payload.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(FilePayload file, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="files">File payloads.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(FilePayload[] files, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// The method runs <c>document.querySelector</c> within the element. If no element matches the selector, the return value resolve to <c>null</c>.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascript function finishes, yielding an <see cref="IElementHandle"/>.
        /// </returns>
        Task<IElementHandle> QuerySelectorAsync(string selector);

        /// <summary>
        /// The method runs <c>Array.from(document.querySelectorAll(selector))</c> within the element.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascript function finishes, yielding an array of <see cref="IElementHandle"/>.
        /// </returns>
        Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to <paramref name="pageFunction"/>.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string pageFunction);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to <paramref name="pageFunction"/>.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<JsonElement?> EvalOnSelectorAsync(string selector, string pageFunction, object arg);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the element and passes it as the first argument to <paramref name="pageFunction"/>.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAsync<T>(string selector, string pageFunction);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the element and passes it as the first argument to <paramref name="pageFunction"/>.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAsync<T>(string selector, string pageFunction, object arg);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to <paramref name="pageFunction"/>.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string pageFunction);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to <paramref name="pageFunction"/>.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string pageFunction, object arg);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the element and passes it as the first argument to <paramref name="pageFunction"/>.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAllAsync<T>(string selector, string pageFunction);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the element and passes it as the first argument to <paramref name="pageFunction"/>.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAllAsync<T>(string selector, string pageFunction, object arg);

        /// <summary>
        /// Calls focus on the element.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task FocusAsync();

        /// <summary>
        /// Triggers a change and input event once all, unselecting all the selected elements.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="value">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="value">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(SelectOption value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="value">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(IElementHandle value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string[] values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(SelectOption[] values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(IElementHandle[] values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(params string[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(params SelectOption[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(params IElementHandle[] values);

        /// <summary>
        /// This method fetches an element with selector, if element is not already checked, it scrolls it into view if needed, and then uses <see cref="IPage.ClickAsync(string, int, MouseButton, int, Modifier[], Point?, int?, bool, bool?)"/> to click in the center of the element.
        /// If there's no element matching selector, the method waits until a matching element appears in the DOM.
        /// If the element is detached during the actionability checks, the action is retried.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully clicked.</returns>
        Task CheckAsync(int? timeout = null, bool force = false, bool? noWaitAfter = null);

        /// <summary>
        /// This method fetches an element with selector, if element is not already unchecked, it scrolls it into view if needed, and then uses <see cref="IPage.ClickAsync(string, int, MouseButton, int, Modifier[], Point?, int?, bool, bool?)"/> to click in the center of the element.
        /// If there's no element matching selector, the method waits until a matching element appears in the DOM.
        /// If the element is detached during the actionability checks, the action is retried.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully clicked.</returns>
        Task UncheckAsync(int? timeout = null, bool force = false, bool? noWaitAfter = null);

        /// <summary>
        /// Waits for a selector to be added to the DOM.
        /// </summary>
        /// <param name="selector">A selector of an element to wait for, relative to the <see cref="IElementHandle"/>.</param>
        /// <param name="state">Wait for element to become in the specified state.</param>
        /// <param name="timeout">
        /// Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when element specified by selector string is added to DOM, yielding the <see cref="IElementHandle"/> to wait for.
        /// Resolves to `null` if waiting for `hidden: true` and selector is not found in DOM.</returns>
        Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForState? state = null, int? timeout = null);

        /// <summary>
        /// Depending on the state parameter, this method waits for one of the actionability checks to pass.
        /// This method throws when the element is detached while waiting, unless waiting for the "hidden" state.
        /// </summary>
        /// <param name="state">State to wait for.</param>
        /// <param name="timeout">
        /// Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the wait condition matches or timed out.</returns>
        Task WaitForElementStateAsync(ElementState state, int? timeout = null);

        /// <summary>
        /// <para>This method taps the element by performing the following steps:</para>
        /// <para>Wait for actionability checks on the element, unless force option is set.
        /// Scroll the element into view if needed.
        /// Use page.touchscreen to tap in the center of the element, or the specified position.
        /// Wait for initiated navigations to either succeed or fail, unless noWaitAfter option is set.
        /// If the element is detached from the DOM at any moment during the action, this method rejects.
        /// </para>
        /// <para>When all steps combined have not finished during the specified timeout, this method rejects with a TimeoutError.
        /// Passing zero timeout disables this.</para>
        /// </summary>
        /// <param name="position">A point to tap relative to the top-left corner of element padding box. If not specified, taps some visible point of the element.</param>
        /// <param name="modifiers">Modifier keys to press. Ensures that only these modifiers are pressed during the tap, and then restores current modifiers back. If not specified, currently pressed modifiers are used.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout. The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <param name="force">Whether to bypass the actionability checks. Defaults to false.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and
        /// for pages to start loading. You can opt out of waiting via setting this flag.
        /// You would only need this option in the exceptional cases such as navigating to inaccessible pages.
        /// Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task TapAsync(Point? position = null, Modifier[] modifiers = null, int? timeout = null, bool force = false, bool? noWaitAfter = null);

        /// <summary>
        /// Returns whether the element is checked. Throws if the element is not a checkbox or radio input.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsCheckedAsync();

        /// <summary>
        /// Returns whether the element is disabled, the opposite of <see cref="IsEnabledAsync"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsDisabledAsync();

        /// <summary>
        /// Returns whether the element is editable.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsEditableAsync();

        /// <summary>
        /// Returns whether the element is enabled.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsEnabledAsync();

        /// <summary>
        /// Returns whether the element is hidden, the opposite of <see cref="IsVisibleAsync"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsHiddenAsync();

        /// <summary>
        /// Returns whether the element is visible.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsVisibleAsync();
    }
}
