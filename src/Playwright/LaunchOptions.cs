using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/>.
    /// </summary>
    public class LaunchOptions : LaunchOptionsBase
    {
        public IEnumerable<KeyValuePair<string, object>> FirefoxUserPrefs { get; set; }

        /// <summary>
        /// Converts the <see cref="LaunchOptions"/> to <see cref="LaunchPersistentOptions"/>.
        /// </summary>
        /// <returns>The object converted to <see cref="LaunchPersistentOptions"/>.</returns>
        public LaunchPersistentOptions ToPersistentOptions()
            => new()
            {
                Headless = Headless,
                Args = Args,
                Devtools = Devtools,
                ExecutablePath = ExecutablePath,
                DownloadsPath = DownloadsPath,
                IgnoreHTTPSErrors = IgnoreHTTPSErrors,
                Timeout = Timeout,
                SlowMo = SlowMo,
                IgnoreAllDefaultArgs = IgnoreAllDefaultArgs,
                IgnoreDefaultArgs = IgnoreDefaultArgs,
                Env = Env,
                Proxy = Proxy,
                ChromiumSandbox = ChromiumSandbox,
                RecordHarOmitContent = RecordHarOmitContent,
                RecordHarPath = RecordHarPath,
                RecordVideoDir = RecordVideoDir,
                RecordVideoSize = RecordVideoSize,
            };
    }
}
