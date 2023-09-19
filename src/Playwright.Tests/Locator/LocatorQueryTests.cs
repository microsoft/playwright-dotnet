/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Locator;

public class LocatorQueryTests : PageTestEx
{
    [PlaywrightTest("locator-query.spec.ts", "should respect first() and last()")]
    public async Task ShouldRespectFirstAndLast()
    {
        await Page.SetContentAsync(@"
                <section>
                    <div><p>A</p></div>
                    <div><p>A</p><p>A</p></div>
                    <div><p>A</p><p>A</p><p>A</p></div>
                </section>");
        Assert.AreEqual(6, await Page.Locator("div >> p").CountAsync());
        Assert.AreEqual(6, await Page.Locator("div").Locator("p").CountAsync());
        Assert.AreEqual(1, await Page.Locator("div").First.Locator("p").CountAsync());
        Assert.AreEqual(3, await Page.Locator("div").Last.Locator("p").CountAsync());
    }

    [PlaywrightTest("locator-query.spec.ts", "should respect nth()")]
    public async Task ShouldRespectNth()
    {
        await Page.SetContentAsync(@"
                <section>
                    <div><p>A</p></div>
                    <div><p>A</p><p>A</p></div>
                    <div><p>A</p><p>A</p><p>A</p></div>
                </section>");
        Assert.AreEqual(1, await Page.Locator("div >> p").Nth(0).CountAsync());
        Assert.AreEqual(2, await Page.Locator("div").Nth(1).Locator("p").CountAsync());
        Assert.AreEqual(3, await Page.Locator("div").Nth(2).Locator("p").CountAsync());
    }

    [PlaywrightTest("locator-query.spec.ts", "should throw on capture w/ nth()")]
    public async Task ShouldThrowOnCaptureWithNth()
    {
        await Page.SetContentAsync("<section><div><p>A</p></div></section>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("*css=div >> p").Nth(1).ClickAsync());
        StringAssert.Contains("Can't query n-th element", exception.Message);
    }

    [PlaywrightTest("locator-query.spec.ts", "should throw on due to strictness")]
    public async Task ShouldThrowDueToStrictness()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("div").IsVisibleAsync());
        StringAssert.Contains("strict mode violation", exception.Message);
    }

    [PlaywrightTest("locator-query.spec.ts", "should throw on due to strictness 2")]
    public async Task ShouldThrowDueToStrictness2()
    {
        await Page.SetContentAsync("<select><option>One</option><option>Two</option></select>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("option").EvaluateAsync("() => {}"));
        StringAssert.Contains("strict mode violation", exception.Message);
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by text")]
    public async Task ShouldFilterByText()
    {
        await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasTextString = "Foo" }).TextContentAsync(), "Foobar");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by text 2")]
    public async Task ShouldFilterByText2()
    {
        await Page.SetContentAsync("<div>foo <span>hello world</span> bar</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasText = "hello world" }).TextContentAsync(), "foo hello world bar");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by regex")]
    public async Task ShouldFilterByRegex()
    {
        await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasTextRegex = new Regex("Foo.*") }).InnerTextAsync(), "Foobar");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by text with quotes")]
    public async Task ShouldFilterByTextWithQuotes()
    {
        await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasTextString = "Hello \"world\"" }).InnerTextAsync(), "Hello \"world\"");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by regex with quotes")]
    public async Task ShouldFilterByRegexWithQuotes()
    {
        await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasTextRegex = new Regex("Hello \"world\"") }).InnerTextAsync(), "Hello \"world\"");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by regex with a single quote")]
    public async Task ShouldFilterByRegexWithASingleQuote()
    {
        await Page.SetContentAsync("<button>let's let's<span>hello</span></button>");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let's", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let's", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let['abc]s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let['abc]s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\\'s", RegexOptions.IgnoreCase) })).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\\'s", RegexOptions.IgnoreCase) })).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let's let\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let's let\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\'s let's", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\'s let's", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");

        await Page.SetContentAsync("<button>let\\'s let\\'s<span>hello</span></button>");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\'s", RegexOptions.IgnoreCase) })).Not.ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\'s", RegexOptions.IgnoreCase) })).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\\'s let\\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\\'s let\\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\\\'s let\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\\\'s let\\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by regex and regexp flags")]
    public async Task ShouldFilterByRegexandRegexpFlags()
    {
        await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasTextRegex = new Regex("hElLo \"wOrld\"", RegexOptions.IgnoreCase) }).InnerTextAsync(), "Hello \"world\"");
    }

    [PlaywrightTest("locator-query.spec.ts", "should filter by case-insensitive regex in a child")]
    public async Task ShouldFilterByCaseInsensitiveRegexInAChild()
    {
        await Page.SetContentAsync("<div class=\"test\"><h5>Title Text</h5></div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("^title text$", RegexOptions.IgnoreCase) })).ToHaveTextAsync("Title Text");
    }


    [PlaywrightTest("locator-query.spec.ts", "should filter by case-insensitive regex in multiple children")]
    public async Task ShouldFilterByCaseInsensitiveRegexInMultipleChildren()
    {
        await Page.SetContentAsync("<div class=\"test\"><h5>Title</h5> <h2><i>Text</i></h2></div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("^title text$", RegexOptions.IgnoreCase) })).ToHaveClassAsync("test");
    }


    [PlaywrightTest("locator-query.spec.ts", "should filter by regex with special symbols")]
    public async Task ShouldFilterByRegexWithSpecialSymbols()
    {
        await Page.SetContentAsync("<div class=\"test\"><h5>First/\"and\"</h5><h2><i>Second\\</i></h2></div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("^first/\\\".*\\\"second\\\\$", RegexOptions.IgnoreCase | RegexOptions.Singleline) })).ToHaveClassAsync("test");
    }

    [PlaywrightTest("locator-query.spec.ts", "should support filter by compiled regex")]
    public async Task ShouldSupportFilterByCompiledRegex()
    {
        await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
        StringAssert.Contains(await Page.Locator("div", new() { HasTextRegex = new Regex("Foo.*", RegexOptions.Compiled) }).InnerTextAsync(), "Foobar");
    }

    [PlaywrightTest("locator-query.spec.ts", "")]
    public async Task ShouldNotEncodeUnicode()
    {
        await Page.GotoAsync(Server.EmptyPage);
        ILocator[] locators = new ILocator[] {
                Page.Locator("button", new() { HasText = "Драматург" }),
                Page.Locator("button", new() { HasTextRegex = new Regex("Драматург") }),
                Page.Locator("button", new() { Has = Page.Locator("text=Драматург") }),
            };
        foreach (var locator in locators)
        {
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => locator.ClickAsync(new() { Timeout = 1_000 }));
            StringAssert.Contains("Драматург", exception.Message);
        }
    }

    [PlaywrightTest("locator-query.spec.ts", "should support has:locator")]
    public async Task ShouldSupportHasLocator()
    {
        await Page.SetContentAsync("<div><span>hello</span></div><div><span>world</span></div>");
        await Expect(Page.Locator("div", new() { Has = Page.Locator("text=world") })).ToHaveCountAsync(1);
        Assert.AreEqual("<div><span>world</span></div>", await Page.Locator("div", new() { Has = Page.Locator("text=world") }).EvaluateAsync<string>("e => e.outerHTML"));
        await Expect(Page.Locator("div", new() { Has = Page.Locator("text=hello") })).ToHaveCountAsync(1);
        Assert.AreEqual("<div><span>hello</span></div>", await Page.Locator("div", new() { Has = Page.Locator("text=hello") }).EvaluateAsync<string>("e => e.outerHTML"));
        await Expect(Page.Locator("div", new() { Has = Page.Locator("xpath=./span") })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div", new() { Has = Page.Locator("span") })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div", new() { Has = Page.Locator("span", new() { HasTextString = "wor" }) })).ToHaveCountAsync(1);
        Assert.AreEqual("<div><span>world</span></div>", await Page.Locator("div", new() { Has = Page.Locator("span", new() { HasTextString = "wor" }) }).EvaluateAsync<string>("e => e.outerHTML"));
        await Expect(Page.Locator("div", new() { HasTextString = "wor", Has = Page.Locator("span") })).ToHaveCountAsync(1);
    }

    [PlaywrightTest("locator-query.spec.ts", "should support locator.filter")]
    public async Task ShouldSupportLocatorFilter()
    {
        await Page.SetContentAsync("<section><div><span>hello</span></div><div><span>world</span></div></section>");
        await Expect(Page.Locator("div").Filter(new() { HasText = "hello" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div", new() { HasTextString = "hello" }).Filter(new() { HasText = "hello" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div", new() { HasText = "hello" }).Filter(new() { HasTextString = "world" })).ToHaveCountAsync(0);
        await Expect(Page.Locator("section", new() { HasText = "hello" }).Filter(new() { HasTextString = "world" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasTextString = "hello" }).Locator("span")).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { Has = Page.Locator("span", new() { HasText = "world" }) })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { Has = Page.Locator("span") })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").Filter(new() { Has = Page.Locator("span"), HasText = "world" })).ToHaveCountAsync(1);

        await Expect(Page.Locator("div").Filter(new() { HasNot = Page.Locator("span", new() { HasText = "world" }) })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasNot = Page.Locator("section") })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").Filter(new() { HasNot = Page.Locator("span") })).ToHaveCountAsync(0);

        await Expect(Page.Locator("div").Filter(new() { HasNotTextString = "hello" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasNotTextString = "foo" })).ToHaveCountAsync(2);
    }

    [PlaywrightTest("locator-query.spec.ts", "should support locator.and")]
    public async Task ShouldSupportLocatorAnd()
    {
        await Page.SetContentAsync(@"
            <div data-testid=foo>hello</div><div data-testid=bar>world</div>
            <span data-testid=foo>hello2</span><span data-testid=bar>world2</span>
        ");
        await Expect(Page.Locator("div").And(Page.Locator("div"))).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").And(Page.GetByTestId("foo"))).ToHaveTextAsync(new string[] { "hello" });
        await Expect(Page.Locator("div").And(Page.GetByTestId("bar"))).ToHaveTextAsync(new string[] { "world" });
        await Expect(Page.GetByTestId("foo").And(Page.Locator("div"))).ToHaveTextAsync(new string[] { "hello" });
        await Expect(Page.GetByTestId("bar").And(Page.Locator("span"))).ToHaveTextAsync(new string[] { "world2" });
        await Expect(Page.Locator("span").And(Page.GetByTestId(new Regex("bar|foo")))).ToHaveCountAsync(2);
    }

    [PlaywrightTest("locator-query.spec.ts", "should support locator.or")]
    public async Task ShouldSupportLocatorOr()
    {
        await Page.SetContentAsync("<div>hello</div><span>world</span>");
        await Expect(Page.Locator("div").Or(Page.Locator("span"))).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").Or(Page.Locator("span"))).ToHaveTextAsync(new[] { "hello", "world" });
        await Expect(Page.Locator("span").Or(Page.Locator("article")).Or(Page.Locator("div"))).ToHaveTextAsync(new[] { "hello", "world" });
        await Expect(Page.Locator("article").Or(Page.Locator("someting"))).ToHaveCountAsync(0);
        await Expect(Page.Locator("article").Or(Page.Locator("div"))).ToHaveTextAsync("hello");
        await Expect(Page.Locator("article").Or(Page.Locator("span"))).ToHaveTextAsync("world");
        await Expect(Page.Locator("div").Or(Page.Locator("article"))).ToHaveTextAsync("hello");
        await Expect(Page.Locator("span").Or(Page.Locator("article"))).ToHaveTextAsync("world");
    }

    [PlaywrightTest("locator-query.spec.ts", "should support locator.locator with and/or")]
    public async Task ShouldSupportLocatorLocatorWithAndOr()
    {
        await Page.SetContentAsync(@"
            <div>one <span>two</span> <button>three</button> </div>
            <span>four</span>
            <button>five</button>
        ");

        await Expect(Page.Locator("div").Locator(Page.Locator("button"))).ToHaveTextAsync(new[] { "three" });
        await Expect(Page.Locator("div").Locator(Page.Locator("button").Or(Page.Locator("span")))).ToHaveTextAsync(new[] { "two", "three" });
        await Expect(Page.Locator("button").Or(Page.Locator("span"))).ToHaveTextAsync(new[] { "two", "three", "four", "five" });

        await Expect(Page.Locator("div").Locator(Page.Locator("button").And(Page.GetByRole(AriaRole.Button)))).ToHaveTextAsync(new[] { "three" });
        await Expect(Page.Locator("button").And(Page.GetByRole(AriaRole.Button))).ToHaveTextAsync(new[] { "three", "five" });
    }

    [PlaywrightTest("locator-query.spec.ts", "should enforce same frame for has:locator'")]
    public async Task ShouldEnforceSameFrameForHasLocator()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/two-frames.html");
        var child = Page.Frames[1];
        var exception = await PlaywrightAssert.ThrowsAsync<ArgumentException>(() =>
        {
            Page.Locator("div", new() { Has = child.Locator("span") });
            return Task.CompletedTask;
        });
        Assert.AreEqual(exception.Message, "Inner \"Has\" locator must belong to the same frame.");
    }
}
