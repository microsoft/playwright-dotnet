using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAddStyleTagTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAddStyleTagTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw an error if no options are provided")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with a url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = "/injectedstyle.css" });
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw an error if loading from url fail")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Assert.ThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = "/nonexistfile.js" }));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with a path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = TestUtils.GetWebServerFile("injectedstyle.css") });
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should include sourceURL when path is provided")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Path = TestUtils.GetWebServerFile("injectedstyle.css") });
            var styleHandle = await Page.QuerySelectorAsync("style");
            string styleContent = await Page.EvaluateAsync<string>("style => style.innerHTML", styleHandle);
            Assert.Contains(TestUtils.GetWebServerFile("injectedstyle.css"), styleContent);
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with content")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "body { background-color: green; }" });
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(0, 128, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw when added with content to the CSP page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "body { background-color: green; }" }));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw when added with URL to the CSP page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new PageAddStyleTagOptions { Url = TestConstants.CrossProcessUrl + "/injectedstyle.css" }));
        }
    }
}
