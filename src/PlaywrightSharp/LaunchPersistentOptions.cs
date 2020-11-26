using System;
using System.Collections.Generic;
using System.Linq;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <seealso cref="IBrowserType.LaunchPersistentContextAsync(string, LaunchPersistentOptions)"/>.
    /// </summary>
    public class LaunchPersistentOptions : LaunchOptions
    {
        /// <summary>
        /// Sets a consistent viewport for each page. Defaults to an 800x600 viewport. null disables the default viewport.
        /// </summary>
        public ViewportSize Viewport { get; set; }

        /// <summary>
        /// Specific user agent to use in this context.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Toggles bypassing page's Content-Security-Policy.
        /// </summary>
        public bool? BypassCSP { get; set; }

        /// <summary>
        /// Whether or not to enable or disable JavaScript in the context. Defaults to true.
        /// </summary>
        public bool? JavaScriptEnabled { get; set; }

        /// <summary>
        /// Changes the timezone of the context. See <see href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU’s metaZones.txt</see> for a list of supported timezone IDs.
        /// </summary>
        public string TimezoneId { get; set; }

        /// <summary>
        /// Changes the Geolocation of the context.
        /// </summary>
        public Geolocation Geolocation { get; set; }

        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.GrantPermissionsAsync(ContextPermission[], string)"/> for more details.
        /// </summary>
        public ContextPermission[] Permissions { get; set; }

        /// <summary>
        /// Gets or sets whether the meta viewport tag is taken into account.
        /// </summary>
        public bool? IsMobile { get; set; }

        /// <summary>
        /// Whether to emulate network being offline. Defaults to `false`.
        /// </summary>
        public bool? Offline { get; set; }

        /// <summary>
        /// Gets or sets the device scale factor.
        /// </summary>
        public decimal? DeviceScaleFactor { get; set; }

        /// <summary>
        /// Credentials for HTTP authentication.
        /// </summary>
        public Credentials HttpCredentials { get; set; }

        /// <summary>
        /// Specifies if viewport supports touch events. Defaults to false.
        /// </summary>
        public bool? HasTouch { get; set; }

        /// <summary>
        /// Whether to automatically download all the attachments. Defaults to false where all the downloads are canceled.
        /// </summary>
        public bool? AcceptDownloads { get; set; }

        /// <summary>
        /// Emulates 'prefers-colors-scheme' media feature.
        /// </summary>
        public ColorScheme? ColorScheme { get; set; }

        /// <summary>
        /// Specify user locale, for example en-GB, de-DE, etc. Locale will affect navigator.language value, Accept-Language request header value as well as number and date formatting rules.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// An object containing additional HTTP headers to be sent with every request.
        /// </summary>
        public Dictionary<string, string> ExtraHttpHeaders { get; set; }

        /// <summary>
        /// Enables HAR recording for all pages into recordHar.path file. If not specified, the HAR is not recorded.
        /// Make sure to await <see cref="IPage.CloseAsync(bool)"/> for the HAR to be saved.
        /// You can use <see cref="Har.HarResult"/> to deserialize the generated JSON file.
        /// </summary>
        public RecordHarOptions RecordHar { get; set; }

        /// <summary>
        /// Enables video recording for all pages into recordVideo.dir directory. If not specified videos are not recorded.
        /// Make sure to await <seealso cref="BrowserContext.CloseAsync"/> for videos to be saved.
        /// </summary>
        public RecordVideoOptions RecordVideo { get; set; }

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
            left.ColorScheme = right.ColorScheme ?? left.ColorScheme;
            left.Locale = right.Locale ?? left.Locale;
            left.ExtraHttpHeaders = right.ExtraHttpHeaders ?? left.ExtraHttpHeaders;
            left.RecordHar = right.RecordHar ?? left.RecordHar;
            left.RecordVideo = right.RecordVideo ?? left.RecordVideo;

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

        internal override Dictionary<string, object> ToChannelDictionary()
        {
            var args = base.ToChannelDictionary();

            if (Viewport == null)
            {
                args["noDefaultViewport"] = true;
            }
            else if (!Viewport.Equals(ViewportSize.None))
            {
                args["viewport"] = Viewport;
            }

            if (!string.IsNullOrEmpty(UserAgent))
            {
                args["userAgent"] = UserAgent;
            }

            if (BypassCSP != null)
            {
                args["bypassCSP"] = BypassCSP;
            }

            if (JavaScriptEnabled != null)
            {
                args["javaScriptEnabled"] = JavaScriptEnabled;
            }

            if (IgnoreHTTPSErrors != null)
            {
                args["ignoreHTTPSErrors"] = IgnoreHTTPSErrors;
            }

            if (!string.IsNullOrEmpty(TimezoneId))
            {
                args["timezoneId"] = TimezoneId;
            }

            if (Geolocation != null)
            {
                args["geolocation"] = Geolocation;
            }

            if (Permissions != null)
            {
                args["permissions"] = Permissions;
            }

            if (IsMobile != null)
            {
                args["isMobile"] = IsMobile;
            }

            if (Offline != null)
            {
                args["offline"] = Offline;
            }

            if (DeviceScaleFactor != null)
            {
                args["deviceScaleFactor"] = DeviceScaleFactor;
            }

            if (HttpCredentials != null)
            {
                args["httpCredentials"] = HttpCredentials;
            }

            if (HasTouch != null)
            {
                args["hasTouch"] = HasTouch;
            }

            if (AcceptDownloads != null)
            {
                args["acceptDownloads"] = AcceptDownloads;
            }

            if (ColorScheme != null)
            {
                args["colorScheme"] = ColorScheme;
            }

            if (Locale != null)
            {
                args["locale"] = Locale;
            }

            if (ExtraHttpHeaders != null)
            {
                args["extraHTTPHeaders"] = ExtraHttpHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray();
            }

            if (RecordHar != null)
            {
                args["recordHar"] = RecordHar;
            }

            if (RecordVideo != null)
            {
                args["recordVideo"] = RecordVideo;
            }

            return args;
        }
    }
}
