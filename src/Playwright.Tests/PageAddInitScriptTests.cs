using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageAddInitScriptTests : PageTestEx
    {
        [PlaywrightTest("page-add-init-script.spec.ts", "should evaluate before anything else on the page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluateBeforeAnythingElseOnThePage()
        {
            await Page.AddInitScriptAsync("window.injected = 123;");
            await Page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with a path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.AddInitScriptAsync(scriptPath: TestUtils.GetWebServerFile("injectedfile.js"));

            await Page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with a path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContents()
        {
            await Page.AddInitScriptAsync("window.injected = 123;");
            await Page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should throw without path and content")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWithoutPathAndContent()
        {
            await AssertThrowsAsync<ArgumentException>(() => Page.AddInitScriptAsync());
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBrowserContextScripts()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync("window.temp = 123;");

            var page = await context.NewPageAsync();
            await page.AddInitScriptAsync("window.injected = window.temp;");
            await page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts with path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBrowserContextScriptsWithPath()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync(scriptPath: TestUtils.GetWebServerFile("injectedfile.js"));

            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts for already created pages")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBrowserContextScriptsForAlreadyCreatedPages()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await context.AddInitScriptAsync("window.temp = 123;");

            await page.AddInitScriptAsync(script: "window.injected = window.temp;");

            await page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should support multiple scripts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportMultipleScripts()
        {
            await Page.AddInitScriptAsync("window.script1 = 1;");
            await Page.AddInitScriptAsync("window.script2 = 2;");
            await Page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.script1"));
            Assert.AreEqual(2, await Page.EvaluateAsync<int>("() => window.script2"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with CSP")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "script-src " + Server.Prefix);
            await Page.AddInitScriptAsync("window.injected = 123;");
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.injected"));

            // Make sure CSP works.
            try
            {
                await Page.AddScriptTagAsync(new() { Content = "window.e = 10;" });
            }
            catch
            {
                //Silent exception
            }

            Assert.Null(await Page.EvaluateAsync("() => window.e"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work after a cross origin navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkAfterACrossOriginNavigation()
        {
            await Page.GotoAsync(Server.CrossProcessPrefix);
            await Page.AddInitScriptAsync("window.injected = 123;");
            await Page.GotoAsync(Server.Prefix + "/tamperable.html");
            Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
        }
    }
}
