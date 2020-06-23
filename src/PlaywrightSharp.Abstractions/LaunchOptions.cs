using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal delegate void TestHookBeforeCreateBrowserDelegate();
    internal delegate Task TestHookBeforeCreateBrowserAsyncDelegate();

    /// <summary>
    /// Options for <see cref="IBrowserType.LaunchAsync(LaunchOptions)"/>.
    /// </summary>
    public class LaunchOptions : LaunchOptionsBase
    {
        /// <summary>
        /// Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.
        /// </summary>
        public int SlowMo { get; set; }

        internal TestHookBeforeCreateBrowserDelegate TestHookBeforeCreateBrowser { get; set; }
    }
}
