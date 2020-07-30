using System.Threading.Tasks;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// Provides methods to interact with the mouse.
    /// </summary>
    public interface IMouse
    {
        /// <summary>
        /// Dispatches a <c>mousemove</c> event.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="steps">Sends intermediate mousemove events.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task MoveAsync(double x, double y, int? steps = null);

        /// <summary>
        /// Shortcut for <see cref="MoveAsync(double, double, int?)"/>, <see cref="DownAsync(MouseButton, int)"/> and <see cref="UpAsync(MouseButton, int)"/>.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="delay">Time to wait between <c>mousedown</c> and <c>mouseup</c> in milliseconds. Defaults to 0.</param>
        /// <param name="button">Button to click. Details to <see cref="MouseButton.Left"/>.</param>
        /// <param name="clickCount">Click count. Defaults to 1.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task ClickAsync(double x, double y, int delay = 0, MouseButton button = MouseButton.Left, int clickCount = 1);

        /// <summary>
        /// Shortcut for <see cref="MoveAsync(double, double, int?)"/>, <see cref="DownAsync(MouseButton, int)"/>, <see cref="UpAsync(MouseButton, int)"/>, <see cref="DownAsync(MouseButton, int)"/> and <see cref="UpAsync(MouseButton, int)"/>.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="options">Extra options.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task DoubleClickAsync(double x, double y, ClickOptions options = null);

        /// <summary>
        /// Dispatches a <c>mousedown</c> event.
        /// </summary>
        /// <param name="button">The button to use for the click. Defaults to <see cref="MouseButton.Left"/>.</param>
        /// <param name="clickCount">Click count. Defaults to 1.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task DownAsync(MouseButton button = MouseButton.Left, int clickCount = 1);

        /// <summary>
        /// Dispatches a <c>mouseup</c> event.
        /// </summary>
        /// <param name="button">The button to use for the click. Defaults to <see cref="MouseButton.Left"/>.</param>
        /// <param name="clickCount">Click count. Defaults to 1.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task UpAsync(MouseButton button = MouseButton.Left, int clickCount = 1);

        /// <summary>
        /// Dispatches a <c>wheel</c> event.
        /// </summary>
        /// <param name="deltaX">delta X.</param>
        /// <param name="deltaY">delta Y.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task WheelAsync(double deltaX, double deltaY);
    }
}
