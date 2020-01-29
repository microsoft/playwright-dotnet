using System;
using System.Text.Json;
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
        /// Navigates to an URL
        /// </summary>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <returns>A <see cref="Task{IResponse}"/> that completes with resolves to the main resource response.
        /// In case of multiple redirects, the navigation will resolve with the response of the last redirect.
        /// </returns>
        /// <remarks>
        /// <see cref="IPage.GoToAsync(string)"/> will throw an error if:
        /// * There's an SSL error (e.g. in case of self-signed certificates).
        /// * Target URL is invalid.
        /// * The timeout is exceeded during navigation.
        /// * The remote server does not respond or is unreachable.
        /// * The main resource failed to load.
        ///
        /// <see cref="IPage.GoToAsync(string)"/> will not throw an error when any valid HTTP status code is returned by the remote server, including 404 "Not Found" and 500 "Internal Server Error".
        /// The status code for such responses can be retrieved by calling response.status().
        ///
        /// NOTE <see cref="IPage.GoToAsync(string)"/> either throws an error or returns a main resource response.
        /// The only exceptions are navigation to about:blank or navigation to the same URL with a different hash, which would succeed and return null.
        ///
        /// NOTE Headless mode doesn't support navigation to a PDF document. See the upstream issue.
        ///
        /// Shortcut for <see cref="IFrame.GoToAsync"/>
        /// </remarks>
        Task<IResponse> GoToAsync(string url);

        /// <summary>
        /// Page is guaranteed to have a main frame which persists during navigations.
        /// </summary>
        IFrame MainFrame { get; }

        /// <summary>
        /// Get the browser context that the page belongs to.
        /// </summary>
        IBrowser BrowserContext { get; }

        /// <summary>
        /// Page Viewport
        /// </summary>
        Viewport Viewport { get; }

        /// <summary>
        /// Executes a script in browser context
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context</param>
        /// <param name="args">Arguments to pass to script</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script</returns>
        Task<T> EvaluateAsync<T>(string script, params object[] args);

        /// <summary>
        /// Executes a script in browser context
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context</param>
        /// <param name="args">Arguments to pass to script</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync(string, object[])"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string script, params object[] args);

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
        /// Takes a screenshot of the page
        /// </summary>
        /// <param name="options">Screenshot options</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="t:byte[]"/>
        /// </returns>
        Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null);

        /// <summary>
        /// Sets the HTML markup to the page
        /// </summary>
        /// <param name="html">HTML markup to assign to the page.</param>
        /// <returns>A <see cref="Task"/> that completes when the javascript code executing injected the HTML finishes</returns>
        Task SetContentAsync(string html);

        /// <summary>
        /// The method runs <c>document.querySelector</c> within the page. If no element matches the selector, the return value resolve to <c>null</c>.
        /// </summary>
        /// <param name="selector">A selector to query page for</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascription function finishes, yielding an <see cref="IElementHandle"/> pointing to the frame element
        /// </returns>
        Task<IElementHandle> QuerySelectorAsync(string selector);

        /// <summary>
        /// Executes a script in browser context
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when function is executed, yielding the return value</returns>
        Task<IJSHandle> EvaluateHandleAsync(string expression);

        /// <summary>
        /// Executes a script in browser context
        /// </summary>
        /// <param name="pageFunction">Script to be evaluated in browser context</param>
        /// <param name="args">Function arguments</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when function is executed, yielding the return value</returns>
        Task<IJSHandle> EvaluateHandleAsync(string pageFunction, params object[] args);

        /// <summary>
        /// Sets the user agent to be used in this page
        /// </summary>
        /// <param name="userAgent">Specific user agent to use in this page</param>
        /// <returns>A <see cref="Task"/> that completes when the user agent is changed</returns>
        Task SetUserAgentAsync(string userAgent);

        /// <summary>
        /// Adds a <c><![CDATA[<script>]]></c> tag into the page with the desired url or content
        /// </summary>
        /// <param name="options">add script tag options</param>
        /// <remarks>
        /// Shortcut for <c>page.MainFrame.AddScriptTagAsync(options)</c>
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the tag is added, yielding the added tag when the script's onload fires or when the script content was injected into frame</returns>
        Task<IElementHandle> AddScriptTagAsync(AddTagOptions options);
    }
}
