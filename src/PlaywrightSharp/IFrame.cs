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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// At every point of time, page exposes its current frame tree via the <see cref="IPage.MainFrame"/> and <see cref="IFrame.ChildFrames"/> methods.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// var frame = page.Frames.FirstOrDefault(frame => frame.Name == "myframe");
    /// var text = await frame.QuerySelectorEvalAsync(".selector", "element => element.textContent");
    /// Console.WriteLine(text);
    /// ]]>
    /// </code>
    /// </example>
    public interface IFrame
    {
        /// <summary>
        /// Child frames of the this frame.
        /// </summary>
        IFrame[] ChildFrames { get; }

        /// <summary>
        /// Gets the frame's name attribute as specified in the tag.
        /// If the name is empty, returns the id attribute instead.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the frame's url.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets the parent <see cref="IFrame"/>, if any. Detached frames and main frames return <c>null</c>.
        /// </summary>
        IFrame ParentFrame { get; }

        /// <summary>
        /// Gets a value indicating if the frame is detached or not.
        /// </summary>
        bool IsDetached { get; }

        /// <summary>
        /// Owner page.
        /// </summary>
        IPage Page { get; }

        /// <summary>
        /// This is an inverse of <see cref="IElementHandle.GetContentFrameAsync"/>. Note that returned handle actually belongs to the parent frame.
        /// This method throws an error if the frame has been detached before frameElement() returns.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the element is resolved, yielding the corresponding <see cref="IElementHandle"/>.</returns>
        Task<IElementHandle> GetFrameElementAsync();

        /// <summary>
        /// Returns page's title.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the action completed, yielding the frame's title.</returns>
        /// <seealso cref="IPage.GetTitleAsync"/>
        Task<string> GetTitleAsync();

        /// <summary>
        /// Navigates to an URL.
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="referer">Referer header value. If provided it will take prefrence over the referer header value set by <see cref="IPage.SetExtraHTTPHeadersAsync(System.Collections.Generic.Dictionary{string, string})"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IPage.GoToAsync(string, LifecycleEvent?, string, int?)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        /// <para/>
        /// <see cref="IPage.GoToAsync(string, LifecycleEvent?, string, int?)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        /// <para/>
        /// NOTE <see cref="IPage.GoToAsync(string, LifecycleEvent?, string, int?)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        /// <para/>
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        /// <para/>
        /// Shortcut for <see cref="IFrame.GoToAsync(string, LifecycleEvent?, string, int?)"/>.
        /// </remarks>
        Task<IResponse> GoToAsync(string url, LifecycleEvent? waitUntil = null, string referer = null, int? timeout = null);

        /// <summary>
        /// Sets the HTML markup to the frame.
        /// </summary>
        /// <param name="html">HTML markup to assign to the page.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>A <see cref="Task"/> that completes when the javascript code executing injected the HTML finishes.</returns>
        /// <seealso cref="IPage.SetContentAsync(string, LifecycleEvent?, int?)"/>
        Task SetContentAsync(string html, LifecycleEvent? waitUntil = null, int? timeout = null);

        /// <summary>
        /// Gets the full HTML contents of the page, including the doctype.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the html content is retrieved, yielding the HTML content.</returns>
        Task<string> GetContentAsync();

        /// <summary>
        /// Adds a <c><![CDATA[<script>]]></c> tag into the frame with the desired url or content.
        /// </summary>
        /// <param name="url">URL of a script to be added.</param>
        /// <param name="path">Path to the JavaScript file to be injected into frame. If path is a relative path, then it is resolved relative to current working directory.</param>
        /// <param name="content">Raw JavaScript content to be injected into frame.</param>
        /// <param name="type">Script type. Use 'module' in order to load a Javascript ES6 module.</param>
        /// <remarks>
        /// Shortcut for <c>page.MainFrame.AddScriptTagAsync(options)</c>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the tag is added, yielding the added tag as an <see cref="IElementHandle"/> when the script's onload fires or when the script content was injected into frame.</returns>
        Task<IElementHandle> AddScriptTagAsync(string url = null, string path = null, string content = null, string type = null);

        /// <summary>
        /// Adds a <c><![CDATA[<link rel="stylesheet">]]></c> tag into the frame with the desired url or a <c><![CDATA[<link rel="stylesheet">]]></c> tag with the content.
        /// </summary>
        /// <param name="url">URL of the link tag.</param>
        /// <param name="path">Path to the CSS file to be injected into frame. If path is a relative path, then it is resolved relative to current working directory.</param>
        /// <param name="content">Raw CSS content to be injected into frame.</param>
        /// <returns>A <see cref="Task"/> that completes when the stylesheet's onload fires or when the CSS content was injected into frame, yieling the added <see cref="IElementHandle"/>.</returns>
        Task<IElementHandle> AddStyleTagAsync(string url = null, string path = null, string content = null);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object)"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string pageFunction);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync(string)"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string pageFunction);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object)"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string pageFunction, object arg);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync(string, object)"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string pageFunction, object arg);

        /// <summary>
        /// Executes a function that returns a <see cref="IJSHandle"/>.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in the frame context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string pageFunction);

        /// <summary>
        /// Executes a function that returns a <see cref="IJSHandle"/>.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in the frame context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string pageFunction, object arg);

        /// <summary>
        /// <![CDATA[
        /// This method focuses the element and triggers an input event after filling. If there's no text <input>, <textarea> or [contenteditable] element matching selector, the method throws an error.
        /// ]]>
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="value"><![CDATA[Value to fill for the <input>, <textarea> or [contenteditable] element]]></param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.</param>
        /// <returns>A <see cref="Task"/> that completes when the fill action is done.</returns>
        Task FillAsync(string selector, string value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Returns a Task that resolves after the timeout.
        /// </summary>
        /// <param name="timeout">A timeout to wait for.</param>
        /// <returns>A <see cref="Task"/> that completes when the timeout is hit.</returns>
        Task WaitForTimeoutAsync(int timeout);

        /// <summary>
        /// Waits for a selector to be added to the DOM.
        /// </summary>
        /// <param name="selector">A selector of an element to wait for.</param>
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
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="polling">An interval at which the <c>pageFunction</c> is executed. defaults to <see cref="Polling.Raf"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, Polling polling, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="polling">An interval at which the function is executed. If no value is specified will use <paramref name="polling"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, int polling, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to <c>script</c>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, object arg, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to <c>script</c>.</param>
        /// <param name="polling">An interval at which the <c>pageFunction</c> is executed. defaults to <see cref="Polling.Raf"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, object arg, Polling polling, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to <c>script</c>.</param>
        /// <param name="polling">An interval at which the function is executed. If no value is specified will use <paramref name="polling"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, object arg, int polling, int? timeout = null);

        /// <summary>
        /// Queries frame for the selector. If there's no such element within the frame, the method will resolve to <c>null</c>.
        /// </summary>
        /// <param name="selector">Selector to query frame for.</param>
        /// <returns>A <see cref="Task"/> that completes when the selector is found (or failed), yielding the <see cref="IElementHandle"/> pointing to the frame element.</returns>
        /// <seealso cref="IPage.QuerySelectorAsync(string)"/>
        Task<IElementHandle> QuerySelectorAsync(string selector);

        /// <summary>
        /// The method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascript function finishes, yielding an array of <see cref="IElementHandle"/>.
        /// </returns>
        Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the frame and passes it as the first argument to pageFunction.
        /// </summary>
        /// <param name="selector">A selector to query frame for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAllAsync(string selector, string pageFunction);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the frame and passes it as the first argument to pageFunction.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query frame for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAllAsync<T>(string selector, string pageFunction);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the frame and passes it as the first argument to pageFunction.
        /// </summary>
        /// <param name="selector">A selector to query frame for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAllAsync(string selector, string pageFunction, object arg);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the frame and passes it as the first argument to pageFunction.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query frame for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAllAsync<T>(string selector, string pageFunction, object arg);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="IPage.Mouse"/> to click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
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
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully clicked.</returns>
        Task ClickAsync(
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            int clickCount = 1,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="IPage.Mouse"/> to double click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
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
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully double clicked.</returns>
        Task DblClickAsync(
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null);

        /// <summary>
        /// This method runs document.querySelector within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAsync(string selector, string pageFunction, object arg);

        /// <summary>
        /// This method runs document.querySelector within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAsync<T>(string selector, string pageFunction, object arg);

        /// <summary>
        /// This method runs document.querySelector within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAsync(string selector, string pageFunction);

        /// <summary>
        /// This method runs document.querySelector within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAsync<T>(string selector, string pageFunction);

        /// <summary>
        /// This resolves when the frame navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var navigationTask = page.WaitForNavigationAsync();
        /// await page.ClickAsync("a.my-link");
        /// await navigationTask;
        /// ]]>
        /// </code>
        /// </example>
        Task<IResponse> WaitForNavigationAsync(LifecycleEvent? waitUntil = null, int? timeout = null);

        /// <summary>
        /// This resolves when the frame navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="url">Wait for this specific URL.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var navigationTask = page.WaitForNavigationAsync();
        /// await page.ClickAsync("a.my-link");
        /// await navigationTask;
        /// ]]>
        /// </code>
        /// </example>
        Task<IResponse> WaitForNavigationAsync(string url, LifecycleEvent? waitUntil = null, int? timeout = null);

        /// <summary>
        /// This resolves when the frame navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="url">Wait for this specific URL Regex.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var navigationTask = page.WaitForNavigationAsync();
        /// await page.ClickAsync("a.my-link");
        /// await navigationTask;
        /// ]]>
        /// </code>
        /// </example>
        Task<IResponse> WaitForNavigationAsync(Regex url, LifecycleEvent? waitUntil = null, int? timeout = null);

        /// <summary>
        /// This resolves when the frame navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="url">Wait for this specific URL that matches the function condition.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var navigationTask = page.WaitForNavigationAsync();
        /// await page.ClickAsync("a.my-link");
        /// await navigationTask;
        /// ]]>
        /// </code>
        /// </example>
        Task<IResponse> WaitForNavigationAsync(Func<string, bool> url, LifecycleEvent? waitUntil = null, int? timeout = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/> and focuses it.
        /// </summary>
        /// <param name="selector">A selector to search for element to focus. If there are multiple elements satisfying the selector, the first will be focused.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the the element matching <paramref name="selector"/> is successfully focused.</returns>
        Task FocusAsync(string selector, int? timeout = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="Mouse"/> to hover over the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to hover. If there are multiple elements satisfying the selector, the first will be hovered.</param>
        /// <param name="position">A point to hover relative to the top-left corner of element padding box. If not specified, hovers over some visible point of the element.</param>
        /// <param name="modifiers">Modifier keys to press. Ensures that only these modifiers are pressed during the hover, and then restores current modifiers back. If not specified, currently pressed modifiers are used.</param>
        /// <param name="force">Whether to bypass the actionability checks. Defaults to false.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully hovered.</returns>
        Task HoverAsync(
            string selector,
            Point? position = null,
            Modifier[] modifiers = null,
            bool force = false,
            int? timeout = null);

        /// <summary>
        /// Sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="text">A text to type into a focused element.</param>
        /// <param name="delay">Time to wait between <c>keydown</c> and <c>keyup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// To press a special key, like <c>Control</c> or <c>ArrowDown</c> use <see cref="IKeyboard.PressAsync(string, int)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// await page.TypeAsync("#mytextarea", "Hello"); // Types instantly
        /// await page.TypeAsync("#mytextarea", "World", new TypeOptions { Delay = 100 }); // Types slower, like a user
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the type message is confirmed by the browser.</returns>
        Task TypeAsync(string selector, string text, int delay = 0, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Focuses the element, and then sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="key">Name of the key to press or a character to generate, such as `ArrowLeft or `a`.</param>
        /// <param name="delay">Time to wait between <c>keydown</c> and <c>keyup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the type message is confirmed by the browser.</returns>
        Task PressAsync(string selector, string key, int delay = 0, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="file"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="file">The file path.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string selector, string file, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="files">File paths.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string selector, string[] files, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="file"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="file">The file payload.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string selector, FilePayload file, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="files">File payloads.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string selector, FilePayload[] files, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Completes when the page reaches a required load state, load by default.
        /// The navigation can be in progress when it is called.
        /// If navigation is already at a required state, completes immediately.
        /// </summary>
        /// <param name="state">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>A <see cref="Task"/> that completes when the load is completed.</returns>
        Task WaitForLoadStateAsync(LifecycleEvent state = LifecycleEvent.Load, int? timeout = null);

        /// <summary>
        /// Triggers a change and input event once all, unselecting all the selected elements.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="value">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, string value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="value">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, SelectOption value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="value">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, IElementHandle value, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, string[] values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, SelectOption[] values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, IElementHandle[] values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, params string[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, params SelectOption[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, params IElementHandle[] values);

        /// <summary>
        /// This method fetches an element with selector, if element is not already checked, it scrolls it into view if needed, and then uses <see cref="IPage.ClickAsync(string, int, MouseButton, int, Modifier[], Point?, int?, bool, bool?)"/> to click in the center of the element.
        /// If there's no element matching selector, the method waits until a matching element appears in the DOM.
        /// If the element is detached during the actionability checks, the action is retried.
        /// </summary>
        /// <param name="selector">A selector to search for element to check. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully clicked.</returns>
        Task CheckAsync(string selector, int? timeout = null, bool force = false, bool? noWaitAfter = null);

        /// <summary>
        /// This method fetches an element with selector, if element is not already unchecked, it scrolls it into view if needed, and then uses <see cref="IPage.ClickAsync(string, int, MouseButton, int, Modifier[], Point?, int?, bool, bool?)"/> to click in the center of the element.
        /// If there's no element matching selector, the method waits until a matching element appears in the DOM.
        /// If the element is detached during the actionability checks, the action is retried.
        /// </summary>
        /// <param name="selector">A selector to search for element to unchecked. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="force">Whether to pass the actionability checks.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully clicked.</returns>
        Task UncheckAsync(string selector, int? timeout = null, bool force = false, bool? noWaitAfter = null);

        /// <summary>
        /// Under the hood, it creates an instance of an event based on the given type, initializes it with eventInit properties and dispatches it on the element.
        /// Events are composed, cancelable and bubble by default.
        /// </summary>
        /// <param name="selector">A selector to search for element to use. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="type">DOM event type: "click", "dragstart", etc.</param>
        /// <param name="eventInit">Event-specific initialization properties.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the event was dispatched.</returns>
        Task DispatchEventAsync(string selector, string type, object eventInit = null, int? timeout = null);

        /// <summary>
        /// Returns element attribute value.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="name">Attribute name to get the value for.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the value or the attribute.</returns>
        Task<string> GetAttributeAsync(string selector, string name, int? timeout = null);

        /// <summary>
        /// Resolves to the element.innerHTML.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the innerHTML of the element.</returns>
        Task<string> GetInnerHtmlAsync(string selector, int? timeout = null);

        /// <summary>
        /// Resolves to the element.innerText.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the innerText of the element.</returns>
        Task<string> GetInnerTextAsync(string selector, int? timeout = null);

        /// <summary>
        /// Resolves to the element.textContent.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the attribute was evaluated (or timeout), yielding the textContent of the element.</returns>
        Task<string> GetTextContentAsync(string selector, int? timeout = null);

        /// <summary>
        /// This method taps an element matching selector by performing the following steps:
        /// 1. Find an element match matching selector.If there is none, wait until a matching element is attached to the DOM.
        /// 2. Wait for actionability checks on the matched element, unless force option is set.If the element is detached during the checks, the whole action is retried.
        /// 3. Scroll the element into view if needed.
        /// 4. Use page.touchscreen to tap the center of the element, or the specified position.
        /// 5. Wait for initiated navigations to either succeed or fail, unless noWaitAfter option is set.
        /// </summary>
        /// <param name="selector">A selector to search for element to tap. If there are multiple elements satisfying the selector, the first will be tapped. See working with selectors for more details.</param>
        /// <param name="modifiers">Modifier keys to press. Ensures that only these modifiers are pressed during the tap, and then restores current modifiers back. If not specified, currently pressed modifiers are used.</param>
        /// <param name="position">A point to tap relative to the top-left corner of element padding box. If not specified, taps some visible point of the element.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout. The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <param name="force">Whether to bypass the actionability checks. Defaults to false.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and
        /// for pages to start loading. You can opt out of waiting via setting this flag.
        /// You would only need this option in the exceptional cases such as navigating to inaccessible pages.
        /// Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when all steps combined succeeded. Or, if they have not finished during the specified timeout, this method rejects with a TimeoutError.</returns>
        Task TapAsync(string selector, Modifier[] modifiers = null, Point? position = null, int? timeout = null, bool force = false, bool? noWaitAfter = null);

        /// <summary>
        /// Returns whether the element is checked. Throws if the element is not a checkbox or radio input.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsCheckedAsync(string selector, int? timeout = null);

        /// <summary>
        /// Returns whether the element is disabled, the opposite of <see cref="IsEnabledAsync"/>.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsDisabledAsync(string selector, int? timeout = null);

        /// <summary>
        /// Returns whether the element is editable.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsEditableAsync(string selector, int? timeout = null);

        /// <summary>
        /// Returns whether the element is enabled.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsEnabledAsync(string selector, int? timeout = null);

        /// <summary>
        /// Returns whether the element is hidden, the opposite of <see cref="IsVisibleAsync"/>.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsHiddenAsync(string selector, int? timeout = null);

        /// <summary>
        /// Returns whether the element is visible.
        /// </summary>
        /// <param name="selector">A selector to search for an element. If there are multiple elements satisfying the selector, the first will be picked.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task<bool> IsVisibleAsync(string selector, int? timeout = null);
    }
}
