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
    /// Page provides methods to interact with a single tab or extension background page in Chromium. One Browser instance might have multiple Page instances.
    /// </summary>
    /// <example>
    /// This example creates a page and navigates it to a URL:
    /// <code>
    /// <![CDATA[
    /// var context = await browser.NewContextAsync();
    /// const page = await context.NewPageAsync("https://example.com");
    /// await browser.CloseAsync();
    /// ]]>
    /// </code>
    /// </example>
    public interface IPage
    {
        /// <summary>
        /// Raised when JavaScript within the page calls one of console API methods, e.g. <c>console.log</c> or <c>console.dir</c>. Also emitted if the page throws an error or a warning.
        /// The arguments passed into <c>console.log</c> appear as arguments on the event handler.
        /// </summary>
        /// <example>
        /// An example of handling <see cref="Console"/> event:
        /// <code>
        /// <![CDATA[
        /// page.Console += (sender, e) =>
        /// {
        ///     for (var i = 0; i < e.Message.Args.Count; ++i)
        ///     {
        ///         System.Console.WriteLine($"{i}: {e.Message.Args[i]}");
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        event EventHandler<ConsoleEventArgs> Console;

        /// <summary>
        /// Emitted when the page opens a new tab or window.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var popupTargetCompletion = new TaskCompletionSource<IPage>();
        /// page.Popup += (sender, e) => popupTargetCompletion.SetResult(e.Page);
        /// await Task.WhenAll(
        ///     popupTargetCompletion.Task,
        ///     page.ClickAsync('a[target=_blank]')
        /// );
        /// ]]>
        /// </code>
        /// </example>
        event EventHandler<PopupEventArgs> Popup;

        /// <summary>
        /// Raised when a page issues a request. The <see cref="IRequest"/> object is read-only.
        /// </summary>
        event EventHandler<RequestEventArgs> Request;

        /// <summary>
        /// Raised when a <see cref="IResponse"/> is received.
        /// </summary>
        /// <example>
        /// An example of handling <see cref="IResponse"/> event:
        /// <code>
        /// <![CDATA[
        /// var tcs = new TaskCompletionSource<string>();
        /// page.Response += async(sender, e) =>
        /// {
        ///     if (e.Response.Url.Contains("script.js"))
        ///     {
        ///         tcs.TrySetResult(await e.Response.TextAsync());
        ///     }
        /// };
        ///
        /// await Task.WhenAll(
        ///     page.GoToAsync(TestConstants.ServerUrl + "/grid.html"),
        ///     tcs.Task);
        /// Console.WriteLine(await tcs.Task);
        /// ]]>
        /// </code>
        /// </example>
        event EventHandler<ResponseEventArgs> Response;

        /// <summary>
        /// Raised when a <see cref="IWebSocket"/> is sent.
        /// </summary>
        event EventHandler<WebSocketEventArgs> WebSocket;

        /// <summary>
        /// Raised when a request finishes successfully.
        /// </summary>
        event EventHandler<RequestEventArgs> RequestFinished;

        /// <summary>
        /// Raised when a request fails, for example by timing out.
        /// </summary>
        event EventHandler<RequestFailedEventArgs> RequestFailed;

        /// <summary>
        /// Raised when a JavaScript dialog appears, such as <c>alert</c>, <c>prompt</c>, <c>confirm</c> or <c>beforeunload</c>.
        /// PlaywrightSharp can respond to the dialog via <see cref="Dialog"/>'s <see cref="IDialog.AcceptAsync(string)"/> or <see cref="IDialog.DismissAsync"/> methods.
        /// </summary>
        event EventHandler<DialogEventArgs> Dialog;

        /// <summary>
        /// Raised when a frame is attached.
        /// </summary>
        event EventHandler<FrameEventArgs> FrameAttached;

        /// <summary>
        /// Raised when a frame is detached.
        /// </summary>
        event EventHandler<FrameEventArgs> FrameDetached;

        /// <summary>
        /// Raised when a frame is navigated to a new url.
        /// </summary>
        event EventHandler<FrameEventArgs> FrameNavigated;

        /// <summary>
        /// Raised when a file chooser is supposed to appear, such as after clicking the <c>&lt;input type=file&gt;</c>`. Playwright can respond to it via setting the input files using <see cref="IElementHandle.SetInputFilesAsync(FilePayload, int?, bool?)"/>.
        /// </summary>
        event EventHandler<FileChooserEventArgs> FileChooser;

        /// <summary>
        /// Raised when the JavaScript <c>load</c> <see href="https://developer.mozilla.org/en-US/docs/Web/Events/load"/> event is dispatched.
        /// </summary>
        event EventHandler Load;

        /// <summary>
        /// The JavaScript <c>DOMContentLoaded</c> <see href="https://developer.mozilla.org/en-US/docs/Web/Events/DOMContentLoaded"/> event
        /// </summary>
        event EventHandler DOMContentLoaded;

        /// <summary>
        /// Raised when the page closes.
        /// </summary>
        event EventHandler Close;

        /// <summary>
        /// Raised when the page crashes.
        /// </summary>
        event EventHandler Crash;

        /// <summary>
        /// Raised when an uncaught exception happens within the page.
        /// </summary>
        event EventHandler<PageErrorEventArgs> PageError;

        /// <summary>
        /// Raised when a dedicated WebWorker (<see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API"/>) is spawned by the page.
        /// </summary>
        event EventHandler<WorkerEventArgs> Worker;

        /// <summary>
        /// Emitted when attachment download started.
        /// User can access basic file operations on downloaded content via the passed Download instance.
        /// </summary>
        event EventHandler<DownloadEventArgs> Download;

        /// <summary>
        /// Get an indication that the page has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Page is guaranteed to have a main frame which persists during navigations.
        /// </summary>
        IFrame MainFrame { get; }

        /// <summary>
        /// Get the browser context that the page belongs to.
        /// </summary>
        IBrowserContext Context { get; }

        /// <summary>
        /// Page Viewport.
        /// </summary>
        ViewportSize ViewportSize { get; }

        /// <summary>
        /// Gets the accessibility.
        /// </summary>
        IAccessibility Accessibility { get; }

        /// <summary>
        /// Gets this page's mouse.
        /// </summary>
        IMouse Mouse { get; }

        /// <summary>
        /// Shortcut for MainFrame.Url.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets all frames attached to the page.
        /// </summary>
        IFrame[] Frames { get; }

        /// <summary>
        /// Gets this page's keyboard.
        /// </summary>
        IKeyboard Keyboard { get; }

        /// <summary>
        /// Gets this page's touchscreen.
        /// </summary>
        ITouchscreen Touchscreen { get; }

        /// <summary>
        /// This setting will change the default maximum time for all the methods accepting timeout option.
        /// </summary>
        int DefaultTimeout { get; set; }

        /// <summary>
        /// Maximum navigation time in milliseconds.
        /// </summary>
        int DefaultNavigationTimeout { get; set; }

        /// <summary>
        /// Gets all workers in the page.
        /// </summary>
        IEnumerable<IWorker> Workers { get; }

        /// <summary>
        /// Browser-specific Coverage implementation, only available for Chromium atm.
        /// </summary>
        ICoverage Coverage { get; }

        /// <summary>
        /// Video object associated with this page.
        /// </summary>
        IVideo Video { get; }

        /// <summary>
        /// Returns page's title.
        /// </summary>
        /// <returns>A <see cref="Task"/> the completes when the title is resolved, yielding the page's title.</returns>
        Task<string> GetTitleAsync();

        /// <summary>
        /// Returns the opener for popup pages and <c>null</c> for others.
        /// </summary>
        /// <remarks>
        /// If the opener has been closed already the task may resolve to <c>null</c>.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// IPage popup;
        /// page.Popup += (sender, e) => popup = e.Page;
        /// await page.EvaluateAsync("() => window.open('about:blank')");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the opener is resolved, yielding the opener <see cref="IPage"/>.</returns>
        Task<IPage> GetOpenerAsync();

        /// <summary>
        /// Completes when the page reaches a required load state, load by default.
        /// The navigation can be in progress when it is called.
        /// If navigation is already at a required state, completes immediately.
        /// </summary>
        /// <param name="state">Load state to wait for. Defaults to <see cref="LifecycleEvent.Load"/>. If the state has been already reached while loading current document, the method resolves immediately.</param>
        /// <param name="timeout">Maximum waiting time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultNavigationTimeout"/>, <see cref="IBrowserContext.DefaultTimeout"/>, <see cref="IPage.DefaultNavigationTimeout"/> or <see cref="IPage.DefaultTimeout"/> properties.</param>
        /// <returns>A <see cref="Task"/> that completes when the load is completed.</returns>
        Task WaitForLoadStateAsync(LifecycleEvent state = LifecycleEvent.Load, int? timeout = null);

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync();

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <param name="media">Changes the CSS media type of the page. Passing null disables CSS media emulation.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync(MediaType? media);

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <param name="colorScheme">Emulates 'prefers-colors-scheme' media feature.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync(ColorScheme? colorScheme);

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <param name="media">Changes the CSS media type of the page. Passing null disables CSS media emulation.</param>
        /// <param name="colorScheme">Emulates 'prefers-colors-scheme' media feature.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync(MediaType? media, ColorScheme? colorScheme);

        /// <summary>
        /// This resolves when the page navigates to a new URL or reloads.
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
        /// This resolves when the page navigates to a new URL or reloads.
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
        /// This resolves when the page navigates to a new URL or reloads.
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
        /// This resolves when the page navigates to a new URL or reloads.
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
        /// Returns frame matching the specified criteria.
        /// </summary>
        /// <param name="name">Frame name specified in the iframe's name attribute.</param>
        /// <param name="url"> A glob pattern.</param>
        /// <returns>The <see cref="IFrame"/> matching the condition if found.</returns>
        IFrame GetFrame(string name = null, string url = null);

        /// <summary>
        /// Returns frame matching the specified criteria.
        /// </summary>
        /// <param name="url">Regex pattern.</param>
        /// <returns>The <see cref="IFrame"/> matching the condition if found.</returns>
        IFrame GetFrame(Regex url);

        /// <summary>
        /// Returns frame matching the specified criteria.
        /// </summary>
        /// <param name="url">A function receiving the framn's URL.</param>
        /// <returns>The <see cref="IFrame"/> matching the condition if found.</returns>
        IFrame GetFrame(Func<string, bool> url);

        /// <summary>
        /// Waits for a request.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var firstRequest = await page.WaitForRequestAsync("http://example.com/resource");
        /// return firstRequest.Url;
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="url">URL to wait for.</param>
        /// <param name="timeout">/// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.</param>
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        Task<IRequest> WaitForRequestAsync(string url, int? timeout = null);

        /// <summary>
        /// Waits for a request.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var firstRequest = await page.WaitForRequestAsync(new Regex("digits\\d\\.png");
        /// return firstRequest.Url;
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="url">Regex to wait for.</param>
        /// <param name="timeout">/// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.</param>
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        Task<IRequest> WaitForRequestAsync(Regex url, int? timeout = null);

        /// <summary>
        /// Waits for a request.
        /// </summary>
        /// <param name="predicate">Function used to evaluate requets.</param>
        /// <param name="timeout">/// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.</param>
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> predicate, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="expression">Function to be evaluated in browser context.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string expression, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="expression">Function to be evaluated in browser context.</param>
        /// <param name="polling">An interval at which the <c>pageFunction</c> is executed. defaults to <see cref="Polling.Raf"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string expression, Polling polling, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="expression">Function to be evaluated in browser context.</param>
        /// <param name="polling">An interval at which the function is executed. If no value is specified will use <paramref name="polling"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string expression, int polling, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="expression">Function to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to <c>script</c>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="expression">Function to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to <c>script</c>.</param>
        /// <param name="polling">An interval at which the <c>pageFunction</c> is executed. defaults to <see cref="Polling.Raf"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, Polling polling, int? timeout = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="expression">Function to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to <c>script</c>.</param>
        /// <param name="polling">An interval at which the function is executed. If no value is specified will use <paramref name="polling"/>.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, int polling, int? timeout = null);

        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function.
        /// </summary>
        /// <param name="pageEvent">Event to wait for.</param>
        /// <param name="predicate">Receives the event data and resolves when the waiting should resolve.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <typeparam name="T">Resulting event args.</typeparam>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // wait for console event:
        /// var console = await page.WaitForEvent(PageEvent.Console);
        ///
        /// // wait for popup event:
        /// var popup = await page.WaitForEvent(PageEvent.Popup);
        ///
        /// // wait for dialog event:
        /// var dialog = await page.WaitForEvent(PageEvent.Dialog);
        ///
        /// // wait for request event:
        /// var request = await page.WaitForEvent(PageEvent.Request);
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<T, bool> predicate = null, int? timeout = null)
            where T : EventArgs;

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
        /// Closes the page.
        /// </summary>
        /// <param name="runBeforeUnload">Defaults to <c>false</c>. Whether to run the beforeunload page handlers.</param>
        /// <returns>A <see cref="Task"/> that completes when the close process finishes.</returns>
        Task CloseAsync(bool runBeforeUnload = false);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string)"/>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string expression);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object)"/>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string expression, object arg);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to expression.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAsync(string selector, string expression, object arg);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to expression.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to expression.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAsync(string selector, string expression);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to expression.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAsync<T>(string selector, string expression);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to expression.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAllAsync(string selector, string expression, object arg);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to expression.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to expression.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task EvalOnSelectorAllAsync(string selector, string expression);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to expression.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression);

        /// <summary>
        /// <![CDATA[
        /// This method focuses the element and triggers an input event after filling. If there's no text <input>, <textarea> or [contenteditable] element matching selector, the method throws an error.
        /// ]]>
        /// Shortcut for MainFrame.FillAsync.
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
        /// To press a special key, like <c>Control</c> or <c>ArrowDown</c> use <see cref="IKeyboard.PressAsync(string, float?)"/>.
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
        /// <param name="key">Name of the key to press or a character to generate, such as `ArrowLeft` or `a`.</param>
        /// <param name="delay">Time to wait between <c>keydown</c> and <c>keyup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> that completes when the type message is confirmed by the browser.</returns>
        Task PressAsync(string selector, string key, int delay = 0, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="files">The file path.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string selector, string files, int? timeout = null, bool? noWaitAfter = null);

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
        /// Sets the value of the file input to these file paths or files. If some of the  <paramref name="files"/> are relative paths, then they are resolved relative to the <see cref="Directory.GetCurrentDirectory"/>.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="files">The file payload.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <remarks>
        /// This method expects <see cref="IElementHandle"/> to point to an <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the files are successfully set.</returns>
        Task SetInputFilesAsync(string selector, ElementHandleFiles files, int? timeout = null, bool? noWaitAfter = null);

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
        Task SetInputFilesAsync(string selector, ElementHandleFiles[] files, int? timeout = null, bool? noWaitAfter = null);

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
        /// <param name="values">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, string values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, ElementHandleValues values, int? timeout = null, bool? noWaitAfter = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Value to select. If the <![CDATA[<select>]]> has the multiple attribute.</param>
        /// <param name="timeout">Maximum time to wait for in milliseconds. Defaults to `30000` (30 seconds).
        /// Pass `0` to disable timeout.
        /// The default value can be changed by using <seealso cref="IPage.DefaultTimeout"/> method.</param>
        /// <param name="noWaitAfter">Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectOptionAsync(string selector, IElementHandle values, int? timeout = null, bool? noWaitAfter = null);

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
        Task<string[]> SelectOptionAsync(string selector, ElementHandleValues[] values, int? timeout = null, bool? noWaitAfter = null);

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
        Task<string[]> SelectOptionAsync(string selector, params ElementHandleValues[] values);

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
        /// Waits for a timeout.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <returns>A <see cref="Task"/> that completes when the timeout is reached.</returns>
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
        Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState? state = null, int? timeout = null);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync(string)"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string expression);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync(string, object)"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string expression, object arg);

        /// <summary>
        /// Takes a screenshot of the page.
        /// </summary>
        /// <param name="fullPage">When <c>true</c>, takes a screenshot of the full scrollable page. Defaults to <c>false</c>.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="t:byte[]"/>.
        /// </returns>
        Task<byte[]> ScreenshotAsync(bool fullPage);

        /// <summary>
        /// Takes a screenshot of the page.
        /// </summary>
        /// <param name="clip">Specifies clipping region of the page.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="t:byte[]"/>.
        /// </returns>
        Task<byte[]> ScreenshotAsync(Rect clip);

        /// <summary>
        /// Takes a screenshot of the page.
        /// </summary>
        /// <param name="path">The file path to save the image to.
        ///  The screenshot type will be inferred from file extension.
        /// If path is a relative path, then it is resolved relative to current working directory.
        /// If no path is provided, the image won't be saved to the disk.</param>
        /// <param name="fullPage">When <c>true</c>, takes a screenshot of the full scrollable page. Defaults to <c>false</c>.</param>
        /// <param name="clip">Specifies clipping region of the page.</param>
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
            bool fullPage = false,
            Rect clip = null,
            bool omitBackground = false,
            ScreenshotType? type = null,
            int? quality = null,
            int? timeout = null);

        /// <summary>
        /// Sets the HTML markup to the main frame.
        /// </summary>
        /// <param name="html">HTML markup to assign to the page.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <returns>A <see cref="Task"/> that completes when the javascript code executing injected the HTML finishes.</returns>
        /// <seealso cref="IFrame.SetContentAsync(string, LifecycleEvent?, int?)"/>
        Task SetContentAsync(string html, LifecycleEvent? waitUntil = null, int? timeout = null);

        /// <summary>
        /// Gets the full HTML contents of the page, including the doctype.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the evaluation is completed, yielding the HTML content.</returns>
        Task<string> GetContentAsync();

        /// <summary>
        /// Sets extra HTTP headers that will be sent with every request the page initiates.
        /// </summary>
        /// <param name="headers">Additional http headers to be sent with every request.</param>
        /// <returns>A <see cref="Task"/> that completes when the headers are set.</returns>
        Task SetExtraHTTPHeadersAsync(Dictionary<string, string> headers);

        /// <summary>
        /// In the case of multiple pages in a single browser, each page can have its own viewport size.
        /// However, <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/> allows to set viewport size (and more) for all pages in the context at once.
        /// <see cref="IPage.SetViewportSizeAsync(ViewportSize)"/> will resize the page.A lot of websites don't expect phones to change size, so you should set the viewport size before navigating to the page.
        /// </summary>
        /// <param name="width">Viewport width.</param>
        /// <param name="height">Viewport height.</param>
        /// <returns>A <see cref="Task"/> that completes when the viewport is set.</returns>
        Task SetViewportSizeAsync(int width, int height);

        /// <summary>
        /// In the case of multiple pages in a single browser, each page can have its own viewport size.
        /// However, <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/> allows to set viewport size (and more) for all pages in the context at once.
        /// <see cref="IPage.SetViewportSizeAsync(ViewportSize)"/> will resize the page.A lot of websites don't expect phones to change size, so you should set the viewport size before navigating to the page.
        /// </summary>
        /// <param name="viewportSize">Viewport to set.</param>
        /// <returns>A <see cref="Task"/> that completes when the viewport is set.</returns>
        Task SetViewportSizeAsync(ViewportSize viewportSize);

        /// <summary>
        /// The method runs <c>document.querySelector</c> within the page. If no element matches the selector, the return value resolve to <c>null</c>.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascript function finishes, yielding an <see cref="IElementHandle"/>.
        /// </returns>
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
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when function is executed, yielding the return value.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string expression);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Function arguments.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when function is executed, yielding the return value.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string expression, object arg);

        /// <summary>
        /// Adds a <c><![CDATA[<script>]]></c> tag into the page with the desired url or content.
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
        /// Adds a <c><![CDATA[<link rel="stylesheet">]]></c> tag into the page with the desired url or a <c><![CDATA[<link rel="stylesheet">]]></c> tag with the content.
        /// </summary>
        /// <param name="url">URL of the link tag.</param>
        /// <param name="path">Path to the CSS file to be injected into frame. If path is a relative path, then it is resolved relative to current working directory.</param>
        /// <param name="content">Raw CSS content to be injected into frame.</param>
        /// <returns>A <see cref="Task"/> that completes when the stylesheet's onload fires or when the CSS content was injected into frame, yieling the added <see cref="IElementHandle"/>.</returns>
        Task<IElementHandle> AddStyleTagAsync(string url = null, string path = null, string content = null);

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
        /// Navigate to the previous page in history.
        /// </summary>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <returns>A <see cref="Task"/> that completes to the main resource response. In case of multiple redirects,
        /// the navigation will resolve with the response of the last redirect. If can not go back, resolves to null.</returns>
        Task<IResponse> GoBackAsync(int? timeout = null, LifecycleEvent? waitUntil = null);

        /// <summary>
        /// Navigate to the next page in history.
        /// </summary>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <returns>A <see cref="Task"/> that completes to the main resource response. In case of multiple redirects,
        /// the navigation will resolve with the response of the last redirect. If can not go forward, resolves to null.</returns>
        Task<IResponse> GoForwardAsync(int? timeout = null, LifecycleEvent? waitUntil = null);

        /// <summary>
        /// Reloads the page.
        /// </summary>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="LifecycleEvent.Load"/>.</param>
        /// <returns>A <see cref="Task"/> that completes to the main resource response. In case of multiple redirects, the navigation will resolve with the response of the last redirect.</returns>
        Task<IResponse> ReloadAsync(int? timeout = null, LifecycleEvent? waitUntil = null);

        /// <summary>
        /// Adds a script which would be evaluated in one of the following scenarios:
        /// * Whenever a page is created in the browser context or is navigated.
        /// * Whenever a child frame is attached or navigated in any page in the browser context.In this case, the script is evaluated in the context of the newly attached frame.
        /// </summary>
        /// <param name="script">Script to be evaluated in all pages in the browser context or script path.</param>
        /// <param name="arg">Optional argument to pass to script .</param>
        /// <param name="path">Gets or sets the path to the JavaScript file to be injected into frame. If its a relative path, then it is resolved relative to <see cref="System.IO.Directory.GetCurrentDirectory"/>.</param>
        /// <param name="content">Gets or sets the raw JavaScript content to be injected into frame.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task AddInitScriptAsync(string script = null, object[] arg = null, string path = null, string content = null);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="callback"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync(string, Action{BindingSource})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync(string name, Action<BindingSource> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="callback"/> completes.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync(string, Action{BindingSource})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{TResult}(string, Func{BindingSource, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T, TResult}(string, Func{BindingSource, T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T, TResult}(string, Func{BindingSource, T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T1, T2, TResult}(string, Func{BindingSource, T1, T2, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T1, T2, T3, TResult}(string, Func{BindingSource, T1, T2, T3, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T4">The fourth parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeBindingAsync{T1, T2, T3, T4, TResult}(string, Func{BindingSource, T1, T2, T3, T4, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="callback"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync(string, Action)"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync(string name, Action callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="callback"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <typeparam name="T">The parameter of <paramref name="callback"/>.</typeparam>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync(string, Action)"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T>(string name, Action<T> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{TResult}(string, Func{TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T, TResult}(string, Func{T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, TResult}(string, Func{T1, T2, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, T3, TResult}(string, Func{T1, T2, T3, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback);

        /// <summary>
        /// The method adds a function called name on the window object of every frame in every page in the context.
        /// When called, the function executes <paramref name="callback"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="callback"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="T4">The fourth parameter of <paramref name="callback"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="callback"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="callback">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="callback"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, T3, T4, TResult}(string, Func{T1, T2, T3, T4, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback);

        /// <summary>
        /// Waits for a response.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var firstResponse = await page.WaitForResponseAsync("http://example.com/resource");
        /// return firstResponse.Url;
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="url">URL to wait for.</param>
        /// <param name="timeout">/// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.</param>
        /// <returns>A <see cref="Task"/> that completes when a matching response is received, yielding the response being waited for.</returns>
        Task<IResponse> WaitForResponseAsync(string url, int? timeout = null);

        /// <summary>
        /// Waits for a response.
        /// </summary>
        /// <param name="url">Regex to wait for.</param>
        /// <param name="timeout">/// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.</param>
        /// <returns>A <see cref="Task"/> that completes when a matching response is received, yielding the response being waited for.</returns>
        Task<IResponse> WaitForResponseAsync(Regex url, int? timeout = null);

        /// <summary>
        /// Waits for a response.
        /// </summary>
        /// <param name="predicate">Function used to evaluate requets.</param>
        /// <param name="timeout">/// Maximum time to wait for in milliseconds. Pass 0 to disable timeout.
        /// The default value can be changed by setting the <see cref="IPage.DefaultTimeout"/> property.</param>
        /// <returns>A <see cref="Task"/> that completes when a matching response is received, yielding the response being waited for.</returns>
        Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> predicate, int? timeout = null);

        /// <summary>
        ///  generates a pdf of the page with <see cref="MediaType.Print"/> css media. To generate a pdf with <see cref="MediaType.Screen"/> media call <see cref="EmulateMediaAsync(MediaType?, ColorScheme?)"/> with <see cref="MediaType.Screen"/>.
        /// </summary>
        /// <param name="path">The file path to save the PDF to. paths are resolved using <see cref="Path.GetFullPath(string)"/>.</param>
        /// <param name="scale">Scale of the webpage rendering. Defaults to <c>1</c>. Scale amount must be between 0.1 and 2.</param>
        /// <param name="displayHeaderFooter">Display header and footer. Defaults to <c>false</c>.</param>
        /// <param name="headerTemplate">HTML template for the print header. Should be valid HTML markup with following classes used to inject printing values into them:
        ///   <c>date</c> - formatted print date
        ///   <c>title</c> - document title
        ///   <c>url</c> - document location
        ///   <c>pageNumber</c> - current page number
        ///   <c>totalPages</c> - total pages in the document.</param>
        /// <param name="footerTemplate">HTML template for the print footer. Should be valid HTML markup with following classes used to inject printing values into them:
        ///   <c>date</c> - formatted print date
        ///   <c>title</c> - document title
        ///   <c>url</c> - document location
        ///   <c>pageNumber</c> - current page number
        ///   <c>totalPages</c> - total pages in the document.</param>
        /// <param name="printBackground">Print background graphics. Defaults to <c>false</c>.</param>
        /// <param name="landscape">Paper orientation.. Defaults to <c>false</c>.</param>
        /// <param name="pageRanges">Paper ranges to print, e.g., <c>1-5, 8, 11-13</c>. Defaults to the empty string, which means print all pages.</param>
        /// <param name="format">Paper format. If set, takes priority over <paramref name="width"/> and <paramref name="height"/>.</param>
        /// <param name="width">Paper width, accepts values labeled with units.</param>
        /// <param name="height">Paper height, accepts values labeled with units.</param>
        /// <param name="margin">Paper margins, defaults to none.</param>
        /// <param name="preferCSSPageSize">Give any CSS <c>@page</c> size declared in the page priority over what is declared in <c>width</c> and <c>height</c> or <c>format</c> options.
        /// Defaults to <c>false</c>, which will scale the content to fit the paper size.</param>
        /// <returns>A <see cref="Task"/> that completes when the PDF was generated.</returns>
        /// <remarks>
        /// Generating a pdf is currently only supported in Chrome headless.
        /// </remarks>
        Task<byte[]> GetPdfAsync(
            string path = "",
            decimal scale = 1,
            bool displayHeaderFooter = false,
            string headerTemplate = "",
            string footerTemplate = "",
            bool printBackground = false,
            bool landscape = false,
            string pageRanges = "",
            PaperFormat? format = null,
            string width = null,
            string height = null,
            Margin margin = null,
            bool preferCSSPageSize = false);

        /// <summary>
        /// Routing provides the capability to modify network requests that are made by a page.
        /// Once route is enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
        /// </summary>
        /// <param name="url">A glob pattern to match while routing.</param>
        /// <param name="handler">Handler function to route the request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task RouteAsync(string url, Action<Route, IRequest> handler);

        /// <summary>
        /// Routing provides the capability to modify network requests that are made by a page.
        /// Once route is enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
        /// </summary>
        /// <param name="url">A regex to match while routing.</param>
        /// <param name="handler">Handler function to route the request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task RouteAsync(Regex url, Action<Route, IRequest> handler);

        /// <summary>
        /// Routing provides the capability to modify network requests that are made by a page.
        /// Once route is enabled, every request matching the url pattern will stall unless it's continued, fulfilled or aborted.
        /// </summary>
        /// <param name="url">A fucntion that evaluate the URL match.</param>
        /// <param name="handler">Handler function to route the request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task RouteAsync(Func<string, bool> url, Action<Route, IRequest> handler);

        /// <summary>
        /// Removes a route created with <see cref="IPage.RouteAsync(string, Action{Route, IRequest})"/>. When handler is not specified, removes all routes for the url.
        /// </summary>
        /// <param name="url">A glob pattern used to match while routing.</param>
        /// <param name="handler">Handler function used to route a request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task UnrouteAsync(string url, Action<Route, IRequest> handler = null);

        /// <summary>
        /// Removes a route created with <see cref="IPage.RouteAsync(Regex, Action{Route, IRequest})"/>. When handler is not specified, removes all routes for the url.
        /// </summary>
        /// <param name="url">A regex used to match while routing.</param>
        /// <param name="handler">Handler function used to route a request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task UnrouteAsync(Regex url, Action<Route, IRequest> handler = null);

        /// <summary>
        /// Removes a route created with <see cref="IPage.RouteAsync(Regex, Action{Route, IRequest})"/>. When handler is not specified, removes all routes for the url.
        /// </summary>
        /// <param name="url">A function used to match while routing.</param>
        /// <param name="handler">Handler function used to route a request.</param>
        /// <returns>A <see cref="Task"/> that completes when the registration was completed.</returns>
        Task UnrouteAsync(Func<string, bool> url, Action<Route, IRequest> handler = null);

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
        /// Brings page to front (activates tab).
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task BringToFrontAsync();

        /// <inheritdoc cref="IFrame.TapAsync(string, Modifier[], Point?, int?, bool, bool?)"/>
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

        /// <summary>
        /// Pauses script execution. Playwright will stop executing the script and wait for the user to either press 'Resume' button
        /// in the page overlay or to call `playwright.resume()` in the DevTools console.
        ///
        /// User can inspect selectors or perform manual steps while paused. Resume will continue running the original script from
        /// the place it was paused.
        ///
        /// > NOTE: This method requires Playwright to be started in a headed mode, with a falsy `headless` value in the <seealso cref="IBrowserType.LaunchAsync(bool?, string[], string, bool?, string, string, bool?, int?, bool?, int?, bool?, string[], Dictionary{string, string}, Dictionary{string, object}, ProxySettings, bool?, bool?, bool?, bool?)"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message is processed by the browser.</returns>
        Task PauseAsync();
    }
}
