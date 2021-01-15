using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Top-level requires</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class TopLevelRequiresTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public TopLevelRequiresTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("launcher.spec.js", "Top-level requires", "should require top-level Errors")]
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelErrors() { }

        [PlaywrightTest("launcher.spec.js", "Top-level requires", "should require top-level DeviceDescriptors")]
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelDeviceDescriptors() { }
    }
}
