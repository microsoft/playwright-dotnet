using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Proxy Settings. See <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/> and <see cref="IBrowserType.LaunchPersistentContextAsync(string, LaunchPersistentOptions)"/>.
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// Proxy to be used for all requests. HTTP and SOCKS proxies are supported, for example http://myproxy.com:3128 or socks5://myproxy.com:3128.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Optional coma-separated domains to bypass proxy, for example ".com, chromium.org, .domain.com".
        /// </summary>
        public string Bypass { get; set; }

        /// <summary>
        ///  Optional username to use if HTTP proxy requires authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Optional password to use if HTTP proxy requires authentication.
        /// </summary>
        public string Password { get; set; }
    }
}
