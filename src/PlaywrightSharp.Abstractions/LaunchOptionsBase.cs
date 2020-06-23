using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.GetDefaultArgs"/>.
    /// </summary>
    public abstract class LaunchOptionsBase
    {
        /// <summary>
        /// Path to a browser executable to run instead of the bundled one.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Additional arguments to pass to the browser instance.
        /// </summary>
        public string[] Args { get; set; } = Array.Empty<string>();

        /// <summary>
        /// If <c>true</c>, then do not use <see cref="IBrowserType.GetDefaultArgs"/>.
        /// Dangerous option; use with care. Defaults to <c>false</c>.
        /// </summary>
        public bool IgnoreDefaultArgs { get; set; }

        /// <summary>
        /// if <see cref="IgnoreDefaultArgs"/> is set to <c>false</c> this list will be used to filter <see cref="IBrowserType.GetDefaultArgs"/>.
        /// </summary>
        public string[] IgnoredDefaultArgs { get; set; }

        /// <summary>
        /// Maximum time in milliseconds to wait for the browser instance to start.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Specify environment variables that will be visible to browser. Defaults to Environment variables.
        /// </summary>
        public IDictionary<string, string> Env { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Whether to run browser in headless mode. Defaults to true unless the devtools option is true.
        /// </summary>
        public bool Headless { get; set; }

        /// <summary>
        /// Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.
        /// </summary>
        public bool Devtools { get; set; }

        /// <summary>
        /// Whether to auto-open DevTools panel for each tab. If this option is true, the headless option will be set false.
        /// </summary>
        public ProxySettings Proxy { get; set; }

        /// <summary>
        /// If specified, accepted downloads are downloaded into this folder. Otherwise, temporary folder is created and is deleted when browser is closed.
        /// </summary>
        public string DownloadsPath { get; set; }
    }
}
