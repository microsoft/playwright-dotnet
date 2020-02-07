namespace PlaywrightSharp
{
    /// <summary>
    /// Browser fetcher options used to construct a <see cref="IBrowserFetcher"/>.
    /// </summary>
    public class BrowserFetcherOptions
    {
        /// <summary>
        /// Platform, defaults to current platform.
        /// </summary>
        public Platform? Platform { get; set; }

        /// <summary>
        /// A path for the downloads folder.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// A download host to be used.
        /// </summary>
        public string Host { get; set; }
    }
}