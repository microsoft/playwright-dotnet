using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/>.
    /// </summary>
    public class LaunchOptions
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
        /// If specified, accepted downloads are downloaded into this folder. Otherwise, temporary folder is created and is deleted when browser is closed.
        /// </summary>
        public string DownloadsPath
        {
            get
            {
                Values.TryGetValue("downloadsPath", out object result);
                return result as string;
            }
            set => Values["downloadsPath"] = value;
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
                Values.TryGetValue("slowMo", out object result);
                return result as int?;
            }
            set => Values["slowmMo"] = value;
        }

        /// <summary>
        /// Logs process counts after launching the browser and after exiting.
        /// </summary>
        public bool? LogProcess
        {
            get
            {
                Values.TryGetValue("logProgress", out object result);
                return result as bool?;
            }
            set => Values["logProgress"] = value;
        }

        /// <summary>
        /// If true, Playwright does not pass its own configurations args and only uses the ones from args.
        /// Dangerous option; use with care. Defaults to false.
        /// </summary>
        public bool? IgnoreDefaultArgs
        {
            get
            {
                Values.TryGetValue("ignoreDefaultAR", out object result);
                return result as bool?;
            }
            set => Values["headless"] = value;
        }

        /// <summary>
        /// if <see cref="IgnoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter default arguments.
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
        /// Firefox user preferences. Learn more about the Firefox user preferences at about:config.
        /// </summary>
        public IDictionary<string, object> FirefoxUserPrefs
        {
            get
            {
                Values.TryGetValue("firefoxUserPrefs", out object result);
                return result as Dictionary<string, object>;
            }
            set => Values["firefoxUserPrefs"] = value;
        }

        /// <summary>
        /// Network proxy settings.
        /// </summary>
        public ProxySettings Proxy
        {
            get
            {
                Values.TryGetValue("proxy", out object result);
                return result as ProxySettings;
            }
            set => Values["proxy"] = value;
        }

        internal Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Converts the <see cref="LaunchOptions"/> to <see cref="LaunchPersistentOptions"/>.
        /// </summary>
        /// <param name="options">Option to convert.</param>
        public static implicit operator LaunchPersistentOptions(LaunchOptions options)
        {
            if (options == null)
            {
                return null;
            }

            var result = new LaunchPersistentOptions();
            foreach (var kv in options.Values)
            {
                result.Values[kv.Key] = kv.Value;
            }

            return result;
        }

        /// <summary>
        /// Converts the <see cref="LaunchOptions"/> to <see cref="LaunchPersistentOptions"/>.
        /// </summary>
        /// <returns>A <see cref="LaunchPersistentOptions"/> with the same information as the <see cref="LaunchOptions"/>.</returns>
        public LaunchPersistentOptions ToLaunchPersistentOptions() => this;

        internal Dictionary<string, object> ToChannelDictionary() => Values;
    }
}
