using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>Page.viewport</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ViewportTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ViewportTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should get the proper viewport size</playwright-it>
        [Retry]
        public async Task ShouldGetTheProperViewPortSize()
        {
            Assert.Equal(800, Page.Viewport.Width);
            Assert.Equal(600, Page.Viewport.Height);

            await Page.SetViewportAsync(new Viewport { Width = 123, Height = 456 });

            Assert.Equal(123, Page.Viewport.Width);
            Assert.Equal(456, Page.Viewport.Height);
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should support mobile emulation</playwright-it>
        [Retry]
        public async Task ShouldSupportMobileEmulation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(800, await Page.EvaluateAsync<int>("window.innerWidth"));

            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);
            Assert.Equal(375, await Page.EvaluateAsync<int>("window.innerWidth"));
            await Page.SetViewportAsync(new Viewport { Width = 400, Height = 300 });
            Assert.Equal(400, await Page.EvaluateAsync<int>("window.innerWidth"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should support touch emulation</playwright-it>
        [Retry]
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

            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.False(await Page.EvaluateAsync<bool>("'ontouchstart' in window"));

            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);
            Assert.True(await Page.EvaluateAsync<bool>("'ontouchstart' in window"));
            Assert.Equal("Received touch", await Page.EvaluateAsync<string>(dispatchTouch));

            await Page.SetViewportAsync(new Viewport { Width = 100, Height = 100 });
            Assert.False(await Page.EvaluateAsync<bool>("'ontouchstart' in window"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should be detectable by Modernizr</playwright-it>
        [Retry]
        public async Task ShouldBeDetectableByModernizr()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/detect-touch.html");
            Assert.Equal("NO", await Page.EvaluateAsync<string>("document.body.textContent.trim()"));
            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);
            await Page.GoToAsync(TestConstants.ServerUrl + "/detect-touch.html");
            Assert.Equal("YES", await Page.EvaluateAsync<string>("document.body.textContent.trim()"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should detect touch when applying viewport with touches</playwright-it>
        [Retry]
        public async Task ShouldDetectTouchWhenApplyingViewportWithTouches()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 800,
                Height = 600,
                IsMobile = true
            });
            await Page.AddScriptTagAsync(new AddTagOptions
            {
                Url = TestConstants.ServerUrl + "/modernizr.js"
            });
            Assert.True(await Page.EvaluateAsync<bool>("() => Modernizr.touchevents"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should support landscape emulation</playwright-it>
        [Retry]
        public async Task ShouldSupportLandscapeEmulation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));
            await Page.SetViewportAsync(TestConstants.IPhoneLandscape.ViewPort);
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>should fire orientationchange event</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldFireOrientationChangeEvent()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);

            await Page.EvaluateAsync(@"() => {
                window.counter = 0;
                window.addEventListener('orientationchange', () => console.log(++window.counter));
            }");

            var event1Task = Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console);
            await Page.SetViewportAsync(TestConstants.IPhoneLandscape.ViewPort);
            var event1 = await event1Task;
            Assert.Equal("1", event1.Message.Text);

            var event2Task = Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console);
            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);
            var event2 = await event2Task;
            Assert.Equal("2", event2.Message.Text);
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>default mobile viewports to 980 width</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task DefaultMobileViewportsTo980Width()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 320,
                Height = 480,
                IsMobile = true
            });

            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(980, await Page.EvaluateAsync<int>("() => window.innerWidth"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.viewport</playwright-describe>
        ///<playwright-it>respect meta viewport tag</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task RespectMetaViewportTag()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 320,
                Height = 480,
                IsMobile = true
            });

            await Page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(320, await Page.EvaluateAsync<int>("() => window.innerWidth"));
        }
    }
}
