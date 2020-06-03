using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Dialog type.
    /// </summary>
    /// <seealso cref="IDialog"/>
    public enum DialogType
    {
        /// <summary>
        /// Alert dialog.
        /// </summary>
        Alert,

        /// <summary>
        /// Prompt dialog.
        /// </summary>
        Prompt,

        /// <summary>
        /// Confirm dialog.
        /// </summary>
        Confirm,

        /// <summary>
        /// Before unload dialog.
        /// </summary>
        [EnumMember(Value = "beforeunload")]
        BeforeUnload,
    }
}
