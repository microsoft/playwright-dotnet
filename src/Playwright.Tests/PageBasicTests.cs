using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageBasicTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageBasicTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-basic.spec.ts", "should reject all promises when page is closed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => TaskUtils.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            Assert.Contains("Protocol error", Assert.IsType<TargetClosedException>(exception).Message);
        }

        [PlaywrightTest("page-basic.spec.ts", "async stacks should work")]
        [Fact(Skip = "We don't need to test this in .NET")]
        public async Task AsyncStacksShouldWork()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Abort(); // is this right?
                return Task.CompletedTask;
            });
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains(nameof(PageBasicTests), exception.StackTrace);
        }

        [PlaywrightTest("page-basic.spec.ts", "Page.press should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PagePressShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.PressAsync("textarea", "a");
            Assert.Equal("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-basic.spec.ts", "Frame.press should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task FramePressShouldWork()
        {
            await Page.SetContentAsync($"<iframe name =inner src=\"{TestConstants.ServerUrl}/input/textarea.html\"></iframe>");
            var frame = Page.Frames.Single(f => f.Name == "inner");
            await frame.PressAsync("textarea", "a");
            Assert.Equal("a", await frame.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-basic.spec.ts", "page.frame should respect name")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheCorrectBrowserInstance()
        {
            await Page.SetContentAsync("<iframe name=target></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Name == "target");
            Assert.Same(Page.MainFrame.ChildFrames.First(), frame);
        }

        [PlaywrightTest("page-basic.spec.ts", "page.frame should respect url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectUrl()
        {
            await Page.SetContentAsync($"<iframe src=\"{TestConstants.EmptyPage}\"></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Url.Contains("empty"));
            Assert.Same(Page.MainFrame.ChildFrames.First(), frame);
        }

        [PlaywrightTest("page-basic.spec.ts", "should provide access to the opener page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProvideAccessToTheOpenerPage()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var opener = await popupEvent.OpenerAsync();
            Assert.Equal(Page, opener);
        }

        [PlaywrightTest("page-basic.spec.ts", "should return null if parent page has been closed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullIfParentPageHasBeenClosed()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            await Page.CloseAsync();
            var opener = await popupEvent.OpenerAsync();
            Assert.Null(opener);
        }

        [PlaywrightTest("page-basic.spec.ts", "should return the page title")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnThePageTitle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/title.html");
            Assert.Equal("Woof-Woof", await Page.TitleAsync());
        }

        [PlaywrightTest("page-basic.spec.ts", "page.url should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageUrlShouldWork()
        {
            Assert.Equal("about:blank", Page.Url);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-basic.spec.ts", "should include hashes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeHashes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(TestConstants.EmptyPage + "#hash", Page.Url);
            await Page.EvaluateAsync("() => window.location.hash = 'dynamic'");
            Assert.Equal(TestConstants.EmptyPage + "#dynamic", Page.Url);
        }

        [PlaywrightTest("page-basic.spec.ts", "should fail with error upon disconnect")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithErrorUponDisconnect()
        {
            var task = Page.WaitForEventAsync(PageEvent.Download);
            await Page.CloseAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => task);
            Assert.Contains("Page closed", exception.Message);
        }

        [PlaywrightTest("page-basic.spec.ts", "should have sane user agent")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveASaneUserAgent()
        {
            string userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
            var regex = new Regex("[()]");
            string[] parts = Regex.Split(userAgent, "[()]").Select(t => t.Trim()).ToArray();

            Assert.Equal("Mozilla/5.0", parts[0]);

            if (TestConstants.IsFirefox)
            {
                string[] engineBrowser = parts[2].Split(' ');
                Assert.StartsWith("Gecko", engineBrowser[0]);
                Assert.StartsWith("Firefox", engineBrowser[1]);
            }
            else
            {
                Assert.StartsWith("AppleWebKit/", parts[2]);
                Assert.Equal("KHTML, like Gecko", parts[3]);
                string[] engineBrowser = parts[4].Split(' ');
                Assert.StartsWith("Safari/", engineBrowser[1]);

                if (TestConstants.IsChromium)
                {
                    Assert.Contains("Chrome/", engineBrowser[0]);
                }
                else
                {
                    Assert.StartsWith("Version", engineBrowser[0]);
                }
            }
        }

        [PlaywrightTest("page-basic.spec.ts", "should work with window.close")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithWindowClose()
        {
            var newPageTask = Page.WaitForEventAsync(PageEvent.Popup);
            await Page.EvaluateAsync<string>("() => window['newPage'] = window.open('about:blank')");
            var newPage = await newPageTask;
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (_, _) => closedTsc.SetResult(true);
            await Page.EvaluateAsync<string>("() => window['newPage'].close()");
            await closedTsc.Task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should work with page.close")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPageClose()
        {
            var newPage = await Context.NewPageAsync();
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (_, _) => closedTsc.SetResult(true);
            await newPage.CloseAsync();
            await closedTsc.Task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should fire load when expected")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireLoadWhenExpected()
        {
            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Load),
                Page.GoToAsync("about:blank")
            );
        }

        [PlaywrightTest("page-basic.spec.ts", "should fire domcontentloaded when expected")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireDOMcontentloadedWhenExpected()
        {
            var task = Page.GoToAsync("about:blank");
            await Page.WaitForEventAsync(PageEvent.DOMContentLoaded);
            await task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should set the page close state")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetThePageCloseState()
        {
            var newPage = await Context.NewPageAsync();
            Assert.False(newPage.IsClosed);
            await newPage.CloseAsync();
            Assert.True(newPage.IsClosed);
        }

        [PlaywrightTest("page-basic.spec.ts", "should terminate network waiters")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => TaskUtils.WhenAll(
                newPage.WaitForRequestAsync(TestConstants.EmptyPage),
                newPage.WaitForResponseAsync(TestConstants.EmptyPage),
                newPage.CloseAsync()
            ));
            for (int i = 0; i < 2; i++)
            {
                string message = exception.Message;
                Assert.Contains("Page closed", message);
                Assert.DoesNotContain("Timeout", message);
            }
        }

        [PlaywrightTest("page-basic.spec.ts", "should be callable twice")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            var newPage = await Context.NewPageAsync();
            await TaskUtils.WhenAll(
                newPage.CloseAsync(),
                newPage.CloseAsync());

            await newPage.CloseAsync();
        }

        [PlaywrightTest("page-basic.spec.ts", "should not be visible in context.pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            Assert.Contains(newPage, Context.Pages);
            await newPage.CloseAsync();
            Assert.DoesNotContain(newPage, Context.Pages);
        }
    }
}
