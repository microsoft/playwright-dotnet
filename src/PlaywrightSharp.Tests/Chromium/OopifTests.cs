using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class OopifTests : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        private IBrowser _browser;
        private IPage _page;

        /// <inheritdoc/>
        public OopifTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should report oopif frames")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportOopifFrames()
        {
            _page = await _browser.NewPageAsync();
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Length);
            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/grid.html", await _page.Frames[1].EvaluateAsync<string>("() => '' + location.href"));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should handle oopif detach")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldHandleOopifDetach()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            Assert.Equal(2, _page.Frames.Count());
            var frame = _page.Frames[1];
            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/grid.html", await frame.EvaluateAsync<string>("() => '' + location.href"));

            var (frameDetached, _) = await TaskUtils.WhenAll(
                _page.WaitForEventAsync(PageEvent.FrameDetached),
                _page.EvaluateAsync<string>("() => document.querySelector('iframe').remove()"));

            Assert.Equal(frame, frameDetached.Frame);
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should handle remote -> local -> remote transitions")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldHandleRemoteLocalRemoteTransitions()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/grid.html", await _page.Frames[1].EvaluateAsync<string>("() => '' + location.href"));

            await TaskUtils.WhenAll(
                _page.Frames[1].WaitForNavigationAsync(),
                _page.EvaluateAsync<string>("() => goLocal()"));

            Assert.Equal(TestConstants.ServerUrl + "/grid.html", await _page.Frames[1].EvaluateAsync<string>("() => '' + location.href"));
            Assert.Equal(0, await CountOOPIFsASync(_browser));

            await TaskUtils.WhenAll(
                _page.Frames[1].WaitForNavigationAsync(),
                _page.EvaluateAsync<string>("() => goRemote()"));

            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/grid.html", await _page.Frames[1].EvaluateAsync<string>("() => '' + location.href"));
            Assert.Equal(1, await CountOOPIFsASync(_browser));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should get the proper viewport")]
        [Fact(Skip = "Ignored in Playwright")]
        public void ShouldGetTheProperViewport()
        {
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should expose function")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldExposeFunction()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));

            var oopif = _page.Frames[1];
            await _page.ExposeFunctionAsync("mul", (int a, int b) => a * b);

            int result = await oopif.EvaluateAsync<int>(@"async function() {
              return await mul(9, 4);
            }");

            Assert.Equal(36, result);
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should emulate media")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldEmulateMedia()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));

            var oopif = _page.Frames[1];
            Assert.False(await oopif.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            await _page.EmulateMediaAsync(ColorScheme.Dark);
            Assert.True(await oopif.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should emulate offline")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldEmulateOffline()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));

            var oopif = _page.Frames[1];
            Assert.True(await oopif.EvaluateAsync<bool?>("() => navigator.onLine"));
            await _page.Context.SetOfflineAsync(true);
            Assert.False(await oopif.EvaluateAsync<bool?>("() => navigator.onLine"));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should support context options")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSupportContextOptions()
        {
            await using var context = await _browser.NewContextAsync(new BrowserContextOptions(TestConstants.iPhone6)
            {
                TimezoneId = "America/Jamaica",
                Locale = "fr-CH",
                UserAgent = "UA",
            });
            var page = await context.NewPageAsync();

            var (userAgent, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/grid.html", r => r.Headers["user-agent"]),
                page.GoToAsync(TestConstants.ServerIpUrl + "/dynamic-oopif.html"));

            Assert.Equal(2, page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));

            var oopif = page.Frames[1];
            Assert.True(await oopif.EvaluateAsync<bool?>("() => 'ontouchstart' in window"));
            Assert.Equal(
                "Sat Nov 19 2016 13:12:34 GMT-0500 (heure normale de l’Est nord-américain)",
                await oopif.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));

            Assert.Equal("fr-CH", await oopif.EvaluateAsync<string>("() => navigator.language"));
            Assert.Equal("UA", await oopif.EvaluateAsync<string>("() => navigator.userAgent"));
            Assert.Equal("UA", userAgent);
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should respect route")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldRespectRoute()
        {
            bool intercepted = false;

            await _page.RouteAsync("**/digits/0.png", (route, _) =>
            {
                intercepted = true;
                route.ContinueAsync();
            });

            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            Assert.True(intercepted);
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should take screenshot")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldTakeScreenshot()
        {
            await _page.SetViewportSizeAsync(500, 500);
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, _page.Frames.Count());
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-oopif.png", await _page.ScreenshotAsync()));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should load oopif iframes with subresources and route")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldLoadOopififramesWithSubresourcesAndRoute()
        {
            await _page.RouteAsync("**/*", (route, _) => route.ContinueAsync());
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(1, await CountOOPIFsASync(_browser));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should report main requests")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportMainRequests()
        {
            var requestFrames = new List<IFrame>();
            var finishedFrames = new List<IFrame>();

            _page.Request += (_, e) => requestFrames.Add(e.Request.Frame);
            _page.RequestFinished += (_, e) => finishedFrames.Add(e.Request.Frame);

            await _page.GoToAsync(TestConstants.ServerUrl + "/empty.html");
            var main = _page.MainFrame;

            await main.EvaluateAsync(@"url => {
              const iframe = document.createElement('iframe');
              iframe.src = url;
              document.body.appendChild(iframe);
              return new Promise(f => iframe.onload = f);
            }", TestConstants.CrossProcessUrl + "/empty.html");

            Assert.Equal(2, _page.Frames.Length);
            var child = main.ChildFrames[0];
            await child.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);

            await child.EvaluateAsync(@"url => {
              const iframe = document.createElement('iframe');
              iframe.src = url;
              document.body.appendChild(iframe);
              return new Promise(f => iframe.onload = f);
            }", TestConstants.ServerUrl + "/empty.html");

            Assert.Equal(3, _page.Frames.Length);
            var grandChild = child.ChildFrames[0];
            await grandChild.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);

            Assert.Equal(2, await CountOOPIFsASync(_browser));
            Assert.Equal(main, requestFrames[0]);
            Assert.Equal(main, finishedFrames[0]);
            Assert.Equal(child, requestFrames[1]);
            Assert.Equal(child, finishedFrames[1]);
            Assert.Equal(grandChild, requestFrames[2]);
            Assert.Equal(grandChild, finishedFrames[2]);
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should support exposeFunction")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSupportExposeFunction()
        {
            await _page.Context.ExposeFunctionAsync("dec", (int a) => a - 1);
            await _page.ExposeFunctionAsync("inc", (int a) => a + 1);
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            Assert.Equal(2, _page.Frames.Count());

            Assert.Equal(4, await _page.Frames[0].EvaluateAsync<int>("() => inc(3)"));
            Assert.Equal(5, await _page.Frames[1].EvaluateAsync<int>("() => inc(4)"));
            Assert.Equal(2, await _page.Frames[0].EvaluateAsync<int>("() => dec(3)"));
            Assert.Equal(3, await _page.Frames[1].EvaluateAsync<int>("() => dec(4)"));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should support addInitScript")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSupportAddInitScript()
        {
            await _page.Context.AddInitScriptAsync("() => window.bar = 17");
            await _page.AddInitScriptAsync("() => window.foo = 42");
            await _page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            Assert.Equal(2, _page.Frames.Count());

            Assert.Equal(42, await _page.Frames[0].EvaluateAsync<int>("() => window.foo"));
            Assert.Equal(42, await _page.Frames[1].EvaluateAsync<int>("() => window.foo"));
            Assert.Equal(17, await _page.Frames[0].EvaluateAsync<int>("() => window.bar"));
            Assert.Equal(17, await _page.Frames[1].EvaluateAsync<int>("() => window.bar"));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should click a button when it overlays oopif")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldClickAButtonWhenItOverlaysOopif()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/button-overlay-oopif.html");
            Assert.Equal(1, await CountOOPIFsASync(_browser));
            await _page.ClickAsync("button");
            Assert.True(await _page.EvaluateAsync<bool?>("() => window.BUTTON_CLICKED"));
        }

        [PlaywrightTest("chromium/oopif.spec.ts", "oopif", "should report google.com frame with headful")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportGoogleComFrameWithHeadful()
        {
            await using var browser = await BrowserType.LaunchDefaultHeadful();
            var page = await browser.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.RouteAsync("**/*", (route, _) => route.FulfillAsync(body: "YO, GOOGLE.COM"));

            await page.EvaluateAsync(@"() => {
              const frame = document.createElement('iframe');
              frame.setAttribute('src', 'https://google.com/');
              document.body.appendChild(frame);
              return new Promise(x => frame.onload = x);
            }");
            await page.WaitForSelectorAsync("iframe[src=\"https://google.com/\"]");
            Assert.Equal(1, await CountOOPIFsASync(browser));
            var urls = page.Frames.Select(f => f.Url);

            Assert.Equal(new[] { TestConstants.EmptyPage, "https://google.com/" }, urls.ToArray());
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { "--site-per-process" };
            _browser = await BrowserType.LaunchAsync(options);
            _page = await _browser.NewPageAsync();
        }

        /// <inheritdoc/>
        public Task DisposeAsync() => _browser.CloseAsync();

        private async Task<int> CountOOPIFsASync(IBrowser browser)
        {
            var browserSession = await ((IChromiumBrowser)browser).NewBrowserCDPSessionAsync();
            var oopifs = new List<JsonElement?>();

            browserSession.MessageReceived += (_, e) =>
            {
                if (e.Method == "Target.targetCreated" && e.Params.Value.GetProperty("targetInfo").GetProperty("type").GetString() == "iframe")
                {
                    oopifs.Add(e.Params);
                }
            };

            await browserSession.SendAsync("Target.setDiscoverTargets", new { discover = true });

            await browserSession.DetachAsync();
            return oopifs.Count;
        }
    }
}
