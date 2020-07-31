using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <seealso cref="IBrowserType.LaunchPersistenContextAsync(string, LaunchPersistentOptions)"/>.
    /// </summary>
    public class LaunchPersistentOptions
    {
        /// <summary>
        /// Whether to run browser in headless mode. Defaults to true unless the devtools option is true.
        /// </summary>
        public bool? Headless
        {
            get
            {
                Values.TryGetValue("headless", out object result);
                return result as bool?;
            }
            set => Values["headless"] = value;
        }

        /// <summary>
        /// Additional arguments to pass to the browser instance.
        /// </summary>
        public string[] Args
        {
            get
            {
                Values.TryGetValue("args", out object result);
                return result as string[];
            }
            set => Values["args"] = value;
        }

        /// <summary>
        /// Path to a User Data Directory.
        /// </summary>
        public string UserDataDir
        {
            get
            {
                Values.TryGetValue("userDataDir", out object result);
                return result as string;
            }
            set => Values["userDataDir"] = value;
        }

        /// <summary>
        /// Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.
        /// </summary>
        public bool? Devtools
        {
            get
            {
                Values.TryGetValue("devtools", out object result);
                return result as bool?;
            }
            set => Values["devtools"] = value;
        }

        /// <summary>
        /// Path to a browser executable to run instead of the bundled one.
        /// </summary>
        public string ExecutablePath
        {
            get
            {
                Values.TryGetValue("executablePath", out object result);
                return result as string;
            }
            set => Values["executablePath"] = value;
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
        /// Maximum time in milliseconds to wait for the browser instance to start.
        /// </summary>
        public int? Timeout
        {
            get
            {
                Values.TryGetValue("timeout", out object result);
                return result as int?;
            }
            set => Values["timeout"] = value;
        }

        /// <summary>
        ///  Whether to pipe browser process stdout and stderr into process.stdout and process.stderr. Defaults to false.
        /// </summary>
        public bool? DumpIO
        {
            get
            {
                Values.TryGetValue("dumpIO", out object result);
                return result as bool?;
            }
            set => Values["dumpIO"] = value;
        }

        /// <summary>
        /// Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.
        /// </summary>
        public int? SlowMo
        {
            get
            {
                Values.TryGetValue("slowmo", out object result);
                return result as int?;
            }
            set => Values["slowmo"] = value;
        }

        /// <summary>
        /// Logs process counts after launching the browser and after exiting.
        /// </summary>
        public bool? LogProcess
        {
            get
            {
                Values.TryGetValue("logProcess", out object result);
                return result as bool?;
            }
            set => Values["logProcess"] = value;
        }

        /// <summary>
        /// If <c>true</c>, then do not use <see cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>.
        /// Dangerous option; use with care. Defaults to <c>false</c>.
        /// </summary>
        public bool? IgnoreDefaultArgs
        {
            get
            {
                Values.TryGetValue("ignoreDefaultArgs", out object result);
                return result as bool?;
            }
            set => Values["ignoreDefaultArgs"] = value;
        }

        /// <summary>
        /// if <see cref="IgnoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter <see cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>.
        /// </summary>
        public string[] IgnoredDefaultArgs
        {
            get
            {
                Values.TryGetValue("ignoredDefaultArgs", out object result);
                return result as string[];
            }
            set => Values["ignoredDefaultArgs"] = value;
        }

        /// <summary>
        /// Specify environment variables that will be visible to browser. Defaults to Environment variables.
        /// </summary>
        public IDictionary<string, string> Env
        {
            get
            {
                Values.TryGetValue("env", out object result);
                return result as Dictionary<string, string>;
            }
            set => Values["env"] = value;
        }

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
        /// A <see cref="Dictionary{TKey, TValue}"/> from origin keys to permissions values. See <see cref="IBrowserContext.SetPermissionsAsync(string, ContextPermission[])"/> for more details.
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
            set => Values["acceptDownloads"] = value;
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