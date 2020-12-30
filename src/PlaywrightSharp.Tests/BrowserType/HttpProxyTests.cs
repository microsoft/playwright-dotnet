using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserType
{
    ///<playwright-file>proxy.spec.js</playwright-file>
    ///<playwright-describe>HTTP Proxy</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class HttpProxyTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public HttpProxyTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>proxy.spec.js</playwright-file>
        ///<playwright-describe>HTTP Proxy</playwright-describe>
        ///<playwright-it>should return all of the pages</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnAllOfThePages()
        {
            Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));

            var defaultBrowserOptions = TestConstants.GetDefaultBrowserOptions();
            defaultBrowserOptions.Proxy = new ProxySettings { Server = $"localhost:{TestConstants.Port}" };

            await using var browser = await BrowserType.LaunchAsync(defaultBrowserOptions);

            var page = await browser.NewPageAsync();
            await page.GoToAsync("http://non-existent.com/target.html");

            Assert.Equal("Served by the proxy", await page.GetTitleAsync());
        }

        ///<playwright-file>proxy.spec.js</playwright-file>
        ///<playwright-describe>HTTP Proxy</playwright-describe>
        ///<playwright-it>should authenticate</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

            var defaultBrowserOptions = TestConstants.GetDefaultBrowserOptions();
            defaultBrowserOptions.Proxy = new ProxySettings
            {
                Server = $"localhost:{TestConstants.Port}",
                Username = "user",
                Password = "secret"
            };

            await using var browser = await BrowserType.LaunchAsync(defaultBrowserOptions);

            var page = await browser.NewPageAsync();
            await page.GoToAsync("http://non-existent.com/target.html");

            Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret")), await page.GetTitleAsync());
        }

        ///<playwright-file>proxy.spec.js</playwright-file>
        ///<playwright-describe>HTTP Proxy</playwright-describe>
        ///<playwright-it>should exclude patterns</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExcludePatterns()
        {
            Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));

            var defaultBrowserOptions = TestConstants.GetDefaultBrowserOptions();
            defaultBrowserOptions.Proxy = new ProxySettings
            {
                Server = $"localhost:{TestConstants.Port}",
                Bypass = "non-existent1.com, .non-existent2.com, .zone",
            };

            await using var browser = await BrowserType.LaunchAsync(defaultBrowserOptions);

            var page = await browser.NewPageAsync();
            await page.GoToAsync("http://non-existent.com/target.html");

            Assert.Equal("Served by the proxy", await page.GetTitleAsync());

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync("http://non-existent1.com/target.html"));
            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync("http://sub.non-existent2.com/target.html"));
            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync("http://foo.zone/target.html"));
        }
    }
}
