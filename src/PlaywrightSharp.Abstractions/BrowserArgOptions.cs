using System;

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
        public bool? Headless { get; set; }

        /// <summary>
        /// Additional arguments to pass to the browser instance.
        /// </summary>
        public string[] Args { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Path to a User Data Directory.
        /// </summary>
        public string UserDataDir { get; set; }

        /// <summary>
        /// Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.
        /// </summary>
        public bool? Devtools { get; set; }
    }
}
