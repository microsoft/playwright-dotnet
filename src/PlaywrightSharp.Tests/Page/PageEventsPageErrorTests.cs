using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.PageError</playwright-describe>
    public class PageEventsPageErrorTests : PlaywrightSharpPageBaseTest
    {
        internal PageEventsPageErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.PageError</playwright-describe>
        ///<playwright-it>should fire</playwright-it>
        [Fact]
        public async Task ShouldFire()
        {
            string error = null;
            Page.Once<PageErrorEventArgs>(PageEvent.PageError, e => error = e.Message);
            await Task.WhenAll(
                Page.GoToAsync(TestConstants.ServerUrl + "/error.html"),
                Page.WaitForEvent<PageErrorEventArgs>(PageEvent.PageError)
            );
            Assert.Contains("Fancy", error);
        }
    }
}
