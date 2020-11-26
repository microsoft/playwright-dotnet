using System.Drawing;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// The Touchscreen class operates in main-frame CSS pixels relative to the top-left corner of the viewport.
    /// Methods on the touchscreen can only be used in browser contexts that have been intialized with hasTouch set to true.
    /// </summary>
    public interface ITouchscreen
    {
        /// <summary>
        /// Dispatches a touchstart and touchend event with a single touch at the position (x,y).
        /// </summary>
        /// <param name="point">The point at which to dispatch the touch.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
       Task TapAsync(Point point);
    }
}
