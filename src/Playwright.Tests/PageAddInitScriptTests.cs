using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>evaluation.spec.ts</playwright-file>
    ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAddInitScriptTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAddInitScriptTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should evaluate before anything else on the page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEvaluateBeforeAnythingElseOnThePage()
        {
            await Page.AddInitScriptAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with a path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.AddInitScriptAsync(scriptPath: TestUtils.GetWebServerFile("injectedfile.js"));

            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with a path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContents()
        {
            await Page.AddInitScriptAsync(script: @"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should throw without path and content")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldThrowWithoutPathAndContent()
            => Assert.ThrowsAnyAsync<ArgumentException>(() => Page.AddInitScriptAsync());

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBrowserContextScripts()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync(@"function(){
                window.temp = 123;
            }");

            var page = await context.NewPageAsync();
            await page.AddInitScriptAsync(script: @"function(){
                window.injected = window.temp;
            }");
            await page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts with path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBrowserContextScriptsWithPath()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync(scriptPath: TestUtils.GetWebServerFile("injectedfile.js"));

            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts for already created pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBrowserContextScriptsForAlreadyCreatedPages()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await context.AddInitScriptAsync(@"function(){
                window.temp = 123;
            }");

            await page.AddInitScriptAsync(script: @"function(){
                window.injected = window.temp;
            }");

            await page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should support multiple scripts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportMultipleScripts()
        {
            await Page.AddInitScriptAsync(@"function(){
                window.script1 = 1;
            }");
            await Page.AddInitScriptAsync(@"function(){
                window.script2 = 2;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(1, await Page.EvaluateAsync<int>("() => window.script1"));
            Assert.Equal(2, await Page.EvaluateAsync<int>("() => window.script2"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work with CSP")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "script-src " + TestConstants.ServerUrl);
            await Page.AddInitScriptAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.injected"));

            // Make sure CSP works.
            try
            {
                await Page.AddScriptTagAsync(content: "window.e = 10;");
            }
            catch
            {
                //Silent exception
            }

            Assert.Null(await Page.EvaluateAsync("() => window.e"));
        }

        [PlaywrightTest("page-add-init-script.spec.ts", "should work after a cross origin navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.CrossProcessUrl);
            await Page.AddInitScriptAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }
    }
}
