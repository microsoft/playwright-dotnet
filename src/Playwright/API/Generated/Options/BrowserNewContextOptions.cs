/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#nullable enable

namespace Microsoft.Playwright;

public class BrowserNewContextOptions
{
    public BrowserNewContextOptions() { }

    public BrowserNewContextOptions(BrowserNewContextOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        AcceptDownloads = clone.AcceptDownloads;
        BaseURL = clone.BaseURL;
        BypassCSP = clone.BypassCSP;
        ClientCertificates = clone.ClientCertificates;
        ColorScheme = clone.ColorScheme;
        DeviceScaleFactor = clone.DeviceScaleFactor;
        ExtraHTTPHeaders = clone.ExtraHTTPHeaders;
        ForcedColors = clone.ForcedColors;
        Geolocation = clone.Geolocation;
        HasTouch = clone.HasTouch;
        HttpCredentials = clone.HttpCredentials;
        IgnoreHTTPSErrors = clone.IgnoreHTTPSErrors;
        IsMobile = clone.IsMobile;
        JavaScriptEnabled = clone.JavaScriptEnabled;
        Locale = clone.Locale;
        Offline = clone.Offline;
        Permissions = clone.Permissions;
        Proxy = clone.Proxy;
        RecordHarContent = clone.RecordHarContent;
        RecordHarMode = clone.RecordHarMode;
        RecordHarOmitContent = clone.RecordHarOmitContent;
        RecordHarPath = clone.RecordHarPath;
        RecordHarUrlFilter = clone.RecordHarUrlFilter;
        RecordHarUrlFilterRegex = clone.RecordHarUrlFilterRegex;
        RecordHarUrlFilterString = clone.RecordHarUrlFilterString;
        RecordVideoDir = clone.RecordVideoDir;
        RecordVideoSize = clone.RecordVideoSize;
        ReducedMotion = clone.ReducedMotion;
        ScreenSize = clone.ScreenSize;
        ServiceWorkers = clone.ServiceWorkers;
        StorageState = clone.StorageState;
        StorageStatePath = clone.StorageStatePath;
        StrictSelectors = clone.StrictSelectors;
        TimezoneId = clone.TimezoneId;
        UserAgent = clone.UserAgent;
        ViewportSize = clone.ViewportSize;
    }

    /// <summary>
    /// <para>
    /// Whether to automatically download all the attachments. Defaults to <c>true</c> where
    /// all the downloads are accepted.
    /// </para>
    /// </summary>
    [JsonPropertyName("acceptDownloads")]
    public bool? AcceptDownloads { get; set; }

    /// <summary>
    /// <para>
    /// When using <see cref="IPage.GotoAsync"/>, <see cref="IPage.RouteAsync"/>, <see cref="IPage.WaitForURLAsync"/>,
    /// <see cref="IPage.RunAndWaitForRequestAsync"/>, or <see cref="IPage.RunAndWaitForResponseAsync"/>
    /// it takes the base URL in consideration by using the <a href="https://developer.mozilla.org/en-US/docs/Web/API/URL/URL"><c>URL()</c></a>
    /// constructor for building the corresponding URL. Unset by default. Examples:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// baseURL: <c>http://localhost:3000</c> and navigating to <c>/bar.html</c> results
    /// in <c>http://localhost:3000/bar.html</c>
    /// </description></item>
    /// <item><description>
    /// baseURL: <c>http://localhost:3000/foo/</c> and navigating to <c>./bar.html</c> results
    /// in <c>http://localhost:3000/foo/bar.html</c>
    /// </description></item>
    /// <item><description>
    /// baseURL: <c>http://localhost:3000/foo</c> (without trailing slash) and navigating
    /// to <c>./bar.html</c> results in <c>http://localhost:3000/bar.html</c>
    /// </description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("baseURL")]
    public string? BaseURL { get; set; }

    /// <summary><para>Toggles bypassing page's Content-Security-Policy. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("bypassCSP")]
    public bool? BypassCSP { get; set; }

