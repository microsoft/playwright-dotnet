using System;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR entry.
    /// </summary>
    public class HarEntry
    {
        /// <summary>
        /// Referenced page.
        /// </summary>
        public string Pageref { get; set; }

        /// <summary>
        /// Request.
        /// </summary>
        public HarEntryRequest Request { get; set; }

        /// <summary>
        /// Response.
        /// </summary>
        public HarEntryResponse Response { get; set; }

        /// <summary>
        /// Started date.
        /// </summary>
        public DateTime StartedDateTime { get; set; }

        /// <summary>
        /// Time.
        /// </summary>
        public decimal Time { get; set; }
    }
}
