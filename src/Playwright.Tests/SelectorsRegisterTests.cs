using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>selectors-register.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class SelectorsRegisterTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public SelectorsRegisterTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("selectors-register.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            const string createTagSelector = @"({
                create(root, target) {
                  return target.nodeName;
                },
                query(root, selector) {
                  return root.querySelector(selector);
                },
                queryAll(root, selector) {
                  return Array.from(root.querySelectorAll(selector));
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "tag", createTagSelector);
            var context = await Browser.NewContextAsync();
            await TestUtils.RegisterEngineAsync(Playwright, "tag2", createTagSelector);
            var page = await context.NewPageAsync();
            await page.SetContentAsync("<div><span></span></div><div></div>");

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.QuerySelectorAsync("tAG=DIV"));
            Assert.Contains("Unknown engine \"tAG\" while parsing selector tAG=DIV", exception.Message);
        }

        [PlaywrightTest("selectors-register.spec.ts", "should work with path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPath()
        {
            await TestUtils.RegisterEngineWithPathAsync(Playwright, "foo", TestUtils.GetWebServerFile("sectionselectorengine.js"));
            await Page.SetContentAsync("<section></section>");
            Assert.Equal("SECTION", await Page.EvalOnSelectorAsync<string>("foo=whatever", "e => e.nodeName"));
        }

        [PlaywrightTest("selectors-register.spec.ts", "should work in main and isolated world")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkInMainAndIsolatedWorld()
        {
            const string createTagSelector = @"({
                create(root, target) { },
                query(root, selector) {
                  return window['__answer'];
                },
                queryAll(root, selector) {
                  return window['__answer'] ? [window['__answer'], document.body, document.documentElement] : [];
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "main", createTagSelector);
            await TestUtils.RegisterEngineAsync(Playwright, "isolated", createTagSelector, true);
            await Page.SetContentAsync("<div><span><section></section></span></div>");
            await Page.EvaluateAsync("() => window['__answer'] = document.querySelector('span')");

            Assert.Equal("SPAN", await Page.EvalOnSelectorAsync<string>("main=ignored", "e => e.nodeName"));
            Assert.Equal("SPAN", await Page.EvalOnSelectorAsync<string>("css=div >> main=ignored", "e => e.nodeName"));
            Assert.True(await Page.EvalOnSelectorAllAsync<bool>("main=ignored", "es => window['__answer'] !== undefined"));
            Assert.Equal(3, await Page.EvalOnSelectorAllAsync<int>("main=ignored", "es => es.filter(e => e).length"));

            Assert.Null(await Page.QuerySelectorAsync("isolated=ignored"));
            Assert.Null(await Page.QuerySelectorAsync("css=div >> isolated=ignored"));
            Assert.True(await Page.EvalOnSelectorAllAsync<bool>("isolated=ignored", "es => window['__answer'] !== undefined"));
            Assert.Equal(3, await Page.EvalOnSelectorAllAsync<int>("isolated=ignored", "es => es.filter(e => e).length"));

            Assert.Equal("SPAN", await Page.EvalOnSelectorAsync<string>("main=ignored >> isolated=ignored", "e => e.nodeName"));
            Assert.Equal("SPAN", await Page.EvalOnSelectorAsync<string>("isolated=ignored >> main=ignored", "e => e.nodeName"));

            Assert.Equal("SECTION", await Page.EvalOnSelectorAsync<string>("main=ignored >> css=section", "e => e.nodeName"));
        }

        [PlaywrightTest("selectors-register.spec.ts", "should handle errors")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHandleErrors()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.QuerySelectorAsync("neverregister=ignored"));
            Assert.Contains("Unknown engine \"neverregister\" while parsing selector neverregister=ignored", exception.Message);

            const string createDummySelector = @"({
                create(root, target) {
                    return target.nodeName;
                },
                query(root, selector) {
                    return root.querySelector('dummy');
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll('dummy'));
                }
            })";

            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Playwright.Selectors.RegisterAsync("$", createDummySelector));
            Assert.Contains("Selector engine name may only contain [a-zA-Z0-9_] characters", exception.Message);

            await TestUtils.RegisterEngineAsync(Playwright, "dummy", createDummySelector);
            await TestUtils.RegisterEngineAsync(Playwright, "duMMy", createDummySelector);

            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Playwright.Selectors.RegisterAsync("dummy", createDummySelector));
            Assert.Contains("\"dummy\" selector engine has been already registered", exception.Message);

            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Playwright.Selectors.RegisterAsync("css", createDummySelector));
            Assert.Contains("\"css\" is a predefined selector engine", exception.Message);
        }
    }
}