    /// <summary>
    /// <para>
    /// TLS Client Authentication allows the server to request a client certificate and
    /// verify it.
    /// </para>
    /// <para>**Details**</para>
    /// <para>
    /// An array of client certificates to be used. Each certificate object must have either
    /// both <c>certPath</c> and <c>keyPath</c>, a single <c>pfxPath</c>, or their corresponding
    /// direct value equivalents (<c>cert</c> and <c>key</c>, or <c>pfx</c>). Optionally,
    /// <c>passphrase</c> property should be provided if the certificate is encrypted. The
    /// <c>origin</c> property should be provided with an exact match to the request origin
    /// that the certificate is valid for.
    /// </para>
    /// <para>
    /// When using WebKit on macOS, accessing <c>localhost</c> will not pick up client certificates.
    /// You can make it work by replacing <c>localhost</c> with <c>local.playwright</c>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// When using WebKit on macOS, accessing <c>localhost</c> will not pick up client certificates.
    /// You can make it work by replacing <c>localhost</c> with <c>local.playwright</c>.
    ///
    /// </para>
    /// </remarks>
    [JsonPropertyName("clientCertificates")]
    public IEnumerable<ClientCertificate>? ClientCertificates { get; set; }

    /// <summary>
    /// <para>
    /// Emulates <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-color-scheme">prefers-colors-scheme</a>
    /// media feature, supported values are <c>'light'</c> and <c>'dark'</c>. See <see cref="IPage.EmulateMediaAsync"/>
    /// for more details. Passing <c>'null'</c> resets emulation to system defaults. Defaults
    /// to <c>'light'</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("colorScheme")]
    public ColorScheme? ColorScheme { get; set; }

    /// <summary>
    /// <para>
    /// Specify device scale factor (can be thought of as dpr). Defaults to <c>1</c>. Learn
    /// more about <a href="https://playwright.dev/dotnet/docs/emulation#devices">emulating
    /// devices with device scale factor</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("deviceScaleFactor")]
    public float? DeviceScaleFactor { get; set; }

    /// <summary>
    /// <para>
    /// An object containing additional HTTP headers to be sent with every request. Defaults
    /// to none.
    /// </para>
    /// </summary>
    [JsonPropertyName("extraHTTPHeaders")]
    public IEnumerable<KeyValuePair<string, string>>? ExtraHTTPHeaders { get; set; }

    /// <summary>
    /// <para>
    /// Emulates <c>'forced-colors'</c> media feature, supported values are <c>'active'</c>,
    /// <c>'none'</c>. See <see cref="IPage.EmulateMediaAsync"/> for more details. Passing
    /// <c>'null'</c> resets emulation to system defaults. Defaults to <c>'none'</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("forcedColors")]
    public ForcedColors? ForcedColors { get; set; }

    [JsonPropertyName("geolocation")]
    public Geolocation? Geolocation { get; set; }

    /// <summary>
    /// <para>
    /// Specifies if viewport supports touch events. Defaults to false. Learn more about
    /// <a href="https://playwright.dev/dotnet/docs/emulation#devices">mobile emulation</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasTouch")]
    public bool? HasTouch { get; set; }

    /// <summary>
    /// <para>
    /// Credentials for <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication">HTTP
    /// authentication</a>. If no origin is specified, the username and password are sent
    /// to any servers upon unauthorized responses.
    /// </para>
    /// </summary>
    [JsonPropertyName("httpCredentials")]
    public HttpCredentials? HttpCredentials { get; set; }

    /// <summary><para>Whether to ignore HTTPS errors when sending network requests. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("ignoreHTTPSErrors")]
    public bool? IgnoreHTTPSErrors { get; set; }

    /// <summary>
    /// <para>
    /// Whether the <c>meta viewport</c> tag is taken into account and touch events are
    /// enabled. isMobile is a part of device, so you don't actually need to set it manually.
    /// Defaults to <c>false</c> and is not supported in Firefox. Learn more about <a href="https://playwright.dev/dotnet/docs/emulation#ismobile">mobile
    /// emulation</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("isMobile")]
    public bool? IsMobile { get; set; }

