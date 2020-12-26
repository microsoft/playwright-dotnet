using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// SameSite values in cookies.
    /// </summary>
    public enum SameSite
    {
        /// <summary>
        /// Strict.
        /// </summary>
        [EnumMember(Value = "Strict")]
        Strict,

        /// <summary>
        /// Lax.
        /// </summary>
        [EnumMember(Value = "Lax")]
        Lax,

        /// <summary>
        /// None.
        /// </summary>
        [EnumMember(Value = "None")]
        None,
    }
}
