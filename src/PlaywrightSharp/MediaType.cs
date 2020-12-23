using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.EmulateMediaAsync(MediaType?, ColorScheme?)"/>.
    /// </summary>
    public enum MediaType
    {
        /// <summary>
        /// Media Print.
        /// </summary>
        [EnumMember(Value = "print")]
        Print,

        /// <summary>
        /// Media Screen.
        /// </summary>
        [EnumMember(Value = "screen")]
        Screen,

        /// <summary>
        /// None.
        /// </summary>
        [EnumMember(Value = "null")]
        Null,
    }
}
