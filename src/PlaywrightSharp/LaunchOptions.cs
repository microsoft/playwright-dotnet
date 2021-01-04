using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/>.
    /// </summary>
    public class LaunchOptions : LaunchOptionsBase
    {
        /// <summary>
        /// Firefox user preferences. Learn more about the Firefox user preferences at about:config.
        /// </summary>
        public Dictionary<string, object> FirefoxUserPrefs { get; set; }

        /// <summary>
        /// Converts the <see cref="LaunchOptions"/> to <see cref="LaunchPersistentOptions"/>.
        /// </summary>
        /// <returns>The object converted to <see cref="LaunchPersistentOptions"/>.</returns>
        public LaunchPersistentOptions ToPersistentOptions()
            => new LaunchPersistentOptions
            {
                Headless = Headless,
                Args = Args,
                UserDataDir = UserDataDir,
                Devtools = Devtools,
                ExecutablePath = ExecutablePath,
                DownloadsPath = DownloadsPath,
                IgnoreHTTPSErrors = IgnoreHTTPSErrors,
                Timeout = Timeout,
                DumpIO = DumpIO,
                SlowMo = SlowMo,
                IgnoreDefaultArgs = IgnoreDefaultArgs,
                IgnoredDefaultArgs = IgnoredDefaultArgs,
                Env = Env,
                Proxy = Proxy,
                ChromiumSandbox = ChromiumSandbox,
            };

        internal override Dictionary<string, object> ToChannelDictionary()
        {
            var args = base.ToChannelDictionary();

            if (FirefoxUserPrefs != null)
            {
                args["firefoxUserPrefs"] = FirefoxUserPrefs;
            }

            return args;
        }
    }
}
