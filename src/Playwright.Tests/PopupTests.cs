using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PopupTests : BrowserTestEx
    {
        [PlaywrightTest("popup.spec.ts", "should inherit user agent from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritUserAgentFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new() { UserAgent = "hey" });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var requestTcs = new TaskCompletionSource<string>();
            _ = Server.WaitForRequest("/popup/popup.html", request => requestTcs.TrySetResult(request.Headers["user-agent"]));

            await page.SetContentAsync("<a target=_blank rel=noopener href=\"/popup/popup.html\">link</a>");
            var popupTask = context.WaitForPageAsync(); // This is based on the python test so we can test WaitForPageAsync
            await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

            await popupTask.Result.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            string userAgent = await popupTask.Result.EvaluateAsync<string>("() => window.initialUserAgent");
            await requestTcs.Task;

            Assert.AreEqual("hey", userAgent);
            Assert.AreEqual("hey", requestTcs.Task.Result);
        }

        [PlaywrightTest("popup.spec.ts", "should respect routes from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectRoutesFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            await page.SetContentAsync("<a target=_blank rel=noopener href=\"empty.html\">link</a>");
            bool intercepted = false;

            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.ContinueAsync();
                intercepted = true;
            });

            var popupTask = context.WaitForPageAsync();
            await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

            Assert.True(intercepted);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit extra headers from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritExtraHeadersFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["foo"] = "bar"
                }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var requestTcs = new TaskCompletionSource<string>();
            _ = Server.WaitForRequest("/dummy.html", request => requestTcs.TrySetResult(request.Headers["foo"]));

            await page.EvaluateAsync(@"url => window._popup = window.open(url)", Server.Prefix + "/dummy.html");
            await requestTcs.Task;

            Assert.AreEqual("bar", requestTcs.Task.Result);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit offline from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritOfflineFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await context.SetOfflineAsync(true);

            bool online = await page.EvaluateAsync<bool>(@"url => {
                const win = window.open(url);
                return win.navigator.onLine;
            }", Server.Prefix + "/dummy.html");

            await page.EvaluateAsync(@"url => window._popup = window.open(url)", Server.Prefix + "/dummy.html");

            Assert.False(online);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit http credentials from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritHttpCredentialsFromBrowserContext()
        {
            Server.SetAuth("/title.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new()
            {
                HttpCredentials = new HttpCredentials() { Username = "user", Password = "pass" },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var popup = page.WaitForPopupAsync();

            await TaskUtils.WhenAll(
                popup,
                page.EvaluateAsync("url => window._popup = window.open(url)", Server.Prefix + "/title.html"));

            await popup.Result.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual("Woof-Woof", await popup.Result.TitleAsync());
        }

        [PlaywrightTest("popup.spec.ts", "should inherit touch support from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritTouchSupportFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = 400, Height = 500 },
                HasTouch = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            bool hasTouch = await page.EvaluateAsync<bool>(@"() => {
                const win = window.open('');
                return 'ontouchstart' in win;
            }");

            Assert.True(hasTouch);
        }

        [PlaywrightTest("popup.spec.ts", "should inherit viewport size from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritViewportSizeFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = 400, Height = 500 },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            var size = await page.EvaluateAsync<ViewportSize>(@"() => {
                const win = window.open('about:blank');
                return { width: win.innerWidth, height: win.innerHeight };
            }");

            AssertEqual(400, 500, size);
        }

        [PlaywrightTest("popup.spec.ts", "should use viewport size from window features")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseViewportSizeFromWindowFeatures()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = 700, Height = 700 },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            var (size, popup) = await TaskUtils.WhenAll(
                page.EvaluateAsync<ViewportSize>(@"() => {
                    const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
                    return { width: win.innerWidth, height: win.innerHeight };
                }"),
                page.WaitForPopupAsync());

            await popup.SetViewportSizeAsync(500, 400);
            await popup.WaitForLoadStateAsync();
            var resized = await popup.EvaluateAsync<ViewportSize>(@"() => ({ width: window.innerWidth, height: window.innerHeight })");

            AssertEqual(600, 300, size);
            AssertEqual(500, 400, resized);
        }

        [PlaywrightTest("popup.spec.ts", "should respect routes from browser context using window.open")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectRoutesFromBrowserContextUsingWindowOpen()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            bool intercepted = false;

            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.ContinueAsync();
                intercepted = true;
            });

            var popupTask = context.WaitForPageAsync();
            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window.__popup = window.open(url)", Server.EmptyPage));

            Assert.True(intercepted);
        }

        [PlaywrightTest("popup.spec.ts", "BrowserContext.addInitScript should apply to an in-process popup")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextAddInitScriptShouldApplyToAnInProcessPopup()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync("window.injected = 123;");
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            int injected = await page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return win.injected;
            }");

            Assert.AreEqual(123, injected);
        }

        [PlaywrightTest("popup.spec.ts", "BrowserContext.addInitScript should apply to a cross-process popup")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextAddInitScriptShouldApplyToACrossProcessPopup()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync("window.injected = 123;");
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            var popup = page.WaitForPopupAsync();

            await TaskUtils.WhenAll(
                popup,
                page.EvaluateAsync("url => window._popup = window.open(url)", Server.CrossProcessPrefix + "/title.html"));

            Assert.AreEqual(123, await popup.Result.EvaluateAsync<int>("injected"));
            await popup.Result.ReloadAsync();
            Assert.AreEqual(123, await popup.Result.EvaluateAsync<int>("injected"));
        }

        [PlaywrightTest("popup.spec.ts", "should expose function from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeFunctionFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.ExposeFunctionAsync("add", (int a, int b) => a + b);
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            int injected = await page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return win.add(9, 4);
            }");

            Assert.AreEqual(13, injected);
        }

        void AssertEqual(int width, int height, ViewportSize size)
        {
            Assert.AreEqual(width, size.Width);
            Assert.AreEqual(height, size.Height);
        }
    }
}
