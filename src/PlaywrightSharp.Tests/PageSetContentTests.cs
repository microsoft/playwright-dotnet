using System;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetContentTests : PlaywrightSharpPageBaseTest
    {
        const string _expectedOutput = "<html><head></head><body><div>hello</div></body></html>";

        /// <inheritdoc />
        public PageSetContentTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<div>hello</div>");
            string result = await Page.ContentAsync();
            Assert.Equal(_expectedOutput, result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with domcontentloaded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDomcontentloaded()
        {
            await Page.SetContentAsync("<div>hello</div>", waitUntil: WaitUntilState.DOMContentLoaded);
            string result = await Page.ContentAsync();
            Assert.Equal(_expectedOutput, result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with doctype")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDoctype()
        {
            string doctype = "<!DOCTYPE html>";
            await Page.SetContentAsync($"{doctype}<div>hello</div>");
            string result = await Page.ContentAsync();
            Assert.Equal($"{doctype}{_expectedOutput}", result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with HTML 4 doctype")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHTML4Doctype()
        {
            string doctype = "<!DOCTYPE html PUBLIC \" -//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">";
            await Page.SetContentAsync($"{doctype}<div>hello</div>");
            string result = await Page.ContentAsync();
            Assert.Equal($"{doctype}{_expectedOutput}", result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            string imgPath = "/img.png";
            // stall for image
            Server.SetRoute(imgPath, _ => Task.Delay(Timeout.Infinite));
            await Assert.ThrowsAsync<TimeoutException>(() =>
                Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\"></img>", timeout: 1)
            );
        }

        [PlaywrightTest("page-set-content.spec.ts", "should respect default navigation timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultNavigationTimeout()
        {
            Page.DefaultNavigationTimeout = 1;
            string imgPath = "/img.png";
            // stall for image
            Server.SetRoute(imgPath, _ => Task.Delay(Timeout.Infinite));
            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\"></img>", timeout: 1)
            );

            Assert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should await resources to load")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAwaitResourcesToLoad()
        {
            string imgPath = "/img.png";
            var imgResponse = new TaskCompletionSource<bool>();
            Server.SetRoute(imgPath, _ => imgResponse.Task);
            bool loaded = false;
            var contentTask = Page.SetContentAsync($"<img src=\"{TestConstants.ServerUrl + imgPath}\"></img>").ContinueWith(_ => loaded = true);
            await Server.WaitForRequest(imgPath);
            Assert.False(loaded);
            imgResponse.SetResult(true);
            await contentTask;
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work fast enough")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkFastEnough()
        {
            for (int i = 0; i < 20; ++i)
            {
                await Page.SetContentAsync("<div>yo</div>");
            }
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with tricky content")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithTrickyContent()
        {
            await Page.SetContentAsync("<div>hello world</div>" + "\x7F");
            Assert.Equal("hello world", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with accents")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAccents()
        {
            await Page.SetContentAsync("<div>aberración</div>");
            Assert.Equal("aberración", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with emojis")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEmojis()
        {
            await Page.SetContentAsync("<div>🐥</div>");
            Assert.Equal("🐥", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with newline")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNewline()
        {
            await Page.SetContentAsync("<div>\n</div>");
            Assert.Equal("\n", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }
    }
}
