using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
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
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
            {
                HasTouch = true
            });

            await page.GotoAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.True(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should work in persistent context")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkInPersistentContext()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });

            await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(980, await page.EvaluateAsync<int>("() => window.innerWidth"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support colorScheme option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportColorSchemeOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
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
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
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
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
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
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
            {
                Geolocation = new Geolocation
                {
                    Latitude = 10,
                    Longitude = 10,
                },
                Permissions = new[] { "geolocation" },
            });

            await page.GotoAsync(TestConstants.EmptyPage);
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
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            var response = await page.GotoAsync(TestConstants.HttpsPrefix + "/empty.html");
            Assert.True(response.Ok);

            tmp.Dispose();
            await context.DisposeAsync();
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support extraHTTPHeaders option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportExtraHTTPHeadersOption()
        {
            var (tmp, context, page) = await LaunchAsync(new BrowserTypeLaunchPersistentContextOptions
            {
                ExtraHTTPHeaders = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
            });

            string fooHeader = string.Empty;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", r => fooHeader = r.Headers["foo"]),
                page.GotoAsync(TestConstants.EmptyPage));

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


            await using (var browserContext = await BrowserType.LaunchDefaultPersistentContext(userDataDir.Path))
            {
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                await page.EvaluateAsync("() => localStorage.hey = 'hello'");
            }

            await using (var browserContext2 = await BrowserType.LaunchDefaultPersistentContext(userDataDir.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                Assert.Equal("hello", await page.EvaluateAsync<string>("() => localStorage.hey"));
            }

            using var userDataDir2 = new TempDirectory();
            await using (var browserContext2 = await BrowserType.LaunchDefaultPersistentContext(userDataDir2.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
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

            await using (var browserContext = await BrowserType.LaunchDefaultPersistentContext(userDataDir.Path))
            {
                var page = await browserContext.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                string documentCookie = await page.EvaluateAsync<string>(@"() => {
                    document.cookie = 'doSomethingOnlyOnce=true; expires=Fri, 31 Dec 9999 23:59:59 GMT';
                    return document.cookie;
                }");

                Assert.Equal("doSomethingOnlyOnce=true", documentCookie);
            }

            await using (var browserContext2 = await BrowserType.LaunchDefaultPersistentContext(userDataDir.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                Assert.Equal("doSomethingOnlyOnce=true", await page.EvaluateAsync<string>("() => document.cookie"));
            }

            var userDataDir2 = new TempDirectory();
            await using (var browserContext2 = await BrowserType.LaunchDefaultPersistentContext(userDataDir2.Path))
            {
                var page = await browserContext2.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
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
            await Assert.ThrowsAnyAsync<PlaywrightException>(() =>
                BrowserType.LaunchDefaultPersistentContext(tmp.Path, new[] { TestConstants.EmptyPage }));

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
        [Fact(Skip = "We won't support coverage")]
        public void CoverageShouldWork()
        {
            /*
            var (tmp, context, page) = await LaunchAsync();

            await page.Coverage.StartJSCoverageAsync();
            await page.GotoAsync(TestConstants.ServerUrl + "/jscoverage/simple.html", LoadState.NetworkIdle);
            var coverage = await page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.Equal(1, coverage[0].Functions.Single(f => f.FunctionName == "foo").Ranges[0].Count);

            tmp.Dispose();
            await context.DisposeAsync();
            */
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "coverage should be missing")]
        [Fact(Skip = "We won't support coverage")]
        public void CoverageShouldBeMissing()
        {
            /*
            var (tmp, context, page) = await LaunchAsync();
            Assert.Null(page.Coverage);
            tmp.Dispose();
            await context.DisposeAsync();
            */
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
            Assert.Equal("hello", await page.InnerHTMLAsync("css=div"));
            Assert.Equal("hello", await page.InnerHTMLAsync("defaultContextCSS=div"));

            tmp.Dispose();
            await context.DisposeAsync();
        }

        private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchAsync(BrowserTypeLaunchPersistentContextOptions options = null)
        {
            var tmp = new TempDirectory();
            var context = await BrowserType.LaunchDefaultPersistentContext(tmp.Path, null, options);
            var page = context.Pages.First();

            return (tmp, context, page);
        }
    }
}
