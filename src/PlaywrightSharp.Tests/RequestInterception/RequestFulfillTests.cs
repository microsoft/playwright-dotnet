using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>request.fulfill</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestFulfillTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestFulfillTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>request.fulfill</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.FulfillAsync(new RouteFilfillResponse
                {
                    Status = HttpStatusCode.Created,
                    Headers = new Dictionary<string, string>
                    {
                        ["foo"] = "bar"
                    },
                    ContentType = "text/html",
                    Body = "Yo, page!"
                });
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Created, response.Status);
            Assert.Equal("bar", response.Headers["foo"]);
            Assert.Equal("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        /// <summary>
        /// In Playwright this method is called ShouldWorkWithStatusCode422.
        /// I found that status 422 is not available in all .NET runtime versions (see https://github.com/dotnet/core/blob/4c4642d548074b3fbfd425541a968aadd75fea99/release-notes/2.1/Preview/api-diff/preview2/2.1-preview2_System.Net.md)
        /// As the goal here is testing HTTP codes that are not in Chromium (see https://cs.chromium.org/chromium/src/net/http/http_status_code_list.h?sq=package:chromium) we will use code 426: Upgrade Required
        /// </summary>
        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>request.fulfill</playwright-describe>
        ///<playwright-it>should work with status code 422</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithStatusCode422()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.FulfillAsync(new RouteFilfillResponse
                {
                    Status = HttpStatusCode.UpgradeRequired,
                    Body = "Yo, page!"
                });
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.UpgradeRequired, response.Status);
            Assert.Equal("Upgrade Required", response.StatusText);
            Assert.Equal("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>request.fulfill</playwright-describe>
        ///<playwright-it>should allow mocking binary responses</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAllowMockingBinaryResponses()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
                route.FulfillAsync(new RouteFilfillResponse
                {
                    ContentType = "image/png",
                    BodyContent = imageBuffer
                });
            });
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
        ///<playwright-describe>request.fulfill</playwright-describe>
        ///<playwright-it>should allow mocking svg with charset</playwright-it>
        [Fact(Skip = "We need screenshots for this")]
        public void ShouldAllowMockingSvgWithCharset()
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>request.fulfill</playwright-describe>
        ///<playwright-it>should work with file path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithFilePath()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.FulfillAsync(new RouteFilfillResponse
                {
                    ContentType = "shouldBeIgnored",
                    Path = TestUtils.GetWebServerFile("pptr.png")
                }); ;
            });

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
        ///<playwright-describe>request.fulfill</playwright-describe>
        ///<playwright-it>should stringify intercepted request response headers</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldStringifyInterceptedRequestResponseHeaders()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.FulfillAsync(new RouteFilfillResponse
                {
                    Status = HttpStatusCode.OK,
                    Headers = new Dictionary<string, string>
                    {
                        ["foo"] = "true"
                    },
                    Body = "Yo, page!"
                });
            });

            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal("true", response.Headers["foo"]);
            Assert.Equal("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }
    }
}
