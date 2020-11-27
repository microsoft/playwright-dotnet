using System;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// Cookie model.
    /// </summary>
    public class HarCookie
    {
        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Domain.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Expire date.
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Is HTTP only.
        /// </summary>
        public bool? HttpOnly { get; set; }

        /// <summary>
        /// Is secure.
        /// </summary>
        public bool? Secure { get; set; }

        /// <summary>
        /// SameSite.
        /// </summary>
        public SameSite SameSite { get; set; } = SameSite.None;
    }
}
