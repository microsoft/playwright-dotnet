using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Evaluation
{
    ///<playwright-file>evaluation.spec.js</playwright-file>
    ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAddInitScriptTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAddInitScriptTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should evaluate before anything else on the page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEvaluateBeforeAnythingElseOnThePage()
        {
            await Page.AddInitScriptAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with a path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.AddInitScriptAsync(path: TestUtils.GetWebServerFile("injectedfile.js"));

            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with a path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithContents()
        {
            await Page.AddInitScriptAsync(content: @"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should throw without path and content</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldThrowWithoutPathAndContent()
            => Assert.ThrowsAnyAsync<ArgumentException>(() => Page.AddInitScriptAsync());

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with browser context scripts</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithBrowserContextScripts()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync(@"function(){
                window.temp = 123;
            }");

            var page = await context.NewPageAsync();
            await page.AddInitScriptAsync(content: @"function(){
                window.injected = window.temp;
            }");
            await page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with browser context scripts with path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithBrowserContextScriptsWithPath()
        {
            await using var context = await Browser.NewContextAsync();
            await context.AddInitScriptAsync(path: TestUtils.GetWebServerFile("injectedfile.js"));

            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with browser context scripts for already created pages</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithBrowserContextScriptsForAlreadyCreatedPages()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await context.AddInitScriptAsync(@"function(){
                window.temp = 123;
            }");

            await page.AddInitScriptAsync(content: @"function(){
                window.injected = window.temp;
            }");

            await page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should support multiple scripts</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with CSP</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work after a cross origin navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
