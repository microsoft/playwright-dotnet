using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.DOMContentLoaded</playwright-describe>
    [Trait("Category", "chromium")]
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
        [Fact]
        public async Task ShouldFireWhenExpected()
        {
            _ = Page.GoToAsync("about:blank");
            await Page.WaitForEvent<EventArgs>(PageEvent.DOMContentLoaded);
        }
    }
}
