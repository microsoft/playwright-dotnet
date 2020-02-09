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

        /// <summary>
        /// Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.
        /// </summary>
        public int SlowMo { get; set; }

        /// <summary>
        /// Logs process counts after launching the browser and after exiting.
        /// </summary>
        public bool LogProcess { get; set; }

        /// <summary>
        /// Optional factory for <see cref="IConnectionTransport"/> implementations.
        /// </summary>
        public TransportFactory TransportFactory { get; set; }

        /// <summary>
        /// If no <see cref="IConnectionTransport"/> is set, this will be use to determine is the default <see cref="IConnectionTransport"/> will enqueue messages.
        /// </summary>
        /// <remarks>
        /// It's set to <c>true</c> by default because it's the safest way to send commands to Chromium.
        /// Setting this to <c>false</c> proved to work in .NET Core but it tends to fail on .NET Framework.
        /// </remarks>
        public bool EnqueueTransportMessages { get; set; } = true;
    }
}
