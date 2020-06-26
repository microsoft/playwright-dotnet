using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/> options.
    /// </summary>
    public class BrowserContextOptions
    {
        /// <summary>
        /// Sets a consistent viewport for each page. Defaults to an 800x600 viewport. null disables the default viewport.
        /// </summary>
        public Viewport Viewport { get; set; } = new Viewport { Width = 1280, Height = 720 };

        /// <summary>
        /// Specific user agent to use in this context.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Toggles bypassing page's Content-Security-Policy.
        /// </summary>
        public bool BypassCSP { get; set; }

        /// <summary>
        /// Whether or not to enable or disable JavaScript in the context. Defaults to true.
        /// </summary>
        public bool JavaScriptEnabled { get; set; } = true;

        /// <summary>
        /// Whether to ignore HTTPS errors during navigation. Defaults to false.
        /// </summary>
        public bool IgnoreHTTPSErrors { get; set; }

        /// <summary>
        /// Changes the timezone of the context. See <see href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU’s metaZones.txt</see> for a list of supported timezone IDs.
        /// </summary>
        public string TimezoneId { get; set; }

        /// <summary>
        /// Changes the Geolocation of the context.
        /// </summary>
        public GeolocationOption Geolocation { get; set; }

        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.SetPermissionsAsync(string, ContextPermission[])"/> for more details.
        /// </summary>
        public Dictionary<string, ContextPermission[]> Permissions { get; set; }

        /// <summary>
        /// Specify device scale factor (can be thought of as dpr). Defaults to 1.
        /// </summary>
        public decimal DeviceScaleFactor { get; set; } = 1;

        /// <summary>
        /// Whether the meta viewport tag is taken into account and touch events are enabled. Defaults to false. Not supported in Firefox.
        /// </summary>
        public bool IsMobile { get; set; }

        /// <summary>
        /// An dictionary containing additional HTTP headers to be sent with every request.
        /// </summary>
        public Dictionary<string, string> ExtraHTTPHeaders { get; set; }

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
