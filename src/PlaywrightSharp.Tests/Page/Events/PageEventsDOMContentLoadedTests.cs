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

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.DOMContentLoaded</playwright-describe>
        ///<playwright-it>should fire when expected</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireWhenExpected()
        {
            var task = Page.GoToAsync("about:blank");
            await Page.WaitForEvent<EventArgs>(PageEvent.DOMContentLoaded);
            await task;
        }
    }
}
