using PlaywrightSharp.Server;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="BrowserTypeBase.LaunchPersistentContextAsync(string, LaunchPersistentOptions)"/> options.
    /// </summary>
    public class LaunchPersistentOptions : LaunchOptions
    {
        /// <summary>
        /// Context options.
        /// </summary>
        public BrowserContextOptions ContextOptions { get; set; } = new BrowserContextOptions();
    }
}
