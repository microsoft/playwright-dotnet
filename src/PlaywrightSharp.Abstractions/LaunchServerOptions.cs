using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchServerAsync(LaunchServerOptions)"/>.
    /// </summary>
    public class LaunchServerOptions : LaunchOptionsBase
    {
        /// <summary>
        /// Port to use for the web socket. Defaults to 0 that picks any available port.
        /// </summary>
        public int? Port { get; set; }
    }
}
