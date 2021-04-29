using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAutoWaitingNotHangTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAutoWaitingNotHangTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "clicking on links which do not commit navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">fooobar</a>");
            await Page.ClickAsync("a");
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.stop async")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowStopAsync()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                setTimeout(() => window.stop(), 100);
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.stop")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowStop()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                window.stop();
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "assigning location to about:blank")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task AssigningLocationToAboutBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("window.location.href = 'about:blank';");
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "assigning location to about:blank after non-about:blank")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task AssigningLocationToAboutBlankAfterNonAboutBlank()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = '{TestConstants.EmptyPage}';
                window.location.href = 'about:blank';
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.open and window.close")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowOpenAndWindowClose()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.EvaluateAsync($@"(url) => {{
                const popup = window.open(window.location.href);
                popup.close();
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "opening a popup")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task OpeningAPopup()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("() => window._popup = window.open(window.location.href)"));
        }
    }
}
