using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LauncherTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LauncherTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("launcher.spec.ts", "should require top-level Errors")]
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelErrors() { }

        [PlaywrightTest("launcher.spec.ts", "should require top-level DeviceDescriptors")]
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelDeviceDescriptors() { }
    }
}
