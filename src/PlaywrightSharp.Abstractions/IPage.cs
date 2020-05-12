using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        /// Raised when a request finishes successfully.
        /// </summary>
        event EventHandler<RequestEventArgs> RequestFinished;

        /// <summary>
        /// Raised when a request fails, for example by timing out.
        /// </summary>
        event EventHandler<RequestEventArgs> RequestFailed;

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
        /// Raised when a file chooser is supposed to appear, such as after clicking the <c>&lt;input type=file&gt;</c>`. Playwright can respond to it via setting the input files using <see cref="IElementHandle.SetInputFilesAsync(string[])"/>.
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
#pragma warning disable CA1716 // Identifiers should not match keywords
        event EventHandler<ErrorEventArgs> Error;
#pragma warning restore CA1716 // Identifiers should not match keywords

        /// <summary>
        /// Raised when an uncaught exception happens within the page.
        /// </summary>
        event EventHandler<PageErrorEventArgs> PageError;

        /// <summary>
        /// Emitted when a dedicated WebWorker (<see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API"/>) is spawned by the page.
        /// </summary>
        event EventHandler<WorkerEventArgs> WorkerCreated;

        /// <summary>
        /// Emitted when a dedicated WebWorker (<see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API"/>) is terminated.
        /// </summary>
        event EventHandler<WorkerEventArgs> WorkerDestroyed;

        /// <summary>
        /// Emitted when a dedicated WebWorker (<see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API"/>) is terminated.
        /// </summary>
        event EventHandler<WebsocketEventArgs> Websocket;

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
        IBrowserContext BrowserContext { get; }

        /// <summary>
        /// Page Viewport.
        /// </summary>
        Viewport Viewport { get; }

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
        /// This setting will change the default maximum times for the following methods:
        /// - <see cref="SetContentAsync(string, NavigationOptions)"/>
        /// - <see cref="WaitForNavigationAsync(WaitUntilNavigation)"/>.
        /// </summary>
        int DefaultTimeout { get; set; }

        /// <summary>
        /// This setting will change the default maximum time for the following methods:
        /// - <see cref="SetContentAsync(string, NavigationOptions)"/>
        /// - <see cref="WaitForNavigationAsync(WaitForNavigationOptions, CancellationToken)"/>
        /// **NOTE** <see cref="DefaultNavigationTimeout"/> takes priority over <seealso cref="DefaultTimeout"/>.
        /// </summary>
        int DefaultNavigationTimeout { get; set; }

        /// <summary>
        /// Gets all workers in the page.
        /// </summary>
        IWorker[] Workers { get; }

        /// <summary>
        /// Browser-specific Coverage implementation, only available for Chromium atm.
        /// </summary>
        ICoverage Coverage { get; }

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
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task"/> that completes when the load is completed.</returns>
        Task WaitForLoadStateAsync(NavigationOptions options = null);

        /// <summary>
        /// Toggles ignoring cache for each request based on the enabled state. By default, caching is enabled.
        /// </summary>
        /// <param name="enabled">sets the <c>enabled</c> state of the cache.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task SetCacheEnabledAsync(bool enabled = true);

        /// <summary>
        /// Setup media emulation.
        /// </summary>
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task EmulateMediaAsync(EmulateMedia options);

        /// <summary>
        /// Navigates to an URL.
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IPage.GoToAsync(string, GoToOptions)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        /// <para/>
        /// <see cref="IPage.GoToAsync(string, GoToOptions)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        /// <para/>
        /// NOTE <see cref="IPage.GoToAsync(string, GoToOptions)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        /// <para/>
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        /// <para/>
        /// Shortcut for <see cref="IFrame.GoToAsync(string, GoToOptions)"/>.
        /// </remarks>
        Task<IResponse> GoToAsync(string url, GoToOptions options = null);

        /// <summary>
        /// This resolves when the page navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="options">navigation options.</param>
        /// <param name="token">Cancellation token.</param>
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
        Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null, CancellationToken token = default);

        /// <summary>
        /// This resolves when the page navigates to a new URL or reloads.
        /// It is useful for when you run code which will indirectly cause the page to navigate.
        /// </summary>
        /// <param name="waitUntil">When to consider navigation succeeded.</param>
        /// <returns>Task which resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// In case of navigation to a different anchor or navigation due to History API usage, the navigation will resolve with `null`.
        /// </returns>
        /// <remarks>
        /// Usage of the <c>History API</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/History_API"/> to change the URL is considered a navigation.
        /// </remarks>
        Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil);

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
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        /// <param name="url">URL to wait for.</param>
        /// <param name="options">Options.</param>
        Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null);

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
        /// <returns>A <see cref="Task"/> that completes when the request was made (or timeout), yielding the matching <see cref="IRequest"/>.</returns>
        /// <param name="regex">Pattern to wait for.</param>
        /// <param name="options">Options.</param>
        Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="options">Wait options.</param>
        /// <param name="args">Arguments to pass to <c>script</c>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args);

        /// <summary>
        /// Waits for a function to be evaluated to a truthy value.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to <c>script</c>.</param>
        /// <returns>A <see cref="Task"/> that resolves when the <c>script</c> returns a truthy value, yielding a <see cref="IJSHandle"/>.</returns>
        Task<IJSHandle> WaitForFunctionAsync(string pageFunction, params object[] args);

        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function.
        /// </summary>
        /// <param name="e">Event to wait for.</param>
        /// <param name="options">Extra options.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // wait for console event:
        /// var console = await page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console);
        ///
        /// // wait for popup event:
        /// var popup = await page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
        ///
        /// // wait for dialog event:
        /// var dialog = await page.WaitForEvent<DialogEventArgs>(PageEvent.Dialog);
        ///
        /// // wait for request event:
        /// var request = await page.WaitForEvent<RequestEventArgs>(PageEvent.Request);
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task<T> WaitForEvent<T>(PageEvent e, WaitForEventOptions<T> options = null);

        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function.
        /// </summary>
        /// <param name="e">Event to wait for.</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // wait for console event:
        /// var console = await page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console);
        ///
        /// // wait for popup event:
        /// var popup = await page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
        ///
        /// // wait for dialog event:
        /// var dialog = await page.WaitForEvent<DialogEventArgs>(PageEvent.Dialog);
        ///
        /// // wait for request event:
        /// var request = await page.WaitForEvent<RequestEventArgs>(PageEvent.Request);
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task WaitForEvent(PageEvent e);

        /// <summary>
        /// Navigates to an url.
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout. </param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="WaitUntilNavigation.Load"/>. Given an array of <see cref="WaitUntilNavigation"/>, navigation is considered to be successful after all events have been fired.</param>
        /// <seealso cref="GoToAsync(string, GoToOptions)"></seealso>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        Task<IResponse> GoToAsync(string url, int timeout, params WaitUntilNavigation[] waitUntil);

        /// <summary>
        /// Navigates to an url.
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="waitUntil">When to consider navigation succeeded, defaults to <see cref="WaitUntilNavigation.Load"/>. Given an array of <see cref="WaitUntilNavigation"/>, navigation is considered to be successful after all events have been fired.</param>
        /// <seealso cref="GoToAsync(string, GoToOptions)"></seealso>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        Task<IResponse> GoToAsync(string url, params WaitUntilNavigation[] waitUntil);

        /// <summary>
        /// Closes the page.
        /// </summary>
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task"/> that completes when the close process finishes.</returns>
        Task CloseAsync(PageCloseOptions options = null);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string script, params object[] args);

        /// <summary>
        /// Adds a function which would be invoked in one of the following scenarios:
        /// - whenever the page is navigated
        /// - whenever the child frame is attached or navigated. In this case, the function is invoked in the context of the newly attached frame.
        /// </summary>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to <c>pageFunction</c>.</param>
        /// <remarks>
        /// The function is invoked after the document was created but before any of its scripts were run. This is useful to amend JavaScript environment, e.g. to seed <c>Math.random</c>.
        /// </remarks>
        /// <example>
        /// An example of overriding the navigator.languages property before the page loads:
        /// <code>
        /// await page.EvaluateOnNewDocumentAsync("() => window.__example = true");
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved.</returns>
        Task EvaluateOnNewDocumentAsync(string pageFunction, params object[] args);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args);

        /// <summary>
        /// This method runs <c>document.querySelector</c> within the page and passes it as the first argument to pageFunction.
        /// If there's no element matching selector, the method throws an error.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to pageFunction.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task QuerySelectorAllEvaluateAsync(string selector, string script, params object[] args);

        /// <summary>
        /// This method runs <c>Array.from(document.querySelectorAll(selector))</c> within the page and passes it as the first argument to pageFunction.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args);

        /// <summary>
        /// <![CDATA[
        /// This method focuses the element and triggers an input event after filling. If there's no text <input>, <textarea> or [contenteditable] element matching selector, the method throws an error.
        /// ]]>
        /// Shortcut for MainFrame.FillAsync.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="text"><![CDATA[Value to fill for the <input>, <textarea> or [contenteditable] element]]></param>
        /// <param name="options">Optional waiting parameters.</param>
        /// <returns>A <see cref="Task"/> that completes when the fill message is confirmed by the browser.</returns>
        Task FillAsync(string selector, string text, WaitForSelectorOptions options = null);

        /// <summary>
        /// Sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="selector">A selector of an element to type into. If there are multiple elements satisfying the selector, the first will be used.</param>
        /// <param name="text">A text to type into a focused element.</param>
        /// <param name="options">Type options.</param>
        /// <remarks>
        /// To press a special key, like <c>Control</c> or <c>ArrowDown</c> use <see cref="IKeyboard.PressAsync(string, PressOptions)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// await page.TypeAsync("#mytextarea", "Hello"); // Types instantly
        /// await page.TypeAsync("#mytextarea", "World", new TypeOptions { Delay = 100 }); // Types slower, like a user
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the type message is confirmed by the browser.</returns>
        Task TypeAsync(string selector, string text, TypeOptions options = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/> and focuses it.
        /// </summary>
        /// <param name="selector">A selector to search for element to focus. If there are multiple elements satisfying the selector, the first will be focused.</param>
        /// <param name="options">Wait options.</param>
        /// <returns>A <see cref="Task"/> that completes when the the element matching <paramref name="selector"/> is successfully focused.</returns>
        Task FocusAsync(string selector, WaitForSelectorOptions options = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="Mouse"/> to hover over the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to hover. If there are multiple elements satisfying the selector, the first will be hovered.</param>
        /// <param name="options">Wait options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully hovered.</returns>
        Task HoverAsync(string selector, WaitForSelectorOptions options = null);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectAsync(string selector, params string[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Values of options to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectAsync(string selector, params SelectOption[] values);

        /// <summary>
        /// Triggers a change and input event once all the provided options have been selected.
        /// If there's no <![CDATA[<select>]]> element matching selector, the method throws an error.
        /// </summary>
        /// <param name="selector">A selector to query page for.</param>
        /// <param name="values">Option elements to select. If the <![CDATA[<select>]]> has the multiple attribute,
        /// all values are considered, otherwise only the first one is taken into account.</param>
        /// <returns>A <see cref="Task"/> the completes when the value have been selected, yielding an array of option values that have been successfully selected.</returns>
        Task<string[]> SelectAsync(string selector, params IElementHandle[] values);

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
        /// <param name="options">Optional waiting parameters.</param>
        /// <returns>A <see cref="Task"/> that completes when element specified by selector string is added to DOM, yielding the <see cref="IElementHandle"/> to wait for.
        /// Resolves to `null` if waiting for `hidden: true` and selector is not found in DOM.</returns>
        Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null);

        /// <summary>
        /// Waits for a selector to be added to the DOM.
        /// </summary>
        /// <param name="selector">A selector of an element to wait for.</param>
        /// <param name="script">Function to execute when the selector is found.</param>
        /// <param name="options">Optional waiting parameters.</param>
        /// <param name="args">Arguments to be passed.</param>
        /// <returns>A <see cref="Task"/> that completes when element specified by selector string is added to DOM, yielding the <see cref="IElementHandle"/> to wait for.
        /// Resolves to `null` if waiting for `hidden: true` and selector is not found in DOM.</returns>
        Task<IJSHandle> WaitForSelectorEvaluateAsync(string selector, string script, WaitForFunctionOptions options = null, params object[] args);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync(string, object[])"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string script, params object[] args);

        /// <summary>
        /// Takes a screenshot of the page.
        /// </summary>
        /// <param name="options">Screenshot options.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="t:byte[]"/>.
        /// </returns>
        Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null);

        /// <summary>
        /// Takes a screenshot of the page.
        /// </summary>
        /// <param name="options">Screenshot options.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a base64 <see cref="string"/>.
        /// </returns>
        Task<string> ScreenshotBase64Async(ScreenshotOptions options = null);

        /// <summary>
        /// Sets the HTML markup to the page.
        /// </summary>
        /// <param name="html">HTML markup to assign to the page.</param>
        /// <param name="options">The navigations options.</param>
        /// <returns>A <see cref="Task"/> that completes when the javascript code executing injected the HTML finishes.</returns>
        /// <seealso cref="IFrame.SetContentAsync(string, NavigationOptions)"/>
        Task SetContentAsync(string html, NavigationOptions options = null);

        /// <summary>
        /// Sets the HTML markup to the page.
        /// </summary>
        /// <param name="html">HTML markup to assign to the page.</param>
        /// <param name="waitUntil">When to consider navigation succeeded.</param>
        /// <returns>A <see cref="Task"/> that completes when the javascript code executing injected the HTML finishes.</returns>
        /// <seealso cref="IFrame.SetContentAsync(string, NavigationOptions)"/>
        Task SetContentAsync(string html, WaitUntilNavigation waitUntil);

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
        Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers);

        /// <summary>
        /// Provide credentials for http authentication <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication"/>.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <returns>A <see cref="Task"/> that completes when the credentials are set.</returns>
        /// <remarks>
        /// To disable authentication, pass <c>null</c>.
        /// </remarks>
        Task AuthenticateAsync(Credentials credentials);

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
        Task<IElementHandle[]> QuerySelectorAllAsync(string selector);

        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="args">Function arguments.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when function is executed, yielding the return value.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string pageFunction, params object[] args);

        /// <summary>
        /// Adds a <c><![CDATA[<script>]]></c> tag into the page with the desired url or content.
        /// </summary>
        /// <param name="options">add script tag options.</param>
        /// <remarks>
        /// Shortcut for <c>page.MainFrame.AddScriptTagAsync(options)</c>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script's onload fires or when the script content was injected into frame, yielding the added <see cref="IElementHandle"/>.</returns>
        Task<IElementHandle> AddScriptTagAsync(AddTagOptions options);

        /// <summary>
        /// Adds a <c><![CDATA[<link rel="stylesheet">]]></c> tag into the page with the desired url or a <c><![CDATA[<link rel="stylesheet">]]></c> tag with the content.
        /// </summary>
        /// <param name="options">add style tag options.</param>
        /// <returns>A <see cref="Task"/> that completes when the stylesheet's onload fires or when the CSS content was injected into frame, yieling the added <see cref="IElementHandle"/>.</returns>
        Task<IElementHandle> AddStyleTagAsync(AddTagOptions options);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="Mouse"/> to click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully clicked.</returns>
        Task ClickAsync(string selector, ClickOptions options = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="Mouse"/> to double click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully double clicked.</returns>
        Task DoubleClickAsync(string selector, ClickOptions options = null);

        /// <summary>
        /// Fetches an element with <paramref name="selector"/>, scrolls it into view if needed, and then uses <see cref="Mouse"/> to triple click in the center of the element.
        /// </summary>
        /// <param name="selector">A selector to search for element to click. If there are multiple elements satisfying the selector, the first will be clicked.</param>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element matching <paramref name="selector"/> is successfully triple clicked.</returns>
        Task TripleClickAsync(string selector, ClickOptions options = null);

        /// <summary>
        /// Sets the viewport.
        /// In the case of multiple pages in a single browser, each page can have its own viewport size.
        /// <see cref="SetViewportAsync(PlaywrightSharp.Viewport)"/> will resize the page. A lot of websites don't expect phones to change size, so you should set the viewport before navigating to the page.
        /// </summary>
        /// <example>
        /// <![CDATA[
        /// using(var page = await context.NewPageAsync())
        /// {
        ///     await page.SetViewPortAsync(new Viewport
        ///     {
        ///         Width = 640,
        ///         Height = 480,
        ///         DeviceScaleFactor = 1
        ///     });
        ///     await page.GoToAsync('https://www.example.com');
        /// }
        /// ]]>
        /// </example>
        /// <param name="viewport">Viewport.</param>
        /// <returns>A<see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task SetViewportAsync(Viewport viewport);

        /// <summary>
        /// Navigate to the previous page in history.
        /// </summary>
        /// <param name="options">Navigation parameters.</param>
        /// <returns>A <see cref="Task"/> that completes to the main resource response. In case of multiple redirects,
        /// the navigation will resolve with the response of the last redirect. If can not go back, resolves to null.</returns>
        Task<IResponse> GoBackAsync(NavigationOptions options = null);

        /// <summary>
        /// Navigate to the next page in history.
        /// </summary>
        /// <param name="options">Navigation parameters.</param>
        /// <returns>A <see cref="Task"/> that completes to the main resource response. In case of multiple redirects,
        /// the navigation will resolve with the response of the last redirect. If can not go forward, resolves to null.</returns>
        Task<IResponse> GoForwardAsync(NavigationOptions options = null);

        /// <summary>
        /// Reloads the page.
        /// </summary>
        /// <param name="options">Navigation options.</param>
        /// <returns>A <see cref="Task"/> that completes to the main resource response. In case of multiple redirects, the navigation will resolve with the response of the last redirect.</returns>
        Task<IResponse> ReloadAsync(NavigationOptions options = null);

        /// <summary>
        /// Activating request interception enables <see cref="IRequest.AbortAsync(RequestAbortErrorCode)">request.AbortAsync</see>,
        /// <see cref="IRequest.ContinueAsync(Payload)">request.ContinueAsync</see> and <see cref="IRequest.FulfillAsync(ResponseData)">request.FulfillAsync</see> methods.
        /// </summary>
        /// <returns>A<see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        /// <param name="enabled">Whether to enable request interception..</param>
        Task SetRequestInterceptionAsync(bool enabled);

        /// <summary>
        /// Set offline mode for the page.
        /// </summary>
        /// <returns>A<see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        /// <param name="enabled">When <c>true</c> enables offline mode for the page.</param>
        Task SetOfflineModeAsync(bool enabled);

        /// <summary>
        /// Adds a function called <c>name</c> on the page's <c>window</c> object.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves when <paramref name="playwrightFunction"/> completes.
        /// </summary>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync(string, Action)"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync(string name, Action playwrightFunction);

        /// <summary>
        /// Adds a function called <c>name</c> on the page's <c>window</c> object.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{TResult}(string, Func{TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction);

        /// <summary>
        /// Adds a function called <c>name</c> on the page's <c>window</c> object.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T">The parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T, TResult}(string, Func{T, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction);

        /// <summary>
        /// Adds a function called <c>name</c> on the page's <c>window</c> object.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, TResult}(string, Func{T1, T2, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction);

        /// <summary>
        /// Adds a function called <c>name</c> on the page's <c>window</c> object.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, T3, TResult}(string, Func{T1, T2, T3, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction);

        /// <summary>
        /// Adds a function called <c>name</c> on the page's <c>window</c> object.
        /// When called, the function executes <paramref name="playwrightFunction"/> in C# and returns a <see cref="Task"/> which resolves to the return value of <paramref name="playwrightFunction"/>.
        /// </summary>
        /// <typeparam name="T1">The first parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T2">The second parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T3">The third parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="T4">The fourth parameter of <paramref name="playwrightFunction"/>.</typeparam>
        /// <typeparam name="TResult">The result of <paramref name="playwrightFunction"/>.</typeparam>
        /// <param name="name">Name of the function on the window object.</param>
        /// <param name="playwrightFunction">Callback function which will be called in Playwright's context.</param>
        /// <remarks>
        /// If the <paramref name="playwrightFunction"/> returns a <see cref="Task"/>, it will be awaited.
        /// Functions installed via <see cref="ExposeFunctionAsync{T1, T2, T3, T4, TResult}(string, Func{T1, T2, T3, T4, TResult})"/> survive navigations.
        /// </remarks>
        /// <returns>Task.</returns>
        Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction);

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
        /// <param name="options">Options.</param>
        /// <returns>A <see cref="Task"/> that completes when a matching response is received, yielding the response being waited for.</returns>
        Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null);

        /// <summary>
        /// Generates a pdf of the page with <see cref="MediaType.Print"/> css media. To generate a pdf with <see cref="MediaType.Screen"/> media call <see cref="EmulateMediaAsync(EmulateMedia)"/> with <see cref="MediaType.Screen"/>.
        /// </summary>
        /// <param name="file">The file path to save the PDF to. paths are resolved using <see cref="Path.GetFullPath(string)"/>.</param>
        /// <remarks>
        /// Generating a pdf is currently only supported in Chrome headless.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the PDF was generated.</returns>
        Task GetPdfAsync(string file);
    }
}
