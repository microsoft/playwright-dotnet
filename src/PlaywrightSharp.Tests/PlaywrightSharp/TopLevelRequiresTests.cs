using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Top-level requires</playwright-describe>
    [Trait("Category", "chromium")]
    public class TopLevelRequiresTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public TopLevelRequiresTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Top-level requires</playwright-describe>
        ///<playwright-it>should require top-level Errors</playwright-it>
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelErrors() { }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Top-level requires</playwright-describe>
        ///<playwright-it>should require top-level DeviceDescriptors</playwright-it>
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldRequireTopLevelDeviceDescriptors() { }
    }
}
