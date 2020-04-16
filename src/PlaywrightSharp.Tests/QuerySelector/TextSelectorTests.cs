using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>text selector</playwright-describe>
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class TextSelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public TextSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        internal ISelectors Selectors { get; set; } = PlaywrightSharp.Selectors.Instance.Value;

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>text selector</playwright-describe>
        ///<playwright-it>query</playwright-it>
        [Fact]
        public async Task Query()
        {
            await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
            Assert.Equal("<div>ya</div>", await Page.QuerySelectorEvaluateAsync<string>("text=ya", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.QuerySelectorEvaluateAsync<string>("text=\"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.QuerySelectorEvaluateAsync<string>("text=/^[ay]+$/", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.QuerySelectorEvaluateAsync<string>("text=/Ya/i", "e => e.outerHTML"));
            Assert.Equal("<div>\nye  </div>", await Page.QuerySelectorEvaluateAsync<string>("text=ye", "e => e.outerHTML"));

            await Page.SetContentAsync("<div> ye </div><div>ye</div>");
            Assert.Equal("<div>ye</div>", await Page.QuerySelectorEvaluateAsync<string>("text=\"ye\"", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo</div><div>\"ya</div><div> hello world! </div>");
            Assert.Equal("<div>\"ya</div>", await Page.QuerySelectorEvaluateAsync<string>("text=\"\\\\\"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div> hello world! </div>", await Page.QuerySelectorEvaluateAsync<string>("text=/hello/", "e => e.outerHTML"));
            Assert.Equal("<div> hello world! </div>", await Page.QuerySelectorEvaluateAsync<string>("text=/^\\s*heLLo/i", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo<div>ya</div>hey<div>hey</div></div>");
            Assert.Equal("<div>yo<div>ya</div>hey<div>hey</div></div>", await Page.QuerySelectorEvaluateAsync<string>("text=hey", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo<span id=\"s1\"></span></div><div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>");
            Assert.Equal("<div>yo<span id=\"s1\"></span></div>\n<div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>", await Page.QuerySelectorAllEvaluateAsync<string>("text=yo", "es => es.map(\"e => e.outerHTML\").join('\n')"));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>text selector</playwright-describe>
        ///<playwright-it>create</playwright-it>
        [Fact]
        public async Task Create()
        {
            await Page.SetContentAsync("<div>yo</div><div>\"ya</div><div>ye ye</div>");
            Assert.Equal("yo", await Selectors.CreateSelectorAsync("text", await Page.QuerySelectorAsync("div")));
            Assert.Equal("\"\\\\\"ya\"", await Selectors.CreateSelectorAsync("text", await Page.QuerySelectorAsync("div:nth-child(2)")));
            Assert.Equal("\"ye ye\"", await Selectors.CreateSelectorAsync("text", await Page.QuerySelectorAsync("div:nth-child(3)")));

            await Page.SetContentAsync("<div>yo</div><div>yo<div>ya</div>hey</div>");
            Assert.Equal("hey", await Selectors.CreateSelectorAsync("text", await Page.QuerySelectorAsync("div:nth-child(2)")));

            await Page.SetContentAsync("<div> yo <div></div>ya</div>");
            Assert.Equal("yo", await Selectors.CreateSelectorAsync("text", await Page.QuerySelectorAsync("div")));

            await Page.SetContentAsync("<div> \"yo <div></div>ya</div>");
            Assert.Equal("\" \\\\\"yo \"", await Selectors.CreateSelectorAsync("text", await Page.QuerySelectorAsync("div")));
        }
    }
}
