using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Page delegate interface.
    /// </summary>
    internal interface IPageDelegate
    {
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
        /// <param name="arg">Argument to adpopt.</param>
        /// <param name="frameExecutionContext">Execution context.</param>
        /// <returns>A <see cref="Task"/> that completes when the argument is adopted, yielding the <see cref="ElementHandle"/>.</returns>
        Task<ElementHandle> AdoptElementHandleAsync(object arg, FrameExecutionContext frameExecutionContext);

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
    }
}
