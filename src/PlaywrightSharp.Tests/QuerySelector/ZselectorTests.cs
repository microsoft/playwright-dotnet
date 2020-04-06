using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>zselector</playwright-describe>
    public class ZselectorTests : PlaywrightSharpPageBaseTest
    {
        internal ZselectorTests(ITestOutputHelper output) : base(output)
        {
        }

        internal ISelectors Selectors { get; set; }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>zselector</playwright-describe>
        ///<playwright-it>query</playwright-it>
        [Fact]
        public async Task Query()
        {
            await Page.SetContentAsync("<div>yo</div><div>ya</div><div>ye</div>");
            Assert.Equal("<div>ya</div>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"ya\"", "e => e.outerHTML"));

            await Page.SetContentAsync("<div foo=\"baz\"></div><div foo=\"bar space\"></div>");
            Assert.Equal("<div foo=\"bar space\"></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=[foo=\"bar space\"]", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo<span></span></div>");
            Assert.Equal("<span></span>", await Page.QuerySelectorEvaluateAsync<string>("zs=span", "e => e.outerHTML"));
            Assert.Equal("<span></span>", await Page.QuerySelectorEvaluateAsync<string>("zs=div > span", "e => e.outerHTML"));
            Assert.Equal("<span></span>", await Page.QuerySelectorEvaluateAsync<string>("zs=div span", "e => e.outerHTML"));
            Assert.Equal("<span></span>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"yo\" > span", "e => e.outerHTML"));
            Assert.Equal("<span></span>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"yo\" span", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ^", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ~div", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ~ \"yo\"", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo</div><div>yo<span></span></div>");
            Assert.Equal("<div>yo</div>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"yo\"#0", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"yo\"#1", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"yo\" ~DIV#1", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ~div#1", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ~div#0", "e => e.outerHTML"));
            Assert.Equal("<div>yo</div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ~ \"yo\"#1 ^ > div", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=span ~ \"yo\"#1 ^ > div#1", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo<span id=\"s1\"></span></div><div>yo<span id=\"s2\"></span><span id = \"s3\" ></ span ></ div >");
            Assert.Equal("<div>yo<span id=\"s1\"></span></div>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"yo\"", "e => e.outerHTML"));
            Assert.Equal("<div>yo<span id=\"s1\"></span></div>\n<div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"yo\"", "es => es.map(\"e => e.outerHTML\").join('\n')"));
            Assert.Equal("<div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"yo\"#1", "es => es.map(\"e => e.outerHTML\").join('\n')"));
            Assert.Equal("<span id=\"s1\"></span>\n<span id=\"s2\"></span>\n<span id=\"s3\"></span>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"yo\" ~span", "es => es.map(\"e => e.outerHTML\").join('\n')"));
            Assert.Equal("<span id=\"s2\"></span>\n<span id=\"s3\"></span>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"yo\"#1 ~ span", "es => es.map(\"e => e.outerHTML\").join('\n')"));
            Assert.Equal("<span id=\"s1\"></span>\n<span id=\"s2\"></span>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"yo\" ~span#0", "es => es.map(\"e => e.outerHTML\").join('\n')"));
            Assert.Equal("<span id=\"s2\"></span>\n<span id=\"s3\"></span>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"yo\" ~span#1", "es => es.map(\"e => e.outerHTML\").join('\n')"));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>zselector</playwright-describe>
        ///<playwright-it>create</playwright-it>
        [Fact]
        public async Task Create()
        {
            await Page.SetContentAsync("<div>yo</div><div>ya</div><div>ya</div>");
            Assert.Equal("\"yo\"", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("div")));
            Assert.Equal("\"ya\"", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("div:nth-child(2)")));
            Assert.Equal("\"ya\"#1", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("div:nth-child(3)")));

            await Page.SetContentAsync("<img alt=\"foo bar\">");
            Assert.Equal("img[alt=\"foo bar\"]", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("img")));

            await Page.SetContentAsync("<div>yo<span></span></div><span></span>");
            Assert.Equal("\"yo\"~SPAN", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("span")));
            Assert.Equal("SPAN#1", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("span:nth-child(2)")));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>zselector</playwright-describe>
        ///<playwright-it>children of various display parents</playwright-it>
        [Fact]
        public async Task ChildrenOfVariousDisplayParents()
        {
            await Page.SetContentAsync("<body><div style='position: fixed;'><span>yo</span></div></body>");
            Assert.Equal("\"yo\"", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("span")));

            await Page.SetContentAsync("<div style='position: relative;'><span>yo</span></div>");
            Assert.Equal("\"yo\"", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("span")));

            // "display: none" makes all children text invisible - fallback to tag name.
            await Page.SetContentAsync("<div style='display: none;'><span>yo</span></div>");
            Assert.Equal("SPAN", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("span")));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>zselector</playwright-describe>
        ///<playwright-it>boundary</playwright-it>
        [Fact]
        public async Task Boundary()
        {
            await Page.SetContentAsync(@"
        <div>hey</div>
        <div>hey</div>
        <div>hey</div>
        <div>
          <div>yo</div>
          <div>hello</div>
          <div>hello</div>
          <div>hello</div>
          <div>unique</div>
          <div>
            <div>hey2<span></span><span></span><span></span></div>
            <div>hello</div>
          </div>
          <div>
            <div>hey<span></span><span></span><span></span></div>
            <div>hello</div>
          </div>
        </div>
        <div>
          <div>ya<div>
          <div id=first>hello</div>
          <div>hello</div>
          <div>hello</div>
          <div>
            <div>hey2<span></span><span></span><span></span></div>
            <div>hello</div>
          </div>
          <div>
            <div>hey<span></span><span></span><span></span></div>
            <div id=target>hello</div>
          </div>
        </div>
        <div>
          <div>ya<div>
          <div id=first2>hello</div>
          <div>hello</div>
          <div>hello</div>
          <div>
            <div>hey2<span></span><span></span><span></span></div>
            <div>hello</div>
          </div>
          <div>
            <div>hey<span></span><span></span><span></span></div>
            <div id=target2>hello</div>
          </div>
        </div>");
            Assert.Equal("\"ya\"~\"hey\"~\"hello\"", await Selectors.CreateSelectorAsync("zs", await Page.QuerySelectorAsync("#target")));
            Assert.Equal("<div id=\"target\">hello</div>", await Page.QuerySelectorEvaluateAsync<string>("zs=\"ya\"~\"hey\"~\"hello\"", "e => e.outerHTML")); ;
            Assert.Equal("Error: failed to find element matching selector \"zs=\"ya\"~\"hey\"~\"unique\"\"", (await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.QuerySelectorEvaluateAsync("zs=\"ya\"~\"hey\"~\"unique\"", "e => e.outerHTML"))).Message);
            Assert.Equal("<div id=\"target\">hello</div>\n<div id=\"target2\">hello</div>", await Page.QuerySelectorAllEvaluateAsync<string>("zs=\"ya\" ~\"hey\" ~\"hello\"", "es => es.map(e => e.outerHTML).join('\n')"));
        }
    }
}
