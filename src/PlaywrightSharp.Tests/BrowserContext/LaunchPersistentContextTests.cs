using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
    /// <playwright-describe>launchPersistentContext()</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LaunchPersistentContextTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LaunchPersistentContextTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>context.cookies() should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ContextCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GoToAsync(TestConstants.EmptyPage);

            string documentCookie = await page.EvaluateAsync<string>(@"() => {
              document.cookie = 'username=John Doe';
              return document.cookie;
            }");

            Assert.Equal("username=John Doe", documentCookie);
            var cookie = (await page.Context.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>context.addCookies() should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ContextAddCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GoToAsync(TestConstants.EmptyPage);
            await context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "username",
                Value = "John Doe",
            });

            Assert.Equal("username=John Doe", await page.EvaluateAsync<string>(@"() => document.cookie"));

            var cookie = (await page.Context.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>context.clearCookies() should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ContextClearCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GoToAsync(TestConstants.EmptyPage);
            await context.AddCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "cookie1",
                    Value = "1",
                },
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "cookie2",
                    Value = "2",
                });

            Assert.Equal("cookie1=1; cookie2=2", await page.EvaluateAsync<string>(@"() => document.cookie"));

            await context.ClearCookiesAsync();
            await page.ReloadAsync();
            Assert.Empty(await page.Context.GetCookiesAsync());
            Assert.Empty(await page.EvaluateAsync<string>(@"() => document.cookie"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should(not) block third party cookies</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotBlockThirdPartyCookies()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GoToAsync(TestConstants.EmptyPage);
            await page.EvaluateAsync(@"src => {
                  let fulfill;
                  const promise = new Promise(x => fulfill = x);
                  const iframe = document.createElement('iframe');
                  document.body.appendChild(iframe);
                  iframe.onload = fulfill;
                  iframe.src = src;
                  return promise;
                }", TestConstants.CrossProcessUrl + "/grid.html");

            await page.FirstChildFrame().EvaluateAsync<string>("document.cookie = 'username=John Doe'");
            await page.WaitForTimeoutAsync(2000);
            bool allowsThirdPart = !TestConstants.IsWebKit;
            var cookies = await context.GetCookiesAsync(TestConstants.CrossProcessUrl + "/grid.html");

            if (allowsThirdPart)
            {
                Assert.Single(cookies);
                var cookie = cookies.First();
                Assert.Equal("127.0.0.1", cookie.Domain);
                Assert.Equal(cookie.Expires, -1);
                Assert.False(cookie.HttpOnly);
                Assert.Equal("username", cookie.Name);
                Assert.Equal("/", cookie.Path);
                Assert.Equal(SameSite.None, cookie.SameSite);
                Assert.False(cookie.Secure);
                Assert.Equal("John Doe", cookie.Value);
            }
            else
            {
                Assert.Empty(cookies);
            }

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support viewport option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportViewportOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 456,
                    Height = 789
                }
            });

            await TestUtils.VerifyViewportAsync(page, 456, 789);
            page = await context.NewPageAsync();
            await TestUtils.VerifyViewportAsync(page, 456, 789);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support deviceScaleFactor option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportDeviceScaleFactorOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                DeviceScaleFactor = 3
            });

            Assert.Equal(3, await page.EvaluateAsync<int>("window.devicePixelRatio"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support userAgent option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportUserAgentOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                UserAgent = "foobar"
            });

            string userAgent = string.Empty;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", r => userAgent = r.Headers["user-agent"]),
                page.GoToAsync(TestConstants.EmptyPage));

            Assert.Equal("foobar", userAgent);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support bypassCSP option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportBypassCSPOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                BypassCSP = true
            });

            await page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await page.AddScriptTagAsync(content: "window.__injected = 42;");
            Assert.Equal(42, await page.EvaluateAsync<int>("window.__injected"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support javascriptEnabled option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportJavascriptEnabledOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                JavaScriptEnabled = false
            });

            await page.GoToAsync("data:text/html, <script>var something = \"forbidden\"</script>");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.EvaluateAsync("something"));

            if (TestConstants.IsWebKit)
            {
                Assert.Contains("Can't find variable: something", exception.Message);
            }
            else
            {
                Assert.Contains("something is not defined", exception.Message);
            }

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support httpCredentials option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRupportHttpCredentialsOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                HttpCredentials = new Credentials
                {
                    Username = "user",
                    Password = "pass",
                }
            });

            Server.SetAuth("/playground.html", "user", "pass");
            var response = await page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support offline option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportOfflineOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                Offline = true
            });

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync(TestConstants.EmptyPage));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>launchPersistentContext()</playwright-describe>
        /// <playwright-it>should support acceptDownloads option</playwright-it>
        [Fact(Skip = "Skipped on playwright")]
        public void ShouldSupportAcceptDownloadsOption()
        {
        }

        private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchAsync(BrowserContextOptions options = null)
        {
            var tmp = new TempDirectory();
            var context = await BrowserType.LaunchPersistentContextAsync(
                tmp.Path,
                TestConstants.GetDefaultBrowserOptions().ToPersistentOptions() + (options ?? new BrowserContextOptions()));
            var page = context.Pages.First();

            return (tmp, context, page);
        }
    }
}
