using System;
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
        public ViewportSize Viewport
        {
            get
            {
                Values.TryGetValue("viewport", out object result);
                return result as ViewportSize;
            }
            set => Values["viewport"] = value;
        }

        /// <summary>
        /// Specific user agent to use in this context.
        /// </summary>
        public string UserAgent
        {
            get
            {
                Values.TryGetValue("userAgent", out object result);
                return result as string;
            }
            set => Values["userAgent"] = value;
        }

        /// <summary>
        /// Toggles bypassing page's Content-Security-Policy.
        /// </summary>
        public bool? BypassCSP
        {
            get
            {
                Values.TryGetValue("bypassCSP", out object result);
                return result as bool?;
            }
            set => Values["bypassCSP"] = value;
        }

        /// <summary>
        /// Whether or not to enable or disable JavaScript in the context. Defaults to true.
        /// </summary>
        public bool? JavaScriptEnabled
        {
            get
            {
                Values.TryGetValue("javaScriptEnabled", out object result);
                return result as bool?;
            }
            set => Values["javaScriptEnabled"] = value;
        }

        /// <summary>
        /// Whether to ignore HTTPS errors during navigation. Defaults to false.
        /// </summary>
        public bool? IgnoreHTTPSErrors
        {
            get
            {
                Values.TryGetValue("ignoreHTTPSErrors", out object result);
                return result as bool?;
            }
            set => Values["ignoreHTTPSErrors"] = value;
        }

        /// <summary>
        /// Changes the timezone of the context. See <see href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU’s metaZones.txt</see> for a list of supported timezone IDs.
        /// </summary>
        public string TimezoneId
        {
            get
            {
                Values.TryGetValue("timezoneId", out object result);
                return result as string;
            }
            set => Values["timezoneId"] = value;
        }

        /// <summary>
        /// Changes the Geolocation of the context.
        /// </summary>
        public GeolocationOption Geolocation
        {
            get
            {
                Values.TryGetValue("geolocation", out object result);
                return result as GeolocationOption;
            }
            set => Values["geolocation"] = value;
        }

        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.GrantPermissionsAsync(ContextPermission[], string)"/> for more details.
        /// </summary>
        public ContextPermission[] Permissions
        {
            get
            {
                Values.TryGetValue("permissions", out object result);
                return result as ContextPermission[];
            }
            set => Values["permissions"] = value;
        }

        /// <summary>
        /// Gets or sets whether the meta viewport tag is taken into account.
        /// </summary>
        public bool? IsMobile
        {
            get
            {
                Values.TryGetValue("isMobile", out object result);
                return result as bool?;
            }
            set => Values["isMobile"] = value;
        }

        /// <summary>
        /// Whether to emulate network being offline. Defaults to `false`.
        /// </summary>
        public bool? Offline
        {
            get
            {
                Values.TryGetValue("offline", out object result);
                return result as bool?;
            }
            set => Values["offline"] = value;
        }

        /// <summary>
        /// Gets or sets the device scale factor.
        /// </summary>
        public double? DeviceScaleFactor
        {
            get
            {
                Values.TryGetValue("deviceScaleFactor", out object result);
                return result as double?;
            }
            set => Values["deviceScaleFactor"] = value;
        }

        /// <summary>
        /// Credentials for HTTP authentication.
        /// </summary>
        public Credentials HttpCredentials
        {
            get
            {
                Values.TryGetValue("httpCredentials", out object result);
                return result as Credentials;
            }
            set => Values["httpCredentials"] = value;
        }

        /// <summary>
        /// Specifies if viewport supports touch events. Defaults to false.
        /// </summary>
        public bool? HasTouch
        {
            get
            {
                Values.TryGetValue("hasTouch", out object result);
                return result as bool?;
            }
            set => Values["hasTouch"] = value;
        }

        /// <summary>
        /// Whether to automatically download all the attachments. Defaults to false where all the downloads are canceled.
        /// </summary>
        public bool? AcceptDownloads
        {
            get
            {
                Values.TryGetValue("acceptDownloads", out object result);
                return result as bool?;
            }
            set => Values["acceptDownloads"] = value;
        }

        /// <summary>
        /// Emulates 'prefers-colors-scheme' media feature.
        /// </summary>
        public ColorScheme? ColorScheme
        {
            get
            {
                Values.TryGetValue("colorScheme", out object result);
                return result as ColorScheme?;
            }
            set => Values["colorScheme"] = value;
        }

        /// <summary>
        /// Specify user locale, for example en-GB, de-DE, etc. Locale will affect navigator.language value, Accept-Language request header value as well as number and date formatting rules.
        /// </summary>
        public string Locale
        {
            get
            {
                Values.TryGetValue("locale", out object result);
                return result as string;
            }
            set => Values["locale"] = value;
        }

        /// <summary>
        /// An object containing additional HTTP headers to be sent with every request.
        /// </summary>
        public Dictionary<string, string> ExtraHttpHeaders
        {
            get
            {
                Values.TryGetValue("extraHTTPHeaders", out object result);
                return result as Dictionary<string, string>;
            }
            set => Values["extraHTTPHeaders"] = value;
        }

        internal Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

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

        internal Dictionary<string, object> ToChannelDictionary() => Values;
    }
}
