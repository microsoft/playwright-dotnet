using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserType provides methods to launch a specific browser instance or connect to an existing one.
    /// </summary>
    public interface IBrowserType
    {
        /// <summary>
        /// Executable path.
        /// </summary>
        string ExecutablePath { get; }

        /// <summary>
        /// Returns browser name. For example: 'chromium', 'webkit' or 'firefox'.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<IBrowser> LaunchAsync(LaunchOptions options);

        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="headless">Whether to run browser in headless mode. Defaults to true unless the devtools option is true.</param>
        /// <param name="args">Additional arguments to pass to the browser instance.</param>
        /// <param name="userDataDir">Path to a User Data Directory.</param>
        /// <param name="devtools">Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.</param>
        /// <param name="executablePath">Path to a browser executable to run instead of the bundled one.</param>
        /// <param name="downloadsPath">If specified, accepted downloads are downloaded into this folder. Otherwise, temporary folder is created and is deleted when browser is closed.</param>
        /// <param name="ignoreHTTPSErrors">Whether to ignore HTTPS errors during navigation. Defaults to false.</param>
        /// <param name="timeout">Maximum time in milliseconds to wait for the browser instance to start.</param>
        /// <param name="dumpIO">Whether to pipe browser process stdout and stderr into process.stdout and process.stderr. Defaults to false.</param>
        /// <param name="slowMo">Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.</param>
        /// <param name="ignoreDefaultArgs">If true, Playwright does not pass its own configurations args and only uses the ones from args.
        /// Dangerous option; use with care. Defaults to false.</param>
        /// <param name="ignoredDefaultArgs">if <paramref name="ignoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter default arguments.</param>
        /// <param name="env">Specify environment variables that will be visible to browser. Defaults to Environment variables.</param>
        /// <param name="firefoxUserPrefs">Firefox user preferences. Learn more about the Firefox user preferences at about:config.</param>
        /// <param name="proxy">Network proxy settings.</param>
        /// <param name="chromiumSandbox">Enable Chromium sandboxing. Defaults to true.</param>
        /// <param name="handleSIGINT">Close the browser process on Ctrl-C. Defaults to true.</param>
        /// <param name="handleSIGTERM">Close the browser process on SIGTERM. Defaults to true.</param>
        /// <param name="handleSIGHUP">Close the browser process on SIGHUP. Defaults to true.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<IBrowser> LaunchAsync(
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

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchOptions options);

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="viewport">Sets a consistent viewport for each page. Defaults to an 800x600 viewport. null disables the default viewport.</param>
        /// <param name="headless">Whether to run browser in headless mode. Defaults to true unless the devtools option is true.</param>
        /// <param name="args">Additional arguments to pass to the browser instance.</param>
        /// <param name="devtools">Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.</param>
        /// <param name="executablePath">Path to a browser executable to run instead of the bundled one.</param>
        /// <param name="downloadsPath">If specified, accepted downloads are downloaded into this folder. Otherwise, temporary folder is created and is deleted when browser is closed.</param>
        /// <param name="ignoreHTTPSErrors">Whether to ignore HTTPS errors during navigation. Defaults to false.</param>
        /// <param name="timeout">Maximum time in milliseconds to wait for the browser instance to start.</param>
        /// <param name="dumpIO">Whether to pipe browser process stdout and stderr into process.stdout and process.stderr. Defaults to false.</param>
        /// <param name="slowMo">Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.</param>
        /// <param name="ignoreDefaultArgs">If true, Playwright does not pass its own configurations args and only uses the ones from args.
        /// Dangerous option; use with care. Defaults to false.</param>
        /// <param name="ignoredDefaultArgs">if <paramref name="ignoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter default arguments.</param>
        /// <param name="env">Specify environment variables that will be visible to browser. Defaults to Environment variables.</param>
        /// <param name="firefoxUserPrefs">Firefox user preferences. Learn more about the Firefox user preferences at about:config.</param>
        /// <param name="proxy">Network proxy settings.</param>
        /// <param name="userAgent">Specific user agent to use in this context.</param>
        /// <param name="bypassCSP">Toggles bypassing page's Content-Security-Policy.</param>
        /// <param name="javaScriptEnabled">Whether or not to enable or disable JavaScript in the context. Defaults to true.</param>
        /// <param name="timezoneId">Changes the timezone of the context. See <see href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU’s metaZones.txt</see> for a list of supported timezone IDs.</param>
        /// <param name="geolocation">Changes the Geolocation of the context.</param>
        /// <param name="permissions">A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.GrantPermissionsAsync(ContextPermission[], string)"/> for more details.</param>
        /// <param name="isMobile">Gets or sets whether the meta viewport tag is taken into account.</param>
        /// <param name="offline">Whether to emulate network being offline. Defaults to `false`.</param>
        /// <param name="deviceScaleFactor">Gets or sets the device scale factor.</param>
        /// <param name="httpCredentials">Credentials for HTTP authentication.</param>
        /// <param name="hasTouch">Specifies if viewport supports touch events. Defaults to false.</param>
        /// <param name="acceptDownloads">Whether to automatically download all the attachments. Defaults to false where all the downloads are canceled.</param>
        /// <param name="colorScheme">Emulates 'prefers-colors-scheme' media feature.</param>
        /// <param name="locale">Specify user locale, for example en-GB, de-DE, etc. Locale will affect navigator.language value, Accept-Language request header value as well as number and date formatting rules.</param>
        /// <param name="extraHttpHeaders">An object containing additional HTTP headers to be sent with every request.</param>
        /// <param name="chromiumSandbox">Enable Chromium sandboxing. Defaults to true.</param>
        /// <param name="handleSIGINT">Close the browser process on Ctrl-C. Defaults to true.</param>
        /// <param name="handleSIGTERM">Close the browser process on SIGTERM. Defaults to true.</param>
        /// <param name="handleSIGHUP">Close the browser process on SIGHUP. Defaults to true.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistentContextAsync(
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
            bool? handleSIGHUP = null);

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="headless">Whether to run browser in headless mode. Defaults to true unless the devtools option is true.</param>
        /// <param name="args">Additional arguments to pass to the browser instance.</param>
        /// <param name="devtools">Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.</param>
        /// <param name="executablePath">Path to a browser executable to run instead of the bundled one.</param>
        /// <param name="downloadsPath">If specified, accepted downloads are downloaded into this folder. Otherwise, temporary folder is created and is deleted when browser is closed.</param>
        /// <param name="ignoreHTTPSErrors">Whether to ignore HTTPS errors during navigation. Defaults to false.</param>
        /// <param name="timeout">Maximum time in milliseconds to wait for the browser instance to start.</param>
        /// <param name="dumpIO">Whether to pipe browser process stdout and stderr into process.stdout and process.stderr. Defaults to false.</param>
        /// <param name="slowMo">Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.</param>
        /// <param name="ignoreDefaultArgs">If true, Playwright does not pass its own configurations args and only uses the ones from args.
        /// Dangerous option; use with care. Defaults to false.</param>
        /// <param name="ignoredDefaultArgs">if <paramref name="ignoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter default arguments.</param>
        /// <param name="env">Specify environment variables that will be visible to browser. Defaults to Environment variables.</param>
        /// <param name="firefoxUserPrefs">Firefox user preferences. Learn more about the Firefox user preferences at about:config.</param>
        /// <param name="proxy">Network proxy settings.</param>
        /// <param name="userAgent">Specific user agent to use in this context.</param>
        /// <param name="bypassCSP">Toggles bypassing page's Content-Security-Policy.</param>
        /// <param name="javaScriptEnabled">Whether or not to enable or disable JavaScript in the context. Defaults to true.</param>
        /// <param name="timezoneId">Changes the timezone of the context. See <see href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU’s metaZones.txt</see> for a list of supported timezone IDs.</param>
        /// <param name="geolocation">Changes the Geolocation of the context.</param>
        /// <param name="permissions">A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.GrantPermissionsAsync(ContextPermission[], string)"/> for more details.</param>
        /// <param name="isMobile">Gets or sets whether the meta viewport tag is taken into account.</param>
        /// <param name="offline">Whether to emulate network being offline. Defaults to `false`.</param>
        /// <param name="deviceScaleFactor">Gets or sets the device scale factor.</param>
        /// <param name="httpCredentials">Credentials for HTTP authentication.</param>
        /// <param name="hasTouch">Specifies if viewport supports touch events. Defaults to false.</param>
        /// <param name="acceptDownloads">Whether to automatically download all the attachments. Defaults to false where all the downloads are canceled.</param>
        /// <param name="colorScheme">Emulates 'prefers-colors-scheme' media feature.</param>
        /// <param name="locale">Specify user locale, for example en-GB, de-DE, etc. Locale will affect navigator.language value, Accept-Language request header value as well as number and date formatting rules.</param>
        /// <param name="extraHttpHeaders">An object containing additional HTTP headers to be sent with every request.</param>
        /// <param name="chromiumSandbox">Enable Chromium sandboxing. Defaults to true.</param>
        /// <param name="handleSIGINT">Close the browser process on Ctrl-C. Defaults to true.</param>
        /// <param name="handleSIGTERM">Close the browser process on SIGTERM. Defaults to true.</param>
        /// <param name="handleSIGHUP">Close the browser process on SIGHUP. Defaults to true.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistentContextAsync(
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
            bool? handleSIGHUP = null);

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options);
    }
}
