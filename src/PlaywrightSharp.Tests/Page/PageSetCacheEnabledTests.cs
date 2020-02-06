using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    //<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Page.setCacheEnabled</playwright-describe>
    public class PageSetCacheEnabledTests : PlaywrightSharpPageBaseTest
    {
        internal PageSetCacheEnabledTests(ITestOutputHelper output) : base(output)
        {
        }

        //<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setCacheEnabled</playwright-describe>
        ///<playwright-it>should stay disabled when toggling request interception on/off</playwright-it>
        [Fact]
        public async Task ShouldStayDisabledWhenTogglingRequestInterceptionOnOff()
        {
            await Page.SetCacheEnabledAsync(false);
            await Page.SetRequestInterceptionAsync(true);
            await Page.SetRequestInterceptionAsync(false);

            await Page.GoToAsync(TestConstants.ServerUrl + "/cached/one-style.html");
            var nonCachedRequestTask = Server.WaitForRequest("/cached/one-style.html", request => request.Headers["if-modified-since"]);
            await Task.WhenAll(
                nonCachedRequestTask,
                Page.ReloadAsync()
            );
            Assert.Equal(StringValues.Empty, nonCachedRequestTask.Result);
        }
    }
}
