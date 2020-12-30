using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Load</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsLoadTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsLoadTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Load</playwright-describe>
        ///<playwright-it>should fire when expected</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireWhenExpected()
        {
            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Load),
                Page.GoToAsync("about:blank")
            );
        }
    }
}
