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
        public PersistentContextOptions ContextOptions { get; set; } = new PersistentContextOptions();
    }
}
