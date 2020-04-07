using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class UserDataDirTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public UserDataDirTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir option</playwright-it>
        [Fact]
        public async Task UserDataDirOption()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = userDataDir.Path;

            using var browser = await Playwright.LaunchAsync(options);
            Assert.True(Directory.GetFiles(userDataDir.Path).Length > 0);
            await browser.CloseAsync();
            Assert.True(Directory.GetFiles(userDataDir.Path).Length > 0);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [Fact]
        public async Task UserDataDirArgument()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();

            if (TestConstants.IsFirefox)
            {
                options.Args = options.Args.Concat(new[] { $"--profile=\"{userDataDir}\"" }).ToArray();
            }
            else
            {
                options.Args = options.Args.Concat(new[] { $"--user-data-dir=\"{userDataDir}\"" }).ToArray();
            }

            using var browser = await Playwright.LaunchAsync(options);
            // Open a page to make sure its functional.
            await browser.DefaultContext.NewPageAsync();
            Assert.True(Directory.GetDirectories(userDataDir.Path).Length > 0);
            await browser.CloseAsync();
            Assert.True(Directory.GetDirectories(userDataDir.Path).Length > 0);
        }

#if NETCOREAPP
        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task UserDataDirOptionShouldRestoreState()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = userDataDir.Path;

            await using (var browser = await Playwright.LaunchAsync(options))
            {
                var page = await browser.DefaultContext.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.EvaluateAsync("localStorage.hey = 'hello'");
                await browser.CloseAsync();
            }

            await using (var browser2 = await Playwright.LaunchAsync(options))
            {
                var page2 = await browser2.DefaultContext.NewPageAsync();
                await page2.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("hello", await page2.EvaluateAsync<string>("localStorage.hey"));
            }

            await using (var browser3 = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions()))
            {
                var page3 = await browser3.DefaultContext.NewPageAsync();
                await page3.GoToAsync(TestConstants.EmptyPage);
            }
        }
#else
        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task UserDataDirOptionShouldRestoreState()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = userDataDir.Path;

            var browser = await Playwright.LaunchAsync(options);

            var page = await browser.DefaultContext.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.EvaluateAsync("localStorage.hey = 'hello'");
            await browser.CloseAsync();

            var browser2 = await Playwright.LaunchAsync(options);
            var page2 = await browser2.DefaultContext.NewPageAsync();
            await page2.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("hello", await page2.EvaluateAsync<string>("localStorage.hey"));
            await browser2.CloseAsync();

            var browser3 = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page3 = await browser3.DefaultContext.NewPageAsync();
            await page3.GoToAsync(TestConstants.EmptyPage);
            await browser3.CloseAsync();
        }
#endif
        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch({userDataDir})</playwright-describe>
        ///<playwright-it>userDataDir argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task UserDataDirOptionShouldRestoreCookies()
        {
            using var userDataDir = new TempDirectory();
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = options.Args.Concat(new[] { $"--user-data-dir=\"{userDataDir}\"" }).ToArray();

            using (var browser = await Playwright.LaunchAsync(options))
            {
                var page = await browser.DefaultContext.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                await page.EvaluateAsync(
                    "document.cookie = 'doSomethingOnlyOnce=true; expires=Fri, 31 Dec 9999 23:59:59 GMT'");
            }

            using (var browser2 = await Playwright.LaunchAsync(options))
            {
                var page2 = await browser2.DefaultContext.NewPageAsync();
                await page2.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("doSomethingOnlyOnce=true", await page2.EvaluateAsync<string>("document.cookie"));
            }
        }
    }
}
