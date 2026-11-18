/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-query.spec.ts.</summary>
public class LocatorQueryTests : UpstreamPageTest
{
    [UpstreamTest("LO-157 should respect first() and last() @smoke")]
    public async Task ShouldRespectFirstAndLast()
    {
        await Page.SetContentAsync(@"
  <section>
    <div><p>A</p></div>
    <div><p>A</p><p>A</p></div>
    <div><p>A</p><p>A</p><p>A</p></div>
  </section>");
        Assert.That(await Page.Locator("div >> p").CountAsync(), Is.EqualTo(6));
        Assert.That(await Page.Locator("div").Locator("p").CountAsync(), Is.EqualTo(6));
        Assert.That(await Page.Locator("div").First.Locator("p").CountAsync(), Is.EqualTo(1));
        Assert.That(await Page.Locator("div").Last.Locator("p").CountAsync(), Is.EqualTo(3));
    }

    [UpstreamTest("LO-158 should respect nth()")]
    public async Task ShouldRespectNth()
    {
        await Page.SetContentAsync(@"
  <section>
    <div><p>A</p></div>
    <div><p>A</p><p>A</p></div>
    <div><p>A</p><p>A</p><p>A</p></div>
  </section>");
        Assert.That(await Page.Locator("div >> p").Nth(0).CountAsync(), Is.EqualTo(1));
        Assert.That(await Page.Locator("div").Nth(1).Locator("p").CountAsync(), Is.EqualTo(2));
        Assert.That(await Page.Locator("div").Nth(2).Locator("p").CountAsync(), Is.EqualTo(3));
    }

    [UpstreamTest("LO-159 should throw on capture w/ nth()")]
    public async Task ShouldThrowOnCaptureWNth()
    {
        await Page.SetContentAsync("<section><div><p>A</p></div></section>");
        var e = await Catch(() => Page.Locator("*css=div >> p").Nth(1).ClickAsync());
        Assert.That(e.Message, Does.Contain("Can't query n-th element"));
    }

    [UpstreamTest("LO-160 should throw on due to strictness")]
    public async Task ShouldThrowOnDueToStrictness()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div>");
        var e = await Catch(() => Page.Locator("div").IsVisibleAsync());
        Assert.That(e.Message, Does.Contain("strict mode violation"));
    }

    [UpstreamTest("LO-161 should throw on due to strictness 2")]
    public async Task ShouldThrowOnDueToStrictness2()
    {
        await Page.SetContentAsync("<select><option>One</option><option>Two</option></select>");
        var e = await Catch(() => Page.Locator("option").EvaluateAsync("e => {}"));
        Assert.That(e.Message, Does.Contain("strict mode violation"));
    }

    [UpstreamTest("LO-162 should filter by text")]
    public async Task ShouldFilterByText()
    {
        await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
        await Expect(Page.Locator("div", new() { HasText = "Foo" })).ToHaveTextAsync("Foobar");
    }

    [UpstreamTest("LO-163 should filter by text 2")]
    public async Task ShouldFilterByText2()
    {
        await Page.SetContentAsync("<div>foo <span>hello world</span> bar</div>");
        await Expect(Page.Locator("div", new() { HasText = "hello world" })).ToHaveTextAsync("foo hello world bar");
    }

    [UpstreamTest("LO-164 should filter by regex")]
    public async Task ShouldFilterByRegex()
    {
        await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("Foo.*") })).ToHaveTextAsync("Foobar");
    }

    [UpstreamTest("LO-165 should filter by text with quotes")]
    public async Task ShouldFilterByTextWithQuotes()
    {
        await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
        await Expect(Page.Locator("div", new() { HasText = "Hello \"world\"" })).ToHaveTextAsync("Hello \"world\"");
    }

    [UpstreamTest("LO-166 should filter by regex with quotes")]
    public async Task ShouldFilterByRegexWithQuotes()
    {
        await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("Hello \"world\"") })).ToHaveTextAsync("Hello \"world\"");
    }

    [UpstreamTest("LO-167 should filter by regex with a single quote")]
    public async Task ShouldFilterByRegexWithASingleQuote()
    {
        await Page.SetContentAsync("<button>let's let's<span>hello</span></button>");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex("let's", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("let's", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"let\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"let\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex("'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex(@"\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(@"\'s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.Locator("button", new() { HasTextRegex = new Regex("let['abc]s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("let['abc]s", RegexOptions.IgnoreCase) }).Locator("span")).ToHaveTextAsync("hello");
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

        await Page.SetContentAsync("<button>let's hello</button>");
        // .NET: await expect.soft(page.locator('button', { hasText: /let's/iu })).toHaveText(`let's hello`) is left out: a Regex has no u flag.
        // .NET: await expect.soft(page.getByRole('button', { name: /let's/iu })).toHaveText(`let's hello`) is left out: a Regex has no u flag.
    }

    [UpstreamTest("LO-168 should filter by regex and regexp flags")]
    public async Task ShouldFilterByRegexAndRegexpFlags()
    {
        await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("hElLo \"world\"", RegexOptions.IgnoreCase) })).ToHaveTextAsync("Hello \"world\"");
    }

    [UpstreamTest("LO-169 should filter by case-insensitive regex in a child")]
    public async Task ShouldFilterByCaseInsensitiveRegexInAChild()
    {
        await Page.SetContentAsync("<div class=\"test\"><h5>Title Text</h5></div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("^title text$", RegexOptions.IgnoreCase) })).ToHaveTextAsync("Title Text");
    }

    [UpstreamTest("LO-170 should filter by case-insensitive regex in multiple children")]
    public async Task ShouldFilterByCaseInsensitiveRegexInMultipleChildren()
    {
        await Page.SetContentAsync("<div class=\"test\"><h5>Title</h5> <h2><i>Text</i></h2></div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex("^title text$", RegexOptions.IgnoreCase) })).ToHaveClassAsync("test");
    }

    [UpstreamTest("LO-171 should filter by regex with special symbols")]
    public async Task ShouldFilterByRegexWithSpecialSymbols()
    {
        await Page.SetContentAsync("<div class=\"test\"><h5>First/\"and\"</h5><h2><i>Second\\</i></h2></div>");
        await Expect(Page.Locator("div", new() { HasTextRegex = new Regex(@"^first\/"".*""second\\$", RegexOptions.Singleline | RegexOptions.IgnoreCase) })).ToHaveClassAsync("test");
    }

    [UpstreamTest("LO-172 should support has:locator")]
    public async Task ShouldSupportHasLocator()
    {
        Info.Skip(Trace == "on");

        await Page.SetContentAsync("<div><span>hello</span></div><div><span>world</span></div>");
        await Expect(Page.Locator("div", new()
        {
            Has = Page.Locator("text=world")
        })).ToHaveCountAsync(1);
        Assert.That(await Page.Locator("div", new()
        {
            Has = Page.Locator("text=world")
        }).EvaluateAsync<string>("e => e.outerHTML"), Is.EqualTo("<div><span>world</span></div>"));
        await Expect(Page.Locator("div", new()
        {
            Has = Page.Locator("text=\"hello\"")
        })).ToHaveCountAsync(1);
        Assert.That(await Page.Locator("div", new()
        {
            Has = Page.Locator("text=\"hello\"")
        }).EvaluateAsync<string>("e => e.outerHTML"), Is.EqualTo("<div><span>hello</span></div>"));
        await Expect(Page.Locator("div", new()
        {
            Has = Page.Locator("xpath=./span")
        })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div", new()
        {
            Has = Page.Locator("span")
        })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div", new()
        {
            Has = Page.Locator("span", new() { HasText = "wor" })
        })).ToHaveCountAsync(1);
        Assert.That(await Page.Locator("div", new()
        {
            Has = Page.Locator("span", new() { HasText = "wor" })
        }).EvaluateAsync<string>("e => e.outerHTML"), Is.EqualTo("<div><span>world</span></div>"));
        await Expect(Page.Locator("div", new()
        {
            Has = Page.Locator("span"),
            HasText = "wor",
        })).ToHaveCountAsync(1);
    }

    [UpstreamTest("LO-173 should support locator.filter")]
    public async Task ShouldSupportLocatorFilter()
    {
        Info.Skip(Trace == "on");

        await Page.SetContentAsync("<section><div><span>hello</span></div><div><span>world</span></div></section>");
        await Expect(Page.Locator("div").Filter(new() { HasText = "hello" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div", new() { HasText = "hello" }).Filter(new() { HasText = "hello" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div", new() { HasText = "hello" }).Filter(new() { HasText = "world" })).ToHaveCountAsync(0);
        await Expect(Page.Locator("section", new() { HasText = "hello" }).Filter(new() { HasText = "world" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasText = "hello" }).Locator("span")).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { Has = Page.Locator("span", new() { HasText = "world" }) })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { Has = Page.Locator("span") })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").Filter(new()
        {
            Has = Page.Locator("span"),
            HasText = "world",
        })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasNot = Page.Locator("span", new() { HasText = "world" }) })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasNot = Page.Locator("section") })).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").Filter(new() { HasNot = Page.Locator("span") })).ToHaveCountAsync(0);
        await Expect(Page.Locator("div").Filter(new() { HasNotText = "hello" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Filter(new() { HasNotText = "foo" })).ToHaveCountAsync(2);
    }

    [UpstreamTest("LO-174 should support locator.and")]
    public async Task ShouldSupportLocatorAnd()
    {
        await Page.SetContentAsync(@"
    <div data-testid=foo>hello</div><div data-testid=bar>world</div>
    <span data-testid=foo>hello2</span><span data-testid=bar>world2</span>
  ");
        await Expect(Page.Locator("div").And(Page.Locator("div"))).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").And(Page.GetByTestId("foo"))).ToHaveTextAsync(new[] { "hello" });
        await Expect(Page.Locator("div").And(Page.GetByTestId("bar"))).ToHaveTextAsync(new[] { "world" });
        await Expect(Page.GetByTestId("foo").And(Page.Locator("div"))).ToHaveTextAsync(new[] { "hello" });
        await Expect(Page.GetByTestId("bar").And(Page.Locator("span"))).ToHaveTextAsync(new[] { "world2" });
        await Expect(Page.Locator("span").And(Page.GetByTestId(new Regex("bar|foo")))).ToHaveCountAsync(2);
    }

    [UpstreamTest("LO-175 should support locator.or")]
    public async Task ShouldSupportLocatorOr()
    {
        await Page.SetContentAsync("<div>hello</div><span>world</span>");
        await Expect(Page.Locator("div").Or(Page.Locator("span"))).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").Or(Page.Locator("span"))).ToHaveTextAsync(new[] { "hello", "world" });
        await Expect(Page.Locator("span").Or(Page.Locator("article")).Or(Page.Locator("div"))).ToHaveTextAsync(new[] { "hello", "world" });
        await Expect(Page.Locator("article").Or(Page.Locator("something"))).ToHaveCountAsync(0);
        await Expect(Page.Locator("article").Or(Page.Locator("div"))).ToHaveTextAsync("hello");
        await Expect(Page.Locator("article").Or(Page.Locator("span"))).ToHaveTextAsync("world");
        await Expect(Page.Locator("div").Or(Page.Locator("article"))).ToHaveTextAsync("hello");
        await Expect(Page.Locator("span").Or(Page.Locator("article"))).ToHaveTextAsync("world");
    }

    [UpstreamTest("LO-176 should support locator.locator with and/or")]
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

    [UpstreamTest("LO-177 should support locator.within")]
    public async Task ShouldSupportLocatorWithin()
    {
        await Page.SetContentAsync(@"
    <table>
      <tr><td>a1</td><td>a2</td><td>a3</td></tr>
      <tr><td>b1</td><td>b2</td><td>b3</td></tr>
      <tr><td>c1</td><td>c2</td><td>c3</td></tr>
    </table>
    <span>outside</span>
  ");

        await Expect(Page.GetByRole(AriaRole.Cell).Within(Page.GetByRole(AriaRole.Row))).ToHaveTextAsync(new[] { "a1", "a2", "a3", "b1", "b2", "b3", "c1", "c2", "c3" });
        await Expect(Page.GetByRole(AriaRole.Cell).Nth(1).Within(Page.GetByRole(AriaRole.Row))).ToHaveTextAsync(new[] { "a2", "b2", "c2" });
        await Expect(Page.GetByRole(AriaRole.Cell).Last.Within(Page.GetByRole(AriaRole.Row))).ToHaveTextAsync(new[] { "a3", "b3", "c3" });
        await Expect(Page.GetByRole(AriaRole.Cell).Nth(1).Within(Page.GetByRole(AriaRole.Row).Nth(2))).ToHaveTextAsync(new[] { "c2" });
        await Expect(Page.Locator("span").Within(Page.GetByRole(AriaRole.Row))).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Cell).Nth(1).Within(Page.GetByRole(AriaRole.Row)).Nth(1)).ToHaveTextAsync("b2");
    }

    [UpstreamTest("LO-178 should allow some, but not all nested frameLocators")]
    [LocalFixme("dotnet: the error has Locator(\"iframe\").ContentFrame.Locator(\"div\").Or(Locator(\"#iframe\").ContentFrame.Locator(\"span\")), upstream expects locator('iframe').contentFrame().locator('div').or(locator('#iframe').contentFrame().locator('span'))")]
    public async Task ShouldAllowSomeButNotAllNestedFrameLocators()
    {
        await Page.SetContentAsync("<iframe srcdoc=\"<span id=target>world</span>\"></iframe><span>hello</span>");
        await Expect(Page.FrameLocator("iframe").Locator("span").Or(Page.FrameLocator("iframe").Locator("article"))).ToHaveTextAsync("world");
        await Expect(Page.FrameLocator("iframe").Locator("article").Or(Page.FrameLocator("iframe").Locator("span"))).ToHaveTextAsync("world");
        await Expect(Page.FrameLocator("iframe").Locator("span").And(Page.FrameLocator("iframe").Locator("#target"))).ToHaveTextAsync("world");
        var error1 = await Catch(() => Expect(Page.FrameLocator("iframe").Locator("div").Or(Page.FrameLocator("#iframe").Locator("span"))).ToHaveTextAsync("world"));
        Assert.That(error1.Message, Does.Contain("Frame locators are not allowed inside composite locators, while querying \"locator('iframe').contentFrame().locator('div').or(locator('#iframe').contentFrame().locator('span'))"));
        var error2 = await Catch(() => Expect(Page.FrameLocator("iframe").Locator("div").And(Page.FrameLocator("#iframe").Locator("span"))).ToHaveTextAsync("world"));
        Assert.That(error2.Message, Does.Contain("Frame locators are not allowed inside composite locators, while querying \"locator('iframe').contentFrame().locator('div').and(locator('#iframe').contentFrame().locator('span'))"));
    }

    [UpstreamTest("LO-179 should keep the capture when removing a common frame prefix")]
    public async Task ShouldKeepTheCaptureWhenRemovingACommonFramePrefix()
    {
        await Page.SetContentAsync("<iframe id=f srcdoc=\"<section id=target><span>hi</span></section>\"></iframe>");
        var inner = Page.FrameLocator("#f").Locator("*css=section >> span");
        await Expect(Page.FrameLocator("#f").Locator("body").Locator(inner)).ToHaveAttributeAsync("id", "target");

        var captureFrame = Page.Locator("*css=#f").ContentFrame.Locator("span");
        var error = await Catch(() => Page.FrameLocator("#f").Locator("body").Locator(captureFrame).CountAsync());
        Assert.That(error.Message, Does.Contain("Can not capture the selector before diving into the frame"));
    }

    [UpstreamTest("LO-180 should enforce same frame for has/leftOf/rightOf/above/below/near")]
    [LocalFixme("dotnet: Locator() throws an ArgumentException with 'Inner \"Has\" locator must belong to the same frame.'; upstream expects 'Inner \"has\" locator must belong to the same frame.'")]
    public async Task ShouldEnforceSameFrameForHasLeftOfRightOfAboveBelowNear()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/two-frames.html");
        var child = Page.Frames[1];
        foreach (var option in new[] { "has" })
        {
            Exception error = null;
            try
            {
                // .NET: an options object has no computed property name: { [option]: child.locator('span') } is Has, the one value of the loop.
                Page.Locator("div", new() { Has = child.Locator("span") });
            }
            catch (Exception e)
            {
                error = e;
            }
            Assert.That(error.Message, Does.Contain($"Inner \"{option}\" locator must belong to the same frame."));
        }
    }

    [UpstreamTest("LO-181 alias methods coverage")]
    public async Task AliasMethodsCoverage()
    {
        await Page.SetContentAsync("<div><button>Submit</button></div>");
        await Expect(Page.Locator("button")).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").Locator("button")).ToHaveCountAsync(1);
        await Expect(Page.Locator("div").GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await Expect(Page.MainFrame.Locator("button")).ToHaveCountAsync(1);
    }

    [UpstreamTest("LO-182 count() should not throw during navigation")]
    public async Task CountShouldNotThrowDuringNavigation()
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      window.location.href = './frames/one-frame.html';
      return document.querySelector(selector);
    },
    queryAll(root, selector) {
      window.location.href = './frames/one-frame.html';
      return Array.from(document.querySelectorAll(selector));
    }
  })";
        // .NET: Script is the source of the engine, not a function: the source calls the function, as the Node.js client does.
        await Playwright.Selectors.RegisterAsync("locatorCountHelper", new() { Script = $"({createDummySelector})()" });
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        await Page.Frames[1].SetContentAsync("<div>A</div>");
        Assert.That(await Page.FrameLocator("locatorCountHelper=iframe").Locator("locatorCountHelper=div").CountAsync(), Is.EqualTo(0));
    }
}
