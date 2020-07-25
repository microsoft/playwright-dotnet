using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
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

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>clicking on links which do not commit navigation</playwright-it>
        [Retry]
        public async Task ClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">fooobar</a>");
            await Page.ClickAsync("a");
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>calling window.stop async</playwright-it>
        [Retry]
        public async Task CallingWindowStopAsync()
        {
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                setTimeout(() => window.stop(), 100);
             }}", TestConstants.EmptyPage);
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>calling window.stop</playwright-it>
        [Retry]
        public async Task CallingWindowStop()
        {
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = url;
                window.stop();
             }}", TestConstants.EmptyPage);
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>assigning location to about:blank</playwright-it>
        [Retry]
        public async Task AssigningLocationToAboutBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("window.location.href = 'about:blank';");
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>assigning location to about:blank after non-about:blank</playwright-it>
        [Retry]
        public async Task AssigningLocationToAboutBlankAfterNonAboutBlank()
        {
            Server.SetRoute("/empty.html", context => Task.CompletedTask);

            await Page.EvaluateAsync($@"(url) => {{
                window.location.href = '{TestConstants.EmptyPage}';
                window.location.href = 'about:blank';
             }}", TestConstants.EmptyPage);
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>calling window.open and window.close</playwright-it>
        [Retry]
        public async Task CallingWindowOpenAndWindowClose()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.EvaluateAsync($@"(url) => {{
                const popup = window.open(window.location.href);
                popup.close();
             }}", TestConstants.EmptyPage);
        }

        ///<playwright-file>autowaiting.spec.js</playwright-file>
        ///<playwright-describe>Auto waiting should not hang when</playwright-describe>
        ///<playwright-it>opening a popup</playwright-it>
        [Retry]
        public async Task OpeningAPopup()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Task.WhenAll(
                Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup),
                Page.EvaluateAsync("() => window._popup = window.open(window.location.href)"));
        }
    }
}
