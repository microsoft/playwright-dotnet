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
        /// <inheritdoc path="/param[@name='firefoxUserPrefs']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
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
