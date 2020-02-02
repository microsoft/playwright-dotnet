using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Keyboard provides an api for managing a virtual keyboard.
    /// </summary>
    public interface IKeyboard
    {
        /// <summary>
        /// <![CDATA[
        /// Dispatches a <c>keydown</c> event
        /// ]]>
        /// </summary>
        /// <param name="key">Name of key to press, such as <c>ArrowLeft</c>.</param>
        /// <param name="options">down options</param>
        /// <remarks>
        /// If <c>key</c> is a single character and no modifier keys besides <c>Shift</c> are being held down, a <c>keypress</c>/<c>input</c> event will also generated. The <c>text</c> option can be specified to force an input event to be generated.
        /// If <c>key</c> is a modifier key, <c>Shift</c>, <c>Meta</c>, <c>Control</c>, or <c>Alt</c>, subsequent key presses will be sent with that modifier active. To release the modifier key, use <see cref="UpAsync(string)"/>
        /// After the key is pressed once, subsequent calls to <see cref="DownAsync(string, DownOptions)"/> will have <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/repeat">repeat</see> set to <c>true</c>. To release the key, use <see cref="UpAsync(string)"/>
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser</returns>
        Task DownAsync(string key, DownOptions options = null);

        /// <summary>
        /// Dispatches a <c>keyup</c> event.
        /// </summary>
        /// <param name="key">Name of key to release, such as `ArrowLeft`.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser</returns>
        Task UpAsync(string key);

        /// <summary>
        /// Sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="text">A text to type into a focused element</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser</returns>
        Task TypeAsync(string text);
    }
}
