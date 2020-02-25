using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Page delegate interface.
    /// </summary>
    public interface IPageDelegate
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
    }
}
