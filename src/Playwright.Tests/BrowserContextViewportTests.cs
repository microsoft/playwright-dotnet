using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextViewportTests : PageTestEx
    {
        [PlaywrightTest("browsercontext-viewport.spec.ts", "should get the proper default viewport size")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public Task ShouldGetTheProperDefaultViewPortSize()
            => TestUtils.VerifyViewportAsync(Page, 1280, 720);


        [PlaywrightTest("browsercontext-viewport.spec.ts", "should set the proper viewport size")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetTheProperViewportSize()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(123, 456);
            await TestUtils.VerifyViewportAsync(Page, 123, 456);
        }

        [PlaywrightTest("browsercontext-viewport.spec.ts", "should emulate device width")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmulateDeviceWidth()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(200, 200);
            Assert.AreEqual(200, await Page.EvaluateAsync<int>("window.innerWidth"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 100px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 100px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 500px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 200px)').matches"));
            await Page.SetViewportSizeAsync(500, 500);
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 400px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-width: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 400px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-width: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 200px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 500px)').matches"));
        }

        [PlaywrightTest("browsercontext-viewport.spec.ts", "should emulate device height")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmulateDeviceHeight()
        {
            await TestUtils.VerifyViewportAsync(Page, 1280, 720);
            await Page.SetViewportSizeAsync(200, 200);
            Assert.AreEqual(200, await Page.EvaluateAsync<int>("window.innerWidth"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 100px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 100px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 300px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 500px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 200px)').matches"));
            await Page.SetViewportSizeAsync(500, 500);
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 400px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(min-device-height: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 400px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(max-device-height: 600px)').matches"));
            Assert.False(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 200px)').matches"));
            Assert.True(await Page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 500px)').matches"));
        }

        [PlaywrightTest("browsercontext-viewport.spec.ts", "should not have touch by default")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotHaveTouchByDefault()
        {
            await Page.GotoAsync(Server.Prefix + "/mobile.html");
            Assert.False(await Page.EvaluateAsync<bool>("'ontouchstart' in window"));
            await Page.GotoAsync(Server.Prefix + "/detect-touch.html");
            Assert.AreEqual("NO", await Page.EvaluateAsync<string>("document.body.textContent.trim()"));
        }

        [PlaywrightTest("browsercontext-viewport.spec.ts", "should support touch with null viewport")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportTouchWithNullViewport()
        {
            await using var context = await Browser.NewContextAsync(new() { ViewportSize = null, HasTouch = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("'ontouchstart' in window"));
        }


        [PlaywrightTest("browsercontext-viewport.spec.ts", "should respect screensize")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportScreenSize()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ScreenSize = new ScreenSize()
                {
                    Width = 750,
                    Height = 1334,
                },
                ViewportSize = new ViewportSize()
                {
                    Width = 375,
                    Height = 667
                }
            });

            var page = await context.NewPageAsync();
            Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 1334px)').matches"));
            Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 750px)').matches"));
            await TestUtils.VerifyViewportAsync(page, 375, 667);
        }

        [PlaywrightTest("browsercontext-viewport.spec.ts", "should ignore screensize when viewport is null")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIgnoreScreensizeWhenViewportIsNull()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ScreenSize = new ScreenSize()
                {
                    Width = 750,
                    Height = 1334,
                },
                ViewportSize = ViewportSize.NoViewport
            });

            var page = await context.NewPageAsync();
            Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(device-height: 1334px)').matches"));
            Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(device-width: 750px)').matches"));
            Assert.IsNull(page.ViewportSize);
        }
    }
}
