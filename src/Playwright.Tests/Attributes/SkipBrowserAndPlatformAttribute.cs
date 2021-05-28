using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.Tests
{
    public class SkipBrowserAndPlatformAttribute : NUnitAttribute, IApplyToTest
    {
        private bool _skip = false;

        public SkipBrowserAndPlatformAttribute(
            bool skipFirefox = false,
            bool skipChromium = false,
            bool skipWebkit = false,
            bool skipOSX = false,
            bool skipWindows = false,
            bool skipLinux = false)
        {
            if (SkipBrowser(skipFirefox, skipChromium, skipWebkit) && SkipPlatform(skipOSX, skipWindows, skipLinux))
            {
                _skip = true;
            }
        }

        public void ApplyToTest(NUnit.Framework.Internal.Test test)
        {
            if (_skip)
            {
                test.RunState = RunState.Ignored;
                test.Properties.Set(NUnit.Framework.Internal.PropertyNames.SkipReason, "Skipped by browser/platform");
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
