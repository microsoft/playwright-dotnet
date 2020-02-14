using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.WaitForNavigationAsync(WaitForNavigationOptions)"/> and <see cref="IFrame.WaitForNavigationAsync(WaitForNavigationOptions)"/>.
    /// </summary>
    public class WaitForNavigationOptions : NavigationOptions
    {
        /// <summary>
        /// Wait for this specific URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Wait for an URL matching this expression.
        /// </summary>
        public string UrlRegEx { get; set; }

        /// <summary>
        /// Function that will check for the URL match.
        /// </summary>
        public Func<string, bool> UrlPredicate { get; set; }
    }
}
