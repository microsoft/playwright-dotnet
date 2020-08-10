using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Emulation
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextViewportTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextViewportTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
        ///<playwright-it>should get the proper default viewport size</playwright-it>
        [Fact]
        public Task ShouldGetTheProperDefaultViewPortSize()
            => TestUtils.VerifyViewportAsync(Page, 1280, 720);


        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
        ///<playwright-it>should set the proper viewport size</playwright-it>
        [Fact]
        public async Task ShouldSetTheProperViewportSize()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 123, Height = 456 });
            await TestUtils.VerifyViewportAsync(Page, 123, 456);
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
        ///<playwright-it>should emulate device width</playwright-it>
        [Fact]
        public async Task ShouldEmulateDeviceWidth()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 200, Height = 200 });
            Assert.Equal(200, await Page.EvaluateAsync<int>("window.innerWidth"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 100px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 100px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 500px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 200px)').matches"));
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 500, Height = 500 });
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 400px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 400px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 200px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 500px)').matches"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
        ///<playwright-it>should emulate device height</playwright-it>
        [Fact]
        public async Task ShouldEmulateDeviceHeight()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 200, Height = 200 });
            Assert.Equal(200, await Page.EvaluateAsync<int>("window.innerWidth"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 100px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 100px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 500px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 200px)').matches"));
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 500, Height = 500 });
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 400px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 400px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 200px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 500px)').matches"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
        ///<playwright-it>should not have touch by default</playwright-it>
        [Fact]
        public async Task ShouldNotHaveTouchByDefault()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.False(await Page.EvaluateAsync<bool>("'ontouchstart' in window"));
            await Page.GoToAsync(TestConstants.ServerUrl + "/detect-touch.html");
            Assert.Equal("NO", await Page.EvaluateAsync<string>("document.body.textContent.trim()"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({viewport})</playwright-describe>
        ///<playwright-it>should support touch with null viewport</playwright-it>
        [Fact]
        public async Task ShouldSupportTouchWithNullViewport()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Viewport = null, HasTouch = true });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("'ontouchstart' in window"));
        }
    }
}
