using System.Threading.Tasks;
using PlaywrightSharp.Input;

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
        /// <remarks>
        /// If <c>key</c> is a single character and no modifier keys besides <c>Shift</c> are being held down, a <c>keypress</c>/<c>input</c> event will also generated. The <c>text</c> option can be specified to force an input event to be generated.
        /// If <c>key</c> is a modifier key, <c>Shift</c>, <c>Meta</c>, <c>Control</c>, or <c>Alt</c>, subsequent key presses will be sent with that modifier active. To release the modifier key, use <see cref="UpAsync(string)"/>
        /// After the key is pressed once, subsequent calls to <see cref="DownAsync(string)"/> will have <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/repeat">repeat</see> set to <c>true</c>. To release the key, use <see cref="UpAsync(string)"/>.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task DownAsync(string key);

        /// <summary>
        /// Dispatches a <c>keyup</c> event.
        /// </summary>
        /// <param name="key">Name of key to release, such as `ArrowLeft`.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task UpAsync(string key);

        /// <summary>
        /// Sends a <c>keydown</c>, <c>keypress</c>/<c>input</c>, and <c>keyup</c> event for each character in the text.
        /// </summary>
        /// <param name="text">A text to type into a focused element.</param>
        /// <param name="delay">Delay between key press.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task TypeAsync(string text, int delay = 0);

        /// <summary>
        /// Shortcut for <see cref="DownAsync(string)"/> and <see cref="UpAsync(string)"/>.
        /// </summary>
        /// <param name="key">Name of key to press, such as <c>ArrowLeft</c>. <see cref="KeyDefinitions"/> for a list of all key names.</param>
        /// <param name="delay">Time to wait between <c>keydown</c> and <c>keyup</c> in milliseconds. Defaults to 0.</param>
        /// <remarks>
        /// If <paramref name="key"/> is a single character and no modifier keys besides <c>Shift</c> are being held down, a <c>keypress</c>/<c>input</c> event will also generated.
        /// Modifier keys DO effect <see cref="IElementHandle.PressAsync(string, int, int?, bool?)"/>. Holding down <c>Shift</c> will type the text in upper case.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task PressAsync(string key, int delay = 0);

        /// <summary>
        /// Dispatches only input event, does not emit the keydown, keyup or keypress events.
        /// </summary>
        /// <param name="text">Sets input to the specified text value.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task InsertTextAsync(string text);
    }
}
