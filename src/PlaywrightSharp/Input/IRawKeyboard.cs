using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    /// <summary>
    /// Browser specific keyboard implementation.
    /// </summary>
    internal interface IRawKeyboard
    {
        /// <summary>
        /// Dispatches a <c>keypress</c> and <c>input</c> event. This does not send a <c>keydown</c> or <c>keyup</c> event.
        /// </summary>
        /// <param name="text">Character to send into the page.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task SendTextAsync(string text);

        /// <summary>
        /// Dispatches a <c>keydown</c> event.
        /// </summary>
        /// <param name="modifiers">The modifiers.</param>
        /// <param name="code">The code.</param>
        /// <param name="keyCode">The keyCode.</param>
        /// <param name="keyCodeWithoutLocation">The keyCode without location.</param>
        /// <param name="key">The key.</param>
        /// <param name="location">The location.</param>
        /// <param name="autoRepeat">The autoRepeat option.</param>
        /// <param name="text">The text.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task KeyDownAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, double location, bool autoRepeat, string text);

        /// <summary>
        /// Dispatches a <c>keyup</c> event.
        /// </summary>
        /// <param name="modifiers">The modifiers.</param>
        /// <param name="code">The code.</param>
        /// <param name="keyCode">The keyCode.</param>
        /// <param name="keyCodeWithoutLocation">The keyCode without location.</param>
        /// <param name="key">The key.</param>
        /// <param name="location">The location.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task KeyUpAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, double location);
    }
}
