using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// It represents an in-page DOM element.
    /// </summary>
    public interface IElementHandle : IJSHandle
    {
        /// <summary>
        /// Takes a screenshot of the element.
        /// </summary>
        /// <param name="options">Screenshot options.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="T:byte[]" />.
        /// </returns>
        Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null);

        Task FillAsync(string text);

        /// <summary>
        /// Content frame for element handles referencing iframe nodes, or null otherwise.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the frame is resolved, yielding element's parent <see cref="IFrame" /></returns>
        Task<IFrame> ContentFrameAsync();

        Task HoverAsync();

        Task ScrollIntoViewIfNeededAsync();

        /// <summary>
        /// Returns the frame containing the given element
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the frame is resolved, yielding element's owner <see cref="IFrame" /></returns>
        Task<IFrame> OwnerFrameAsync();

        /// <summary>
        /// Gets the bounding box of the element (relative to the main frame), 
        /// or null if the element is not visible.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the <see cref="BoundingBox"/> is resolved, yielding element's <see cref="BoundingBox"/></returns>
        Task<BoundingBox> BoundingBoxAsync();

        Task<double> VisibleRatioAsync();

        /// <summary>
        /// Scrolls element into view if needed, and then uses <see cref="IPage.Mouse"/> to click in the center of the element.
        /// </summary>
        /// <param name="options">click options.</param>
        /// <returns>A <see cref="Task"/> that completes when the element is successfully clicked.</returns>
        Task ClickAsync(ClickOptions options = null);

        /// <summary>
        /// Executes a function in browser context, passing the current <see cref="IElementHandle"/> as the first argument.
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script is executed, yieling the return value of that script.</returns>
        Task<JsonElement?> EvaluateAsync(string script, params object[] args);
    }
}
