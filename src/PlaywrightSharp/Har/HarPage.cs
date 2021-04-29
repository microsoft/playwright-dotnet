using System;

namespace Microsoft.Playwright.Har
{
    /// <summary>
    /// HAR Page.
    /// </summary>
    public partial class HarPage
    {
        /// <summary>
        /// Page Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Page title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Started date.
        /// </summary>
        public DateTime StartedDateTime { get; set; }

        /// <summary>
        /// Timings info.
        /// </summary>
        public HarPageTimings PageTimings { get; set; }
    }
}
