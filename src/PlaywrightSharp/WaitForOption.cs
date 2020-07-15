using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Wait for options for <see cref="WaitForSelectorOptions.State"/>.
    /// </summary>
    public enum WaitForState
    {
        /// <summary>
        /// Attached.
        /// </summary>
        Attached,

        /// <summary>
        /// Detached.
        /// </summary>
        Detached,

        /// <summary>
        /// Wait for visible.
        /// </summary>
        Visible,

        /// <summary>
        /// Wait for hidden.
        /// </summary>
        Hidden,
    }
}
