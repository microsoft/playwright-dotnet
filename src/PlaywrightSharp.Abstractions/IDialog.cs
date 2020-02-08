using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IDialog"/> objects are dispatched by page via the <see cref="IPage.Dialog"/> event.
    /// </summary>
    /// <example>
    /// An example of using Dialog class:
    /// <code>
    /// <![CDATA[
    /// Page.Dialog += async (sender, e) =>
    /// {
    ///     await e.Dialog.AcceptAsync();
    /// }
    /// await Page.EvaluateAsync("alert('yo');");
    /// ]]>
    /// </code>
    /// </example>
    public interface IDialog
    {
        /// <summary>
        /// Dialog's type, can be one of alert, beforeunload, confirm or prompt.
        /// </summary>
        /// <value>The type of the dialog.</value>
        DialogType DialogType { get; set; }

        /// <summary>
        /// If dialog is prompt, returns default prompt value. Otherwise, returns empty string.
        /// </summary>
        /// <value>The default value.</value>
        string DefaultValue { get; set; }

        /// <summary>
        /// A message displayed in the dialog.
        /// </summary>
        /// <value>The message.</value>
        string Message { get; set; }

        /// <summary>
        /// Accept the Dialog.
        /// </summary>
        /// <param name="promptText">A text to enter in prompt. Does not cause any effects if the dialog's type is not prompt.</param>
        /// <returns>A <see cref="Task"/> that completes when the dialog has been accepted.</returns>
        Task AcceptAsync(string promptText = "");

        /// <summary>
        /// Dismiss the dialog.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the dialog has been dismissed.</returns>
        Task DismissAsync();
    }
}
