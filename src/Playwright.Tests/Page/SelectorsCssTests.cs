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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/selectors-css.spec.ts.</summary>
public class SelectorsCssTests : UpstreamPageTest
{
    [UpstreamTest("SE-001 should work with large DOM @smoke")]
    public async Task ShouldWorkWithLargeDOM()
    {
        await Page.EvaluateAsync(@"() => {
    let id = 0;
    const next = (tag) => {
      const e = document.createElement(tag);
      const eid = ++id;
      e.textContent = 'id' + eid;
      e.id = '' + eid;
      return e;
    };
    const generate = (depth) => {
      const div = next('div');
      const span1 = next('span');
      const span2 = next('span');
      div.appendChild(span1);
      div.appendChild(span2);
      if (depth > 0) {
        div.appendChild(generate(depth - 1));
        div.appendChild(generate(depth - 1));
      }
      return div;
    };
    document.body.appendChild(generate(12));
  }");
        var selectors = new[]
        {
            "div div div span",
            "div > div div > span",
            "div + div div div span + span",
            "div ~ div div > span ~ span",
            "div > div > div + div > div + div > span ~ span",
            "div div div div div div div div div div span",
            "div > div > div > div > div > div > div > div > div > div > span",
            "div ~ div div ~ div div ~ div div ~ div div ~ div span",
            "span",
        };

        var measure = false;
        foreach (var selector in selectors)
        {
            var counts1 = new List<int>();
            var time1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < (measure ? 10 : 1); i++)
            {
                counts1.Add(await Page.EvalOnSelectorAllAsync<int>(selector, "els => els.length"));
            }
            if (measure)
            {
                // console.log('pw: ' + (Date.now() - time1));
            }

            var time2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var counts2 = new List<int>();
            for (var i = 0; i < (measure ? 10 : 1); i++)
            {
                counts2.Add(await Page.EvaluateAsync<int>("selector => document.querySelectorAll(selector).length", selector));
            }
            if (measure)
            {
                // console.log('qs: ' + (Date.now() - time2));
            }

            Assert.That(counts1, Is.EqualTo(counts2));
        }
    }

    [UpstreamTest("SE-002 should work for open shadow roots")]
    public async Task ShouldWorkForOpenShadowRoots()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=span", "e => e.textContent"), Is.EqualTo("Hello from root1"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[attr=\"value\\ space\"]", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[attr='value\\ \\space']", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div div span", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div span + span", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=span + [attr*=\"value\"]", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[data-testid=\"foo\"] + [attr*=\"value\"]", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=#target", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div #target", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div div #target", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await Page.QuerySelectorAsync("css=div div div #target"), Is.Null);
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=section > div div span", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=section > div div span:nth-child(2)", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await Page.QuerySelectorAsync("css=section div div div div"), Is.Null);

        var root2 = await Page.QuerySelectorAsync("css=div div");
        Assert.That(await root2.EvalOnSelectorAsync<string>("css=#target", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await root2.QuerySelectorAsync("css:light=#target"), Is.Null);
        var root2Shadow = (IElementHandle)await root2.EvaluateHandleAsync("r => r.shadowRoot");
        Assert.That(await root2Shadow.EvalOnSelectorAsync<string>("css:light=#target", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        var root3 = (await Page.QuerySelectorAllAsync("css=div div"))[1];
        Assert.That(await root3.EvalOnSelectorAsync<string>("text=root3", "e => e.textContent"), Is.EqualTo("Hello from root3"));
        Assert.That(await root3.EvalOnSelectorAsync<string>("css=[attr*=\"value\"]", "e => e.textContent"), Is.EqualTo("Hello from root3 #2"));
        Assert.That(await root3.QuerySelectorAsync("css:light=[attr*=\"value\"]"), Is.Null);
    }

    [UpstreamTest("SE-003 should work with > combinator and spaces")]
    public async Task ShouldWorkWithCombinatorAndSpaces()
    {
        await Page.SetContentAsync("<div foo=\"bar\" bar=\"baz\"><span></span></div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"]> span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] >span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"]>span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"]   >    span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"]>    span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"]     >span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"]> span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] >span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"]>span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"]   >    span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"]>    span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"]     >span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
    }

    [UpstreamTest("SE-004 should work with comma separated list")]
    public async Task ShouldWorkWithCommaSeparatedList()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span,section #root1", "els => els.length"), Is.EqualTo(5));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=section #root1, div span", "els => els.length"), Is.EqualTo(5));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=doesnotexist , section #root1", "e => e.id"), Is.EqualTo("root1"));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=doesnotexist ,section #root1", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span,div span", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span,div span,div div span", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#target,[attr=\"value\\ space\"]", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#target,[data-testid=\"foo\"],[attr=\"value\\ space\"]", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#target,[data-testid=\"foo\"],[attr=\"value\\ space\"],span", "els => els.length"), Is.EqualTo(4));
    }

    [UpstreamTest("SE-005 should keep dom order with comma separated list")]
    public async Task ShouldKeepDomOrderWithCommaSeparatedList()
    {
        await Page.SetContentAsync("<section><span><div><x></x><y></y></div></span></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=span,div", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("SPAN,DIV"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=div,span", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("SPAN,DIV"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=span div, div", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("DIV"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("*css=section >> css=div,span", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("SECTION"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> *css=div >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("DIV"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> *css=div,span >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("SPAN,DIV"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> *css=div,span >> css=y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("SPAN,DIV"));
    }

    [UpstreamTest("SE-006 should return multiple captures for the same node")]
    public async Task ShouldReturnMultipleCapturesForTheSameNode()
    {
        await Page.SetContentAsync("<div><div><div><span></span></div></div></div>");
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("*css=div >> span", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("DIV,DIV,DIV"));
    }

    [UpstreamTest("SE-007 should return multiple captures when going up the hierarchy")]
    public async Task ShouldReturnMultipleCapturesWhenGoingUpTheHierarchy()
    {
        await Page.SetContentAsync("<section>Hello<ul><li></li><li></li></ul></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("*css=li >> ../.. >> text=Hello", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("LI,LI"));
    }

    [UpstreamTest("SE-008 should work with comma separated list in various positions")]
    public async Task ShouldWorkWithCommaSeparatedListInVariousPositions()
    {
        await Page.SetContentAsync("<section><span><div><x></x><y></y></div></span></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=span,div >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X,Y"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=span,div >> css=x", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=div >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X,Y"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=div >> css=x", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X"));

        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> css=div >> css=x", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> css=span >> css=div >> css=y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("Y"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> css=div >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X,Y"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> css=div,span >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X,Y"));
        Assert.That(await Page.EvalOnSelectorAllAsync<string>("css=section >> css=span >> css=x,y", "els => els.map(e => e.nodeName).join(',')"), Is.EqualTo("X,Y"));
    }

    [UpstreamTest("SE-009 should work with comma inside text")]
    public async Task ShouldWorkWithCommaInsideText()
    {
        await Page.SetContentAsync("<span></span><div attr=\"hello,world!\"></div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div[attr=\"hello,world!\"]", "e => e.outerHTML"), Is.EqualTo("<div attr=\"hello,world!\"></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[attr=\"hello,world!\"]", "e => e.outerHTML"), Is.EqualTo("<div attr=\"hello,world!\"></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div[attr='hello,world!']", "e => e.outerHTML"), Is.EqualTo("<div attr=\"hello,world!\"></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=[attr='hello,world!']", "e => e.outerHTML"), Is.EqualTo("<div attr=\"hello,world!\"></div>"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div[attr=\"hello,world!\"],span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
    }

    [UpstreamTest("SE-010 should work with attribute selectors")]
    public async Task ShouldWorkWithAttributeSelectors()
    {
        await Page.SetContentAsync("<div attr=\"hello world\" attr2=\"hello-''>>foo=bar[]\" attr3=\"] span\"><span></span></div>");
        await Page.EvaluateAsync("() => window['div'] = document.querySelector('div')");
        var selectors = new[]
        {
            "[attr=\"hello world\"]",
            "[attr = \"hello world\"]",
            "[attr ~= world]",
            "[attr ^=hello ]",
            "[attr $= world ]",
            "[attr *= \"llo wor\" ]",
            "[attr2 |= hello]",
            "[attr = \"Hello World\" i ]",
            "[attr *= \"llo WOR\"i]",
            "[attr $= woRLD i]",
            "[attr2 = \"hello-''>>foo=bar[]\"]",
            "[attr2 $=\"foo=bar[]\"]",
        };
        foreach (var selector in selectors)
        {
            Assert.That(await Page.EvalOnSelectorAsync<bool>(selector, "e => e === window['div']"), Is.EqualTo(true));
        }
        Assert.That(await Page.EvalOnSelectorAsync<bool>("[attr*=hello] span", "e => e.parentNode === window['div']"), Is.EqualTo(true));
        Assert.That(await Page.EvalOnSelectorAsync<bool>("[attr*=hello] >> span", "e => e.parentNode === window['div']"), Is.EqualTo(true));
        Assert.That(await Page.EvalOnSelectorAsync<bool>("[attr3=\"] span\"] >> span", "e => e.parentNode === window['div']"), Is.EqualTo(true));
    }

    [UpstreamTest("SE-011 should not match root after >>")]
    public async Task ShouldNotMatchRootAfter()
    {
        await Page.SetContentAsync("<section><div>test</div></section>");
        var element = await Page.QuerySelectorAsync("css=section >> css=section");
        Assert.That(element, Is.Null);
    }

    [UpstreamTest("SE-012 should work with numerical id")]
    public async Task ShouldWorkWithNumericalId()
    {
        await Page.SetContentAsync("<section id=\"123\"></section>");
        var element = await Page.QuerySelectorAsync("#\\31\\32\\33");
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("SE-013 should work with wrong-case id")]
    public async Task ShouldWorkWithWrongCaseId()
    {
        await Page.SetContentAsync("<section id=\"Hello\"></section>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("#Hello", "e => e.tagName"), Is.EqualTo("SECTION"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("#hello", "e => e.tagName"), Is.EqualTo("SECTION"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("#HELLO", "e => e.tagName"), Is.EqualTo("SECTION"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("#helLO", "e => e.tagName"), Is.EqualTo("SECTION"));
    }

    [UpstreamTest("SE-014 should work with *")]
    public async Task ShouldWorkWith_SE014()
    {
        await Page.SetContentAsync("<div id=div1></div><div id=div2><span><span></span></span></div>");
        // Includes html, head and body.
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*", "els => els.length"), Is.EqualTo(7));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*#div1", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*:not(#div1)", "els => els.length"), Is.EqualTo(6));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*:not(div)", "els => els.length"), Is.EqualTo(5));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*:not(span)", "els => els.length"), Is.EqualTo(5));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*:not(*)", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("*:is(*)", "els => els.length"), Is.EqualTo(7));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("* *", "els => els.length"), Is.EqualTo(6));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("* *:not(span)", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div > *", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("div *", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("* > *", "els => els.length"), Is.EqualTo(6));

        var body = await Page.QuerySelectorAsync("body");
        // Does not include html, head or body.
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*", "els => els.length"), Is.EqualTo(4));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*#div1", "els => els.length"), Is.EqualTo(1));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*:not(#div1)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*:not(div)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*:not(span)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*:not(*)", "els => els.length"), Is.EqualTo(0));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("*:is(*)", "els => els.length"), Is.EqualTo(4));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("div > *", "els => els.length"), Is.EqualTo(1));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("div *", "els => els.length"), Is.EqualTo(2));
        // Selectors v2 matches jquery in the sense that matching starts with the element scope,
        // not the document scope.
        Assert.That(await body.EvalOnSelectorAllAsync<int>("* > *", "els => els.length"), Is.EqualTo(2));
        // Adding scope makes querySelectorAll work like jquery.
        Assert.That(await body.EvalOnSelectorAllAsync<int>(":scope * > *", "els => els.length"), Is.EqualTo(2));
        // Note that the following two selectors are following jquery logic even
        // with selectors v1. Just running `body.querySelectorAll` returns 4 and 2 respectively.
        // That's probably a bug in v1, but oh well.
        Assert.That(await body.EvalOnSelectorAllAsync<int>("* *", "els => els.length"), Is.EqualTo(2));
        Assert.That(await body.EvalOnSelectorAllAsync<int>("* *:not(span)", "els => els.length"), Is.EqualTo(0));
    }

    [UpstreamTest("SE-015 should work with :nth-child")]
    public async Task ShouldWorkWithNthChild()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(odd)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(even)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(n+1)", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(n+2)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(2n)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(2n+1)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(-n)", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(-n+1)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(-n+2)", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(23n+2)", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-016 should work with :nth-child(of) notation with nested functions")]
    public async Task ShouldWorkWithNthChildOfNotationWithNestedFunctions()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 111, "https://caniuse.com/css-nth-child-of");

        await Page.SetContentAsync(@"
    <div>
      <span>span1</span>
      <span class=foo>span2<dd></dd></span>
      <span class=foo>span3<dd class=marker></dd></span>
      <span class=foo>span4<dd class=marker></dd></span>
      <span class=foo>span5<dd></dd></span>
      <span>span6</span>
    </div>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-child(1)", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span1" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-child(1 of .foo)", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span2" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-child(1 of .foo:has(dd.marker))", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span3" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-last-child(1 of .foo:has(dd.marker))", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span4" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-last-child(1 of .foo)", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span5" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-last-child(  1  )", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span6" }));

        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-child(1 of .foo:nth-child(3))", "els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "span3" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("css=span:nth-child(1 of .foo:nth-child(6))", "els => els.map(e => e.textContent)"), Is.EqualTo(Array.Empty<string>()));
    }

    [UpstreamTest("SE-017 should work with :not")]
    public async Task ShouldWorkWithNot()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:not(#root1)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=body :not(span)", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div > :not(span):not(div)", "els => els.length"), Is.EqualTo(0));
    }

    [UpstreamTest("SE-018 should work with ~")]
    public async Task ShouldWorkWith_SE018()
    {
        await Page.SetContentAsync(@"
    <div id=div1></div>
    <div id=div2></div>
    <div id=div3></div>
    <div id=div4></div>
    <div id=div5></div>
    <div id=div6></div>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> :scope ~ div", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div4", "div5", "div6" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> :scope ~ *", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div4", "div5", "div6" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> ~ div", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div4", "div5", "div6" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> ~ *", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div4", "div5", "div6" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> #div1 ~ :scope", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div3" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> #div4 ~ :scope", "els => els.map(e => e.id)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div1 ~ div ~ #div6", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div1 ~ div ~ div", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div3 ~ div ~ div", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div4 ~ div ~ div", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div5 ~ div ~ div", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div3 ~ #div2 ~ #div6", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div3 ~ #div4 ~ #div5", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-019 should work with +")]
    public async Task ShouldWorkWith_SE019()
    {
        await Page.SetContentAsync(@"
    <section>
      <div id=div1></div>
      <div id=div2></div>
      <div id=div3></div>
      <div id=div4></div>
      <div id=div5></div>
      <div id=div6></div>
    </section>
  ");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div1 >> :scope+div", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div2" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div1 >> :scope+*", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div2" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div1 >> + div", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div2" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div1 >> + *", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div2" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> div + :scope", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "div3" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#div3 >> #div1 + :scope", "els => els.map(e => e.id)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div1 ~ div + #div6", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div1 ~ div + div", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div3 + div + div", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div4 ~ #div5 + div", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div5 + div + div", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div3 ~ #div2 + #div6", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#div3 + #div4 + #div5", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div + #div1", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=section > div + div ~ div", "els => els.length"), Is.EqualTo(4));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=section > div + #div4 ~ div", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=section:has(:scope > div + #div2)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=section:has(:scope > div + #div1)", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAsync<string>("css=div:has(:scope + #div5)", "e => e.id"), Is.EqualTo("div4"));
    }

    [UpstreamTest("SE-020 should work with spaces in :nth-child and :not")]
    public async Task ShouldWorkWithSpacesInNthChildAndNot()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(23n +2)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child(23n+ 2)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:nth-child( 23n + 2 )", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:not(#root1 #target)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:not(:not(#root1 #target))", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span:not(span:not(#root1 #target))", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div > :not(span)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=body :not(span, div)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=span, section:not(span, div)", "els => els.length"), Is.EqualTo(5));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("span:nth-child(23n+ 2) >> xpath=.", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-021 should work with :is")]
    public async Task ShouldWorkWithIs()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(#root1)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(#root1, #target)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(span, #target)", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(span, #root1 > *)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(section div)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=:is(div, span)", "els => els.length"), Is.EqualTo(7));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=section:is(section) div:is(section div)", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=:is(div, span) > *", "els => els.length"), Is.EqualTo(6));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#root1:has(:is(#root1))", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=#root1:has(:is(:scope, #root1))", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-022 should work with :has")]
    public async Task ShouldWorkWithHas()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:has(#target)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:has([data-testid=foo])", "els => els.length"), Is.EqualTo(3));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:has([attr*=value])", "els => els.length"), Is.EqualTo(2));

        await Page.SetContentAsync("<section><span></span><div></div></section><section><br></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section:has(span, div)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section:has(span, div)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section:has(br)", "els => els.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section:has(span, br)", "els => els.length"), Is.EqualTo(2));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("section:has(span, br, div)", "els => els.length"), Is.EqualTo(2));
    }

    [UpstreamTest("SE-023 should work with :scope")]
    public async Task ShouldWorkWithScope()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        // 'is' does not change the scope, so it remains 'html'.
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(:scope#root1)", "els => els.length"), Is.EqualTo(0));
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:is(:scope #root1)", "els => els.length"), Is.EqualTo(1));
        // 'has' does change the scope, so it becomes the 'div' we are querying.
        Assert.That(await Page.EvalOnSelectorAllAsync<int>("css=div:has(:scope > #target)", "els => els.length"), Is.EqualTo(1));

        var handle = await Page.QuerySelectorAsync("css=span");
        // .NET: for (const scope of [page, handle]) - IPage and IElementHandle have no common type with EvalOnSelectorAllAsync
        foreach (var scope in new Func<string, string, Task<int>>[] { (selector, expression) => Page.EvalOnSelectorAllAsync<int>(selector, expression), (selector, expression) => handle.EvalOnSelectorAllAsync<int>(selector, expression) })
        {
            Assert.That(await scope("css=:scope", "els => els.length"), Is.EqualTo(1));
            Assert.That(await scope("css=* :scope", "els => els.length"), Is.EqualTo(0));
            Assert.That(await scope("css=* + :scope", "els => els.length"), Is.EqualTo(0));
            Assert.That(await scope("css=* > :scope", "els => els.length"), Is.EqualTo(0));
            Assert.That(await scope("css=* ~ :scope", "els => els.length"), Is.EqualTo(0));
        }

        await Page.SetContentAsync("<article><div class=target>hello<span></span></div></article>");
        // 'scope' should allow additional native css modifiers
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> :scope.target", "e => e.textContent"), Is.EqualTo("hello"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> :scope:nth-child(1)", "e => e.textContent"), Is.EqualTo("hello"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> :scope.target:has(span)", "e => e.textContent"), Is.EqualTo("hello"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("html:scope", "e => e.nodeName"), Is.EqualTo("HTML"));

        await Page.SetContentAsync("<section><span id=span1><span id=inner></span></span><span id=span2></span></section>");
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#span1 >> span:not(:has(:scope > div))", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "inner" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#span1 >> #inner,:scope", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "span1", "inner" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#span1 >> span,:scope", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "span1", "inner" }));
        Assert.That(await Page.EvalOnSelectorAllAsync<string[]>("#span1 >> span:not(:scope)", "els => els.map(e => e.id)"), Is.EqualTo(new[] { "inner" }));
        // TODO: the following two do not work. We do not expand the context for the inner :scope,
        // because we should only expand for one clause of :is() that contains :scope, but not the other.
        // expect(await page.$$eval(`#span1 >> span:is(:scope)`, els => els.map(e => e.id))).toEqual(['span1']);
        // expect(await page.$$eval(`#span1 >> span:is(:scope,#inner)`, els => els.map(e => e.id))).toEqual(['span1', 'inner']);
    }

    [UpstreamTest("SE-024 should work with :scope and class")]
    public async Task ShouldWorkWithScopeAndClass()
    {
        await Page.SetContentAsync(@"<div class=""apple""></div>
                         <div class=""apple selected""></div>");
        var apples = Page.Locator(".apple");
        var selectedApples = apples.Locator(":scope.selected");
        await Expect(selectedApples).ToHaveCountAsync(1);
    }

    [UpstreamTest("SE-025 should absolutize relative selectors")]
    public async Task ShouldAbsolutizeRelativeSelectors()
    {
        await Page.SetContentAsync("<div><span>Hi</span></div>");
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> >span", "e => e.textContent"), Is.EqualTo("Hi"));
        Assert.That(await Page.Locator("div").Locator(">span").TextContentAsync(), Is.EqualTo("Hi"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div:has(> span)", "e => e.outerHTML"), Is.EqualTo("<div><span>Hi</span></div>"));
        Assert.That(await Page.QuerySelectorAsync("div:has(> div)"), Is.Null);
    }

    [UpstreamTest("SE-026 css on the handle should be relative")]
    public async Task CssOnTheHandleShouldBeRelative()
    {
        await Page.SetContentAsync(@"
    <span class=""find-me"" id=target1>1</span>
    <div>
      <span class=""find-me"" id=target2>2</span>
    </div>
  ");
        Assert.That(await Page.EvalOnSelectorAsync<string>(".find-me", "e => e.id"), Is.EqualTo("target1"));

        var div = await Page.QuerySelectorAsync("div");
        Assert.That(await div.EvalOnSelectorAsync<string>(".find-me", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("div >> .find-me", "e => e.id"), Is.EqualTo("target2"));
    }

    [UpstreamTest("SE-027 should use light DOM structure for child combinator with slotted content")]
    public async Task ShouldUseLightDOMStructureForChildCombinatorWithSlottedContent()
    {
        await Page.SetContentAsync(@"
    <my-button>
      <template shadowrootmode=""open"">
        <button><slot></slot></button>
      </template>
      <div class=""content"">Foo</div>
    </my-button>
  ");
        Assert.That(await Page.EvalOnSelectorAsync<string>("my-button > div", "e => e.className"), Is.EqualTo("content"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("my-button > .content", "e => e.textContent"), Is.EqualTo("Foo"));
    }
}
