using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Playwright.MSTest;

namespace Microsoft.Playwright.Tests
{
    public class SkipAttribute : Attribute
    {
        private readonly TestTargets[] _combinations;

        public SkipAttribute(params TestTargets[] combinations)
        {
            _combinations = combinations;
        }

        public bool ShouldSkipTest()
        {
            if (_combinations.Any(combination =>
            {
                var requirements = (Enum.GetValues(typeof(TestTargets)) as TestTargets[])?.Where(x => combination.HasFlag(x));
                return requirements.All(flag =>
                    flag switch
                    {
                        TestTargets.Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                        TestTargets.Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                        TestTargets.OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                        TestTargets.Chromium => PlaywrightTest.BrowserName == BrowserType.Chromium,
                        TestTargets.Firefox => PlaywrightTest.BrowserName == BrowserType.Firefox,
                        TestTargets.Webkit => PlaywrightTest.BrowserName == BrowserType.Webkit,
                        _ => false,
                    });
            }))
            {
                return true;
            }

            return false;
        }
    }
}
