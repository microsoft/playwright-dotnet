using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAddScriptTagTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAddScriptTagTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw an error if no options are provided")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var scriptHandle = await Page.AddScriptTagAsync(url: "/injectedfile.js");
            Assert.NotNull(scriptHandle);
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => __injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a url and type=module")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrlAndTypeModule()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.AddScriptTagAsync(url: "/es6/es6import.js", type: "module");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => __es6injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a path and type=module")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPathAndTypeModule()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.AddScriptTagAsync(path: TestUtils.GetWebServerFile("es6/es6pathimport.js"), type: "module");
            await Page.WaitForFunctionAsync("window.__es6injected");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => __es6injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a content and type=module")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAContentAndTypeModule()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.AddScriptTagAsync(content: "import num from '/es6/es6module.js'; window.__es6injected = num;", type: "module");
            await Page.WaitForFunctionAsync("window.__es6injected");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => __es6injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw an error if loading from url fail")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.AddScriptTagAsync(url: "/nonexistfile.js"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var scriptHandle = await Page.AddScriptTagAsync(path: TestUtils.GetWebServerFile("injectedfile.js"));
            Assert.NotNull(scriptHandle);
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => __injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should include sourceURL when path is provided")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.AddScriptTagAsync(path: TestUtils.GetWebServerFile("injectedfile.js"));
            string result = await Page.EvaluateAsync<string>("() => __injectedError.stack");
            Assert.Contains(TestUtils.GetWebServerFile("injectedfile.js"), result);
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with content")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var scriptHandle = await Page.AddScriptTagAsync(content: "window.__injected = 35;");
            Assert.NotNull(scriptHandle);
            Assert.Equal(35, await Page.EvaluateAsync<int>("() => __injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw when added with content to the CSP page")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddScriptTagAsync(content: "window.__injected = 35;"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw when added with URL to the CSP page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/csp.html");
            await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.AddScriptTagAsync(url: TestConstants.CrossProcessUrl + "/injectedfile.js"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw a nice error when the request fails")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowANiceErrorWhenTheEequestFails()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            string url = TestConstants.ServerUrl + "/this_does_not_exists.js";
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.AddScriptTagAsync(url));
            Assert.Contains(url, exception.Message);
        }
    }
}
