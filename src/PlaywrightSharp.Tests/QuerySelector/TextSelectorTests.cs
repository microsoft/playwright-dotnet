using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>text selector</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class TextSelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public TextSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "query")]
        [Fact(Skip = "We need to update this test", Timeout = TestConstants.DefaultTestTimeout)]
        public async Task Query()
        {
            await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=ya", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=/^[ay]+$/", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=/Ya/i", "e => e.outerHTML"));
            Assert.Equal("<div>\nye  </div>", await Page.EvalOnSelectorAsync<string>("text=ye", "e => e.outerHTML"));

            await Page.SetContentAsync("<div> ye </div><div>ye</div>");
            Assert.Equal("<div> ye </div>", await Page.EvalOnSelectorAsync<string>("text=\"ye\"", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo</div><div>\"ya</div><div> hello world! </div>");
            Assert.Equal("<div>\"ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\\"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div> hello world! </div>", await Page.EvalOnSelectorAsync<string>("text=/hello/", "e => e.outerHTML"));
            Assert.Equal("<div> hello world! </div>", await Page.EvalOnSelectorAsync<string>("text=/^\\s*heLLo/i", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo<div>ya</div>hey<div>hey</div></div>");
            Assert.Equal("<div>yo<div>ya</div>hey<div>hey</div></div>", await Page.EvalOnSelectorAsync<string>("text=hey", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"yo\" >> text =\"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text='yo' >> text =\"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"yo\" >> text='ya'", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text='yo' >> text='ya'", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("'yo' >> \"ya\"", "e => e.outerHTML"));
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("\"yo\" >> 'ya'", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>yo<span id=\"s1\"></span></div><div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>");
            Assert.Equal("<div>yo<span id=\"s1\"></span></div>\n<div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>", await Page.EvalOnSelectorAllAsync<string>("text=yo", "es => es.map(e => e.outerHTML).join('\\n')"));

            await Page.SetContentAsync("<div>'</div><div>\"</div><div>\\</div><div>x</div>");
            Assert.Equal("<div>\'</div>", await Page.EvalOnSelectorAsync<string>("text='\\''", "e => e.outerHTML"));
            Assert.Equal("<div>\"</div>", await Page.EvalOnSelectorAsync<string>("text='\"'", "e => e.outerHTML"));
            Assert.Equal("<div>\"</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\\"\"", "e => e.outerHTML"));
            Assert.Equal("<div>\'</div>", await Page.EvalOnSelectorAsync<string>("text=\"'\"", "e => e.outerHTML"));
            Assert.Equal("<div>x</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\x\"", "e => e.outerHTML"));
            Assert.Equal("<div>x</div>", await Page.EvalOnSelectorAsync<string>("text='\\x'", "e => e.outerHTML"));
            Assert.Equal("<div>\\</div>", await Page.EvalOnSelectorAsync<string>("text='\\\\'", "e => e.outerHTML"));
            Assert.Equal("<div>\\</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\\\\"", "e => e.outerHTML"));
            Assert.Equal("<div>\"</div>", await Page.EvalOnSelectorAsync<string>("text=\"", "e => e.outerHTML"));
            Assert.Equal("<div>\'</div>", await Page.EvalOnSelectorAsync<string>("text='", "e => e.outerHTML"));
            Assert.Equal("<div>x</div>", await Page.EvalOnSelectorAsync<string>("\"x\"", "e => e.outerHTML"));
            Assert.Equal("<div>x</div>", await Page.EvalOnSelectorAsync<string>("'x'", "e => e.outerHTML"));

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.QuerySelectorAsync("\""));
            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.QuerySelectorAsync("'"));

            await Page.SetContentAsync("<div> ' </div><div> \" </div>");
            Assert.Equal("<div> \" </div>", await Page.EvalOnSelectorAsync<string>("text=\"", "e => e.outerHTML"));
            Assert.Equal("<div> \' </div>", await Page.EvalOnSelectorAsync<string>("text='", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>Hi''&gt;&gt;foo=bar</div>");
            Assert.Equal("<div>Hi''&gt;&gt;foo=bar</div>", await Page.EvalOnSelectorAsync<string>("text=\"Hi''>>foo=bar\"", "e => e.outerHTML"));
            await Page.SetContentAsync("<div>Hi'\"&gt;&gt;foo=bar</div> ");
            Assert.Equal("<div>Hi'\"&gt;&gt;foo=bar</div>", await Page.EvalOnSelectorAsync<string>("text=\"Hi'\\\">>foo=bar\"", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>Hi&gt;&gt;<span></span></div>");
            Assert.Equal("<span></span>", await Page.EvalOnSelectorAsync<string>("text=\"Hi>>\">>span", "e => e.outerHTML"));

            await Page.SetContentAsync("<div>a<br>b</div><div>a</div>");
            Assert.Equal("<div>a<br>b</div>", await Page.EvalOnSelectorAsync<string>("text=a", "e => e.outerHTML"));
            Assert.Equal("<div>a<br>b</div>", await Page.EvalOnSelectorAsync<string>("text=b", "e => e.outerHTML"));
            Assert.Null(await Page.QuerySelectorAsync("text=ab"));
            Assert.Equal(2, await Page.EvalOnSelectorAllAsync<int>("text=a", "els => els.length"));
            Assert.Equal(1, await Page.EvalOnSelectorAllAsync<int>("text=b", "els => els.length"));
            Assert.Equal(0, await Page.EvalOnSelectorAllAsync<int>("text=ab", "els => els.length"));

            await Page.SetContentAsync("<div></div><span></span>");
            await Page.EvalOnSelectorAsync("div", @"div =>
            {
                div.appendChild(document.createTextNode('hello'));
                div.appendChild(document.createTextNode('world'));
            }");

            await Page.EvalOnSelectorAsync("span", @"span =>
            {
                span.appendChild(document.createTextNode('hello'));
                span.appendChild(document.createTextNode('world'));
            }");
            Assert.Equal("<div>helloworld</div>", await Page.EvalOnSelectorAsync<string>("text=lowo", "e => e.outerHTML"));
            Assert.Equal("<div>helloworld</div><span>helloworld</span>", await Page.EvalOnSelectorAllAsync<string>("text=lowo", "els => els.map(e => e.outerHTML).join('')"));
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "create")]
        [Fact(Skip = "Skip Hooks")]
        public void Create()
        {
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should be case sensitive if quotes are specified")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCaseSensitiveIfQuotesAreSpecified()
        {
            await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
            Assert.Equal("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=ya", "e => e.outerHTML"));
            Assert.Null(await Page.QuerySelectorAsync("text=\"yA\""));
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should search for a substring without quotes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSearchForASubstringWithoutQuotes()
        {
            await Page.SetContentAsync("<div>textwithsubstring</div>");
            Assert.Equal("<div>textwithsubstring</div>", await Page.EvalOnSelectorAsync<string>("text=with", "e => e.outerHTML"));
            Assert.Null(await Page.QuerySelectorAsync("text=\"with\""));
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should skip head, script and style")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSkipHeadScriptAndStyle()
        {
            await Page.SetContentAsync(@"
                <head>
                    <title>title</title>
                    <script>var script</script>
                    <style>.style {}</style>
                </head>
                <body>
                    <script>var script</script>
                    <style>.style {}</style>
                    <div>title script style</div>
                </body>");

            var head = await Page.QuerySelectorAsync("head");
            var title = await Page.QuerySelectorAsync("title");
            var script = await Page.QuerySelectorAsync("body script");
            var style = await Page.QuerySelectorAsync("body style");

            foreach (string text in new[] { "title", "script", "style" })
            {
                Assert.Equal("DIV", await Page.EvalOnSelectorAsync<string>($"text={text}", "e => e.nodeName"));
                Assert.Equal("DIV", await Page.EvalOnSelectorAllAsync<string>($"text={text}", "els => els.map(e => e.nodeName).join('|')"));

                foreach (var root in new[] { head, title, script, style })
                {
                    Assert.Null(await root.QuerySelectorAsync($"text={text}"));
                    Assert.Equal(0, await root.EvalOnSelectorAllAsync<int>($"text={text}", "els => els.length"));
                }
            }
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should match input[type=button|submit]")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldMatchInputTypeButtonSubmit()
        {
            await Page.SetContentAsync("<input type=\"submit\" value=\"hello\"><input type=\"button\" value=\"world\">");
            Assert.Equal("<input type=\"submit\" value=\"hello\">", await Page.EvalOnSelectorAsync<string>("text=hello", "e => e.outerHTML"));
            Assert.Equal("<input type=\"button\" value=\"world\">", await Page.EvalOnSelectorAsync<string>("text=world", "e => e.outerHTML"));
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should work for open shadow roots")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForOpenShadowRoots()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            Assert.Equal("Hello from root1", await Page.EvalOnSelectorAsync<string>("text=root1", "e => e.textContent"));
            Assert.Equal("Hello from root2", await Page.EvalOnSelectorAsync<string>("text=root2", "e => e.textContent"));
            Assert.Equal("Hello from root3", await Page.EvalOnSelectorAsync<string>("text=root3", "e => e.textContent"));
            Assert.Equal("Hello from root3", await Page.EvalOnSelectorAsync<string>("#root1 >> text=from root3", "e => e.textContent"));
            Assert.Equal("Hello from root2", await Page.EvalOnSelectorAsync<string>("#target >> text=from root2", "e => e.textContent"));

            Assert.Null(await Page.QuerySelectorAsync("text:light=root1"));
            Assert.Null(await Page.QuerySelectorAsync("text:light=root2"));
            Assert.Null(await Page.QuerySelectorAsync("text:light=root3"));
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should prioritize light dom over shadow dom in the same parent")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPrioritizeLightDomOverShadowDomInTheSameParent()
        {
            await Page.EvaluateAsync(@"
                const div = document.createElement('div');
                document.body.appendChild(div);

                div.attachShadow({ mode: 'open' });
                const shadowSpan = document.createElement('span');
                shadowSpan.textContent = 'Hello from shadow';
                div.shadowRoot.appendChild(shadowSpan);

                const lightSpan = document.createElement('span');
                lightSpan.textContent = 'Hello from light';
                div.appendChild(lightSpan);
            ");

            Assert.Equal("Hello from light", await Page.EvalOnSelectorAsync<string>("div >> text=Hello", "e => e.textContent"));
        }

        [PlaywrightTest("queryselector.spec.js", "text selector", "should waitForSelector with distributed elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForSelectorWithDistributedElements()
        {
            var task = Page.WaitForSelectorAsync("div >> text=Hello");
            await Page.EvaluateAsync(@"
                const div = document.createElement('div');
                document.body.appendChild(div);

                div.attachShadow({ mode: 'open' });
                const shadowSpan = document.createElement('span');
                shadowSpan.textContent = 'Hello from shadow';
                div.shadowRoot.appendChild(shadowSpan);
                div.shadowRoot.appendChild(document.createElement('slot'));

                const lightSpan = document.createElement('span');
                lightSpan.textContent = 'Hello from light';
                div.appendChild(lightSpan);
            ");

            var handle = await task;
            Assert.Equal("Hello from light", await handle.GetTextContentAsync());
        }
    }
}
