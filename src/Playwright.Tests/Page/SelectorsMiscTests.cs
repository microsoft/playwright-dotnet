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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/selectors-misc.spec.ts.</summary>
public class SelectorsMiscTests : UpstreamPageTest
{
    [UpstreamTest("SE-133 should work for open shadow roots")]
    public async Task ShouldWorkForOpenShadowRoots()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAsync<string>("id=target", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.textContent"), Is.EqualTo("Hello from root1"));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("data-testid=foo", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.QuerySelectorAsync("id:light=target"), Is.Null);
        Assert.That(await Page.QuerySelectorAsync("data-testid:light=foo"), Is.Null);
        Assert.That(await Page.QuerySelectorAllAsync("data-testid:light=foo"), Is.EqualTo(new IElementHandle[] { }));
    }

    [UpstreamTest("SE-134 should click on links in shadow dom")]
    public async Task ShouldClickOnLinksInShadowDom()
    {
        await Page.GotoAsync(Server.Prefix + "/shadow-dom-link.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window.clickCount"), Is.EqualTo(0));
        await Page.ClickAsync("#inner-link");
        Assert.That(await Page.EvaluateAsync<int>("() => window.clickCount"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-135 should work with :visible")]
    public async Task ShouldWorkWithVisible_SE135()
    {
        await Page.SetContentAsync(@"
    <section>
      <div id=target1></div>
      <div id=target2></div>
    </section>
  ");
        Assert.That(await Page.QuerySelectorAsync("div:visible"), Is.Null);

        var error = await Catch(() => Page.WaitForSelectorAsync("div:visible", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("1000ms"));

        var promise = Page.WaitForSelectorAsync("div:visible", new() { State = WaitForSelectorState.Attached });
        await Page.EvalOnSelectorAsync("#target2", "div => div.textContent = 'Now visible'");
        var element = await promise;
        Assert.That(await element.EvaluateAsync<string>("e => e.id"), Is.EqualTo("target2"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div:visible", "div => div.id"), Is.EqualTo("target2"));
    }

    [UpstreamTest("SE-136 should work with >> visible=")]
    public async Task ShouldWorkWithVisible_SE136()
    {
        await Page.SetContentAsync(@"
    <section>
      <div id=target1></div>
      <div id=target2></div>
    </section>
  ");
        Assert.That(await Page.QuerySelectorAsync("div >> visible=true"), Is.Null);

        var error = await Catch(() => Page.WaitForSelectorAsync("div >> visible=true", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("1000ms"));

        var promise = Page.WaitForSelectorAsync("div >> visible=true", new() { State = WaitForSelectorState.Attached });
        await Page.EvalOnSelectorAsync("#target2", "div => div.textContent = 'Now visible'");
        var element = await promise;
        Assert.That(await element.EvaluateAsync<string>("e => e.id"), Is.EqualTo("target2"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> visible=true", "div => div.id"), Is.EqualTo("target2"));
    }

    [UpstreamTest("SE-137 should work with >> visible=false")]
    public async Task ShouldWorkWithVisibleFalse()
    {
        await Page.SetContentAsync(@"
    <section>
      <div id=target1></div>
      <div id=target2></div>
    </section>
  ");
        await Expect(Page.Locator("div >> visible=false")).ToHaveCountAsync(2);
        await Page.Locator("#target2").EvaluateAsync("div => div.textContent = 'Now visible'");
        await Expect(Page.Locator("div >> visible=false")).ToHaveCountAsync(1);
    }

    [UpstreamTest("SE-138 should work with :nth-match")]
    public async Task ShouldWorkWithNthMatch()
    {
        await Page.SetContentAsync(@"
    <section>
      <div id=target1></div>
      <div id=target2></div>
    </section>
  ");
        Assert.That(await Page.QuerySelectorAsync(":nth-match(div, 3)"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAsync<string>(":nth-match(div, 1)", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await Page.EvalOnSelectorAsync<string>(":nth-match(div, 2)", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>(":nth-match(section > div, 2)", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>(":nth-match(section, div, 2)", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await Page.EvalOnSelectorAsync<string>(":nth-match(div, section, 3)", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>(":is(:nth-match(div, 1), :nth-match(div, 2))", "els => els.length"), Is.EqualTo(2));

        Exception error;
        error = await Catch(() => Page.QuerySelectorAsync(":nth-match(div, bar, 0)"));
        Assert.That(error.Message, Does.Contain("\"nth-match\" engine expects a one-based index as the last argument"));

        error = await Catch(() => Page.QuerySelectorAsync(":nth-match(2)"));
        Assert.That(error.Message, Does.Contain("\"nth-match\" engine expects non-empty selector list and an index argument"));

        error = await Catch(() => Page.QuerySelectorAsync(":nth-match(div, bar, foo)"));
        Assert.That(error.Message, Does.Contain("\"nth-match\" engine expects a one-based index as the last argument"));

        var promise = Page.WaitForSelectorAsync(":nth-match(div, 3)", new() { State = WaitForSelectorState.Attached });
        await Page.EvalOnSelectorAsync("section", @"section => {
    const div = document.createElement('div');
    div.setAttribute('id', 'target3');
    section.appendChild(div);
  }");
        var element = await promise;
        Assert.That(await element.EvaluateAsync<string>("e => e.id"), Is.EqualTo("target3"));
    }

    [UpstreamTest("SE-139 should work with nth=")]
    public async Task ShouldWorkWithNth()
    {
        await Page.SetContentAsync(@"
    <section>
      <div id=target1></div>
      <div id=target2></div>
    </section>
  ");
        Assert.That(await Page.QuerySelectorAsync("div >> nth=2"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> nth=0", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> nth=1", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("section > div >> nth=1", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("section, div >> nth=1", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div, section >> nth=2", "e => e.id"), Is.EqualTo("target2"));

        var promise = Page.WaitForSelectorAsync("div >> nth=2", new() { State = WaitForSelectorState.Attached });
        await Page.EvalOnSelectorAsync("section", @"section => {
    const div = document.createElement('div');
    div.setAttribute('id', 'target3');
    section.appendChild(div);
  }");
        var element = await promise;
        Assert.That(await element.EvaluateAsync<string>("e => e.id"), Is.EqualTo("target3"));

        await Page.SetContentAsync(@"
    <div>
      <div>
        <div>
          <span>hi</span>
          <span>hello</span>
        </div>
      </div>
    </div>
  ");
        Assert.That(await Page.Locator("div >> div >> span >> nth=1").TextContentAsync(), Is.EqualTo("hello"));
    }

    [UpstreamTest("SE-140 should work with strict mode and chaining")]
    public async Task ShouldWorkWithStrictModeAndChaining()
    {
        await Page.SetContentAsync(@"
    <div>
      <div>
        <div>
          <span>hi</span>
        </div>
      </div>
    </div>
  ");
        Assert.That(await Page.Locator("div >> div >> span").TextContentAsync(), Is.EqualTo("hi"));
    }

    [UpstreamTest("SE-141 should work with layout selectors")]
    public async Task ShouldWorkWithLayoutSelectors()
    {
        Info.Skip(Trace == "on");

        /*

             +--+  +--+
             | 1|  | 2|
             +--+  ++-++
             | 3|   | 4|
        +-------+  ++-++
        |   0   |  | 5|
        | +--+  +--+--+
        | | 6|  | 7|
        | +--+  +--+
        |       |
        O-------+
                +--+
                | 8|
                +--++--+
                    | 9|
                    +--+

        */

        var boxes = new[]
        {
            // x, y, width, height
            new[] { 0, 0, 150, 150 },
            new[] { 100, 200, 50, 50 },
            new[] { 200, 200, 50, 50 },
            new[] { 100, 150, 50, 50 },
            new[] { 201, 150, 50, 50 },
            new[] { 200, 100, 50, 50 },
            new[] { 50, 50, 50, 50 },
            new[] { 150, 50, 50, 50 },
            new[] { 150, -51, 50, 50 },
            new[] { 201, -101, 50, 50 },
        };
        await Page.SetContentAsync("<container style=\"width: 500px; height: 500px; position: relative;\"></container>");
        await Page.EvalOnSelectorAsync("container", @"(container, boxes) => {
    for (let i = 0; i < boxes.length; i++) {
      const div = document.createElement('div');
      div.style.position = 'absolute';
      div.style.overflow = 'hidden';
      div.style.boxSizing = 'border-box';
      div.style.border = '1px solid black';
      div.id = 'id' + i;
      div.textContent = 'id' + i;
      const box = boxes[i];
      div.style.left = box[0] + 'px';
      // Note that top is a flipped y coordinate.
      div.style.top = (250 - box[1] - box[3]) + 'px';
      div.style.width = box[2] + 'px';
      div.style.height = box[3] + 'px';
      container.appendChild(div);
      const span = document.createElement('span');
      span.textContent = '' + i;
      div.appendChild(span);
    }
  }", boxes);

        Assert.That(await Page.EvalOnSelectorAsync<string>("div:right-of(#id6)", "e => e.id"), Is.EqualTo("id7"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:right-of(#id1)", "e => e.id"), Is.EqualTo("id2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:right-of(#id3)", "e => e.id"), Is.EqualTo("id4"));
        Assert.That(await Page.QuerySelectorAsync("div:right-of(#id4)"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:right-of(#id0)", "e => e.id"), Is.EqualTo("id7"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:right-of(#id8)", "e => e.id"), Is.EqualTo("id9"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id3)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id4,id2,id5,id7,id8,id9"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id3, 50)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id2,id5,id7,id8"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id3, 49)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id7,id8"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div:left-of(#id2)", "e => e.id"), Is.EqualTo("id1"));
        Assert.That(await Page.QuerySelectorAsync("div:left-of(#id0)"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:left-of(#id5)", "e => e.id"), Is.EqualTo("id0"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:left-of(#id9)", "e => e.id"), Is.EqualTo("id8"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:left-of(#id4)", "e => e.id"), Is.EqualTo("id3"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:left-of(#id5)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id0,id7,id3,id1,id6,id8"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:left-of(#id5, 3)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id7,id8"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div:above(#id0)", "e => e.id"), Is.EqualTo("id3"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:above(#id5)", "e => e.id"), Is.EqualTo("id4"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:above(#id7)", "e => e.id"), Is.EqualTo("id5"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:above(#id8)", "e => e.id"), Is.EqualTo("id0"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:above(#id9)", "e => e.id"), Is.EqualTo("id8"));
        Assert.That(await Page.QuerySelectorAsync("div:above(#id2)"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:above(#id5)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id4,id2,id3,id1"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:above(#id5, 20)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id4,id3"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id4)", "e => e.id"), Is.EqualTo("id5"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id3)", "e => e.id"), Is.EqualTo("id0"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id2)", "e => e.id"), Is.EqualTo("id4"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id6)", "e => e.id"), Is.EqualTo("id8"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id7)", "e => e.id"), Is.EqualTo("id8"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id8)", "e => e.id"), Is.EqualTo("id9"));
        Assert.That(await Page.QuerySelectorAsync("div:below(#id9)"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:below(#id3)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id0,id5,id6,id7,id8,id9"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:below(#id3, 105)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id0,id5,id6,id7"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div:near(#id0)", "e => e.id"), Is.EqualTo("id3"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:near(#id7)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id0,id5,id3,id6"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:near(#id0)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id3,id6,id7,id8,id1,id5"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:near(#id6)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id0,id3,id7"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:near(#id6, 10)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id0"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:near(#id0, 100)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id3,id6,id7,id8,id1,id5,id4,id2"));

        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:below(#id5):above(#id8)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id7,id6"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:below(#id5):above(#id8)", "e => e.id"), Is.EqualTo("id7"));

        Assert.That(await Page.EvalOnSelectorAllAsync<string>("div:right-of(#id0) + div:above(#id8)", "els => els.map(e => e.id).join(',')"), Is.EqualTo("id5,id6,id3"));

        var error = await Catch(() => Page.QuerySelectorAsync(":near(50)"));
        Assert.That(error.Message, Does.Contain("\"near\" engine expects a selector list and optional maximum distance in pixels"));
    }

    [UpstreamTest("SE-142 should escape the scope with >>")]
    public async Task ShouldEscapeTheScopeWith()
    {
        await Page.SetContentAsync("<div><label>Test</label><input id='myinput'></div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("label >> xpath=.. >> input", "e => e.id"), Is.EqualTo("myinput"));
    }

    [UpstreamTest("SE-143 xpath should be relative")]
    public async Task XpathShouldBeRelative()
    {
        await Page.SetContentAsync(@"
    <span class=""find-me"" id=target1>1</span>
    <div>
      <span class=""find-me"" id=target2>2</span>
    </div>
  ");
        Assert.That(await Page.EvalOnSelectorAsync<string>("//*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target1"));

        var div = await Page.QuerySelectorAsync("div");
        Assert.That(await div.EvalOnSelectorAsync<string>("xpath=./*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await div.EvalOnSelectorAsync<string>("xpath=.//*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await div.EvalOnSelectorAsync<string>("//*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await div.EvalOnSelectorAsync<string>("xpath=/*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));

        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> xpath=./*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> xpath=.//*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> //*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> xpath=/*[@class=\"find-me\"]", "e => e.id"), Is.EqualTo("target2"));
    }

    [UpstreamTest("SE-144 should work with pipe in xpath")]
    public async Task ShouldWorkWithPipeInXpath()
    {
        await Page.SetContentAsync(@"
    <span class=""find-me"" id=t1>1</span>
    <div>
      <span class=""find-me"" id=t2>2</span>
    </div>
    <div id=t3>3</span>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("//*[@id=\"t1\"]|//*[@id=\"t3\"]", "els => els.length"), Is.EqualTo(2));

        var e1 = await Page.WaitForSelectorAsync("//*[@id=\"t1\"]|//*[@id=\"t3\"]");
        Assert.That(e1, Is.Not.Null);
        Assert.That(await e1.EvaluateAsync<string>("e => e.id"), Is.EqualTo("t1"));

        var e2 = await Page.WaitForSelectorAsync("//*[@id=\"unknown\"]|//*[@id=\"t2\"]");
        Assert.That(e2, Is.Not.Null);
        Assert.That(await e2.EvaluateAsync<string>("e => e.id"), Is.EqualTo("t2"));

        await Page.ClickAsync("//code|//span[@id=\"t2\"]");
    }

    [UpstreamTest("SE-145 should print original xpath in error")]
    [LocalFixme("dotnet: the call log has Locator(\"//*[contains(@Class, 'foo']\"), upstream expects locator('//*[contains(@Class, \\'foo\\']')")]
    public async Task ShouldPrintOriginalXpathInError()
    {
        var error = await Catch(() => Page.Locator("//*[contains(@Class, 'foo']").IsVisibleAsync());
        Assert.That(error.Message, Does.Contain("//*[contains(@Class, \\'foo\\']"));
        Assert.That(error.Message, Does.Not.Contain(".//*[contains(@Class, 'foo']"));
    }

    [UpstreamTest("SE-146 data-testid on the handle should be relative")]
    public async Task DataTestidOnTheHandleShouldBeRelative()
    {
        await Page.SetContentAsync(@"
    <span data-testid=""find-me"" id=target1>1</span>
    <div>
      <span data-testid=""find-me"" id=target2>2</span>
    </div>
  ");
        Assert.That(await Page.EvalOnSelectorAsync<string>("data-testid=find-me", "e => e.id"), Is.EqualTo("target1"));

        var div = await Page.QuerySelectorAsync("div");
        Assert.That(await div.EvalOnSelectorAsync<string>("data-testid=find-me", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> data-testid=find-me", "e => e.id"), Is.EqualTo("target2"));
    }

    [UpstreamTest("SE-147 should properly determine visibility of display:contents elements")]
    public async Task ShouldProperlyDetermineVisibilityOfDisplayContentsElements()
    {
        await Page.SetContentAsync(@"
    <div>
      <p style=""display:contents"">DISPLAY CONTENTS</p>
    </div>");
        await Page.WaitForSelectorAsync("\"DISPLAY CONTENTS\"");

        await Page.SetContentAsync(@"
    <div>
      <article style=""display:contents""><div>DISPLAY CONTENTS</div></article>
    </div>");
        await Page.WaitForSelectorAsync("article");

        await Page.SetContentAsync(@"
    <div>
      <article style=""display:contents""><div style=""display:contents"">DISPLAY CONTENTS</div></article>
    </div>");
        await Page.WaitForSelectorAsync("article");

        await Page.SetContentAsync(@"
    <div>
      <article style=""display:contents""><div></div>DISPLAY CONTENTS<span></span></article>
    </div>");
        await Page.WaitForSelectorAsync("article");

        await Page.SetContentAsync(@"
    <div>
      <article style=""display:contents""><div></div></article>
    </div>");
        await Page.WaitForSelectorAsync("article", new() { State = WaitForSelectorState.Hidden });
    }

    [UpstreamTest("SE-148 should work with internal:has=")]
    public async Task ShouldWorkWithInternalHas()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"#target\"", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"[data-testid=foo]\"", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"[attr*=value]\"", "els => els.length"), Is.EqualTo(2));

        await Page.SetContentAsync("<section><span></span><div></div></section><section><br></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has=\"span, div\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has=\"span, div\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has=\"br\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has=\"span, br\"", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has=\"span, br, div\"", "els => els.length"), Is.EqualTo(2));

        await Page.SetContentAsync("<div><span>hello</span></div><div><span>world</span></div>");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"text=world\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> internal:has=\"text=world\"", "e => e.outerHTML"), Is.EqualTo("<div><span>world</span></div>"));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"text=\\\"hello\\\"\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> internal:has=\"text=\\\"hello\\\"\"", "e => e.outerHTML"), Is.EqualTo("<div><span>hello</span></div>"));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"xpath=./span\"", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"span\"", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div >> internal:has=\"span >> text=wor\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> internal:has=\"span >> text=wor\"", "e => e.outerHTML"), Is.EqualTo("<div><span>world</span></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> internal:has=\"span >> text=wor\" >> span", "e => e.outerHTML"), Is.EqualTo("<span>world</span>"));

        var error1 = await Catch(() => Page.QuerySelectorAsync("div >> internal:has=abc"));
        Assert.That(error1.Message, Does.Contain("Malformed selector: internal:has=abc"));
        var error2 = await Catch(() => Page.QuerySelectorAsync("internal:has=\"div\""));
        Assert.That(error2.Message, Does.Contain("\"internal:has\" selector cannot be first"));
        var error3 = await Catch(() => Page.QuerySelectorAsync("div >> internal:has=33"));
        Assert.That(error3.Message, Does.Contain("Malformed selector: internal:has=33"));
        var error4 = await Catch(() => Page.QuerySelectorAsync("div >> internal:has=\"span!\""));
        Assert.That(error4.Message, Does.Contain("Unexpected token \"!\" while parsing css selector \"span!\""));
    }

    [UpstreamTest("SE-149 should work with internal:has-not=")]
    public async Task ShouldWorkWithInternalHasNot()
    {
        await Page.SetContentAsync("<section><span></span><div></div></section><section><br></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has-not=\"span\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has-not=\"span, div, br\"", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has-not=\"br\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has-not=\"span, div\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section >> internal:has-not=\"article\"", "els => els.length"), Is.EqualTo(2));
    }

    [UpstreamTest("SE-150 should work with internal:and=")]
    public async Task ShouldWorkWithInternalAnd()
    {
        await Page.SetContentAsync(@"
    <div class=foo>hello</div><div class=bar>world</div>
    <span class=foo>hello2</span><span class=bar>world2</span>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("div >> internal:and=\"span\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new string[] { }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("div >> internal:and=\".foo\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "hello" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("div >> internal:and=\".bar\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "world" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("span >> internal:and=\"span\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "hello2", "world2" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>(".foo >> internal:and=\"div\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "hello" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>(".bar >> internal:and=\"span\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "world2" }));
    }

    [UpstreamTest("SE-151 should work with internal:or=")]
    public async Task ShouldWorkWithInternalOr()
    {
        await Page.SetContentAsync(@"
    <div>hello</div>
    <span>world</span>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("div >> internal:or=\"span\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "hello", "world" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("span >> internal:or=\"div\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "hello", "world" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("article >> internal:or=\"something\"", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.Locator("article >> internal:or=\"div\"").TextContentAsync(), Is.EqualTo("hello"));
        Assert.That(await Page.Locator("article >> internal:or=\"span\"").TextContentAsync(), Is.EqualTo("world"));
        Assert.That(await Page.Locator("div >> internal:or=\"article\"").TextContentAsync(), Is.EqualTo("hello"));
        Assert.That(await Page.Locator("span >> internal:or=\"article\"").TextContentAsync(), Is.EqualTo("world"));
    }

    [UpstreamTest("SE-152 should work with internal:chain=")]
    public async Task ShouldWorkWithInternalChain()
    {
        await Page.SetContentAsync(@"
    <div>one <span>two</span> <button>three</button> </div>
    <span>four</span>
    <button>five</button>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("div >> internal:chain=\"button\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "three" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("div >> internal:chain=\"span >> internal:or=\\\"button\\\"\"", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "two", "three" }));
    }

    [UpstreamTest("SE-153 chaining should work with large DOM @smoke")]
    public async Task ChainingShouldWorkWithLargeDOM()
    {
        await Page.EvaluateAsync(@"() => {
    let last = document.body;
    for (let i = 0; i < 100; i++) {
      const e = document.createElement('div');
      last.appendChild(e);
      last = e;
    }
    const target = document.createElement('span');
    target.textContent = 'Found me!';
    last.appendChild(target);
  }");

        // Naive implementation generates C(100, 9) ~= 1.9*10^12 entries.
        var selectors = new[]
        {
            "div >> div >> div >> div >> div >> div >> div >> div >> span",
            "div div div div div div div div span",
            "div div >> div div >> div div >> div div >> span",
        };

        var counts = new List<int>();
        var times = new List<object>();
        foreach (var selector in selectors)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            counts.Add(await Page.EvalOnSelectorAllAsync<int>(selector, "els => els.length"));
            times.Add(new { selector, time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time });
        }
        Assert.That(counts, Is.EqualTo(new[] { 1, 1, 1 }));
        // Uncomment to see performance results.
        // console.log(times);
    }
}
