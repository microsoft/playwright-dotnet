using System.Runtime.Serialization;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Dialog type.
    /// </summary>
    /// <seealso cref="IDialog"/>
    public static class DialogType
    {
        /// <summary>
        /// Alert dialog.
        /// </summary>
        public const string Alert = "alert";

        /// <summary>
        /// Prompt dialog.
        /// </summary>
        public const string Prompt = "prompt";

        /// <summary>
        /// Confirm dialog.
        /// </summary>
        public const string Confirm = "confirm";

        /// <summary>
        /// Before unload dialog.
        /// </summary>
        public const string BeforeUnload = "beforeunload";
    }
}
