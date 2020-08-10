using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextUserAgentTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextUserAgentTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                Assert.Contains("Mozilla", await page.EvaluateAsync<string>("() => navigator.userAgent"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { UserAgent = "foobar" }))
            {
                var page = await context.NewPageAsync();

                var (userAgent, _) = await TaskUtils.WhenAll(
                    Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString()),
                    page.GoToAsync(TestConstants.EmptyPage)
                );
                Assert.Equal("foobar", userAgent);
            }
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should work for subframes</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForSubframes()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                Assert.Contains("Mozilla", await page.EvaluateAsync<string>("navigator.userAgent"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { UserAgent = "foobar" }))
            {
                var page = await context.NewPageAsync();

                var (userAgent, _) = await TaskUtils.WhenAll(
                    Server.WaitForRequest<string>("/empty.html", (request) => request.Headers["user-agent"]),
                    FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.EmptyPage));

                Assert.Equal("foobar", userAgent);
            }
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should emulate device user-agent</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmulateDeviceUserAgent()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
                Assert.DoesNotContain("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { UserAgent = Playwright.Devices[DeviceDescriptorName.IPhone6].UserAgent }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
                Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
            }
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({setUserAgent})</playwright-describe>
        ///<playwright-it>should make a copy of default options</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldMakeACopyOfDefaultOptions()
        {
            var options = new BrowserContextOptions
            {
                UserAgent = "foobar"
            };

            await using var context = await Browser.NewContextAsync(options);
            options.UserAgent = "wrong";
            var page = await context.NewPageAsync();

            var (userAgent, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString()),
                page.GoToAsync(TestConstants.EmptyPage)
            );
            Assert.Equal("foobar", userAgent);
        }
    }
}
