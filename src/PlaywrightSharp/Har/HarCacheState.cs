using System;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR Cache state.
    /// </summary>
    public class HarCacheState
    {
        /// <summary>
        /// Expiration date.
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Last access date.
        /// </summary>
        public DateTime? LastAccess { get; set; }

        /// <summary>
        /// eTag.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Hit count.
        /// </summary>
        public int HitCount { get; set; }
    }
}
