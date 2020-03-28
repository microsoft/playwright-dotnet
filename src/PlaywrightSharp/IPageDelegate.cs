using System;
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
        /// Navigates a frame to an url.
        /// </summary>
        /// <param name="frame">Frame to navigate.</param>
        /// <param name="url">URL to navigate to.</param>
        /// <param name="referrer">Referer.</param>
        /// <returns>A <see cref="Task"/> that completes when the navigation is complete, yieldin its <see cref="GotoResult"/>.</returns>
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
        /// Sets extra HTTP headers that will be sent with every request the page initiates.
        /// </summary>
        /// <param name="headers">Additional http headers to be sent with every request.</param>
        /// <returns>A <see cref="Task"/> that completes when the headers are set.</returns>
        Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers);
    }
}
