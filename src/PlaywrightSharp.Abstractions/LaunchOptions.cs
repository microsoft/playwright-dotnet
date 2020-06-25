namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/>.
    /// </summary>
    public class LaunchOptions : LaunchOptionsBase
    {
        /// <summary>
        /// Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.
        /// </summary>
        public int SlowMo { get; set; }

        internal TestHookDelegate TestHook { get; set; }

        internal LaunchServerOptions ToLaunchServerOptions()
            => new LaunchServerOptions
            {
                ExecutablePath = ExecutablePath,
                Args = Args,
                IgnoreDefaultArgs = IgnoreDefaultArgs,
                IgnoredDefaultArgs = IgnoredDefaultArgs,
                Timeout = Timeout,
                Headless = Headless,
                Devtools = Devtools,
                Proxy = Proxy,
                DownloadsPath = DownloadsPath,
            };
    }
}
