using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PopupTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public PopupTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("popup.spec.ts", "should inherit user agent from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritUserAgentFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { UserAgent = "hey" });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var requestTcs = new TaskCompletionSource<string>();
            _ = Server.WaitForRequest("/popup/popup.html", request => requestTcs.TrySetResult(request.Headers["user-agent"]));

            await page.SetContentAsync("<a target=_blank rel=noopener href=\"/popup/popup.html\">link</a>");
            var popupTask = context.WaitForPageAsync(); // This is based on the python test so we can test WaitForPageAsync
            await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

            await popupTask.Result.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            string userAgent = await popupTask.Result.EvaluateAsync<string>("() => window.initialUserAgent");
            await requestTcs.Task;

            Assert.Equal("hey", userAgent);
            Assert.Equal("hey", requestTcs.Task.Result);
        }

        [PlaywrightTest("popup.spec.ts", "should respect routes from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectRoutesFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await page.SetContentAsync("<a target=_blank rel=noopener href=\"empty.html\">link</a>");
            bool intercepted = false;

            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.ResumeAsync();
                intercepted = true;
            });

            var popupTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

            Assert.True(intercepted);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit extra headers from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritExtraHeadersFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["foo"] = "bar"
                }
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var requestTcs = new TaskCompletionSource<string>();
            _ = Server.WaitForRequest("/dummy.html", request => requestTcs.TrySetResult(request.Headers["foo"]));

            await page.EvaluateAsync(@"url => window._popup = window.open(url)", TestConstants.ServerUrl + "/dummy.html");
            await requestTcs.Task;

            Assert.Equal("bar", requestTcs.Task.Result);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit offline from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritOfflineFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await context.SetOfflineAsync(true);

            bool online = await page.EvaluateAsync<bool>(@"url => {
                const win = window.open(url);
                return win.navigator.onLine;
            }", TestConstants.ServerUrl + "/dummy.html");

            await page.EvaluateAsync(@"url => window._popup = window.open(url)", TestConstants.ServerUrl + "/dummy.html");

            Assert.False(online);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit http credentials from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritHttpCredentialsFromBrowserContext()
        {
            Server.SetAuth("/title.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new HttpCredentials() { Username = "user", Password = "pass" },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popup = page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popup,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/title.html"));

            await popup.Result.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal("Woof-Woof", await popup.Result.TitleAsync());
        }

        [PlaywrightTest("popup.spec.ts", "should inherit touch support from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritTouchSupportFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize { Width = 400, Height = 500 },
                HasTouch = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            bool hasTouch = await page.EvaluateAsync<bool>(@"() => {
                const win = window.open('');
                return 'ontouchstart' in win;
            }");

            Assert.True(hasTouch);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit viewport size from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritViewportSizeFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize { Width = 400, Height = 500 },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            var size = await page.EvaluateAsync<ViewportSize>(@"() => {
                const win = window.open('about:blank');
                return { width: win.innerWidth, height: win.innerHeight };
            }");

            Assert.Equal(new ViewportSize { Width = 400, Height = 500 }, size);
        }

        [PlaywrightTest("popup.spec.ts", "should use viewport size from window features")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseViewportSizeFromWindowFeatures()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize { Width = 700, Height = 700 },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            var (size, popup) = await TaskUtils.WhenAll(
                page.EvaluateAsync<ViewportSize>(@"() => {
                    const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
                    return { width: win.innerWidth, height: win.innerHeight };
                }"),
                page.WaitForEventAsync(PageEvent.Popup));

            await popup.SetViewportSizeAsync(500, 400);
            await popup.WaitForLoadStateAsync();
            var resized = await popup.EvaluateAsync<ViewportSize>(@"() => ({ width: window.innerWidth, height: window.innerHeight })");

            Assert.Equal(new ViewportSize { Width = 600, Height = 300 }, size);
            Assert.Equal(new ViewportSize { Width = 500, Height = 400 }, resized);
        }

        [PlaywrightTest("popup.spec.ts", "should respect routes from browser context using window.open")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectRoutesFromBrowserContextUsingWindowOpen()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            bool intercepted = false;

            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.ResumeAsync();
                intercepted = true;
            });

            var popupTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window.__popup = window.open(url)", TestConstants.EmptyPage));

            Assert.True(intercepted);
        }

        [PlaywrightTest("popup.spec.ts", "BrowserContext.addInitScript should apply to an in-process popup")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextAddInitScriptShouldApplyToAnInProcessPopup()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync("() => window.injected = 123");
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            int injected = await page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return win.injected;
            }");

            Assert.Equal(123, injected);
        }

        [PlaywrightTest("popup.spec.ts", "BrowserContext.addInitScript should apply to a cross-process popup")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextAddInitScriptShouldApplyToACrossProcessPopup()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync("() => window.injected = 123");
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            var popup = page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popup,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.CrossProcessUrl + "/title.html"));

            Assert.Equal(123, await popup.Result.EvaluateAsync<int>("injected"));
            await popup.Result.ReloadAsync();
            Assert.Equal(123, await popup.Result.EvaluateAsync<int>("injected"));
        }

        [PlaywrightTest("popup.spec.ts", "should expose function from browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeFunctionFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.ExposeFunctionAsync("add", (int a, int b) => a + b);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            int injected = await page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return win.add(9, 4);
            }");

            Assert.Equal(13, injected);
        }
    }
}
