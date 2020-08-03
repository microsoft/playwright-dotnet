using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>.
    /// </summary>
    public class BrowserArgOptions
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

        internal Dictionary<string, object> Values { get; } = new Dictionary<string, object>();

        internal Dictionary<string, object> ToChannelDictionary() => Values;
    }
}
