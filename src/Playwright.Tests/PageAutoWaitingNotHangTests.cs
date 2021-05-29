using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageAutoWaitingNotHangTests : PageTestEx
    {
        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "clicking on links which do not commit navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">fooobar</a>");
            await Page.ClickAsync("a");
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.stop async")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowStopAsync()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                setTimeout(() => window.stop(), 100);
             }}", Server.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.stop")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowStop()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                window.stop();
             }}", Server.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "assigning location to about:blank")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task AssigningLocationToAboutBlank()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync("window.location.href = 'about:blank';");
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "assigning location to about:blank after non-about:blank")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task AssigningLocationToAboutBlankAfterNonAboutBlank()
        {
            Server.SetRoute("/empty.html", _ => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = '{Server.EmptyPage}';
                window.location.href = 'about:blank';
             }}", Server.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "calling window.open and window.close")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowOpenAndWindowClose()
        {
            await Page.GotoAsync(Server.EmptyPage);

            await Page.EvaluateAsync($@"(url) => {{
                const popup = window.open(window.location.href);
                popup.close();
             }}", Server.EmptyPage);
        }

        [PlaywrightTest("page-autowaiting-no-hang.spec.ts", "opening a popup")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task OpeningAPopup()
        {
            await Page.GotoAsync(Server.EmptyPage);

            await TaskUtils.WhenAll(
                Page.WaitForPopupAsync(),
                Page.EvaluateAsync("() => window._popup = window.open(window.location.href)"));
        }
    }
}
