namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/> and <see cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>.
    /// </summary>
    public class LaunchOptions : BrowserArgOptions
    {
        /// <summary>
        /// Path to a browser executable to run instead of the bundled one.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Whether to ignore HTTPS errors during navigation. Defaults to false.
        /// </summary>
        public bool IgnoreHTTPSErrors { get; set; }

        /// <summary>
        /// Maximum time in milliseconds to wait for the browser instance to start.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        ///  Whether to pipe browser process stdout and stderr into process.stdout and process.stderr. Defaults to false.
        /// </summary>
        public bool DumpIO { get; set; }
    }
}
