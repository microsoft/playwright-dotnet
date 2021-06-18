using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ProxyTests : PlaywrightTestEx
    {
        [PlaywrightTest("proxy.spec.ts", "should use proxy")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseProxy()
        {
            Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));

            var proxy = new Proxy { Server = $"localhost:{Server.Port}" };

            await using var browser = await BrowserType.LaunchAsync(new() { Proxy = proxy });

            var page = await browser.NewPageAsync();
            await page.GotoAsync("http://non-existent.com/target.html");

            Assert.AreEqual("Served by the proxy", await page.TitleAsync());
        }

        [PlaywrightTest("proxy.spec.ts", "should authenticate")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAuthenticate()
        {
            Server.SetRoute("/target.html", ctx =>
            {
                string auth = ctx.Request.Headers["proxy-authorization"];

                if (string.IsNullOrEmpty(auth))
                {
                    ctx.Response.StatusCode = 407;
                    ctx.Response.Headers["Proxy-Authenticate"] = "Basic realm=\"Access to internal site\"";
                }

                return ctx.Response.WriteAsync($"<html><title>{auth}</title></html>");
            });

            var proxy = new Proxy
            {
                Server = $"localhost:{Server.Port}",
                Username = "user",
                Password = "secret"
            };

            await using var browser = await BrowserType.LaunchAsync(new() { Proxy = proxy });

            var page = await browser.NewPageAsync();
            await page.GotoAsync("http://non-existent.com/target.html");

            Assert.AreEqual("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret")), await page.TitleAsync());
        }

        [PlaywrightTest("proxy.spec.ts", "should exclude patterns")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldExcludePatterns()
        {
            Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));

            var proxy = new Proxy
            {
                Server = $"localhost:{Server.Port}",
                Bypass = "non-existent1.com, .non-existent2.com, .zone",
            };

            await using var browser = await BrowserType.LaunchAsync(new() { Proxy = proxy });

            var page = await browser.NewPageAsync();
            await page.GotoAsync("http://non-existent.com/target.html");

            Assert.AreEqual("Served by the proxy", await page.TitleAsync());

            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://non-existent1.com/target.html"));
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://sub.non-existent2.com/target.html"));
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://foo.zone/target.html"));
        }
    }
}
