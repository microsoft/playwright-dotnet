using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    /// <playwright-file>defaultbrowsercontext-2.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DefaultBrowsercontext2Tests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public DefaultBrowsercontext2Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support hasTouch option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should work in persistent context")]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support colorScheme option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support timezoneId option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support locale option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support geolocation and permissions options")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support ignoreHTTPSErrors option")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support extraHTTPHeaders option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportExtraHTTPHeadersOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserContextOptions
            {
                ExtraHTTPHeaders = new Dictionary<string, string>
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should accept userDataDir")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptUserDataDir()
        {
            var (tmp, context, _) = await LaunchAsync();
            Assert.NotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());
            await context.CloseAsync();
            Assert.NotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());

            tmp.Dispose();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should restore state from userDataDir")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should restore cookies from userDataDir")]
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

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should have default URL when launching browser")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveDefaultURLWhenLaunchingBrowser()
        {
            var (tmp, context, page) = await LaunchAsync();

            string[] urls = context.Pages.Select(p => p.Url).ToArray();
            Assert.Equal(new[] { "about:blank" }, urls);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should throw if page argument is passed")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowIfPageArgumentIsPassed()
        {
            var tmp = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { TestConstants.EmptyPage };

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => BrowserType.LaunchPersistentContextAsync(tmp.Path, options));

            tmp.Dispose();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should have passed URL when launching with ignoreDefaultArgs: true")]
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldHavePassedURLWhenLaunchingWithIgnoreDefaultArgsTrue()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should handle timeout")]
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldHandleTimeout()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should handle exception")]
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldHandleException()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should fire close event for a persistent context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireCloseEventForAPersistentContext()
        {
            var (tmp, context, _) = await LaunchAsync();
            bool closed = false;
            context.Close += (_, _) => closed = true;
            await context.CloseAsync();

            Assert.True(closed);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "coverage should work")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task CoverageShouldWork()
        {
            var (tmp, context, page) = await LaunchAsync();

            await page.Coverage.StartJSCoverageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/simple.html", LoadState.Networkidle);
            var coverage = await page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.Equal(1, coverage[0].Functions.Single(f => f.FunctionName == "foo").Ranges[0].Count);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "coverage should be missing")]
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task CoverageShouldBeMissing()
        {
            var (tmp, context, page) = await LaunchAsync();
            Assert.Null(page.Coverage);
            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should respect selectors")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal("hello", await page.GetInnerHTMLAsync("css=div"));
            Assert.Equal("hello", await page.GetInnerHTMLAsync("defaultContextCSS=div"));

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
