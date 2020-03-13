using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    /// <summary>
    /// Browser specific mouse implementation.
    /// </summary>
    internal interface IRawMouse
    {
        /// <summary>
        /// Move implementation.
        /// </summary>
        /// <param name="x">X cordinate.</param>
        /// <param name="y">Y cordinate.</param>
        /// <param name="lastButton">Last button pressed.</param>
        /// <param name="buttons">Buttons pressed.</param>
        /// <param name="modifiers">Keys modifiers.</param>
        /// <returns>A <see cref="Task"/> that completes when the action is confirmed by the browser.</returns>
        Task MoveAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers);

        /// <summary>
        /// Down implementation.
        /// </summary>
        /// <param name="x">X cordinate.</param>
        /// <param name="y">Y cordinate.</param>
        /// <param name="lastButton">Last button pressed.</param>
        /// <param name="buttons">Buttons pressed.</param>
        /// <param name="modifiers">Keys modifiers.</param>
        /// <param name="clickCount">Click count.</param>
        /// <returns>A <see cref="Task"/> that completes when the action is confirmed by the browser.</returns>
        Task DownAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount);

        /// <summary>
        /// Up implementation.
        /// </summary>
        /// <param name="x">X cordinate.</param>
        /// <param name="y">Y cordinate.</param>
        /// <param name="lastButton">Last button pressed.</param>
        /// <param name="buttons">Buttons pressed.</param>
        /// <param name="modifiers">Keys modifiers.</param>
        /// <param name="clickCount">Click count.</param>
        /// <returns>A <see cref="Task"/> that completes when the action is confirmed by the browser.</returns>
        Task UpAsync(double x, double y, MouseButton lastButton, List<MouseButton> buttons, Modifier[] modifiers, int clickCount);
    }
}
