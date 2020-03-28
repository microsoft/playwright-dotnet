using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class SetUserAgentTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public SetUserAgentTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var page = await NewPageAsync();
            Assert.Contains("Mozilla", await page.EvaluateAsync<string>("() => navigator.userAgent"));

            page = await NewPageAsync(new BrowserContextOptions { UserAgent = "foobar" });

            var userAgentTask = Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString());
            await Task.WhenAll(
                userAgentTask,
                page.GoToAsync(TestConstants.EmptyPage)
            );
            Assert.Equal("foobar", userAgentTask.Result);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should work for subframes</playwright-it>
        [Fact]
        public async Task ShouldWorkForSubframes()
        {
            var page = await NewPageAsync();
            Assert.Contains("Mozilla", await page.EvaluateAsync<string>("navigator.userAgent"));
            page = await NewPageAsync(new BrowserContextOptions { UserAgent = "foobar" });
            var userAgentTask = Server.WaitForRequest<string>("/empty.html", (request) => request.Headers["user-agent"]);

            await Task.WhenAll(
              userAgentTask,
              FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.EmptyPage));

            Assert.Equal("foobar", userAgentTask.Result);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should emulate device user-agent</playwright-it>
        [Fact]
        public async Task ShouldEmulateDeviceUserAgent()
        {
            var page = await NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.DoesNotContain("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
            page = await NewPageAsync(new BrowserContextOptions { UserAgent = TestConstants.IPhone.UserAgent });
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should make a copy of default options</playwright-it>
        [Fact]
        public async Task ShouldMakeACopyOfDefaultOptions()
        {
            var options = new BrowserContextOptions
            {
                UserAgent = "foobar"
            };

            var context = await NewContextAsync(options);
            options.UserAgent = "wrong";
            var page = await context.NewPageAsync();

            var userAgentTask = Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString());
            await Task.WhenAll(
                userAgentTask,
                page.GoToAsync(TestConstants.EmptyPage)
            );
            Assert.Equal("foobar", userAgentTask.Result);
        }
    }
}