    /// <summary>
    /// <para>
    /// Whether or not to enable JavaScript in the context. Defaults to <c>true</c>. Learn
    /// more about <a href="https://playwright.dev/dotnet/docs/emulation#javascript-enabled">disabling
    /// JavaScript</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("javaScriptEnabled")]
    public bool? JavaScriptEnabled { get; set; }

    /// <summary>
    /// <para>
    /// Specify user locale, for example <c>en-GB</c>, <c>de-DE</c>, etc. Locale will affect
    /// <c>navigator.language</c> value, <c>Accept-Language</c> request header value as
    /// well as number and date formatting rules. Defaults to the system default locale.
    /// Learn more about emulation in our <a href="https://playwright.dev/dotnet/docs/emulation#locale--timezone">emulation
    /// guide</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// <para>
    /// Whether to emulate network being offline. Defaults to <c>false</c>. Learn more about
    /// <a href="https://playwright.dev/dotnet/docs/emulation#offline">network emulation</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("offline")]
    public bool? Offline { get; set; }

    /// <summary>
    /// <para>
    /// A list of permissions to grant to all pages in this context. See <see cref="IBrowserContext.GrantPermissionsAsync"/>
    /// for more details. Defaults to none.
    /// </para>
    /// </summary>
    [JsonPropertyName("permissions")]
    public IEnumerable<string>? Permissions { get; set; }

    /// <summary><para>Network proxy settings to use with this context. Defaults to none.</para></summary>
    [JsonPropertyName("proxy")]
    public Proxy? Proxy { get; set; }

    /// <summary>
    /// <para>
    /// Optional setting to control resource content management. If <c>omit</c> is specified,
    /// content is not persisted. If <c>attach</c> is specified, resources are persisted
    /// as separate files and all of these files are archived along with the HAR file. Defaults
    /// to <c>embed</c>, which stores content inline the HAR file as per HAR specification.
    /// </para>
    /// </summary>
    [JsonPropertyName("recordHarContent")]
    public HarContentPolicy? RecordHarContent { get; set; }

    /// <summary>
    /// <para>
    /// When set to <c>minimal</c>, only record information necessary for routing from HAR.
    /// This omits sizes, timing, page, cookies, security and other types of HAR information
    /// that are not used when replaying from HAR. Defaults to <c>full</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("recordHarMode")]
    public HarMode? RecordHarMode { get; set; }

    /// <summary>
    /// <para>
    /// Optional setting to control whether to omit request content from the HAR. Defaults
    /// to <c>false</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("recordHarOmitContent")]
    public bool? RecordHarOmitContent { get; set; }

    /// <summary>
    /// <para>
    /// Enables <a href="http://www.softwareishard.com/blog/har-12-spec">HAR</a> recording
    /// for all pages into the specified HAR file on the filesystem. If not specified, the
    /// HAR is not recorded. Make sure to call <see cref="IBrowserContext.CloseAsync"/>
    /// for the HAR to be saved.
    /// </para>
    /// </summary>
    [JsonPropertyName("recordHarPath")]
    public string? RecordHarPath { get; set; }

    [JsonPropertyName("recordHarUrlFilter")]
    public string? RecordHarUrlFilter { get; set; }

    [JsonPropertyName("recordHarUrlFilterRegex")]
    public Regex? RecordHarUrlFilterRegex { get; set; }

    [JsonPropertyName("recordHarUrlFilterString")]
    public string? RecordHarUrlFilterString { get; set; }

    /// <summary>
    /// <para>
    /// Enables video recording for all pages into the specified directory. If not specified
    /// videos are not recorded. Make sure to call <see cref="IBrowserContext.CloseAsync"/>
    /// for videos to be saved.
    /// </para>
    /// </summary>
    [JsonPropertyName("recordVideoDir")]
    public string? RecordVideoDir { get; set; }

