using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.EmulateMediaAsync(MediaType?, ColorScheme?)"/>.
    /// </summary>
    public enum ColorScheme
    {
        /// <summary>
        /// Light.
        /// </summary>
        [EnumMember(Value = "light")]
        Light,

        /// <summary>
        /// Dark.
        /// </summary>
        [EnumMember(Value = "dark")]
        Dark,

        /// <summary>
        /// No preference.
        /// </summary>
        [EnumMember(Value = "no-preference")]
        NoPreference,
    }
}
