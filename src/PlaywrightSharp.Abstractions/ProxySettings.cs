namespace PlaywrightSharp
{
    /// <summary>
    /// Network proxy settings. See <seealso cref="LaunchOptions"/>
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// Proxy to be used for all requests. HTTP and SOCKS proxies are supported.
        /// For example http://myproxy.com:3128 or socks5://myproxy.com:3128.
        /// Short form myproxy.com:3128 is considered an HTTP proxy.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Optional coma-separated domains to bypass proxy, for example ".com, chromium.org, .domain.com".
        /// </summary>
        public string Bypass { get; set; }

        /// <summary>
        /// Optional username to use if HTTP proxy requires authentication.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Optional password to use if HTTP proxy requires authentication.
        /// </summary>
        public string Password { get; set; }
    }
}
