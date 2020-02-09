namespace PlaywrightSharp
{
    /// <summary>
    /// Configuration for <see cref="BrowserFetcher"/>.
    /// </summary>
    public class BrowserFetcherConfig
    {
        /// <summary>
        /// Download URL based on the platform and revision.
        /// </summary>
        public string DownloadURL { get; set; }

        /// <summary>
        /// Executable path based on the platform and revision.
        /// </summary>
        public string ExecutablePath { get; set; }
    }
}
