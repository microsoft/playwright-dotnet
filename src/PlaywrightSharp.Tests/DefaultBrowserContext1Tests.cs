using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DefaultBrowserContext1Tests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public DefaultBrowserContext1Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.cookies() should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ContextCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GoToAsync(TestConstants.EmptyPage);

            string documentCookie = await page.EvaluateAsync<string>(@"() => {
              document.cookie = 'username=John Doe';
              return document.cookie;
            }");

            Assert.Equal("username=John Doe", documentCookie);
            var cookie = (await page.Context.GetCookiesAsync()).Single();
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.addCookies() should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

            var cookie = (await page.Context.GetCookiesAsync()).Single();
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.clearCookies() should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should(not) block third party cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support viewport option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support deviceScaleFactor option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support userAgent option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support bypassCSP option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support javascriptEnabled option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support httpCredentials option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRupportHttpCredentialsOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "user",
                    Password = "pass",
                }
            });

            Server.SetAuth("/playground.html", "user", "pass");
            var response = await page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support offline option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support acceptDownloads option")]
        [Fact(Skip = "Skipped on playwright")]
        public void ShouldSupportAcceptDownloadsOption()
        {
        }

        private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchAsync(BrowserContextOptions options = null)
        {
            var tmp = new TempDirectory();
            var context = await BrowserType.LaunchDefaultPersistentContext(
                tmp.Path,
                options: options);
            var page = context.Pages.First();

            return (tmp, context, page);
        }
    }
}
