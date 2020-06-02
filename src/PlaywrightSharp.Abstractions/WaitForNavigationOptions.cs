using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.WaitForNavigationAsync(WaitForNavigationOptions, CancellationToken)"/> and <see cref="IFrame.WaitForNavigationAsync(WaitForNavigationOptions, CancellationToken)"/>.
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
            WaitUntil = options?.WaitUntil;
        }

        /// <summary>
        /// Wait for this specific URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Wait for an URL matching this expression.
        /// </summary>
        public Regex UrlRegEx { get; set; }

        /// <summary>
        /// Function that will check for the URL match.
        /// </summary>
        public Func<string, bool> UrlPredicate { get; set; }
    }
}
