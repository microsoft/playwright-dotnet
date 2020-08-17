using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.WaitForNavigationAsync(WaitForNavigationOptions)"/> and <see cref="IFrame.WaitForNavigationAsync(WaitForNavigationOptions)"/>.
    /// </summary>
    public class WaitForNavigationOptions : NavigationOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForNavigationOptions"/> class.
        /// </summary>
        public WaitForNavigationOptions()
        {
        }

        internal WaitForNavigationOptions(NavigationOptions options = null)
        {
            Timeout = options?.Timeout;
            WaitUntil = options?.WaitUntil ?? LifecycleEvent.Load;
        }

        /// <summary>
        /// Wait for this specific URL. Regex or URL Predicate.
        /// </summary>
        public string Url { get; set; }
    }
}
