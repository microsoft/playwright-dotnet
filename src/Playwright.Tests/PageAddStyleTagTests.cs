using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageAddStyleTagTests : PageTestEx
    {
        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw an error if no options are provided")]
        [Test, Ignore("Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with a url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = "/injectedstyle.css" });
            Assert.NotNull(styleHandle);
            Assert.AreEqual("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw an error if loading from url fail")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await AssertThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = "/nonexistfile.js" }));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with a path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = TestUtils.GetWebServerFile("injectedstyle.css") });
            Assert.NotNull(styleHandle);
            Assert.AreEqual("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should include sourceURL when path is provided")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = TestUtils.GetWebServerFile("injectedstyle.css") });
            var styleHandle = await Page.QuerySelectorAsync("style");
            string styleContent = await Page.EvaluateAsync<string>("style => style.innerHTML", styleHandle);
            StringAssert.Contains(TestUtils.GetWebServerFile("injectedstyle.css"), styleContent);
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with content")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "body { background-color: green; }" });
            Assert.NotNull(styleHandle);
            Assert.AreEqual("rgb(0, 128, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw when added with content to the CSP page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GotoAsync(Server.Prefix + "/csp.html");
            await AssertThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "body { background-color: green; }" }));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw when added with URL to the CSP page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GotoAsync(Server.Prefix + "/csp.html");
            await AssertThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = Server.CrossProcessPrefix + "/injectedstyle.css" }));
        }
    }
}
