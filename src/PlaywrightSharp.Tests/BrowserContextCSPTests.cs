using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextCSPTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextCSPTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP meta tag")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassCSPMetatag()
        {
            // Make sure CSP prohibits addScriptTag.
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
                await page.AddScriptTagAsync(content: "window.__injected = 42;").ContinueWith(_ => Task.CompletedTask);
                Assert.Null(await page.EvaluateAsync("window.__injected"));
            }
            // By-pass CSP and try one more time.
            await using (var context = await Browser.NewContextAsync(bypassCSP: true))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
                await page.AddScriptTagAsync(content: "window.__injected = 42;");
                Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));
            }
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP header")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassCSPHeader()
        {
            // Make sure CSP prohibits addScriptTag.
            Server.SetCSP("/empty.html", "default-src 'self'");

            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.AddScriptTagAsync(content: "window.__injected = 42;").ContinueWith(_ => Task.CompletedTask);
                Assert.Null(await page.EvaluateAsync("window.__injected"));
            }

            // By-pass CSP and try one more time.
            await using (var context = await Browser.NewContextAsync(bypassCSP: true))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.AddScriptTagAsync(content: "window.__injected = 42;");
                Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));
            }
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass after cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassAfterCrossProcessNavigation()
        {
            await using var context = await Browser.NewContextAsync(bypassCSP: true);
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await page.AddScriptTagAsync(content: "window.__injected = 42;");
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));

            await page.GoToAsync(TestConstants.CrossProcessUrl + "/csp.html");
            await page.AddScriptTagAsync(content: "window.__injected = 42;");
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));
        }

        [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP in iframes as well")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBypassCSPInIframesAsWell()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);

                // Make sure CSP prohibits addScriptTag in an iframe.
                var frame = await FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.ServerUrl + "/csp.html");
                await frame.AddScriptTagAsync(content: "window.__injected = 42;").ContinueWith(_ => Task.CompletedTask);
                Assert.Null(await frame.EvaluateAsync<int?>("() => window.__injected"));
            }

            // By-pass CSP and try one more time.
            await using (var context = await Browser.NewContextAsync(bypassCSP: true))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);

                // Make sure CSP prohibits addScriptTag in an iframe.
                var frame = await FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.ServerUrl + "/csp.html");
                await frame.AddScriptTagAsync(content: "window.__injected = 42;").ContinueWith(_ => Task.CompletedTask);
                Assert.Equal(42, await frame.EvaluateAsync<int?>("() => window.__injected"));

            }
        }
    }
}
