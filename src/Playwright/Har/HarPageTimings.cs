namespace Microsoft.Playwright.Har
{
    /// <summary>
    /// Page timings.
    /// </summary>
    public class HarPageTimings
    {
        /// <summary>
        /// On content load enlapsed time.
        /// </summary>
        public decimal OnContentLoad { get; set; }

        /// <summary>
        /// On load enlapsed time.
        /// </summary>
        public decimal OnLoad { get; set; }
    }
}
