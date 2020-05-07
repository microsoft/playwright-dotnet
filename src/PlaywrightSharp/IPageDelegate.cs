using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// Page delegate interface.
    /// </summary>
    internal interface IPageDelegate
    {
        /// <summary>
        /// Internal keyboard implementation.
        /// </summary>
        IRawKeyboard RawKeyboard { get; }

        /// <summary>
        /// Internal mouse implementation.
        /// </summary>
        IRawMouse RawMouse { get; }

        /// <summary>
        /// <see cref="FrameExecutionContext"/> map.
        /// </summary>
        ConcurrentDictionary<object, FrameExecutionContext> ContextIdToContext { get; }

        /// <summary>
        /// Used by <see cref="Accessibility"/>.
        /// </summary>
        /// <param name="needle">Root element.</param>
        /// <returns>A <see cref="Task"/> that completes when the accessibility tree is resolved, yielding the tree.</returns>
        Task<AccessibilityTree> GetAccessibilityTreeAsync(IElementHandle needle);

        /// <summary>
        /// Navigates a frame to an url.
        /// </summary>
        /// <param name="frame">Frame to navigate.</param>
        /// <param name="url">URL to navigate to.</param>
        /// <param name="referrer">Referer.</param>
        /// <returns>A <see cref="Task"/> that completes when the navigation is complete, yielding its <see cref="GotoResult"/>.</returns>
        Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer);

        /// <summary>
        /// Adopt argument.
        /// </summary>
        /// <param name="handle">Argument to adpopt.</param>
        /// <param name="to">Execution context.</param>
        /// <returns>A <see cref="Task"/> that completes when the argument is adopted, yielding the <see cref="ElementHandle"/>.</returns>
        Task<ElementHandle> AdoptElementHandleAsync(ElementHandle handle, FrameExecutionContext to);

        /// <summary>
        /// Gets the element's bounding box.
        /// </summary>
        /// <param name="handle">Element to evaluate.</param>
        /// <returns>A <see cref="Task"/> that completes when the bounding box is evaluated, yielding a <see cref="Rect"/> representing the bounding box.</returns>
        Task<Rect> GetBoundingBoxAsync(ElementHandle handle);

        /// <summary>
        /// Gets the iFrame representation of an <see cref="IElementHandle"/>.
        /// </summary>
        /// <param name="elementHandle">Element to evaluate.</param>
        /// <returns>A <see cref="Task"/> that completes when the <see cref="IFrame"/> is found, yielding the <see cref="IFrame"/>.</returns>
        Task<IFrame> GetContentFrameAsync(ElementHandle elementHandle);

        /// <summary>
        /// Sets the viewport.
        /// In the case of multiple pages in a single browser, each page can have its own viewport size.
        /// <see cref="SetViewportAsync(Viewport)"/> will resize the page. A lot of websites don't expect phones to change size, so you should set the viewport before navigating to the page.
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
        /// Closes the page.
        /// </summary>
        /// <param name="runBeforeUnload">Should run before unload.</param>
        /// <returns>A <see cref="Task"/> that completes when the close process finishes.</returns>
        Task ClosePageAsync(bool runBeforeUnload);

        /// <summary>
        /// Check if the <see cref="JsonElement"/> is an <see cref="ElementHandle"/>.
        /// </summary>
        /// <param name="remoteObject">Object to check.</param>
        /// <returns>Whether the <see cref="JsonElement"/> is an <see cref="ElementHandle"/> or not.</returns>
        bool IsElementHandle(IRemoteObject remoteObject);

        /// <summary>
        /// Gets the content quads.
        /// </summary>
        /// <param name="elementHandle">Element to evaluate.</param>
        /// <returns>A <see cref="Task"/> that completes when the quads are returned by the browser, yielding an array of <see cref="Quad"/>.</returns>
        Task<Quad[][]> GetContentQuadsAsync(ElementHandle elementHandle);

        /// <summary>
        /// Gets the metrics of the viewport layout.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the metrics are returned by the browser, yielding its <see cref="LayoutMetric"/>.</returns>
        Task<LayoutMetric> GetLayoutViewportAsync();

        /// <summary>
        /// Evaluates if <see cref="Screenshotter"/> can take a full page screenshot.
        /// </summary>
        /// <returns>Whether the <see cref="Screenshotter"/> can take a full page screenshot.</returns>
        bool CanScreenshotOutsideViewport();

        /// <summary>
        /// Resets the viewport.
        /// </summary>
        /// <param name="viewport">Viewport to reset to.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task ResetViewportAsync(Viewport viewport);

        /// <summary>
        /// Sets the background color of the page.
        /// </summary>
        /// <param name="color">Color to set.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetBackgroundColorAsync(Color? color = null);

        /// <summary>
        /// Performs the screenshot action.
        /// </summary>
        /// <param name="format">Screenshot format.</param>
        /// <param name="options">Options.</param>
        /// <param name="viewport">Viewport.</param>
        /// <returns>A <see cref="Task"/> that completes when the screenshot was taken, yielding the screenshot as a <see cref="byte"/> array.</returns>
        Task<byte[]> TakeScreenshotAsync(ScreenshotFormat format, ScreenshotOptions options, Viewport viewport);

        /// <summary>
        ///  Gets the element's bounding box.
        /// </summary>
        /// <param name="handle">Element to evaluate.</param>
        /// <returns>A <see cref="Task"/> that completes when the bounding box was built, yielding the <see cref="ElementHandle"/> <see cref="Rect"/>.</returns>
        Task<Rect> GetBoundingBoxForScreenshotAsync(ElementHandle handle);

        /// <summary>
        /// Adds a function in the browser.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="functionString">Function string.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task ExposeBindingAsync(string name, string functionString);

        /// <summary>
        /// Adds a function which would be invoked in one of the following scenarios:
        /// - whenever the page is navigated
        /// - whenever the child frame is attached or navigated. In this case, the function is invoked in the context of the newly attached frame.
        /// </summary>
        /// <param name="source">Expression to be evaluated in browser context.</param>
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
        Task EvaluateOnNewDocumentAsync(string source);

        /// <summary>
        /// Get's the frame ID associated to the <see cref="ElementHandle"/>.
        /// </summary>
        /// <param name="elementHandle">Element to evaluate.</param>
        /// <returns>A <see cref="Task"/> that completes when the frame is found, yielding its frame ID.</returns>
        Task<string> GetOwnerFrameAsync(ElementHandle elementHandle);

        /// <summary>
        /// Sets extra HTTP headers that will be sent with every request the page initiates.
        /// </summary>
        /// <param name="headers">Additional http headers to be sent with every request.</param>
        /// <returns>A <see cref="Task"/> that completes when the headers are set.</returns>
        Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers);

        /// <summary>
        /// Reloads the page.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the redirect action completes.</returns>
        Task ReloadAsync();

        /// <summary>
        /// Navigate to the previous page in history.
        /// </summary>
        /// <returns>A <see cref="Task"/> that resolves to the main resource response. If can not go back, resolves to <c>false</c>.</returns>
        Task<bool> GoBackAsync();

        /// <summary>
        /// Navigate to the next page in history.
        /// </summary>
        /// <returns>A <see cref="Task"/>  that resolves to the main resource response. If can not go forward, resolves to <c>false</c>.</returns>
        Task<bool> GoForwardAsync();

        /// <summary>
        /// Add <paramref name="files"/> to the <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">Element to add the files to.</param>
        /// <param name="files">Files payloads to add.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task SetInputFilesAsync(ElementHandle handle, IEnumerable<FilePayload> files);

        /// <summary>
        /// Enables file chooser interception.
        /// </summary>
        /// <param name="enabled">Whether to enable interception or not.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task SetFileChooserInterceptedAsync(bool enabled);

        /// <summary>
        /// Toggles ignoring cache for each request based on the enabled state. By default, caching is enabled.
        /// </summary>
        /// <param name="enabled">sets the <c>enabled</c> state of the cache.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task SetCacheEnabledAsync(bool enabled);

        /// <summary>
        /// Activating request interception enables <see cref="IRequest.AbortAsync(RequestAbortErrorCode)">request.AbortAsync</see>,
        /// <see cref="IRequest.ContinueAsync(Payload)">request.ContinueAsync</see> and <see cref="IRequest.FulfillAsync(ResponseData)">request.FulfillAsync</see> methods.
        /// </summary>
        /// <returns>A<see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        /// <param name="enabled">Whether to enable request interception..</param>
        Task SetRequestInterceptionAsync(bool enabled);

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
        /// Set offline mode for the page.
        /// </summary>
        /// <returns>A<see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        /// <param name="enabled">When <c>true</c> enables offline mode for the page.</param>
        Task SetOfflineModeAsync(bool enabled);
    }
}
