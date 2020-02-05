using System.IO;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>interception.fulfill</playwright-describe>
    public class InterceptionFulfillTests : PlaywrightSharpPageBaseTest
    {
        internal InterceptionFulfillTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>interception.fulfill</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                await e.Request.FulfillAsync(new ResponseData
                {
                    Status = HttpStatusCode.Created,
                    Headers = {
                        ["foo"]= "bar"
                    },
                    ContentType = "text/html",
                    Body = "Yo, page!"
                });
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Created, response.Status);
            Assert.Equal("bar", response.Headers["foo"]);
            Assert.Equal("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>interception.fulfill</playwright-describe>
        ///<playwright-it>should work with status code 422</playwright-it>
        [Fact]
        public async Task ShouldWorkWithStatusCode422()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                e.Request.FulfillAsync(new ResponseData
                {
                    Status = HttpStatusCode.UnprocessableEntity,
                    Body = "Yo, page!"
                });
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.Status);
            Assert.Equal("Unprocessable Entity", response.StatusText);
            Assert.Equal("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>interception.fulfill</playwright-describe>
        ///<playwright-it>should allow mocking binary responses</playwright-it>
        [Fact]
        public async Task ShouldAllowMockingBinaryResponses()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                var imageBuffer = File.ReadAllBytes(Path.Combine("assets", "pptr.png"));
                e.Request.FulfillAsync(new ResponseData
                {
                    ContentType = "image/png",
                    BodyData = imageBuffer
                });
            };
            await Page.EvaluateAsync(@"PREFIX => {
                const img = document.createElement('img');
                img.src = PREFIX + '/does-not-exist.png';
                document.body.appendChild(img);
                return new Promise(fulfill => img.onload = fulfill);
            }", TestConstants.ServerUrl);
            var img = await Page.QuerySelectorAsync("img");
            Assert.True(ScreenshotHelper.PixelMatch("mock-binary-response.png", await img.ScreenshotAsync()));
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>interception.fulfill</playwright-describe>
        ///<playwright-it>should stringify intercepted request response headers</playwright-it>
        [Fact]
        public async Task ShouldStringifyInterceptedRequestResponseHeaders()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                await e.Request.FulfillAsync(new ResponseData
                {
                    Status = HttpStatusCode.OK,
                    Headers =
                    {
                        ["foo"] = true
                    },
                    Body = "Yo, page!"
                });
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal("true", response.Headers["foo"]);
            Assert.Equal("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }
    }
}
