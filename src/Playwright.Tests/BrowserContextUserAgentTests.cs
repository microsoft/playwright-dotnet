using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextUserAgentTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-user-agent.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                StringAssert.Contains("Mozilla", await page.EvaluateAsync<string>("() => navigator.userAgent"));
            }

            await using (var context = await Browser.NewContextAsync(new() { UserAgent = "foobar" }))
            {
                var page = await context.NewPageAsync();

                var (userAgent, _) = await TaskUtils.WhenAll(
                    Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString()),
                    page.GotoAsync(Server.EmptyPage)
                );
                Assert.AreEqual("foobar", userAgent);
            }
        }

        [PlaywrightTest("browsercontext-user-agent.spec.ts", "should work for subframes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForSubframes()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                StringAssert.Contains("Mozilla", await page.EvaluateAsync<string>("navigator.userAgent"));
            }

            await using (var context = await Browser.NewContextAsync(new() { UserAgent = "foobar" }))
            {
                var page = await context.NewPageAsync();

                var (userAgent, _) = await TaskUtils.WhenAll(
                    Server.WaitForRequest<string>("/empty.html", request => request.Headers["user-agent"]),
                    FrameUtils.AttachFrameAsync(page, "frame1", Server.EmptyPage));

                Assert.AreEqual("foobar", userAgent);
            }
        }

        [PlaywrightTest("browsercontext-user-agent.spec.ts", "should emulate device user-agent")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmulateDeviceUserAgent()
        {
            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.Prefix + "/mobile.html");
                CollectionAssert.DoesNotContain("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
            }

            await using (var context = await Browser.NewContextAsync(new() { UserAgent = "iPhone" }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(Server.Prefix + "/mobile.html");
                StringAssert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
            }
        }

        [PlaywrightTest("browsercontext-user-agent.spec.ts", "should make a copy of default options")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldMakeACopyOfDefaultOptions()
        {
            var options = new BrowserNewContextOptions
            {
                UserAgent = "foobar"
            };

            await using var context = await Browser.NewContextAsync(options);
            options.UserAgent = "wrong";
            var page = await context.NewPageAsync();

            var (userAgent, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString()),
                page.GotoAsync(Server.EmptyPage)
            );
            Assert.AreEqual("foobar", userAgent);
        }
    }
}
