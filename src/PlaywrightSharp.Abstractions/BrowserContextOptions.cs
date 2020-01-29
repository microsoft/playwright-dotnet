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
        public Viewport Viewport { get; set; }
        /// <summary>
        /// Specific user agent to use in this context.
        /// </summary>
        public string UserAgent { get; set; }
        /// <summary>
        /// Toggles bypassing page's Content-Security-Policy.
        /// </summary>
        public bool BypassCSP { get; set; }
        /// <summary>
        /// Whether or not to enable or disable JavaScript in the context. Defaults to true.
        /// </summary>
        public bool JavaScriptEnabled { get; set; }
    }
}