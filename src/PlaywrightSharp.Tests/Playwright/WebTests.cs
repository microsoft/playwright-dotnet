using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Playwright
{
    // I don't know yet if this will be a valid test for us, we will see when we implement it.
    ///<playwright-file>web.spec.js</playwright-file>
    ///<playwright-describe>Web SDK</playwright-describe>
    [Collection(TestConstants.TestFixtureWebCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class WebTests : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        private IPage Page { get; set; }

        /// <inheritdoc/>
        public WebTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        [Fact(Skip = "It's node.js only")]
        public async Task InitializeAsync()
        {
            Page = await PlaywrightSharpWebLoaderFixture.HostBrowser.DefaultContext.NewPageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/test/assets/playwrightweb.html");
            /*
            await Page.EvaluateAsync(
                "(product, connectOptions) => setup(product, connectOptions)",
                TestConstants.Product.ToLower(), PlaywrightSharpWebLoaderFixture.ControlledBrowserApp.ConnectOptions);
            */
        }

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public async Task DisposeAsync()
        {
            await Page.EvaluateAsync("() => teardown()");
            await Page.CloseAsync();
            Page = null;
        }

        ///<playwright-file>web.spec.js</playwright-file>
        ///<playwright-describe>Web SDK</playwright-describe>
        ///<playwright-it>should navigate</playwright-it>
        [Fact(Skip = "It's node.js only")]
        public async Task ShouldNavigate()
        {
            string url = await Page.EvaluateAsync<string>(
                @"async url => {
                    await page.goto(url);
                    return page.evaluate(() => window.location.href);
                }",
                TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, url);
        }

        ///<playwright-file>web.spec.js</playwright-file>
        ///<playwright-describe>Web SDK</playwright-describe>
        ///<playwright-it>should receive events</playwright-it>
        [Fact(Skip = "It's node.js only")]
        public async Task ShouldReceiveEvents()
        {
            string[] logs = await Page.EvaluateAsync<string[]>(@"async () => {
                const logs = [];
                page.on('console', message => logs.push(message.text()));
                await page.evaluate(() => console.log('hello'));
                await page.evaluate(() => console.log('world'));
                return logs;
            }");
            Assert.Equal(new[] { "hello", "world" }, logs);
        }

        ///<playwright-file>web.spec.js</playwright-file>
        ///<playwright-describe>Web SDK</playwright-describe>
        ///<playwright-it>should take screenshot</playwright-it>
        [Fact(Skip = "It's node.js only")]
        public async Task ShouldTakeScreenshot()
        {
            string[] result = await Page.EvaluateAsync<string[]>(
                @"async url => {
                    await page.setViewport({width: 500, height: 500});
                    await page.goto(url);
                    const screenshot = await page.screenshot();
                    return [screenshot.toString('base64'), screenshot.constructor.name] };
                }",
                TestConstants.ServerUrl + "/grid.html");

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", Convert.FromBase64String(result[0])));
            Assert.Equal("BufferImpl", result[1]);
        }
    }
}
