using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Provides methods to interact with the mouse
    /// </summary>
    public interface IMouse
    {
        /// <summary>
        /// Dispatches a <c>mousemove</c> event.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="options">Extra options</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task MoveAsync(decimal x, decimal y, MoveOptions options = null);

        /// <summary>
        /// Shortcut for <see cref="MoveAsync(decimal, decimal, MoveOptions)"/>, <see cref="DownAsync(ClickOptions)"/> and <see cref="UpAsync(ClickOptions)"/>
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="options">Extra options</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task ClickAsync(decimal x, decimal y, ClickOptions options = null);

        /// <summary>
        /// Dispatches a <c>mousedown</c> event.
        /// </summary>
        /// <param name="options">Extra options</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task DownAsync(ClickOptions options = null);

        /// <summary>
        /// Dispatches a <c>mouseup</c> event.
        /// </summary>
        /// <param name="options">Extra options</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task UpAsync(ClickOptions options = null);

        /// <summary>
        /// Dispatches a <c>wheel</c> event.
        /// </summary>
        /// <param name="deltaX">delta X</param>
        /// <param name="deltaY">delta Y</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task WheelAsync(decimal deltaX, decimal deltaY);
    }
}