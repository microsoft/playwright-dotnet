using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.postData</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class RequestPostDataTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestPostDataTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.postData</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.Equal("{\"foo\":\"bar\"}", request.PostData);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.postData</playwright-describe>
        ///<playwright-it>should be |undefined| when there is no post data</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response.Request.PostData);
        }
    }
}
