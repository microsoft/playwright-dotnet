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
    }
}
