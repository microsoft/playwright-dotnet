using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class LauncherTests : PlaywrightTestEx
    {
        [PlaywrightTest("launcher.spec.ts", "should require top-level Errors")]
        [Test, Ignore("We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelErrors() { }

        [PlaywrightTest("launcher.spec.ts", "should require top-level DeviceDescriptors")]
        [Test, Ignore("We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelDeviceDescriptors() { }
    }
}
