using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.addStyleTag</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAddStyleTagTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAddStyleTagTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should throw an error if no options are provided")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should work with a url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(url: "/injectedstyle.css");
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should throw an error if loading from url fail")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddStyleTagAsync(url: "/nonexistfile.js"));
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should work with a path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(path: TestUtils.GetWebServerFile("injectedstyle.css"));
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should include sourceURL when path is provided")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.AddStyleTagAsync(path: TestUtils.GetWebServerFile("injectedstyle.css"));
            var styleHandle = await Page.QuerySelectorAsync("style");
            string styleContent = await Page.EvaluateAsync<string>("style => style.innerHTML", styleHandle);
            Assert.Contains(TestUtils.GetWebServerFile("injectedstyle.css"), styleContent);
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should work with content")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(content: "body { background-color: green; }");
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(0, 128, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should throw when added with content to the CSP page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddStyleTagAsync(content: "body { background-color: green; }"));
        }

        [PlaywrightTest("page.spec.js", "Page.addStyleTag", "should throw when added with URL to the CSP page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddStyleTagAsync(url: TestConstants.CrossProcessUrl + "/injectedstyle.css"));
        }
    }
}
