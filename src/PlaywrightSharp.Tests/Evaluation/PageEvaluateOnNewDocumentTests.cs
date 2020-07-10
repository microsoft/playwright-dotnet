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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageEvaluateOnNewDocumentTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEvaluateOnNewDocumentTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should evaluate before anything else on the page</playwright-it>
        [Retry]
        public async Task ShouldEvaluateBeforeAnythingElseOnThePage()
        {
            await Page.EvaluateOnNewDocumentAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should support multiple scripts</playwright-it>
        [Retry]
        public async Task ShouldSupportMultipleScripts()
        {
            await Page.EvaluateOnNewDocumentAsync(@"function(){
                window.script1 = 1;
            }");
            await Page.EvaluateOnNewDocumentAsync(@"function(){
                window.script2 = 2;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(1, await Page.EvaluateAsync<int>("() => window.script1"));
            Assert.Equal(2, await Page.EvaluateAsync<int>("() => window.script2"));
        }

        ///<playwright-file>evaluation.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateOnNewDocument</playwright-describe>
        ///<playwright-it>should work with CSP</playwright-it>
        [Retry]
        public async Task ShouldWorkWithCSP()
        {
            Server.SetCSP("/empty.html", "script-src " + TestConstants.ServerUrl);
            await Page.EvaluateOnNewDocumentAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.injected"));

            // Make sure CSP works.
            try
            {
                await Page.AddScriptTagAsync(new AddTagOptions { Content = "window.e = 10;" });
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
        [Retry]
        public async Task ShouldWorkAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.CrossProcessUrl);
            await Page.EvaluateOnNewDocumentAsync(@"function(){
                window.injected = 123;
            }");
            await Page.GoToAsync(TestConstants.ServerUrl + "/tamperable.html");
            Assert.Equal(123, await Page.EvaluateAsync<int>("() => window.result"));
        }
    }
}
