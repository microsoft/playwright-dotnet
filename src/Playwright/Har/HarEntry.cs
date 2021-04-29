using System;

namespace Microsoft.Playwright.Har
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
        /// Cache.
        /// </summary>
        public HarCache Cache { get; set; }

        /// <summary>
        /// Entry Timings.
        /// </summary>
        public HarEntryTimings Timings { get; set; }

        /// <summary>
        /// Started date.
        /// </summary>
        public DateTime StartedDateTime { get; set; }

        /// <summary>
        /// Time.
        /// </summary>
        public decimal Time { get; set; }

        /// <summary>
        /// Server IP address.
        /// </summary>
        public string ServerIPAddress { get; set; }

        /// <summary>
        /// Connection.
        /// </summary>
        public string Connection { get; set; }
    }
}
