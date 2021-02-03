using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Autowaiting
{
    ///<playwright-file>autowaiting.spec.js</playwright-file>
    ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class AutoWaitingShouldNotHangTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public AutoWaitingShouldNotHangTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "clicking on links which do not commit navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">fooobar</a>");
            await Page.ClickAsync("a");
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "calling window.stop async")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowStopAsync()
        {
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                setTimeout(() => window.stop(), 100);
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "calling window.stop")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowStop()
        {
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                window.stop();
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "assigning location to about:blank")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task AssigningLocationToAboutBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("window.location.href = 'about:blank';");
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "assigning location to about:blank after non-about:blank")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task AssigningLocationToAboutBlankAfterNonAboutBlank()
        {
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = '{TestConstants.EmptyPage}';
                window.location.href = 'about:blank';
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "calling window.open and window.close")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CallingWindowOpenAndWindowClose()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.EvaluateAsync($@"(url) => {{
                const popup = window.open(window.location.href);
                popup.close();
             }}", TestConstants.EmptyPage);
        }

        [PlaywrightTest("autowaiting.spec.js", "Auto waiting should not hang when", "opening a popup")]
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
