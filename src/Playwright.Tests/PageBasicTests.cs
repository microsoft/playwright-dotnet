using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageBasicTests : PageTestEx
    {
        [PlaywrightTest("page-basic.spec.ts", "should reject all promises when page is closed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => TaskUtils.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            StringAssert.Contains("Protocol error", exception.Message);
        }

        [PlaywrightTest("page-basic.spec.ts", "async stacks should work")]
        [Test, Ignore("We don't need to test this in .NET")]
        public async Task AsyncStacksShouldWork()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Abort(); // is this right?
                return Task.CompletedTask;
            });
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains(nameof(PageBasicTests), exception.StackTrace);
        }

        [PlaywrightTest("page-basic.spec.ts", "Page.press should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PagePressShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
            await Page.PressAsync("textarea", "a");
            Assert.AreEqual("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-basic.spec.ts", "Frame.press should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task FramePressShouldWork()
        {
            await Page.SetContentAsync($"<iframe name =inner src=\"{Server.Prefix}/input/textarea.html\"></iframe>");
            var frame = Page.Frames.Single(f => f.Name == "inner");
            await frame.PressAsync("textarea", "a");
            Assert.AreEqual("a", await frame.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-basic.spec.ts", "page.frame should respect name")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheCorrectBrowserInstance()
        {
            await Page.SetContentAsync("<iframe name=target></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Name == "target");
            Assert.AreEqual(Page.MainFrame.ChildFrames.First(), frame);
        }

        [PlaywrightTest("page-basic.spec.ts", "page.frame should respect url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectUrl()
        {
            await Page.SetContentAsync($"<iframe src=\"{Server.EmptyPage}\"></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Url.Contains("empty"));
            Assert.AreEqual(Page.MainFrame.ChildFrames.First(), frame);
        }

        [PlaywrightTest("page-basic.spec.ts", "should provide access to the opener page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldProvideAccessToTheOpenerPage()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForPopupAsync(),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var opener = await popupEvent.OpenerAsync();
            Assert.AreEqual(Page, opener);
        }

        [PlaywrightTest("page-basic.spec.ts", "should return null if parent page has been closed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullIfParentPageHasBeenClosed()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForPopupAsync(),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            await Page.CloseAsync();
            var opener = await popupEvent.OpenerAsync();
            Assert.Null(opener);
        }

        [PlaywrightTest("page-basic.spec.ts", "should return the page title")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnThePageTitle()
        {
            await Page.GotoAsync(Server.Prefix + "/title.html");
            Assert.AreEqual("Woof-Woof", await Page.TitleAsync());
        }

        [PlaywrightTest("page-basic.spec.ts", "page.url should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageUrlShouldWork()
        {
            Assert.AreEqual("about:blank", Page.Url);
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-basic.spec.ts", "should include hashes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeHashes()
        {
            await Page.GotoAsync(Server.EmptyPage + "#hash");
            Assert.AreEqual(Server.EmptyPage + "#hash", Page.Url);
            await Page.EvaluateAsync("() => window.location.hash = 'dynamic'");
            Assert.AreEqual(Server.EmptyPage + "#dynamic", Page.Url);
        }

        [PlaywrightTest("page-basic.spec.ts", "should fail with error upon disconnect")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithErrorUponDisconnect()
        {
            var task = Page.WaitForDownloadAsync();
            await Page.CloseAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => task);
            StringAssert.Contains("Page closed", exception.Message);
        }

        [PlaywrightTest("page-basic.spec.ts", "should have sane user agent")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveASaneUserAgent()
        {
            string userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
            string[] parts = Regex.Split(userAgent, "[()]").Select(t => t.Trim()).ToArray();

            Assert.AreEqual("Mozilla/5.0", parts[0]);

            if (TestConstants.IsFirefox)
            {
                string[] engineBrowser = parts[2].Split(' ');
                Assert.That(engineBrowser[0], Does.StartWith("Gecko"));
                Assert.That(engineBrowser[1], Does.StartWith("Firefox"));
            }
            else
            {
                Assert.That(parts[2], Does.StartWith("AppleWebKit/"));
                Assert.AreEqual("KHTML, like Gecko", parts[3]);
                string[] engineBrowser = parts[4].Split(' ');
                Assert.That(engineBrowser[1], Does.StartWith("Safari/"));

                if (TestConstants.IsChromium)
                {
                    StringAssert.Contains("Chrome/", engineBrowser[0]);
                }
                else
                {
                    Assert.That(engineBrowser[0], Does.StartWith("Version"));
                }
            }
        }

        [PlaywrightTest("page-basic.spec.ts", "should work with window.close")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithWindowClose()
        {
            var newPageTask = Page.WaitForPopupAsync();
            await Page.EvaluateAsync<string>("() => window['newPage'] = window.open('about:blank')");
            var newPage = await newPageTask;
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (_, _) => closedTsc.SetResult(true);
            await Page.EvaluateAsync<string>("() => window['newPage'].close()");
            await closedTsc.Task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should work with page.close")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPageClose()
        {
            var newPage = await Context.NewPageAsync();
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (_, _) => closedTsc.SetResult(true);
            await newPage.CloseAsync();
            await closedTsc.Task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should fire load when expected")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireLoadWhenExpected()
        {
            var loadEvent = new TaskCompletionSource<bool>();
            Page.Load += (_, _) => loadEvent.TrySetResult(true);

            await TaskUtils.WhenAll(
                loadEvent.Task,
                Page.GotoAsync("about:blank")
            );
        }

        [PlaywrightTest("page-basic.spec.ts", "should fire domcontentloaded when expected")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireDOMcontentloadedWhenExpected()
        {
            var task = Page.GotoAsync("about:blank");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should set the page close state")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetThePageCloseState()
        {
            var newPage = await Context.NewPageAsync();
            Assert.False(newPage.IsClosed);
            await newPage.CloseAsync();
            Assert.True(newPage.IsClosed);
        }

        [PlaywrightTest("page-basic.spec.ts", "should terminate network waiters")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => TaskUtils.WhenAll(
                newPage.WaitForRequestAsync(Server.EmptyPage),
                newPage.WaitForResponseAsync(Server.EmptyPage),
                newPage.CloseAsync()
            ));
            for (int i = 0; i < 2; i++)
            {
                string message = exception.Message;
                StringAssert.Contains("Page closed", message);
                Assert.That(message, Does.Not.Contain("Timeout"));
            }
        }

        [PlaywrightTest("page-basic.spec.ts", "should be callable twice")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            var newPage = await Context.NewPageAsync();
            await TaskUtils.WhenAll(
                newPage.CloseAsync(),
                newPage.CloseAsync());

            await newPage.CloseAsync();
        }

        [PlaywrightTest("page-basic.spec.ts", "should not be visible in context.pages")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            CollectionAssert.Contains(Context.Pages, newPage);
            await newPage.CloseAsync();
            CollectionAssert.DoesNotContain(Context.Pages, newPage);
        }
    }
}