    /// <summary>
    /// <para>
    /// Dimensions of the recorded videos. If not specified the size will be equal to <c>viewport</c>
    /// scaled down to fit into 800x800. If <c>viewport</c> is not configured explicitly
    /// the video size defaults to 800x450. Actual picture of each page will be scaled down
    /// if necessary to fit the specified size.
    /// </para>
    /// </summary>
    [JsonPropertyName("recordVideoSize")]
    public RecordVideoSize? RecordVideoSize { get; set; }

    /// <summary>
    /// <para>
    /// Emulates <c>'prefers-reduced-motion'</c> media feature, supported values are <c>'reduce'</c>,
    /// <c>'no-preference'</c>. See <see cref="IPage.EmulateMediaAsync"/> for more details.
    /// Passing <c>'null'</c> resets emulation to system defaults. Defaults to <c>'no-preference'</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("reducedMotion")]
    public ReducedMotion? ReducedMotion { get; set; }

    /// <summary>
    /// <para>
    /// Emulates consistent window screen size available inside web page via <c>window.screen</c>.
    /// Is only used when the <see cref="IBrowser.NewContextAsync"/> is set.
    /// </para>
    /// </summary>
    [JsonPropertyName("screen")]
    public ScreenSize? ScreenSize { get; set; }

    /// <summary>
    /// <para>Whether to allow sites to register Service workers. Defaults to <c>'allow'</c>.</para>
    /// <list type="bullet">
    /// <item><description>
    /// <c>'allow'</c>: <a href="https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API">Service
    /// Workers</a> can be registered.
    /// </description></item>
    /// <item><description><c>'block'</c>: Playwright will block all registration of Service Workers.</description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("serviceWorkers")]
    public ServiceWorkerPolicy? ServiceWorkers { get; set; }

    /// <summary>
    /// <para>
    /// Populates context with given storage state. This option can be used to initialize
    /// context with logged-in information obtained via <see cref="IBrowserContext.StorageStateAsync"/>.
    /// </para>
    /// </summary>
    [JsonPropertyName("storageState")]
    public string? StorageState { get; set; }

    /// <summary>
    /// <para>
    /// Populates context with given storage state. This option can be used to initialize
    /// context with logged-in information obtained via <see cref="IBrowserContext.StorageStateAsync"/>.
    /// Path to the file with saved storage state.
    /// </para>
    /// </summary>
    [JsonPropertyName("storageStatePath")]
    public string? StorageStatePath { get; set; }

    /// <summary>
    /// <para>
    /// If set to true, enables strict selectors mode for this context. In the strict selectors
    /// mode all operations on selectors that imply single target DOM element will throw
    /// when more than one element matches the selector. This option does not affect any
    /// Locator APIs (Locators are always strict). Defaults to <c>false</c>. See <see cref="ILocator"/>
    /// to learn more about the strict mode.
    /// </para>
    /// </summary>
    [JsonPropertyName("strictSelectors")]
    public bool? StrictSelectors { get; set; }

    /// <summary>
    /// <para>
    /// Changes the timezone of the context. See <a href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU's
    /// metaZones.txt</a> for a list of supported timezone IDs. Defaults to the system timezone.
    /// </para>
    /// </summary>
    [JsonPropertyName("timezoneId")]
    public string? TimezoneId { get; set; }

    /// <summary><para>Specific user agent to use in this context.</para></summary>
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// <para>
    /// Emulates consistent viewport for each page. Defaults to an 1280x720 viewport. Use
    /// <c>ViewportSize.NoViewport</c> to disable the consistent viewport emulation. Learn
    /// more about <a href="https://playwright.dev/dotnet/docs/emulation#viewport">viewport
    /// emulation</a>.
    /// </para>
    /// <para>
    /// The <c>ViewportSize.NoViewport</c> value opts out from the default presets, makes
    /// viewport depend on the host window size defined by the operating system. It makes
    /// the execution of the tests non-deterministic.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>ViewportSize.NoViewport</c> value opts out from the default presets, makes
    /// viewport depend on the host window size defined by the operating system. It makes
    /// the execution of the tests non-deterministic.
    /// </para>
    /// </remarks>
    [JsonPropertyName("viewport")]
    public ViewportSize? ViewportSize { get; set; }
}

#nullable disable
