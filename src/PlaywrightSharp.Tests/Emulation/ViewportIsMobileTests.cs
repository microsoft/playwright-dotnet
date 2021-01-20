using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Emulation
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>viewport.isMobile</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ViewportIsMobileTests : PlaywrightSharpBrowserBaseTest
    {
        private readonly BrowserContextOptions _iPhone;
        private readonly BrowserContextOptions _iPhoneLandscape;

        /// <inheritdoc/>
        public ViewportIsMobileTests(ITestOutputHelper output) : base(output)
        {
            _iPhone = Playwright.Devices["iPhone 6"];
            _iPhoneLandscape = Playwright.Devices["iPhone 6 landscape"];
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should support mobile emulation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportMobileEmulation()
        {
            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            await page.SetViewportSizeAsync(new ViewportSize { Width = 400, Height = 300 });
            Assert.Equal(400, await page.EvaluateAsync<int>("window.innerWidth"));
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should support touch emulation")]
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

            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("'ontouchstart' in window"));
            Assert.Equal("Received touch", await page.EvaluateAsync<string>(dispatchTouch));
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should be detectable by Modernizr")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldBeDetectableByModernizr()
        {
            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/detect-touch.html");
            Assert.Equal("YES", await page.EvaluateAsync<string>("document.body.textContent.trim()"));
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should detect touch when applying viewport with touches")]
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

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should support landscape emulation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportLandscapeEmulation()
        {
            await using var context1 = await Browser.NewContextAsync(_iPhone);
            var page1 = await context1.NewPageAsync();
            await page1.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.False(await page1.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));

            await using var context2 = await Browser.NewContextAsync(_iPhoneLandscape);
            var page2 = await context2.NewPageAsync();
            await page2.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page2.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should support window.orientation emulation")]
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

            await page.SetViewportSizeAsync(new ViewportSize { Width = 400, Height = 300 });
            Assert.Equal(90, await page.EvaluateAsync<int?>("() => window.orientation"));
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "should fire orientationchange event")]
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
            await page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 400,
                Height = 300,
            });
            var event1 = await event1Task;
            Assert.Equal("1", event1.Message.Text);

            var event2Task = page.WaitForEventAsync(PageEvent.Console);
            await page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 300,
                Height = 400,
            });
            var event2 = await event2Task;
            Assert.Equal("2", event2.Message.Text);
        }

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "default mobile viewports to 980 width")]
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

        [PlaywrightTest("emulation.spec.js", "viewport.isMobile", "respect meta viewport tag")]
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
