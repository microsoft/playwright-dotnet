using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextCSPTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP meta tag")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassCSPMetatag()
        {
            // Make sure CSP prohibits addScriptTag.
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.Prefix + "/csp.html");
                await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }).ContinueWith(_ => Task.CompletedTask);
                Assert.Null(await page.EvaluateAsync("window.__injected"));
            }
            // By-pass CSP and try one more time.
            await using (var context = await Browser.NewContextAsync(new() { BypassCSP = true }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.Prefix + "/csp.html");
                await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
                Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));
            }
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP header")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassCSPHeader()
        {
            // Make sure CSP prohibits addScriptTag.
            Server.SetCSP("/empty.html", "default-src 'self'");

            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }).ContinueWith(_ => Task.CompletedTask);
                Assert.Null(await page.EvaluateAsync("window.__injected"));
            }

            // By-pass CSP and try one more time.
            await using (var context = await Browser.NewContextAsync(new() { BypassCSP = true }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);
                await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
                Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));
            }
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass after cross-process navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassAfterCrossProcessNavigation()
        {
            await using var context = await Browser.NewContextAsync(new() { BypassCSP = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/csp.html");
            await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
            Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));

            await page.GotoAsync(Server.CrossProcessPrefix + "/csp.html");
            await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
            Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP in iframes as well")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassCSPInIframesAsWell()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);

                // Make sure CSP prohibits addScriptTag in an iframe.
                var frame = await FrameUtils.AttachFrameAsync(page, "frame1", Server.Prefix + "/csp.html");
                await frame.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }).ContinueWith(_ => Task.CompletedTask);
                Assert.Null(await frame.EvaluateAsync<int?>("() => window.__injected"));
            }

            // By-pass CSP and try one more time.
            await using (var context = await Browser.NewContextAsync(new() { BypassCSP = true }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.EmptyPage);

                // Make sure CSP prohibits addScriptTag in an iframe.
                var frame = await FrameUtils.AttachFrameAsync(page, "frame1", Server.Prefix + "/csp.html");
                await frame.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }).ContinueWith(_ => Task.CompletedTask);
                Assert.AreEqual(42, await frame.EvaluateAsync<int?>("() => window.__injected"));

            }
        }
    }
}
