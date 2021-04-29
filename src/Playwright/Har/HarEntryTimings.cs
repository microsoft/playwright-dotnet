namespace Microsoft.Playwright.Har
{
    /// <summary>
    /// HAR Entry timings.
    /// </summary>
    public class HarEntryTimings
    {
        /// <summary>
        /// Blocked.
        /// </summary>
        public decimal? Blocked { get; set; }

        /// <summary>
        /// Dns time.
        /// </summary>
        public decimal Dns { get; set; }

        /// <summary>
        /// Connect time.
        /// </summary>
        public decimal Connect { get; set; }

        /// <summary>
        /// Ssl time.
        /// </summary>
        public decimal Ssl { get; set; }

        /// <summary>
        /// Send time.
        /// </summary>
        public decimal Send { get; set; }

        /// <summary>
        /// Wait time.
        /// </summary>
        public decimal Wait { get; set; }

        /// <summary>
        /// Receive time.
        /// </summary>
        public decimal Receive { get; set; }
    }
}
