using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserType"/>
    public interface IChromiumBrowserType : IBrowserType
    {
        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        new Task<IChromiumBrowser> LaunchAsync(LaunchOptions options);

        /// <inheritdoc cref="IBrowserType.LaunchAsync(bool?, string[], string, bool?, string, string, bool?, int?, bool?, int?, bool?, string[], Dictionary{string, string}, Dictionary{string, object}, ProxySettings, bool?, bool?, bool?, bool?)"/>
        new Task<IChromiumBrowser> LaunchAsync(
            bool? headless = null,
            string[] args = null,
            string userDataDir = null,
            bool? devtools = null,
            string executablePath = null,
            string downloadsPath = null,
            bool? ignoreHTTPSErrors = null,
            int? timeout = null,
            bool? dumpIO = null,
            int? slowMo = null,
            bool? ignoreDefaultArgs = null,
            string[] ignoredDefaultArgs = null,
            Dictionary<string, string> env = null,
            Dictionary<string, object> firefoxUserPrefs = null,
            ProxySettings proxy = null,
            bool? chromiumSandbox = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? handleSIGHUP = null);

        /// <inheritdoc cref="IBrowserType.LaunchPersistentContextAsync(string, LaunchOptions)"/>
        new Task<IChromiumBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchOptions options);

        /// <inheritdoc cref="IBrowserType.LaunchPersistentContextAsync(string, ViewportSize, bool?, string[], bool?, string, string, bool?, int?, bool?, int?, bool?, string[], Dictionary{string, string}, Dictionary{string, object}, ProxySettings, string, bool?, bool?, string, Geolocation, ContextPermission[], bool?, bool?, decimal?, Credentials, bool?, bool?, ColorScheme?, string, Dictionary{string, string}, bool?, bool?, bool?, bool?, RecordHarOptions, RecordVideoOptions)"/>
        new Task<IChromiumBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            ViewportSize viewport,
            bool? headless = null,
            string[] args = null,
            bool? devtools = null,
            string executablePath = null,
            string downloadsPath = null,
            bool? ignoreHTTPSErrors = null,
            int? timeout = null,
            bool? dumpIO = null,
            int? slowMo = null,
            bool? ignoreDefaultArgs = null,
            string[] ignoredDefaultArgs = null,
            Dictionary<string, string> env = null,
            Dictionary<string, object> firefoxUserPrefs = null,
            ProxySettings proxy = null,
            string userAgent = null,
            bool? bypassCSP = null,
            bool? javaScriptEnabled = null,
            string timezoneId = null,
            Geolocation geolocation = null,
            ContextPermission[] permissions = null,
            bool? isMobile = null,
            bool? offline = null,
            decimal? deviceScaleFactor = null,
            Credentials httpCredentials = null,
            bool? hasTouch = null,
            bool? acceptDownloads = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null,
            bool? chromiumSandbox = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? handleSIGHUP = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null);

        /// <inheritdoc cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, string[], bool?, string, string, bool?, int?, bool?, int?, bool?, string[], Dictionary{string, string}, Dictionary{string, object}, ProxySettings, string, bool?, bool?, string, Geolocation, ContextPermission[], bool?, bool?, decimal?, Credentials, bool?, bool?, ColorScheme?, string, Dictionary{string, string}, bool?, bool?, bool?, bool?, RecordHarOptions, RecordVideoOptions)"/>
        new Task<IChromiumBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            bool? headless = null,
            string[] args = null,
            bool? devtools = null,
            string executablePath = null,
            string downloadsPath = null,
            bool? ignoreHTTPSErrors = null,
            int? timeout = null,
            bool? dumpIO = null,
            int? slowMo = null,
            bool? ignoreDefaultArgs = null,
            string[] ignoredDefaultArgs = null,
            Dictionary<string, string> env = null,
            Dictionary<string, object> firefoxUserPrefs = null,
            ProxySettings proxy = null,
            string userAgent = null,
            bool? bypassCSP = null,
            bool? javaScriptEnabled = null,
            string timezoneId = null,
            Geolocation geolocation = null,
            ContextPermission[] permissions = null,
            bool? isMobile = null,
            bool? offline = null,
            decimal? deviceScaleFactor = null,
            Credentials httpCredentials = null,
            bool? hasTouch = null,
            bool? acceptDownloads = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null,
            bool? chromiumSandbox = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? handleSIGHUP = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null);

        /// <inheritdoc cref="IBrowserType.LaunchPersistentContextAsync(string, LaunchPersistentOptions)"/>
        new Task<IChromiumBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options);
    }
}
