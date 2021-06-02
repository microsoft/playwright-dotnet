using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageSetContentTests : PageTestEx
    {
        const string _expectedOutput = "<html><head></head><body><div>hello</div></body></html>";

        /// <inheritdoc />
        [PlaywrightTest("page-set-content.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<div>hello</div>");
            string result = await Page.ContentAsync();
            Assert.AreEqual(_expectedOutput, result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with domcontentloaded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDomcontentloaded()
        {
            await Page.SetContentAsync("<div>hello</div>", new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            string result = await Page.ContentAsync();
            Assert.AreEqual(_expectedOutput, result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with doctype")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDoctype()
        {
            string doctype = "<!DOCTYPE html>";
            await Page.SetContentAsync($"{doctype}<div>hello</div>");
            string result = await Page.ContentAsync();
            Assert.AreEqual($"{doctype}{_expectedOutput}", result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with HTML 4 doctype")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHTML4Doctype()
        {
            string doctype = "<!DOCTYPE html PUBLIC \" -//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">";
            await Page.SetContentAsync($"{doctype}<div>hello</div>");
            string result = await Page.ContentAsync();
            Assert.AreEqual($"{doctype}{_expectedOutput}", result);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            string imgPath = "/img.png";
            // stall for image
            Server.SetRoute(imgPath, _ => Task.Delay(Timeout.Infinite));
            await AssertThrowsAsync<TimeoutException>(() =>
                Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>", new PageSetContentOptions { Timeout = 1 })
            );
        }

        [PlaywrightTest("page-set-content.spec.ts", "should respect default navigation timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultNavigationTimeout()
        {
            Page.SetDefaultNavigationTimeout(1);
            string imgPath = "/img.png";
            // stall for image
            Server.SetRoute(imgPath, _ => Task.Delay(Timeout.Infinite));
            var exception = await AssertThrowsAsync<TimeoutException>(() =>
                Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>", new PageSetContentOptions { Timeout = 1 })
            );

            StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        [PlaywrightTest("page-set-content.spec.ts", "should await resources to load")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAwaitResourcesToLoad()
        {
            string imgPath = "/img.png";
            var imgResponse = new TaskCompletionSource<bool>();
            Server.SetRoute(imgPath, _ => imgResponse.Task);
            bool loaded = false;
            var contentTask = Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>").ContinueWith(_ => loaded = true);
            await Server.WaitForRequest(imgPath);
            Assert.False(loaded);
            imgResponse.SetResult(true);
            await contentTask;
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work fast enough")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkFastEnough()
        {
            for (int i = 0; i < 20; ++i)
            {
                await Page.SetContentAsync("<div>yo</div>");
            }
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with tricky content")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithTrickyContent()
        {
            await Page.SetContentAsync("<div>hello world</div>" + "\x7F");
            Assert.AreEqual("hello world", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with accents")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAccents()
        {
            await Page.SetContentAsync("<div>aberración</div>");
            Assert.AreEqual("aberración", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with emojis")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEmojis()
        {
            await Page.SetContentAsync("<div>🐥</div>");
            Assert.AreEqual("🐥", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }

        [PlaywrightTest("page-set-content.spec.ts", "should work with newline")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNewline()
        {
            await Page.SetContentAsync("<div>\n</div>");
            Assert.AreEqual("\n", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
        }
    }
}
