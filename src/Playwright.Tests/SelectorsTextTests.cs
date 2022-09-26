/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

public class SelectorsTextTests : PageTestEx
{
    [PlaywrightTest("selectors-text.spec.ts", "query")]
    public async Task Query()
    {
        await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=ya", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"ya\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=/^[ay]+$/", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=/Ya/i", "e => e.outerHTML"));
        Assert.AreEqual("<div>\nye  </div>", await Page.EvalOnSelectorAsync<string>("text=ye", "e => e.outerHTML"));

        await Page.SetContentAsync("<div> ye </div><div>ye</div>");
        Assert.AreEqual("<div> ye </div>", await Page.EvalOnSelectorAsync<string>("text=\"ye\"", "e => e.outerHTML"));

        await Page.SetContentAsync("<div>yo</div><div>\"ya</div><div> hello world! </div>");
        Assert.AreEqual("<div>\"ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\\"ya\"", "e => e.outerHTML"));
        Assert.AreEqual("<div> hello world! </div>", await Page.EvalOnSelectorAsync<string>("text=/hello/", "e => e.outerHTML"));
        Assert.AreEqual("<div> hello world! </div>", await Page.EvalOnSelectorAsync<string>("text=/^\\s*heLLo/i", "e => e.outerHTML"));

        await Page.SetContentAsync("<div>yo<div>ya</div>hey<div>hey</div></div>");
        Assert.AreEqual("<div>hey</div>", await Page.EvalOnSelectorAsync<string>("text=hey", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"yo\" >> text =\"ya\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text='yo' >> text =\"ya\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=\"yo\" >> text='ya'", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text='yo' >> text='ya'", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("'yo' >> \"ya\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("\"yo\" >> 'ya'", "e => e.outerHTML"));

        await Page.SetContentAsync("<div>yo<span id=\"s1\"></span></div><div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>");
        Assert.AreEqual("<div>yo<span id=\"s1\"></span></div>\n<div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>", await Page.EvalOnSelectorAllAsync<string>("text=yo", "es => es.map(e => e.outerHTML).join('\\n')"));

        await Page.SetContentAsync("<div>'</div><div>\"</div><div>\\</div><div>x</div>");
        Assert.AreEqual("<div>'</div>", await Page.EvalOnSelectorAsync<string>("text='\\''", "e => e.outerHTML"));
        Assert.AreEqual("<div>\"</div>", await Page.EvalOnSelectorAsync<string>("text='\"'", "e => e.outerHTML"));
        Assert.AreEqual("<div>\"</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\\"\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>'</div>", await Page.EvalOnSelectorAsync<string>("text=\"'\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>x</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\x\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>x</div>", await Page.EvalOnSelectorAsync<string>("text='\\x'", "e => e.outerHTML"));
        Assert.AreEqual("<div>\\</div>", await Page.EvalOnSelectorAsync<string>("text='\\\\'", "e => e.outerHTML"));
        Assert.AreEqual("<div>\\</div>", await Page.EvalOnSelectorAsync<string>("text=\"\\\\\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>\"</div>", await Page.EvalOnSelectorAsync<string>("text=\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>'</div>", await Page.EvalOnSelectorAsync<string>("text='", "e => e.outerHTML"));
        Assert.AreEqual("<div>x</div>", await Page.EvalOnSelectorAsync<string>("\"x\"", "e => e.outerHTML"));
        Assert.AreEqual("<div>x</div>", await Page.EvalOnSelectorAsync<string>("'x'", "e => e.outerHTML"));

        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("\""));
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("'"));

        await Page.SetContentAsync("<div> ' </div><div> \" </div>");
        Assert.AreEqual("<div> \" </div>", await Page.EvalOnSelectorAsync<string>("text=\"", "e => e.outerHTML"));
        Assert.AreEqual("<div> ' </div>", await Page.EvalOnSelectorAsync<string>("text='", "e => e.outerHTML"));

        await Page.SetContentAsync("<div>Hi''&gt;&gt;foo=bar</div>");
        Assert.AreEqual("<div>Hi''&gt;&gt;foo=bar</div>", await Page.EvalOnSelectorAsync<string>("text=\"Hi''>>foo=bar\"", "e => e.outerHTML"));
        await Page.SetContentAsync("<div>Hi'\"&gt;&gt;foo=bar</div> ");
        Assert.AreEqual("<div>Hi'\"&gt;&gt;foo=bar</div>", await Page.EvalOnSelectorAsync<string>("text=\"Hi'\\\">>foo=bar\"", "e => e.outerHTML"));

        await Page.SetContentAsync("<div>Hi&gt;&gt;<span></span></div>");
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("text=\"Hi>>\">>span", "e => e.outerHTML"));

        await Page.SetContentAsync("<div>a<br>b</div><div>a</div>");
        Assert.AreEqual("<div>a<br>b</div>", await Page.EvalOnSelectorAsync<string>("text=a", "e => e.outerHTML"));
        Assert.AreEqual("<div>a<br>b</div>", await Page.EvalOnSelectorAsync<string>("text=b", "e => e.outerHTML"));
        Assert.AreEqual("<div>a<br>b</div>", await Page.EvalOnSelectorAsync<string>("text=ab", "e => e.outerHTML"));
        Assert.Null(await Page.QuerySelectorAsync("text=abc"));
        Assert.AreEqual(2, await Page.EvalOnSelectorAllAsync<int>("text=a", "els => els.length"));
        Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("text=b", "els => els.length"));
        Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("text=ab", "els => els.length"));
        Assert.AreEqual(0, await Page.EvalOnSelectorAllAsync<int>("text=abc", "els => els.length"));

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
        Assert.AreEqual("<div>helloworld</div>", await Page.EvalOnSelectorAsync<string>("text=lowo", "e => e.outerHTML"));
        Assert.AreEqual("<div>helloworld</div><span>helloworld</span>", await Page.EvalOnSelectorAllAsync<string>("text=lowo", "els => els.map(e => e.outerHTML).join('')"));
    }

    [PlaywrightTest("selectors-text.spec.ts", "should be case sensitive if quotes are specified")]
    public async Task ShouldBeCaseSensitiveIfQuotesAreSpecified()
    {
        await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        Assert.AreEqual("<div>ya</div>", await Page.EvalOnSelectorAsync<string>("text=ya", "e => e.outerHTML"));
        Assert.Null(await Page.QuerySelectorAsync("text=\"yA\""));
    }

    [PlaywrightTest("selectors-text.spec.ts", "should search for a substring without quotes")]
    public async Task ShouldSearchForASubstringWithoutQuotes()
    {
        await Page.SetContentAsync("<div>textwithsubstring</div>");
        Assert.AreEqual("<div>textwithsubstring</div>", await Page.EvalOnSelectorAsync<string>("text=with", "e => e.outerHTML"));
        Assert.Null(await Page.QuerySelectorAsync("text=\"with\""));
    }

    [PlaywrightTest("selectors-text.spec.ts", "should skip head, script and style")]
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
            Assert.AreEqual("DIV", await Page.EvalOnSelectorAsync<string>($"text={text}", "e => e.nodeName"));
            Assert.AreEqual("DIV", await Page.EvalOnSelectorAllAsync<string>($"text={text}", "els => els.map(e => e.nodeName).join('|')"));

            foreach (var root in new[] { head, title, script, style })
            {
                Assert.Null(await root.QuerySelectorAsync($"text={text}"));
                Assert.AreEqual(0, await root.EvalOnSelectorAllAsync<int>($"text={text}", "els => els.length"));
            }
        }
    }

    [PlaywrightTest("selectors-text.spec.ts", "should match input[type=button|submit]")]
    public async Task ShouldMatchInputTypeButtonSubmit()
    {
        await Page.SetContentAsync("<input type=\"submit\" value=\"hello\"><input type=\"button\" value=\"world\">");
        Assert.AreEqual("<input type=\"submit\" value=\"hello\">", await Page.EvalOnSelectorAsync<string>("text=hello", "e => e.outerHTML"));
        Assert.AreEqual("<input type=\"button\" value=\"world\">", await Page.EvalOnSelectorAsync<string>("text=world", "e => e.outerHTML"));
    }

    [PlaywrightTest("selectors-text.spec.ts", "should work for open shadow roots")]
    public async Task ShouldWorkForOpenShadowRoots()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.AreEqual("Hello from root1", await Page.EvalOnSelectorAsync<string>("text=root1", "e => e.textContent"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("text=root2", "e => e.textContent"));
        Assert.AreEqual("Hello from root3", await Page.EvalOnSelectorAsync<string>("text=root3", "e => e.textContent"));
        Assert.AreEqual("Hello from root3", await Page.EvalOnSelectorAsync<string>("#root1 >> text=from root3", "e => e.textContent"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("#target >> text=from root2", "e => e.textContent"));

        Assert.Null(await Page.QuerySelectorAsync("text:light=root1"));
        Assert.Null(await Page.QuerySelectorAsync("text:light=root2"));
        Assert.Null(await Page.QuerySelectorAsync("text:light=root3"));
    }

    [PlaywrightTest("selectors-text.spec.ts", "should prioritize light dom over shadow dom in the same parent")]
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

        Assert.AreEqual("Hello from light", await Page.EvalOnSelectorAsync<string>("div >> text=Hello", "e => e.textContent"));
    }

    [PlaywrightTest("selectors-text.spec.ts", "should waitForSelector with distributed elements")]
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
        Assert.AreEqual("Hello from light", await handle.TextContentAsync());
    }
}
