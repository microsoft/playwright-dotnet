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
        public bool? AcceptDownloads { get; set; }

        public ViewportSize Viewport { get; set; }

        public string UserAgent { get; set; }

        public bool? BypassCSP { get; set; }

        public bool? JavaScriptEnabled { get; set; }

        public string TimezoneId { get; set; }

        public Geolocation Geolocation { get; set; }

        public string[] Permissions { get; set; }

        public bool? IsMobile { get; set; }

        public bool? Offline { get; set; }

        public float? DeviceScaleFactor { get; set; }

        public HttpCredentials HttpCredentials { get; set; }

        public bool? HasTouch { get; set; }

        public ColorScheme ColorScheme { get; set; }

        public string Locale { get; set; }

        public Dictionary<string, string> ExtraHTTPHeaders { get; set; }

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
