using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.error</playwright-describe>
    public class PageEventsErrorTests : PlaywrightSharpPageBaseTest
    {
        internal PageEventsErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.error</playwright-describe>
        ///<playwright-it>should throw when page crashes</playwright-it>
        [Fact]
        public async Task ShouldThrowWhenPageCrashes()
        {
            string error = null;
            Page.Error += (sender, e) => error = e.Error;
            if (TestConstants.IsChromium)
            {
                _ = Page.GoToAsync("chrome://crash").ContinueWith(t => { });
            }
            else if (TestConstants.IsWebKit)
            {
                // TODO: expose PageDelegate
                // Page._delegate._session.send('Page.crash', }
            }
            await Page.WaitForEvent<ErrorEventArgs>(PageEvent.Error);
            Assert.Equal("Page crashed!", error);
        }
    }

}
