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
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class InterceptionFulfillTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public InterceptionFulfillTests(ITestOutputHelper output) : base(output)
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

        /// <summary>
        /// In Playwright this method is called ShouldWorkWithStatusCode422.
        /// I found that status 422 is not available in all .NET runtimes (see https://github.com/dotnet/core/blob/4c4642d548074b3fbfd425541a968aadd75fea99/release-notes/2.1/Preview/api-diff/preview2/2.1-preview2_System.Net.md)
        /// As the goal here is testing HTTP codes that are not in Chromium (see https://cs.chromium.org/chromium/src/net/http/http_status_code_list.h?sq=package:chromium) we will use code 426: Upgrade Required
        /// </summary>
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
                    Status = (HttpStatusCode)422,
                    Body = "Yo, page!"
                });
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal((HttpStatusCode)422, response.Status);
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
                byte[] imageBuffer = File.ReadAllBytes(Path.Combine("assets", "pptr.png"));
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
                        ["foo"] = "true"
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
