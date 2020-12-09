using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>headful.spec.js</playwright-file>

    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class HeadfulTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public HeadfulTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should have default url when launching browser</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveDefaultUrlWhenLaunchingBrowser()
        {
            using var tempDir = new TempDirectory();
            await using var browserContext = await BrowserType.LaunchPersistentContextAsync(tempDir.Path, TestConstants.GetHeadfulOptions());
            string[] pages = browserContext.Pages.Select(page => page.Url).ToArray();
            Assert.Equal(new[] { "about:blank" }, pages);
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>headless should be able to read cookies written by headful</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true, skipWindows: true)]
        public async Task HeadlessShouldBeAbleToReadCookiesWrittenByHeadful()
        {
            using var userDataDir = new TempDirectory();

            // Write a cookie in headful chrome
            var headfulOptions = TestConstants.GetHeadfulOptions();
            headfulOptions.UserDataDir = userDataDir.Path;

            await using var headfulContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetHeadfulOptions());
            var headfulPage = await headfulContext.NewPageAsync();
            await headfulPage.GoToAsync(TestConstants.EmptyPage);
            await headfulPage.EvaluateAsync("() => document.cookie = 'foo=true; expires=Fri, 31 Dec 9999 23:59:59 GMT'");
            await headfulContext.CloseAsync();

            var headlessContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetDefaultBrowserOptions());
            var headlessPage = await headlessContext.NewPageAsync();
            await headlessPage.GoToAsync(TestConstants.EmptyPage);
            string cookie = await headlessPage.EvaluateAsync<string>("() => document.cookie");
            await headlessContext.CloseAsync();

            Assert.Equal("foo=true", cookie);
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should close browser with beforeunload page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldCloseBrowserWithBeforeunloadPage()
        {
            using var userDataDir = new TempDirectory();
            await using var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, TestConstants.GetHeadfulOptions());
            var page = await browserContext.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers fire.
            await page.ClickAsync("body");
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should not crash when creating second context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotCrashWhenCreatingSecondContext()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetHeadfulOptions());

            await using (var browserContext = await browser.NewContextAsync())
            {
                await browserContext.NewPageAsync();
            }

            await using (var browserContext = await browser.NewContextAsync())
            {
                await browserContext.NewPageAsync();
            }
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should click background tab</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldClickBackgroundTab()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetHeadfulOptions());
            var page = await browser.NewPageAsync();
            await page.SetContentAsync($"<button>Hello</button><a target=_blank href=\"{TestConstants.EmptyPage}\">empty.html</a>");
            await page.ClickAsync("a");
            await page.ClickAsync("button");
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should close browser after context menu was triggered</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldCloseBrowserAfterContextMenuWasTriggered()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetHeadfulOptions());
            var page = await browser.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await page.ClickAsync("body", button: MouseButton.Right);
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should(not) block third party cookies</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotBlockThirdPartyCookies()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetHeadfulOptions());
            var page = await browser.NewPageAsync();
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

            string documentCookie = await page.Frames[1].EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                return document.cookie;
            }");

            await page.WaitForTimeoutAsync(2000);
            bool allowsThirdParty = TestConstants.IsChromium || TestConstants.IsFirefox;
            Assert.Equal(allowsThirdParty ? "username=John Doe" : string.Empty, documentCookie);
            var cookies = await page.Context.GetCookiesAsync(TestConstants.CrossProcessUrl + "/grid.html");

            if (allowsThirdParty)
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
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>should not override viewport size when passed null</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldNotOverrideViewportSizeWhenPassedNull()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetHeadfulOptions());
            var context = await browser.NewContextAsync(new BrowserContextOptions { Viewport = null });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync(@"() => {
                    const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
                    win.resizeTo(500, 450);
                }"));

            var popup = popupTask.Result.Page;
            await popup.WaitForLoadStateAsync();
            await popup.WaitForFunctionAsync("() => window.outerWidth === 500 && window.outerHeight === 450");
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-it>Page.bringToFront should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task PageBringToFrontShouldWork()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetHeadfulOptions());
            var context = await browser.NewContextAsync(new BrowserContextOptions { Viewport = null });
            var page1 = await context.NewPageAsync();
            await page1.SetContentAsync("Page1");
            var page2 = await context.NewPageAsync();
            await page2.SetContentAsync("Page2");

            await page1.BringToFrontAsync();
            Assert.Equal("visible", await page1.EvaluateAsync<string>("document.visibilityState"));
            Assert.Equal("visible", await page2.EvaluateAsync<string>("document.visibilityState"));

            await page2.BringToFrontAsync();
            Assert.Equal("visible", await page1.EvaluateAsync<string>("document.visibilityState"));
            Assert.Equal("visible", await page2.EvaluateAsync<string>("document.visibilityState"));
        }
    }
}
