using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// It represents an in-page DOM element. 
    /// </summary>
    public interface IElementHandle : IJSHandle
    {
        /// <summary>
        /// Takes a screenshot of the element
        /// </summary>
        /// <param name="options">Screenshot options</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the screenshot is done, yielding the screenshot as a <see cref="T:byte[]" />
        /// </returns>
        Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null);

        /// <summary>
        /// Content frame for element handles referencing iframe nodes, or null otherwise.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the frame is resolved, yielding element's parent <see cref="IFrame" /></returns>
        Task<IFrame> ContentFrameAsync();
    }
}