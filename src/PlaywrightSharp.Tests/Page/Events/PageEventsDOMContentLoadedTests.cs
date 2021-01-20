using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.DOMContentLoaded</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsDOMContentLoadedTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsDOMContentLoadedTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page.Events.DOMContentLoaded", "should fire when expected")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireWhenExpected()
        {
            var task = Page.GoToAsync("about:blank");
            await Page.WaitForEventAsync(PageEvent.DOMContentLoaded);
            await task;
        }
    }
}
