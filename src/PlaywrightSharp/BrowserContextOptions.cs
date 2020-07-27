using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/> options.
    /// </summary>
    public class BrowserContextOptions
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        /// <summary>
        /// Sets a consistent viewport for each page. Defaults to an 800x600 viewport. null disables the default viewport.
        /// </summary>
        public ViewportSize Viewport
        {
            get
            {
                _values.TryGetValue("viewport", out object result);
                return result as ViewportSize;
            }
            set => _values["viewport"] = value;
        }

        /// <summary>
        /// Specific user agent to use in this context.
        /// </summary>
        public string UserAgent
        {
            get
            {
                _values.TryGetValue("userAgent", out object result);
                return result as string;
            }
            set => _values["userAgent"] = value;
        }

        /// <summary>
        /// Toggles bypassing page's Content-Security-Policy.
        /// </summary>
        public bool? BypassCSP
        {
            get
            {
                _values.TryGetValue("bypassCSP", out object result);
                return result as bool?;
            }
            set => _values["bypassCSP"] = value;
        }

        /// <summary>
        /// Whether or not to enable or disable JavaScript in the context. Defaults to true.
        /// </summary>
        public bool? JavaScriptEnabled
        {
            get
            {
                _values.TryGetValue("javaScriptEnabled", out object result);
                return result as bool?;
            }
            set => _values["javaScriptEnabled"] = value;
        }

        /// <summary>
        /// Whether to ignore HTTPS errors during navigation. Defaults to false.
        /// </summary>
        public bool? IgnoreHTTPSErrors
        {
            get
            {
                _values.TryGetValue("ignoreHTTPSErrors", out object result);
                return result as bool?;
            }
            set => _values["ignoreHTTPSErrors"] = value;
        }

        /// <summary>
        /// Changes the timezone of the context. See <see href="https://cs.chromium.org/chromium/src/third_party/icu/source/data/misc/metaZones.txt?rcl=faee8bc70570192d82d2978a71e2a615788597d1">ICU’s metaZones.txt</see> for a list of supported timezone IDs.
        /// </summary>
        public string TimezoneId
        {
            get
            {
                _values.TryGetValue("timezoneId", out object result);
                return result as string;
            }
            set => _values["timezoneId"] = value;
        }

        /// <summary>
        /// Changes the Geolocation of the context.
        /// </summary>
        public GeolocationOption Geolocation
        {
            get
            {
                _values.TryGetValue("geolocation", out object result);
                return result as GeolocationOption;
            }
            set => _values["geolocation"] = value;
        }

        /// <summary>
        /// A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.SetPermissionsAsync(string, ContextPermission[])"/> for more details.
        /// </summary>
        public Dictionary<string, ContextPermission[]> Permissions
        {
            get
            {
                _values.TryGetValue("permissions", out object result);
                return result as Dictionary<string, ContextPermission[]>;
            }
            set => _values["permissions"] = value;
        }

        /// <summary>
        /// Gets or sets whether the meta viewport tag is taken into account.
        /// </summary>
        public bool? IsMobile
        {
            get
            {
                _values.TryGetValue("isMobile", out object result);
                return result as bool?;
            }
            set => _values["isMobile"] = value;
        }

        /// <summary>
        /// Whether to emulate network being offline. Defaults to `false`.
        /// </summary>
        public bool? Offline
        {
            get
            {
                _values.TryGetValue("offline", out object result);
                return result as bool?;
            }
            set => _values["offline"] = value;
        }

        /// <summary>
        /// Gets or sets the device scale factor.
        /// </summary>
        public double? DeviceScaleFactor
        {
            get
            {
                _values.TryGetValue("deviceScaleFactor", out object result);
                return result as double?;
            }
            set => _values["deviceScaleFactor"] = value;
        }

        /// <summary>
        /// Credentials for HTTP authentication.
        /// </summary>
        public Credentials HttpCredentials
        {
            get
            {
                _values.TryGetValue("httpCredentials", out object result);
                return result as Credentials;
            }
            set => _values["httpCredentials"] = value;
        }

        /// <summary>
        /// Specifies if viewport supports touch events. Defaults to false.
        /// </summary>
        public bool? HasTouch
        {
            get
            {
                _values.TryGetValue("hasTouch", out object result);
                return result as bool?;
            }
            set => _values["hasTouch"] = value;
        }

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

        internal Dictionary<string, object> ToChannelDictionary() => _values;
    }
}
