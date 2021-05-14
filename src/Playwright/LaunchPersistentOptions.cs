using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Options for <seealso cref="IBrowserType.LaunchPersistentContextAsync(string, LaunchPersistentOptions)"/>.
    /// </summary>
    public class LaunchPersistentOptions : LaunchOptionsBase
    {
        /// <inheritdoc path="/param[@name='acceptDownloads']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? AcceptDownloads { get; set; }

        /// <inheritdoc path="/param[@name='viewport']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public ViewportSize Viewport { get; set; }

        /// <inheritdoc path="/param[@name='userAgent']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string UserAgent { get; set; }

        /// <inheritdoc path="/param[@name='bypassCSP']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? BypassCSP { get; set; }

        /// <inheritdoc path="/param[@name='javascriptEnabled']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? JavaScriptEnabled { get; set; }

        /// <inheritdoc path="/param[@name='timezoneId']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string TimezoneId { get; set; }

        /// <inheritdoc path="/param[@name='geolocation']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public Geolocation Geolocation { get; set; }

        /// <inheritdoc path="/param[@name='permissions']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string[] Permissions { get; set; }

        /// <inheritdoc path="/param[@name='isMobile']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? IsMobile { get; set; }

        /// <inheritdoc path="/param[@name='offline']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? Offline { get; set; }

        /// <inheritdoc path="/param[@name='deviceScaleFactor']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public float? DeviceScaleFactor { get; set; }

        /// <inheritdoc path="/param[@name='httpCredentials']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public HttpCredentials HttpCredentials { get; set; }

        /// <inheritdoc path="/param[@name='hasTouch']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public bool? HasTouch { get; set; }

        /// <inheritdoc path="/param[@name='colorScheme']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public ColorScheme ColorScheme { get; set; }

        /// <inheritdoc path="/param[@name='locale']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public string Locale { get; set; }

        /// <inheritdoc path="/param[@name='extraHTTPHeaders']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public Dictionary<string, string> ExtraHTTPHeaders { get; set; }

        /// <inheritdoc path="/param[@name='screenSize']" cref="IBrowserType.LaunchPersistentContextAsync(string, bool?, BrowserChannel, string, IEnumerable{string}, Proxy, string, bool?, bool?, bool?, bool?, float?, IEnumerable{KeyValuePair{string, string}}, bool?, float?, bool?, bool?, bool?, ViewportSize, ScreenSize, string, float?, bool?, bool?, bool?, string, Geolocation, string, IEnumerable{string}, IEnumerable{KeyValuePair{string, string}}, bool?, HttpCredentials, Microsoft.Playwright.ColorScheme, string, bool?, string, RecordVideoSize, IEnumerable{string}, bool?)" />
        public ScreenSize ScreenSize { get; set; }

        /// <summary>
        /// Adds all the values set int <paramref name="right"/> into <paramref name="left"/>.
        /// </summary>
        /// <param name="left"><see cref="LaunchPersistentOptions"/> to hidratate.</param>
        /// <param name="right"><see cref="BrowserContextOptions"/> to get the values from.</param>
        /// <returns><paramref name="left"/> with the values of <paramref name="right"/>.</returns>
        public static LaunchPersistentOptions operator +(LaunchPersistentOptions left, BrowserContextOptions right)
        {
            if (left == null || right == null)
            {
                return null;
            }

            left.Viewport = right.Viewport ?? left.Viewport;
            left.UserAgent = right.UserAgent ?? left.UserAgent;
            left.BypassCSP = right.BypassCSP ?? left.BypassCSP;
            left.JavaScriptEnabled = right.JavaScriptEnabled ?? left.JavaScriptEnabled;
            left.IgnoreHTTPSErrors = right.IgnoreHTTPSErrors ?? left.IgnoreHTTPSErrors;
            left.TimezoneId = right.TimezoneId ?? left.TimezoneId;
            left.Geolocation = right.Geolocation ?? left.Geolocation;
            left.Permissions = right.Permissions ?? left.Permissions;
            left.IsMobile = right.IsMobile ?? left.IsMobile;
            left.Offline = right.Offline ?? left.Offline;
            left.DeviceScaleFactor = right.DeviceScaleFactor ?? left.DeviceScaleFactor;
            left.HttpCredentials = right.HttpCredentials ?? left.HttpCredentials;
            left.HasTouch = right.HasTouch ?? left.HasTouch;
            left.AcceptDownloads = right.AcceptDownloads ?? left.AcceptDownloads;
            left.ColorScheme = right.ColorScheme == ColorScheme.Undefined ? left.ColorScheme : right.ColorScheme;
            left.Locale = right.Locale ?? left.Locale;
            left.ExtraHTTPHeaders = right.ExtraHTTPHeaders ?? left.ExtraHTTPHeaders;
            left.RecordHarOmitContent = right.RecordHarOmitContent ?? left.RecordHarOmitContent;
            left.RecordHarPath = right.RecordHarPath ?? left.RecordHarPath;
            left.RecordVideoDir = right.RecordVideoDir ?? left.RecordVideoDir;
            left.RecordVideoSize = right.RecordVideoSize ?? left.RecordVideoSize;

            return left;
        }

        /// <summary>
        /// Adds all the values set int <paramref name="right"/> into <paramref name="left"/>.
        /// </summary>
        /// <param name="left"><see cref="LaunchPersistentOptions"/> to hidratate.</param>
        /// <param name="right"><see cref="BrowserContextOptions"/> to get the values from.</param>
        /// <returns><paramref name="left"/> with the values of <paramref name="right"/>.</returns>
        public static LaunchPersistentOptions Add(LaunchPersistentOptions left, BrowserContextOptions right) => left + right;

        /// <summary>
        /// Clones the <see cref="BrowserContextOptions"/>.
        /// </summary>
        /// <returns>A copy of the current <see cref="BrowserContextOptions"/>.</returns>
        public BrowserContextOptions Clone()
        {
            var copy = (BrowserContextOptions)MemberwiseClone();
            copy.Viewport = Viewport?.Clone();
            copy.Geolocation = Geolocation?.Clone();
            return copy;
        }
    }
}
