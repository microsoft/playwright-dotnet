using System;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.setContent</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageSetContentTests : PlaywrightSharpPageBaseTest
    {
        const string _expectedOutput = "<html><head></head><body><div>hello</div></body></html>";

        /// <inheritdoc />
        public PageSetContentTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<div>hello</div>");
            string result = await Page.GetContentAsync();
            Assert.Equal(_expectedOutput, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with domcontentloaded</playwright-it>
        [Retry]
        public async Task ShouldWorkWithDomcontentloaded()
        {
            await Page.SetContentAsync("<div>hello</div>", LifecycleEvent.DOMContentLoaded);
            string result = await Page.GetContentAsync();
            Assert.Equal(_expectedOutput, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should not confuse with previous navigation</playwright-it>
        [Retry]
        public async Task ShouldNotConfuseWithPreviousNavigation()
        {
            string imgPath = "/img.png";
            var imgResponse = new TaskCompletionSource<bool>();
            Server.SetRoute(imgPath, context => imgResponse.Task);
            bool loaded = false;
            // get the global object to make sure that the main execution context is alive and well.
            await Page.EvaluateAsync("() => this");
            // Trigger navigation which might resolve next setContent call.
            var evalPromise = Page.EvaluateAsync("url => window.location.href = url", TestConstants.EmptyPage);
            var contentPromise = Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\" ></img>").ContinueWith(_ => loaded = true);
            await Server.WaitForRequest(imgPath);

            Assert.False(loaded);
            for (int i = 0; i < 5; i++)
            {
                await Page.EvaluateAsync("1");  // Roundtrips to give setContent a chance to resolve.
            }

            Assert.False(loaded);

            imgResponse.SetResult(true);
            await contentPromise;
            await evalPromise;
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with doctype</playwright-it>
        [Retry]
        public async Task ShouldWorkWithDoctype()
        {
            string doctype = "<!DOCTYPE html>";
            await Page.SetContentAsync($"{doctype}<div>hello</div>");
            string result = await Page.GetContentAsync();
            Assert.Equal($"{doctype}{_expectedOutput}", result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with HTML 4 doctype</playwright-it>
        [Retry]
        public async Task ShouldWorkWithHTML4Doctype()
        {
            string doctype = "<!DOCTYPE html PUBLIC \" -//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">";
            await Page.SetContentAsync($"{doctype}<div>hello</div>");
            string result = await Page.GetContentAsync();
            Assert.Equal($"{doctype}{_expectedOutput}", result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Retry]
        public async Task ShouldRespectTimeout()
        {
            string imgPath = "/img.png";
            // stall for image
            Server.SetRoute(imgPath, context => Task.Delay(Timeout.Infinite));
            await Assert.ThrowsAsync<TimeoutException>(() =>
                Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\"></img>", new NavigationOptions { Timeout = 1 })
            );
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should respect default navigation timeout</playwright-it>
        [Retry]
        public async Task ShouldRespectDefaultNavigationTimeout()
        {
            Page.DefaultNavigationTimeout = 1;
            string imgPath = "/img.png";
            // stall for image
            Server.SetRoute(imgPath, context => Task.Delay(Timeout.Infinite));
            await Assert.ThrowsAsync<TimeoutException>(() =>
                Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\"></img>", new NavigationOptions { Timeout = 1 })
            );
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should await resources to load</playwright-it>
        [Retry]
        public async Task ShouldAwaitResourcesToLoad()
        {
            string imgPath = "/img.png";
            var imgResponse = new TaskCompletionSource<bool>();
            Server.SetRoute(imgPath, context => imgResponse.Task);
            bool loaded = false;
            var contentTask = Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\"></img>").ContinueWith(_ => loaded = true);
            await Server.WaitForRequest(imgPath);
            Assert.False(loaded);
            imgResponse.SetResult(true);
            await contentTask;
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work fast enough</playwright-it>
        [Retry]
        public async Task ShouldWorkFastEnough()
        {
            for (int i = 0; i < 20; ++i)
            {
                await Page.SetContentAsync("<div>yo</div>");
            }
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with tricky content</playwright-it>
        [Retry]
        public async Task ShouldWorkWithTrickyContent()
        {
            await Page.SetContentAsync("<div>hello world</div>" + "\x7F");
            Assert.Equal("hello world", await Page.QuerySelectorEvaluateAsync<string>("div", "div => div.textContent"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with accents</playwright-it>
        [Retry]
        public async Task ShouldWorkWithAccents()
        {
            await Page.SetContentAsync("<div>aberración</div>");
            Assert.Equal("aberración", await Page.QuerySelectorEvaluateAsync<string>("div", "div => div.textContent"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with emojis</playwright-it>
        [Retry]
        public async Task ShouldWorkWithEmojis()
        {
            await Page.SetContentAsync("<div>🐥</div>");
            Assert.Equal("🐥", await Page.QuerySelectorEvaluateAsync<string>("div", "div => div.textContent"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.setContent</playwright-describe>
        ///<playwright-it>should work with newline</playwright-it>
        [Retry]
        public async Task ShouldWorkWithNewline()
        {
            await Page.SetContentAsync("<div>\n</div>");
            Assert.Equal("\n", await Page.QuerySelectorEvaluateAsync<string>("div", "div => div.textContent"));
        }
    }
}
