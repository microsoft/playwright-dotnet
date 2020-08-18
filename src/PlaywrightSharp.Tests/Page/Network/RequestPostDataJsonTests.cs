using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.postDataJSON</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestPostDataJsonTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestPostDataJsonTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.postDataJSON</playwright-describe>
        ///<playwright-it>should parse the JSON payload</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldParseTheJSONPayload()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.Equal("bar", request.GetJsonAsync().RootElement.GetProperty("foo").ToString());
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.postDataJSON</playwright-describe>
        ///<playwright-it>should parse the data if content-type is application/x-www-form-urlencoded</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencoded()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
            await Page.ClickAsync("input[type=submit]");

            Assert.NotNull(request);
            var element = request.GetJsonAsync().RootElement;
            Assert.Equal("bar", element.GetProperty("foo").ToString());
            Assert.Equal("123", element.GetProperty("baz").ToString());
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.postDataJSON</playwright-describe>
        ///<playwright-it>should be |undefined| when there is no post data</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response.Request.GetJsonAsync());
        }
    }
}
