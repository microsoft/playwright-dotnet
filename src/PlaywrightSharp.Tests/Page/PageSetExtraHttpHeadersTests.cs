using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetExtraHttpHeadersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetExtraHttpHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await Task.WhenAll(Page.GoToAsync(TestConstants.EmptyPage), headerTask);

            Assert.Equal("Bar", headerTask.Result);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should throw for non-string header values</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForNonStringHeaderValues() { }
    }
}
