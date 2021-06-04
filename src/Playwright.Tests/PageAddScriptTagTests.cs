using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageAddScriptTagTests : PageTestEx
    {
        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw an error if no options are provided")]
        [Test, Ignore("Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var scriptHandle = await Page.AddScriptTagAsync(new() { Url = "/injectedfile.js" });
            Assert.NotNull(scriptHandle);
            Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a url and type=module")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrlAndTypeModule()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.AddScriptTagAsync(new() { Url = "/es6/es6import.js", Type = "module" });
            Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __es6injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a path and type=module")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPathAndTypeModule()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.AddScriptTagAsync(new() { Path = TestUtils.GetWebServerFile("es6/es6pathimport.js"), Type = "module" });
            await Page.WaitForFunctionAsync("window.__es6injected");
            Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __es6injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a content and type=module")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAContentAndTypeModule()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.AddScriptTagAsync(new() { Content = "import num from '/es6/es6module.js'; window.__es6injected = num;", Type = "module" });
            await Page.WaitForFunctionAsync("window.__es6injected");
            Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __es6injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw an error if loading from url fail")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await AssertThrowsAsync<PlaywrightException>(() => Page.AddScriptTagAsync(new() { Url = "/nonexistfile.js" }));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var scriptHandle = await Page.AddScriptTagAsync(new() { Path = TestUtils.GetWebServerFile("injectedfile.js") });
            Assert.NotNull(scriptHandle);
            Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should include sourceURL when path is provided")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.AddScriptTagAsync(new() { Path = TestUtils.GetWebServerFile("injectedfile.js") });
            string result = await Page.EvaluateAsync<string>("() => __injectedError.stack");
            StringAssert.Contains(TestUtils.GetWebServerFile("injectedfile.js"), result);
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should work with content")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var scriptHandle = await Page.AddScriptTagAsync(new() { Content = "window.__injected = 35;" });
            Assert.NotNull(scriptHandle);
            Assert.AreEqual(35, await Page.EvaluateAsync<int>("() => __injected"));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw when added with content to the CSP page")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GotoAsync(Server.Prefix + "/csp.html");
            await AssertThrowsAsync<PlaywrightException>(() =>
                Page.AddScriptTagAsync(new() { Content = "window.__injected = 35;" }));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw when added with URL to the CSP page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GotoAsync(Server.Prefix + "/csp.html");
            await AssertThrowsAsync<PlaywrightException>(() =>
                Page.AddScriptTagAsync(new() { Url = Server.CrossProcessPrefix + "/injectedfile.js" }));
        }

        [PlaywrightTest("page-add-script-tag.spec.ts", "should throw a nice error when the request fails")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowANiceErrorWhenTheEequestFails()
        {
            await Page.GotoAsync(Server.EmptyPage);
            string url = Server.Prefix + "/this_does_not_exists.js";
            var exception = await AssertThrowsAsync<PlaywrightException>(() => Page.AddScriptTagAsync(new() { Url = url }));
            StringAssert.Contains(url, exception.Message);
        }
    }
}
