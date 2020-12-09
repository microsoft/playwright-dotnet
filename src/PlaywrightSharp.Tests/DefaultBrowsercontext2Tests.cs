using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DefaultBrowsercontext2Tests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public DefaultBrowsercontext2Tests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support hasTouch option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportHasTouchOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                HasTouch = true
            });

            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should work in persistent context</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkInPersistentContext()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });

            await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(980, await page.EvaluateAsync<int>("() => window.innerWidth"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support colorScheme option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportColorSchemeOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                ColorScheme = ColorScheme.Dark,
            });

            Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support timezoneId option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportTimezoneIdOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                TimezoneId = "America/Jamaica",
            });

            Assert.Equal("Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)", await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support locale option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportLocaleOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                Locale = "fr-CH",
            });

            Assert.Equal("fr-CH", await page.EvaluateAsync<string>("() => navigator.language"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support geolocation and permissions options</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportGeolocationAndPermissionsOptions()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                Geolocation = new Geolocation
                {
                    Latitude = 10,
                    Longitude = 10,
                },
                Permissions = new[] { ContextPermission.Geolocation },
            });

            await page.GoToAsync(TestConstants.EmptyPage);
            var geolocation = await page.EvaluateAsync<Geolocation>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
            Assert.Equal(10, geolocation.Latitude);
            Assert.Equal(10, geolocation.Longitude);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support ignoreHTTPSErrors option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportIgnoreHTTPSErrorsOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            var response = await page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");
            Assert.True(response.Ok);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should support extraHTTPHeaders option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportExtraHTTPHeadersOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                ExtraHttpHeaders = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
            });

            string fooHeader = string.Empty;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", r => fooHeader = r.Headers["foo"]),
                page.GoToAsync(TestConstants.EmptyPage));

            Assert.Equal("bar", fooHeader);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should accept userDataDir</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAcceptUserDataDir()
        {
            var (tmp, context, _) = await LaunchAsync();
            Assert.NotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());
            await context.CloseAsync();
            Assert.NotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());

            tmp.Dispose();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should restore state from userDataDir</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRestoreStateFromUserDataDir()
        {
            using var userDataDir = new TempDirectory();

            await using (var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetDefaultBrowserOptions()))
            {
                var page = await browserContext.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.EvaluateAsync("() => localStorage.hey = 'hello'");
            }

            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetDefaultBrowserOptions()))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("hello", await page.EvaluateAsync<string>("() => localStorage.hey"));
            }

            using var userDataDir2 = new TempDirectory();
            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir2.Path, TestConstants.GetDefaultBrowserOptions()))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.NotEqual("hello", await page.EvaluateAsync<string>("() => localStorage.hey"));
            }

            userDataDir2.Dispose();
            userDataDir.Dispose();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should restore cookies from userDataDir</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true, skipWindows: true, skipOSX: true)]
        public async Task ShouldRestoreCookiesFromUserDataDir()
        {
            var userDataDir = new TempDirectory();

            await using (var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetDefaultBrowserOptions()))
            {
                var page = await browserContext.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                string documentCookie = await page.EvaluateAsync<string>(@"() => {
                    document.cookie = 'doSomethingOnlyOnce=true; expires=Fri, 31 Dec 9999 23:59:59 GMT';
                    return document.cookie;
                }");

                Assert.Equal("doSomethingOnlyOnce=true", documentCookie);
            }

            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetDefaultBrowserOptions()))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("doSomethingOnlyOnce=true", await page.EvaluateAsync<string>("() => document.cookie"));
            }

            var userDataDir2 = new TempDirectory();
            await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir2.Path, TestConstants.GetDefaultBrowserOptions()))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.NotEqual("doSomethingOnlyOnce=true", await page.EvaluateAsync<string>("() => document.cookie"));
            }

            userDataDir2.Dispose();
            userDataDir.Dispose();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should have default URL when launching browser</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveDefaultURLWhenLaunchingBrowser()
        {
            var (tmp, context, page) = await LaunchAsync();

            string[] urls = context.Pages.Select(p => p.Url).ToArray();
            Assert.Equal(new[] { "about:blank" }, urls);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should throw if page argument is passed</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowIfPageArgumentIsPassed()
        {
            var tmp = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { TestConstants.EmptyPage };

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => BrowserType.LaunchPersistentContextAsync(tmp.Path, options));

            tmp.Dispose();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should have passed URL when launching with ignoreDefaultArgs: true</playwright-it>
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldHavePassedURLWhenLaunchingWithIgnoreDefaultArgsTrue()
        {
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should handle timeout</playwright-it>
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldHandleTimeout()
        {
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should handle exception</playwright-it>
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldHandleException()
        {
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should fire close event for a persistent context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireCloseEventForAPersistentContext()
        {
            var (tmp, context, page) = await LaunchAsync();
            bool closed = false;
            context.Close += (sender, e) => closed = true;
            await context.CloseAsync();

            Assert.True(closed);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        ///<playwright-it>coverage should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task CoverageShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.Coverage.StartJSCoverageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/simple.html", LifecycleEvent.Networkidle);
            var coverage = await page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.Equal(1, coverage[0].Functions.FirstOrDefault(f => f.FunctionName == "foo").Ranges[0].Count);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        ///<playwright-it>coverage should be missing</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task CoverageShouldBeMissing()
        {
            var (tmp, context, page) = await LaunchAsync();
            Assert.Null(page.Coverage);
            tmp.Dispose();
            await context.DisposeAsync();
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        ///<playwright-it>should respect selectors</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectSelectors()
        {
            var (tmp, context, page) = await LaunchAsync();
            const string defaultContextCSS = @"({
                create(root, target) {},
                query(root, selector) {
                    return root.querySelector(selector);
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll(selector));
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "defaultContextCSS", defaultContextCSS);
            await page.SetContentAsync("<div>hello</div>");
            Assert.Equal("hello", await page.GetInnerHtmlAsync("css=div"));
            Assert.Equal("hello", await page.GetInnerHtmlAsync("defaultContextCSS=div"));

            tmp.Dispose();
            await context.DisposeAsync();
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
