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

        [PlaywrightTest("emulation.spec.js", "BrowserContext({viewport})", "should get the proper default viewport size")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldGetTheProperDefaultViewPortSize()
            => TestUtils.VerifyViewportAsync(Page, 1280, 720);


        [PlaywrightTest("emulation.spec.js", "BrowserContext({viewport})", "should set the proper viewport size")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetTheProperViewportSize()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 123, Height = 456 });
            await TestUtils.VerifyViewportAsync(Page, 123, 456);
        }

        [PlaywrightTest("emulation.spec.js", "BrowserContext({viewport})", "should emulate device width")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("emulation.spec.js", "BrowserContext({viewport})", "should emulate device height")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("emulation.spec.js", "BrowserContext({viewport})", "should not have touch by default")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotHaveTouchByDefault()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.False(await Page.EvaluateAsync<bool>("'ontouchstart' in window"));
            await Page.GoToAsync(TestConstants.ServerUrl + "/detect-touch.html");
            Assert.Equal("NO", await Page.EvaluateAsync<string>("document.body.textContent.trim()"));
        }

        [PlaywrightTest("emulation.spec.js", "BrowserContext({viewport})", "should support touch with null viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportTouchWithNullViewport()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Viewport = null, HasTouch = true });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("'ontouchstart' in window"));
        }
    }
}
