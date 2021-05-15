using System.Collections.Generic;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Base class for <see cref="LaunchOptions"/> and <see cref="LaunchOptionsBase"/>.
    /// </summary>
    public abstract class LaunchOptionsBase
    {
        /// <inheritdoc path="/param[@name='headless']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? Headless { get; set; }

        /// <inheritdoc path="/param[@name='args']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string[] Args { get; set; }

        /// <inheritdoc path="/param[@name='devtools']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? Devtools { get; set; }

        /// <inheritdoc path="/param[@name='executablePath']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string ExecutablePath { get; set; }

        /// <inheritdoc path="/param[@name='downloadsPath']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string DownloadsPath { get; set; }

        /// <inheritdoc path="/param[@name='ignoreHTTPSErrors']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? IgnoreHTTPSErrors { get; set; }

        /// <inheritdoc path="/param[@name='timeout']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public int? Timeout { get; set; }

        /// <inheritdoc path="/param[@name='slowMo']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public int? SlowMo { get; set; }

        /// <inheritdoc path="/param[@name='ignoreAllDefaultArgs']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? IgnoreAllDefaultArgs { get; set; }

        /// <inheritdoc path="/param[@name='handleSIGINT']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? HandleSIGINT { get; set; }

        /// <inheritdoc path="/param[@name='handleSIGTERM']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? HandleSIGTERM { get; set; }

        /// <inheritdoc path="/param[@name='handleSIGHUP']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? HandleSIGHUP { get; set; }

        /// <inheritdoc path="/param[@name='chromiumSandbox']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? ChromiumSandbox { get; set; }

        /// <inheritdoc path="/param[@name='ignoreDefaultArgs']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string[] IgnoreDefaultArgs { get; set; }

        /// <inheritdoc path="/param[@name='end']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public Dictionary<string, string> Env { get; set; }

        /// <inheritdoc path="/param[@name='proxy']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public Proxy Proxy { get; set; }

        /// <inheritdoc path="/param[@name='viewportSize']" cref="IBrowser.NewPageAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)" />
        public string RecordHarPath { get; set; }

        /// <inheritdoc path="/param[@name='recordHarOmitContent']" cref="IBrowser.NewPageAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)" />
        public bool? RecordHarOmitContent { get; set; }

        /// <inheritdoc path="/param[@name='recordVideoDir']" cref="IBrowser.NewPageAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)" />
        public string RecordVideoDir { get; set; }

        /// <inheritdoc path="/param[@name='recordVideoSize']" cref="IBrowser.NewPageAsync(bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, Proxy, string, string)" />
        public RecordVideoSize RecordVideoSize { get; set; }

        /// <inheritdoc path="/param[@name='channel']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public BrowserChannel Channel { get; set; }
    }
}
