using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextViewportMobileTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextViewportMobileTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support mobile emulation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportMobileEmulation()
        {
            await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            await page.SetViewportSizeAsync(400, 300);
            Assert.Equal(400, await page.EvaluateAsync<int>("window.innerWidth"));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support touch emulation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportTouchEmulation()
        {
            const string dispatchTouch = @"
            function dispatchTouch() {
              let fulfill;
              const promise = new Promise(x => fulfill = x);
              window.ontouchstart = function(e) {
                fulfill('Received touch');
              };
              window.dispatchEvent(new Event('touchstart'));

              fulfill('Did not receive touch');

              return promise;
            }";

            await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("'ontouchstart' in window"));
            Assert.Equal("Received touch", await page.EvaluateAsync<string>(dispatchTouch));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should be detectable by Modernizr")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldBeDetectableByModernizr()
        {
            await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/detect-touch.html");
            Assert.Equal("YES", await page.EvaluateAsync<string>("document.body.textContent.trim()"));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should detect touch when applying viewport with touches")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldDetectTouchWhenApplyingViewportWithTouches()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 800,
                    Height = 600,
                },
                HasTouch = true,
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.AddScriptTagAsync(url: TestConstants.ServerUrl + "/modernizr.js");
            Assert.True(await page.EvaluateAsync<bool>("() => Modernizr.touchevents"));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support landscape emulation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportLandscapeEmulation()
        {
            await using var context1 = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
            var page1 = await context1.NewPageAsync();
            await page1.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.False(await page1.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));

            await using var context2 = await Browser.NewContextAsync(Playwright.Devices["iPhone 6 landscape"]);
            var page2 = await context2.NewPageAsync();
            await page2.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page2.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support window.orientation emulation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportWindowOrientationEmulation()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 300,
                    Height = 400,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(0, await page.EvaluateAsync<int?>("() => window.orientation"));

            await page.SetViewportSizeAsync(400, 300);
            Assert.Equal(90, await page.EvaluateAsync<int?>("() => window.orientation"));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should fire orientationchange event")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldFireOrientationChangeEvent()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 300,
                    Height = 400,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            await page.EvaluateAsync(@"() => {
                window.counter = 0;
                window.addEventListener('orientationchange', () => console.log(++window.counter));
            }");

            var event1Task = page.WaitForEventAsync(PageEvent.Console);
            await page.SetViewportSizeAsync(400, 300);
            var event1 = await event1Task;
            Assert.Equal("1", event1.Text);

            var event2Task = page.WaitForEventAsync(PageEvent.Console);
            await page.SetViewportSizeAsync(300, 400);
            var event2 = await event2Task;
            Assert.Equal("2", event2.Text);
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "default mobile viewports to 980 width")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task DefaultMobileViewportsTo980Width()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(980, await page.EvaluateAsync<int>("() => window.innerWidth"));
        }

        [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "respect meta viewport tag")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task RespectMetaViewportTag()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(320, await page.EvaluateAsync<int>("() => window.innerWidth"));
        }
    }
}
