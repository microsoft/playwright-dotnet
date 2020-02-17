using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.DOMContentLoaded</playwright-describe>
    public class PageEventsDOMContentLoadedTests : PlaywrightSharpPageBaseTest
    {
        internal PageEventsDOMContentLoadedTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.DOMContentLoaded</playwright-describe>
        ///<playwright-it>should fire when expected</playwright-it>
        [Fact]
        public async Task ShouldFireWhenExpected()
        {
            _ = Page.GoToAsync("about:blank");
            await Page.WaitForEvent<object>(PageEvent.DOMContentLoaded);
        }
    }
}
