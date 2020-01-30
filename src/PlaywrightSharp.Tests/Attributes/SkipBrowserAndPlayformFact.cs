using System;
using System.Runtime.InteropServices;
using Xunit;

namespace PlaywrightSharp.Tests.Attributes
{
    /// <summary>
    /// Skip browsers and/or platforms
    /// </summary>
    public class SkipBrowserAndPlayformFact : FactAttribute
    {
        /// <summary>
        /// Creates a new <seealso cref="SkipBrowserAndPlayformFact"/>
        /// </summary>
        /// <param name="skipFirefox">Skip firefox</param>
        /// <param name="skipChromium">Skip Chromium</param>
        /// <param name="skipWebkit">Skip Webkit</param>
        /// <param name="skipOSX">Skip OSX</param>
        /// <param name="skipWindows">Skip Windows</param>
        /// <param name="skipLinux">Skip Linux</param>
        public SkipBrowserAndPlayformFact(
            bool skipFirefox = false,
            bool skipChromium = false,
            bool skipWebkit = false,
            bool skipOSX = false,
            bool skipWindows = false,
            bool skipLinux = false)
        {
            if (
                (skipFirefox && TestConstants.IsFirefox) ||
                (skipWebkit && TestConstants.IsWebKit) ||
                (skipChromium && TestConstants.IsChromium) ||
                (skipOSX && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) ||
                (skipWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ||
                (skipLinux && RuntimeInformation.IsOSPlatform(OSPlatform.Linux)))
            {
                Skip = "Skipped by browser/platform";
            }
        }

    }
}
