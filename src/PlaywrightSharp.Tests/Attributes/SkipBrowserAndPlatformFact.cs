using System;
using System.Runtime.InteropServices;
using Xunit;

namespace PlaywrightSharp.Tests.Attributes
{
    /// <summary>
    /// Skip browsers and/or platforms
    /// </summary>
    public class SkipBrowserAndPlatformFact : FactAttribute
    {
        /// <summary>
        /// Creates a new <seealso cref="SkipBrowserAndPlatformFact"/>
        /// </summary>
        /// <param name="skipFirefox">Skip firefox</param>
        /// <param name="skipChromium">Skip Chromium</param>
        /// <param name="skipWebkit">Skip Webkit</param>
        /// <param name="skipOSX">Skip OSX</param>
        /// <param name="skipWindows">Skip Windows</param>
        /// <param name="skipLinux">Skip Linux</param>
        public SkipBrowserAndPlatformFact(
            bool skipFirefox = false,
            bool skipChromium = false,
            bool skipWebkit = false,
            bool skipOSX = false,
            bool skipWindows = false,
            bool skipLinux = false)
        {
            if (SkipBrowser(skipFirefox, skipChromium, skipWebkit) && SkipPlatform(skipOSX, skipWindows, skipLinux))
            {
                Skip = "Skipped by browser/platform";
            }
        }

        private static bool SkipPlatform(bool skipOSX, bool skipWindows, bool skipLinux)
            =>
            (
                (skipOSX && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) ||
                (skipWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ||
                (skipLinux && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ) ||
            (
                !skipOSX &&
                !skipLinux &&
                !skipWindows
            );

        private static bool SkipBrowser(bool skipFirefox, bool skipChromium, bool skipWebkit)
            =>
            (
                (skipFirefox && TestConstants.IsFirefox) ||
                (skipWebkit && TestConstants.IsWebKit) ||
                (skipChromium && TestConstants.IsChromium)
            ) ||
            (
                !skipFirefox &&
                !skipWebkit &&
                !skipChromium
            );
    }
}
