using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// A Browser is created when Playwright connects to a browser instance.
    /// </summary>
    public interface IBrowser : IAsyncDisposable
    {
        /// <summary>
        /// Raised when the <see cref="IBrowser"/> gets disconnected from the browser instance.
        /// This might happen because one of the following:
        /// - Browser is closed or crashed
        /// - <see cref="CloseAsync"/> method was called
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Returns the browser version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Indicates that the browser is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns an array of all open browser contexts. In a newly created browser, this will return zero browser contexts.
        /// </summary>
        IBrowserContext[] Contexts { get; }

        /// <summary>
        /// Starts tracing.
        /// </summary>
        /// <param name="page">Optional, if specified, tracing includes screenshots of the given page.</param>
        /// <param name="screenshots">Gets or sets a value indicating whether Tracing should captures screenshots in the trace.</param>
        /// <param name="path">A path to write the trace file to.</param>
        /// <param name="categories">Specify custom categories to use instead of default.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task StartTracingAsync(IPage page = null, bool screenshots = false, string path = null, IEnumerable<string> categories = null);

        /// <summary>
        /// Stops tracing.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser, yielding the tracing result.</returns>
        Task<string> StopTracingAsync();

        /// <summary>
        /// Closes browser and all of its pages (if any were opened).
        /// The Browser object itself is considered to be disposed and cannot be used anymore.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// Creates a new browser context. It won't share cookies/cache with other browser contexts.
        /// </summary>
        /// <param name="viewport">Sets a consistent viewport for each page. Defaults to an 800x600 viewport. null disables the default viewport.</param>
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
        /// <param name="ignoreHTTPSErrors"> Whether to ignore HTTPS errors during navigation. Defaults to false.</param>
        /// <param name="colorScheme">Emulates 'prefers-colors-scheme' media feature.</param>
        /// <param name="locale">Specify user locale, for example en-GB, de-DE, etc. Locale will affect navigator.language value, Accept-Language request header value as well as number and date formatting rules.</param>
        /// <param name="extraHttpHeaders">An object containing additional HTTP headers to be sent with every request.</param>
        /// <example>.
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IBrowserContext}"/> that completes when a new <see cref="IBrowserContext"/> is created.</returns>
        Task<IBrowserContext> NewContextAsync(
            ViewportSize viewport,
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
            bool? ignoreHTTPSErrors = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null);

        /// <summary>
        /// Creates a new browser context. It won't share cookies/cache with other browser contexts.
        /// </summary>
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
        /// <example>.
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IBrowserContext}"/> that completes when a new <see cref="IBrowserContext"/> is created.</returns>
        Task<IBrowserContext> NewContextAsync(
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
            Dictionary<string, string> extraHttpHeaders = null);

        /// <summary>
        /// Creates a new browser context. It won't share cookies/cache with other browser contexts.
        /// </summary>
        /// <param name="options">Context options.</param>
        /// <example>.
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IBrowserContext}"/> that completes when a new <see cref="IBrowserContext"/> is created.</returns>
        Task<IBrowserContext> NewContextAsync(BrowserContextOptions options);

        /// <summary>
        /// Creates a new page in a new browser context. Closing this page will close the context as well.
        /// </summary>
        /// <param name="viewport">Sets a consistent viewport for each page. Defaults to an 800x600 viewport. null disables the default viewport.</param>
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
        /// <param name="ignoreHTTPSErrors"> Whether to ignore HTTPS errors during navigation. Defaults to false.</param>
        /// <param name="colorScheme">Emulates 'prefers-colors-scheme' media feature.</param>
        /// <param name="locale">Specify user locale, for example en-GB, de-DE, etc. Locale will affect navigator.language value, Accept-Language request header value as well as number and date formatting rules.</param>
        /// <param name="extraHttpHeaders">An object containing additional HTTP headers to be sent with every request.</param>
        /// <example>.
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created.</returns>
        Task<IPage> NewPageAsync(
            ViewportSize viewport,
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
            bool? ignoreHTTPSErrors = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null);

        /// <summary>
        /// Creates a new page in a new browser context. Closing this page will close the context as well.
        /// </summary>
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
        /// <example>.
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created.</returns>
        Task<IPage> NewPageAsync(
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
            Dictionary<string, string> extraHttpHeaders = null);

        /// <summary>
        /// Creates a new page in a new browser context. Closing this page will close the context as well.
        /// </summary>
        /// <param name="options">Context options.</param>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created.</returns>
        Task<IPage> NewPageAsync(BrowserContextOptions options);

        /// <summary>
        /// Creates a new browser session.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser session was created, yielding the new session.</returns>
        Task<ICDPSession> NewBrowserCDPSessionAsync();
    }
}
