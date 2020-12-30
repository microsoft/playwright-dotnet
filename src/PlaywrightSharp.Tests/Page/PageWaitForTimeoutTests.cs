using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Page.WaitFor</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForTimeoutTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForTimeoutTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForTimeout</playwright-describe>
        ///<playwright-it>should timeout</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeout()
        {
            var startTime = DateTime.Now;
            int timeout = 42;
            await Page.WaitForTimeoutAsync(timeout);
            Assert.True((DateTime.Now - startTime).TotalMilliseconds > timeout / 2);
        }
    }
}
