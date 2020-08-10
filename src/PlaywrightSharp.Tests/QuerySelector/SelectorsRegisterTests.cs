using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    /*
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>selectors.register</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class SelectorsRegisterTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public SelectorsRegisterTests(ITestOutputHelper output) : base(output)
        {
        }

        internal ISelectors Selectors { get; set; } = PlaywrightSharp.Selectors.Instance.Value;

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            string createTagSelector = @"() => ({
                name: 'tag',
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
            await Selectors.RegisterAsync($"({createTagSelector})()");
            await Page.SetContentAsync("<div><span></span></div><div></div>");
            Assert.Equal("DIV", await Selectors.CreateSelectorAsync("tag", await Page.QuerySelectorAsync("div")));
            Assert.Equal("DIV", await Page.QuerySelectorEvaluateAsync<string>("tag=DIV", "e => e.nodeName"));
            Assert.Equal("SPAN", await Page.QuerySelectorEvaluateAsync<string>("tag=SPAN", "e => e.nodeName"));
            Assert.Equal(2, await Page.QuerySelectorAllEvaluateAsync<int>("tag=DIV", "es => es.length"));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should update</playwright-it>
        [Fact]
        public async Task ShouldUpdate()
        {
            await Page.SetContentAsync("<div><dummy id=d1></dummy></div><span><dummy id=d2></dummy></span>");
            Assert.Equal("DIV", await Page.QuerySelectorEvaluateAsync<string>("div", "e => e.nodeName"));
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.QuerySelectorAsync("dummy=foo"));
            Assert.Contains("Unknown engine dummy while parsing selector dummy=foo", exception.Message);
            string createDummySelector = @"(name) => ({
                name,
                create(root, target) {
                    return target.nodeName;
                },
                query(root, selector) {
                    return root.querySelector(name);
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll(name));
                }
            })";
            await Selectors.RegisterAsync(createDummySelector, "dummy");
            Assert.Equal("d1", await Page.QuerySelectorEvaluateAsync<string>("dummy=foo", "e => e.id"));
            Assert.Equal("d2", await Page.QuerySelectorEvaluateAsync<string>("css=span >> dummy=foo", "e => e.id"));
        }
    }
    */
}
