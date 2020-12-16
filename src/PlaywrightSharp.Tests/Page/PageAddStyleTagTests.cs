using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
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

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should throw an error if no options are provided</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should work with a url</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(url: "/injectedstyle.css");
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should throw an error if loading from url fail</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddStyleTagAsync(url: "/nonexistfile.js"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should work with a path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(path: TestUtils.GetWebServerFile("injectedstyle.css"));
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should include sourceURL when path is provided</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.AddStyleTagAsync(path: TestUtils.GetWebServerFile("injectedstyle.css"));
            var styleHandle = await Page.QuerySelectorAsync("style");
            string styleContent = await Page.EvaluateAsync<string>("style => style.innerHTML", styleHandle);
            Assert.Contains(TestUtils.GetWebServerFile("injectedstyle.css"), styleContent);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should work with content</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(style: "body { background-color: green; }");
            Assert.NotNull(styleHandle);
            Assert.Equal("rgb(0, 128, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should throw when added with content to the CSP page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddStyleTagAsync(style: "body { background-color: green; }"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.addStyleTag</playwright-describe>
        ///<playwright-it>should throw when added with URL to the CSP page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddStyleTagAsync(url: TestConstants.CrossProcessUrl + "/injectedstyle.css"));
        }
    }
}
