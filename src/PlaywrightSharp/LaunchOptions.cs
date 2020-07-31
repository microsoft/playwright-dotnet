using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/>.
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
        public int? Timeout { get; set; }

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
        /// If <c>true</c>, then do not use <see cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>.
        /// Dangerous option; use with care. Defaults to <c>false</c>.
        /// </summary>
        public bool IgnoreDefaultArgs { get; set; }

        /// <summary>
        /// if <see cref="IgnoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter <see cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>.
        /// </summary>
        public string[] IgnoredDefaultArgs { get; set; }

        /// <summary>
        /// Specify environment variables that will be visible to browser. Defaults to Environment variables.
        /// </summary>
        public IDictionary<string, string> Env { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Converts the <see cref="LaunchOptions"/> to <see cref="LaunchPersistentOptions"/>.
        /// </summary>
        /// <param name="options">Option to convert.</param>
        public static implicit operator LaunchPersistentOptions(LaunchOptions options)
            => options == null
            ? null
            : new LaunchPersistentOptions
            {
                Headless = options.Headless,
                Args = options.Args,
                UserDataDir = options.UserDataDir,
                Devtools = options.Devtools,
                ExecutablePath = options.ExecutablePath,
                IgnoreHTTPSErrors = options.IgnoreHTTPSErrors,
                Timeout = options.Timeout,
                DumpIO = options.DumpIO,
                SlowMo = options.SlowMo,
                LogProcess = options.LogProcess,
                IgnoreDefaultArgs = options.IgnoreDefaultArgs,
                IgnoredDefaultArgs = options.IgnoredDefaultArgs,
                Env = options.Env,
            };

        /// <summary>
        /// Converts the <see cref="LaunchOptions"/> to <see cref="LaunchPersistentOptions"/>.
        /// </summary>
        /// <returns>A <see cref="LaunchPersistentOptions"/> with the same information as the <see cref="LaunchOptions"/>.</returns>
        public LaunchPersistentOptions ToLaunchPersistentOptions() => this;
    }
}
