using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class DefaultBrowserContext1Tests : PlaywrightTestEx
    {
        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.cookies() should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ContextCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GotoAsync(Server.EmptyPage);

            string documentCookie = await page.EvaluateAsync<string>(@"() => {
              document.cookie = 'username=John Doe';
              return document.cookie;
            }");

            Assert.AreEqual("username=John Doe", documentCookie);
            var cookie = (await page.Context.CookiesAsync()).Single();
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("John Doe", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.addCookies() should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ContextAddCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GotoAsync(Server.EmptyPage);
            await context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "username",
                    Value = "John Doe",
                }
            });

            Assert.AreEqual("username=John Doe", await page.EvaluateAsync<string>(@"() => document.cookie"));

            var cookie = (await page.Context.CookiesAsync()).Single();
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("John Doe", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.clearCookies() should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ContextClearCookiesShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GotoAsync(Server.EmptyPage);
            await context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie1",
                    Value = "1",
                },
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie2",
                    Value = "2",
                },
            });

            Assert.AreEqual("cookie1=1; cookie2=2", await page.EvaluateAsync<string>(@"() => document.cookie"));

            await context.ClearCookiesAsync();
            await page.ReloadAsync();
            Assert.IsEmpty(await page.Context.CookiesAsync());
            Assert.IsEmpty(await page.EvaluateAsync<string>(@"() => document.cookie"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should(not) block third party cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotBlockThirdPartyCookies()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.GotoAsync(Server.EmptyPage);
            await page.EvaluateAsync(@"src => {
                  let fulfill;
                  const promise = new Promise(x => fulfill = x);
                  const iframe = document.createElement('iframe');
                  document.body.appendChild(iframe);
                  iframe.onload = fulfill;
                  iframe.src = src;
                  return promise;
                }", Server.CrossProcessPrefix + "/grid.html");

            await page.FirstChildFrame().EvaluateAsync<string>("document.cookie = 'username=John Doe'");
            await page.WaitForTimeoutAsync(2000);
            bool allowsThirdPart = !TestConstants.IsWebKit;
            var cookies = await context.CookiesAsync(new[] { Server.CrossProcessPrefix + "/grid.html" });

            if (allowsThirdPart)
            {
                Assert.That(cookies, Has.Count.EqualTo(1));
                var cookie = cookies.First();
                Assert.AreEqual("127.0.0.1", cookie.Domain);
                Assert.AreEqual(cookie.Expires, -1);
                Assert.False(cookie.HttpOnly);
                Assert.AreEqual("username", cookie.Name);
                Assert.AreEqual("/", cookie.Path);
                Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
                Assert.False(cookie.Secure);
                Assert.AreEqual("John Doe", cookie.Value);
            }
            else
            {
                Assert.IsEmpty(cookies);
            }

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support viewport option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportViewportOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                ViewportSize = new()
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDeviceScaleFactorOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                DeviceScaleFactor = 3
            });

            Assert.AreEqual(3, await page.EvaluateAsync<int>("window.devicePixelRatio"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support userAgent option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportUserAgentOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                UserAgent = "foobar"
            });

            string userAgent = string.Empty;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", r => userAgent = r.Headers["user-agent"]),
                page.GotoAsync(Server.EmptyPage));

            Assert.AreEqual("foobar", userAgent);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support bypassCSP option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportBypassCSPOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                BypassCSP = true
            });

            await page.GotoAsync(Server.Prefix + "/csp.html");
            await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
            Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support javascriptEnabled option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportJavascriptEnabledOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                JavaScriptEnabled = false
            });

            await page.GotoAsync("data:text/html, <script>var something = \"forbidden\"</script>");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.EvaluateAsync("something"));

            if (TestConstants.IsWebKit)
            {
                StringAssert.Contains("Can't find variable: something", exception.Message);
            }
            else
            {
                StringAssert.Contains("something is not defined", exception.Message);
            }

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support httpCredentials option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportHttpCredentialsOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                HttpCredentials = new()
                {
                    Username = "user",
                    Password = "pass",
                }
            });

            Server.SetAuth("/playground.html", "user", "pass");
            var response = await page.GotoAsync(Server.Prefix + "/playground.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support offline option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportOfflineOption()
        {
            var (tmp, context, page) = await LaunchAsync(new()
            {
                Offline = true
            });

            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync(Server.EmptyPage));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support acceptDownloads option")]
        [Test, Ignore("Skipped on playwright")]
        public void ShouldSupportAcceptDownloadsOption()
        {
        }

        private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchAsync(BrowserTypeLaunchPersistentContextOptions options = null)
        {
            var tmp = new TempDirectory();
            var context = await BrowserType.LaunchPersistentContextAsync(
                tmp.Path,
                options: options);
            var page = context.Pages.First();

            return (tmp, context, page);
        }
    }
}
