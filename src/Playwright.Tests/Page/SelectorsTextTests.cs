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

/// <summary>Port of tests/page/selectors-text.spec.ts.</summary>
public class SelectorsTextTests
{
    [UpstreamTest("SE-028 should work @smoke")]
    public async Task ShouldWork(IPage page)
    {
        await page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=ya", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"ya\"", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/^[ay]+$/", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/Ya/i", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=ye", "e => e.outerHTML"), Is.EqualTo("<div>\nye  </div>"));
        Assert.That(await page.GetByText("ye").EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));

        await page.SetContentAsync("<div> ye </div><div>ye</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"ye\"", "e => e.outerHTML"), Is.EqualTo("<div> ye </div>"));
        Assert.That(await page.GetByText("ye", new() { Exact = true }).First.EvaluateAsync<string>("e => e.outerHTML"), Does.Contain("> ye </div>"));

        await page.SetContentAsync("<div>yo</div><div>\"ya</div><div> hello world! </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"\\\"ya\"", "e => e.outerHTML"), Is.EqualTo("<div>\"ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/hello/", "e => e.outerHTML"), Is.EqualTo("<div> hello world! </div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/^\\s*heLLo/i", "e => e.outerHTML"), Is.EqualTo("<div> hello world! </div>"));

        await page.SetContentAsync("<div>yo<div>ya</div>hey<div>hey</div></div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=hey", "e => e.outerHTML"), Is.EqualTo("<div>hey</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=yo>>text=\"ya\"", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=yo>> text=\"ya\"", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=yo >>text='ya'", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=yo >> text='ya'", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("'yo'>>\"ya\"", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("\"yo\" >> 'ya'", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));

        await page.SetContentAsync("<div>yo<span id=\"s1\"></span></div><div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>");
        Assert.That(await page.EvalOnSelectorAllAsync<string>("text=yo", "es => es.map(e => e.outerHTML).join('\\n')"), Is.EqualTo("<div>yo<span id=\"s1\"></span></div>\n<div>yo<span id=\"s2\"></span><span id=\"s3\"></span></div>"));

        await page.SetContentAsync("<div>'</div><div>\"</div><div>\\</div><div>x</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text='\\''", "e => e.outerHTML"), Is.EqualTo("<div>'</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text='\"'", "e => e.outerHTML"), Is.EqualTo("<div>\"</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"\\\"\"", "e => e.outerHTML"), Is.EqualTo("<div>\"</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"'\"", "e => e.outerHTML"), Is.EqualTo("<div>'</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"\\x\"", "e => e.outerHTML"), Is.EqualTo("<div>x</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text='\\x'", "e => e.outerHTML"), Is.EqualTo("<div>x</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text='\\\\'", "e => e.outerHTML"), Is.EqualTo("<div>\\</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"\\\\\"", "e => e.outerHTML"), Is.EqualTo("<div>\\</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"", "e => e.outerHTML"), Is.EqualTo("<div>\"</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text='", "e => e.outerHTML"), Is.EqualTo("<div>'</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("\"x\"", "e => e.outerHTML"), Is.EqualTo("<div>x</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("'x'", "e => e.outerHTML"), Is.EqualTo("<div>x</div>"));
        var error = await Catch(() => page.QuerySelectorAsync("\""));
        Assert.That(error, Is.InstanceOf<Exception>());
        error = await Catch(() => page.QuerySelectorAsync("'"));
        Assert.That(error, Is.InstanceOf<Exception>());

        await page.SetContentAsync("<div> ' </div><div> \" </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"", "e => e.outerHTML"), Is.EqualTo("<div> \" </div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text='", "e => e.outerHTML"), Is.EqualTo("<div> ' </div>"));

        await page.SetContentAsync("<div>Hi''&gt;&gt;foo=bar</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hi''>>foo=bar\"", "e => e.outerHTML"), Is.EqualTo("<div>Hi''&gt;&gt;foo=bar</div>"));
        await page.SetContentAsync("<div>Hi'\"&gt;&gt;foo=bar</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hi'\\\">>foo=bar\"", "e => e.outerHTML"), Is.EqualTo("<div>Hi'\"&gt;&gt;foo=bar</div>"));

        await page.SetContentAsync("<div>Hi&gt;&gt;<span></span></div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hi>>\">>span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/Hi\\>\\>/ >> span", "e => e.outerHTML"), Is.EqualTo("<span></span>"));

        await page.SetContentAsync("<div>a<br>b</div><div>a</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=a", "e => e.outerHTML"), Is.EqualTo("<div>a<br>b</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=b", "e => e.outerHTML"), Is.EqualTo("<div>a<br>b</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=ab", "e => e.outerHTML"), Is.EqualTo("<div>a<br>b</div>"));
        Assert.That(await page.QuerySelectorAsync("text=abc"), Is.Null);
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=a", "els => els.length"), Is.EqualTo(2));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=b", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=ab", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=abc", "els => els.length"), Is.EqualTo(0));

        await page.SetContentAsync("<div></div><span></span>");
        await page.EvalOnSelectorAsync("div", @"div => {
    div.appendChild(document.createTextNode('hello'));
    div.appendChild(document.createTextNode('world'));
  }");
        await page.EvalOnSelectorAsync("span", @"span => {
    span.appendChild(document.createTextNode('hello'));
    span.appendChild(document.createTextNode('world'));
  }");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=lowo", "e => e.outerHTML"), Is.EqualTo("<div>helloworld</div>"));
        Assert.That(await page.EvalOnSelectorAllAsync<string>("text=lowo", "els => els.map(e => e.outerHTML).join('')"), Is.EqualTo("<div>helloworld</div><span>helloworld</span>"));

        await page.SetContentAsync("<span>Sign&nbsp;in</span><span>Hello\n \nworld</span>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=Sign in", "e => e.outerHTML"), Is.EqualTo("<span>Sign&nbsp;in</span>"));
        Assert.That((await page.QuerySelectorAllAsync("text=Sign \tin")).Count, Is.EqualTo(1));
        Assert.That((await page.QuerySelectorAllAsync("text=\"Sign in\"")).Count, Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=lo wo", "e => e.outerHTML"), Is.EqualTo("<span>Hello\n \nworld</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hello world\"", "e => e.outerHTML"), Is.EqualTo("<span>Hello\n \nworld</span>"));
        Assert.That(await page.QuerySelectorAsync("text=\"lo wo\""), Is.Null);
        Assert.That((await page.QuerySelectorAllAsync("text=lo \nwo")).Count, Is.EqualTo(1));
        Assert.That((await page.QuerySelectorAllAsync("text=\"lo \nwo\"")).Count, Is.EqualTo(0));

        await page.SetContentAsync("<div>let's<span>hello</span></div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/let's/i >> span", "e => e.outerHTML"), Is.EqualTo("<span>hello</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/let\\'s/i >> span", "e => e.outerHTML"), Is.EqualTo("<span>hello</span>"));
    }

    [UpstreamTest("SE-029 should work with :text")]
    public async Task ShouldWorkWithText(IPage page)
    {
        await page.SetContentAsync("<div>yo</div><div>ya</div><div>\nHELLO   \n world  </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"ya\")", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-is(\"ya\")", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"y\")", "e => e.outerHTML"), Is.EqualTo("<div>yo</div>"));
        Assert.That(await page.QuerySelectorAsync(":text-is(\"Y\")"), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"hello world\")", "e => e.outerHTML"), Is.EqualTo("<div>\nHELLO   \n world  </div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-is(\"HELLO world\")", "e => e.outerHTML"), Is.EqualTo("<div>\nHELLO   \n world  </div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"lo wo\")", "e => e.outerHTML"), Is.EqualTo("<div>\nHELLO   \n world  </div>"));
        Assert.That(await page.QuerySelectorAsync(":text-is(\"lo wo\")"), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-matches(\"^[ay]+$\")", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-matches(\"y\", \"g\")", "e => e.outerHTML"), Is.EqualTo("<div>yo</div>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-matches(\"Y\", \"i\")", "e => e.outerHTML"), Is.EqualTo("<div>yo</div>"));
        Assert.That(await page.QuerySelectorAsync(":text-matches(\"^y$\")"), Is.Null);

        var error1 = await Catch(() => page.QuerySelectorAsync(":text(\"foo\", \"bar\")"));
        Assert.That(error1.Message, Does.Contain("\"text\" engine expects a single string"));
        var error2 = await Catch(() => page.QuerySelectorAsync(":text(foo > bar)"));
        Assert.That(error2.Message, Does.Contain("\"text\" engine expects a single string"));
    }

    [UpstreamTest("SE-030 should support empty string")]
    public async Task ShouldSupportEmptyString(IPage page)
    {
        await page.SetContentAsync("<div></div><div>ya</div><div>\nHELLO   \n world  </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("div:text-is(\"\")", "e => e.outerHTML"), Is.EqualTo("<div></div>"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("div:text-is(\"\")", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:text(\"\")", "e => e.outerHTML"), Is.EqualTo("<div></div>"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("div:text(\"\")", "els => els.length"), Is.EqualTo(3));
        Assert.That(await page.EvalOnSelectorAsync<string>("div >> text=\"\"", "e => e.outerHTML"), Is.EqualTo("<div></div>"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("div >> text=\"\"", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("div >> text=/^$/", "e => e.outerHTML"), Is.EqualTo("<div></div>"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("div >> text=/^$/", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:text-matches(\"\")", "e => e.outerHTML"), Is.EqualTo("<div></div>"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("div:text-matches(\"\")", "els => els.length"), Is.EqualTo(3));
    }

    [UpstreamTest("SE-031 should work across nodes")]
    public async Task ShouldWorkAcrossNodes(IPage page)
    {
        await page.SetContentAsync("<div id=target1>Hello<i>,</i> <span id=target2>world</span><b>!</b></div>");

        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"Hello, world!\")", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"Hello\")", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"world\")", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>(":text(\"world\")", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.QuerySelectorAsync(":text(\"hello world\")"), Is.Null);
        Assert.That(await page.QuerySelectorAsync("div:text(\"world\")"), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>("text=Hello, world!", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=Hello", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=world", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=world", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.QuerySelectorAsync("text=hello world"), Is.Null);

        Assert.That(await page.QuerySelectorAsync(":text-is(\"Hello, world!\")"), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-is(\"Hello\")", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-is(\"world\")", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>(":text-is(\"world\")", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.QuerySelectorAsync("text=\"Hello, world!\""), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hello\"", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"world\"", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=\"world\"", "els => els.length"), Is.EqualTo(1));

        Assert.That(await page.EvalOnSelectorAsync<string>(":text-matches(\".*\")", "e => e.nodeName"), Is.EqualTo("I"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-matches(\"world?\")", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>(":text-matches(\"world\")", "els => els.length"), Is.EqualTo(1));
        Assert.That(await page.QuerySelectorAsync("div:text(\".*\")"), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/.*/", "e => e.nodeName"), Is.EqualTo("I"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=/world?/", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=/world/", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-032 text-is() should ignore comments")]
    public async Task TextIsShouldIgnoreComments(IPage page)
    {
        await page.SetContentAsync(@"<div id=me>hel<!-- comment -->lo
  <!-- comment -->
  world</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>(":text-is(\"hello world\")", "e => e.id"), Is.EqualTo("me"));
        Assert.That(await page.Locator("div", new() { HasText = "hello world" }).GetAttributeAsync("id"), Is.EqualTo("me"));
        Assert.That(await page.GetByText("hello world", new() { Exact = true }).GetAttributeAsync("id"), Is.EqualTo("me"));
    }

    [UpstreamTest("SE-033 should work with text nodes in quoted mode")]
    public async Task ShouldWorkWithTextNodesInQuotedMode(IPage page)
    {
        await page.SetContentAsync("<div id=target1>Hello<span id=target2>wo  rld  </span>  Hi again  </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hello\"", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"Hi again\"", "e => e.id"), Is.EqualTo("target1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=\"wo rld\"", "e => e.id"), Is.EqualTo("target2"));
        Assert.That(await page.QuerySelectorAsync("text=\"Hellowo rld Hi again\""), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=\"Hellowo\""), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=\"Hellowo rld\""), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=\"wo rld Hi ag\""), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=\"again\""), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=\"hi again\""), Is.Null);
        Assert.That(await page.EvalOnSelectorAsync<string>("text=hi again", "e => e.id"), Is.EqualTo("target1"));
    }

    [UpstreamTest("SE-034 should clear caches")]
    public async Task ShouldClearCaches(IPage page)
    {
        await page.SetContentAsync("<div id=target1>text</div><div id=target2>text</div>");
        var div = await page.QuerySelectorAsync("#target1");

        await div.EvaluateAsync("div => div.textContent = 'text'");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=text", "e => e.id"), Is.EqualTo("target1"));
        await div.EvaluateAsync("div => div.textContent = 'foo'");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=text", "e => e.id"), Is.EqualTo("target2"));

        await div.EvaluateAsync("div => div.textContent = 'text'");
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"text\")", "e => e.id"), Is.EqualTo("target1"));
        await div.EvaluateAsync("div => div.textContent = 'foo'");
        Assert.That(await page.EvalOnSelectorAsync<string>(":text(\"text\")", "e => e.id"), Is.EqualTo("target2"));

        await div.EvaluateAsync("div => div.textContent = 'text'");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=text", "els => els.length"), Is.EqualTo(2));
        await div.EvaluateAsync("div => div.textContent = 'foo'");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("text=text", "els => els.length"), Is.EqualTo(1));

        await div.EvaluateAsync("div => div.textContent = 'text'");
        Assert.That(await page.EvalOnSelectorAllAsync<int>(":text(\"text\")", "els => els.length"), Is.EqualTo(2));
        await div.EvaluateAsync("div => div.textContent = 'foo'");
        Assert.That(await page.EvalOnSelectorAllAsync<int>(":text(\"text\")", "els => els.length"), Is.EqualTo(1));
    }

    [UpstreamTest("SE-035 should work with :has-text")]
    public async Task ShouldWorkWithHasText(IPage page)
    {
        await page.SetContentAsync(@"
    <input id=input2>
    <div id=div1>
      <span>  Find me  </span>
      or
      <wrap><span id=span2>maybe me  </span></wrap>
      <div><input id=input1></div>
    </div>
  ");
        Assert.That(await page.EvalOnSelectorAsync<string>(":has-text(\"find me\")", "e => e.tagName"), Is.EqualTo("HTML"));
        Assert.That(await page.EvalOnSelectorAsync<string>("span:has-text(\"find me\")", "e => e.outerHTML"), Is.EqualTo("<span>  Find me  </span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"find me\")", "e => e.id"), Is.EqualTo("div1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"find me\") input", "e => e.id"), Is.EqualTo("input1"));
        Assert.That(await page.EvalOnSelectorAsync<string>(":has-text(\"find me\") input", "e => e.id"), Is.EqualTo("input2"));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"find me or maybe me\")", "e => e.id"), Is.EqualTo("div1"));
        Assert.That(await page.QuerySelectorAsync("div:has-text(\"find noone\")"), Is.Null);
        Assert.That(await page.EvalOnSelectorAllAsync<string>(":is(div,span):has-text(\"maybe\")", "els => els.map(e => e.id).join(';')"), Is.EqualTo("div1;span2"));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"find me\") :has-text(\"maybe me\")", "e => e.tagName"), Is.EqualTo("WRAP"));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"find me\") span:has-text(\"maybe me\")", "e => e.id"), Is.EqualTo("span2"));

        await page.SetContentAsync(@"<div id=me>hello
  wo""r>>ld</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"hello wo\\\"r>>ld\")", "e => e.id"), Is.EqualTo("me"));
        Assert.That(await page.EvalOnSelectorAsync<string>("div:has-text(\"hello\\a wo\\\"r>>ld\")", "e => e.id"), Is.EqualTo("me"));
        Assert.That(await page.Locator("div", new() { HasText = "hello\nwo\"r>>ld" }).GetAttributeAsync("id"), Is.EqualTo("me"));

        var error1 = await Catch(() => page.QuerySelectorAsync(":has-text(\"foo\", \"bar\")"));
        Assert.That(error1.Message, Does.Contain("\"has-text\" engine expects a single string"));
        var error2 = await Catch(() => page.QuerySelectorAsync(":has-text(foo > bar)"));
        Assert.That(error2.Message, Does.Contain("\"has-text\" engine expects a single string"));
    }

    [UpstreamTest("SE-036 should work with large DOM")]
    public async Task ShouldWorkWithLargeDOM(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    let id = 0;
    const next = (tag) => {
      const e = document.createElement(tag);
      const eid = ++id;
      e.textContent = 'id' + eid;
      e.id = 'id' + eid;
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
            ":has-text(\"id18\")",
            ":has-text(\"id12345\")",
            ":has-text(\"id\")",
            ":text(\"id18\")",
            ":text(\"id12345\")",
            ":text(\"id\")",
            ":text-matches(\"id12345\", \"i\")",
            "text=id18",
            "text=id12345",
            "text=id",
            "#id18",
            "#id12345",
            "*",
        };

        var measure = false;
        foreach (var selector in selectors)
        {
            var time1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (var i = 0; i < (measure ? 10 : 1); i++)
            {
                await page.EvalOnSelectorAllAsync(selector, "els => els.length");
            }
            if (measure)
            {
                // console.log(`pw("${selector}"): ` + (Date.now() - time1));
            }

            if (measure && !selector.Contains("text", StringComparison.Ordinal))
            {
                var time2 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                for (var i = 0; i < (measure ? 10 : 1); i++)
                {
                    await page.EvaluateAsync("selector => document.querySelectorAll(selector).length", selector);
                }
                // console.log(`qs("${selector}"): ` + (Date.now() - time2));
            }
        }
    }

    [UpstreamTest("SE-037 should be case sensitive if quotes are specified")]
    public async Task ShouldBeCaseSensitiveIfQuotesAreSpecified(IPage page)
    {
        await page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=yA", "e => e.outerHTML"), Is.EqualTo("<div>ya</div>"));
        Assert.That(await page.QuerySelectorAsync("text=\"yA\""), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text= \"ya\""), Is.Null);
    }

    [UpstreamTest("SE-038 should search for a substring without quotes")]
    public async Task ShouldSearchForASubstringWithoutQuotes(IPage page)
    {
        await page.SetContentAsync("<div>textwithsubstring</div>");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=with", "e => e.outerHTML"), Is.EqualTo("<div>textwithsubstring</div>"));
        Assert.That(await page.QuerySelectorAsync("text=\"with\""), Is.Null);
    }

    [UpstreamTest("SE-039 should skip head, script and style")]
    public async Task ShouldSkipHeadScriptAndStyle(IPage page)
    {
        await page.SetContentAsync(@"
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
        var head = await page.QuerySelectorAsync("head");
        var title = await page.QuerySelectorAsync("title");
        var script = await page.QuerySelectorAsync("body script");
        var style = await page.QuerySelectorAsync("body style");
        foreach (var text in new[] { "title", "script", "style" })
        {
            Assert.That(await page.EvalOnSelectorAsync<string>($"text={text}", "e => e.nodeName"), Is.EqualTo("DIV"));
            Assert.That(await page.EvalOnSelectorAllAsync<string>($"text={text}", "els => els.map(e => e.nodeName).join('|')"), Is.EqualTo("DIV"));
            foreach (var root in new[] { head, title, script, style })
            {
                Assert.That(await root.QuerySelectorAsync($"text={text}"), Is.Null);
                Assert.That(await root.EvalOnSelectorAllAsync<int>($"text={text}", "els => els.length"), Is.EqualTo(0));
            }
        }
    }

    [UpstreamTest("SE-040 should match input[type=button|submit|reset]")]
    public async Task ShouldMatchInputTypeButtonSubmitReset(IPage page)
    {
        await page.SetContentAsync("<input type=\"submit\" value=\"hello\"><input type=\"button\" value=\"world\"><input type=\"reset\" value=\"clear\">");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=hello", "e => e.outerHTML"), Is.EqualTo("<input type=\"submit\" value=\"hello\">"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=world", "e => e.outerHTML"), Is.EqualTo("<input type=\"button\" value=\"world\">"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=clear", "e => e.outerHTML"), Is.EqualTo("<input type=\"reset\" value=\"clear\">"));
    }

    [UpstreamTest("SE-041 should work for open shadow roots")]
    public async Task ShouldWorkForOpenShadowRoots(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/deep-shadow.html");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=root1", "e => e.textContent"), Is.EqualTo("Hello from root1"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=root2", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=root3", "e => e.textContent"), Is.EqualTo("Hello from root3"));
        Assert.That(await page.EvalOnSelectorAsync<string>("#root1 >> text=from root3", "e => e.textContent"), Is.EqualTo("Hello from root3"));
        Assert.That(await page.EvalOnSelectorAsync<string>("#target >> text=from root2", "e => e.textContent"), Is.EqualTo("Hello from root2"));
        Assert.That(await page.QuerySelectorAsync("text:light=root1"), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text:light=root2"), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text:light=root3"), Is.Null);
    }

    [UpstreamTest("SE-042 should prioritize light dom over shadow dom in the same parent")]
    public async Task ShouldPrioritizeLightDomOverShadowDomInTheSameParent(IPage page, Server server)
    {
        await page.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    document.body.appendChild(div);

    div.attachShadow({ mode: 'open' });
    const shadowSpan = document.createElement('span');
    shadowSpan.textContent = 'Hello from shadow';
    div.shadowRoot.appendChild(shadowSpan);

    const lightSpan = document.createElement('span');
    lightSpan.textContent = 'Hello from light';
    div.appendChild(lightSpan);
  }");
        Assert.That(await page.EvalOnSelectorAsync<string>("div >> text=Hello", "e => e.textContent"), Is.EqualTo("Hello from light"));
    }

    [UpstreamTest("SE-043 should waitForSelector with distributed elements")]
    public async Task ShouldWaitForSelectorWithDistributedElements(IPage page, Server server)
    {
        var promise = page.WaitForSelectorAsync("div >> text=Hello");
        await page.EvaluateAsync(@"() => {
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
  }");
        var handle = await promise;
        Assert.That(await handle.TextContentAsync(), Is.EqualTo("Hello from light"));
    }

    [UpstreamTest("SE-044 should match root after >>")]
    public async Task ShouldMatchRootAfter(IPage page, Server server)
    {
        await page.SetContentAsync("<section>test</section>");
        var element = await page.QuerySelectorAsync("css=section >> text=test");
        Assert.That(element, Is.Not.Null);
        var element2 = await page.QuerySelectorAsync("text=test >> text=test");
        Assert.That(element2, Is.Not.Null);
    }

    [UpstreamTest("SE-045 should match root after >> with *")]
    public async Task ShouldMatchRootAfterWith(IPage page)
    {
        await page.SetContentAsync("<button> hello world </button> <button> hellow <span> world </span> </button>");
        Assert.That(await page.EvalOnSelectorAllAsync<int>("*css=button >> text=hello >> text=world", "els => els.length"), Is.EqualTo(2));
    }

    [UpstreamTest("SE-046 should work with leading and trailing spaces")]
    public async Task ShouldWorkWithLeadingAndTrailingSpaces(IPage page)
    {
        await page.SetContentAsync("<button> Add widget </button>");
        await Expect(page.Locator("text=Add widget")).ToBeVisibleAsync();
        await Expect(page.Locator("text= Add widget ")).ToBeVisibleAsync();
    }

    [UpstreamTest("SE-047 should work with unpaired quotes when not at the start")]
    public async Task ShouldWorkWithUnpairedQuotesWhenNotAtTheStart(IPage page)
    {
        await page.SetContentAsync(@"
    <div>hello""world<span>yay</span></div>
    <div>hello'world<span>nay</span></div>
    <div>hello`world<span>oh</span></div>
    <div>hello`world<span>oh2</span></div>
  ");
        Assert.That(await page.EvalOnSelectorAsync<string>("text=lo\" >> span", "e => e.outerHTML"), Is.EqualTo("<span>yay</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("  text=lo\" >> span", "e => e.outerHTML"), Is.EqualTo("<span>yay</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text  =lo\" >> span", "e => e.outerHTML"), Is.EqualTo("<span>yay</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=  lo\" >> span", "e => e.outerHTML"), Is.EqualTo("<span>yay</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>(" text = lo\" >> span", "e => e.outerHTML"), Is.EqualTo("<span>yay</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=o\"wor >> span", "e => e.outerHTML"), Is.EqualTo("<span>yay</span>"));

        Assert.That(await page.EvalOnSelectorAsync<string>("text=lo'wor >> span", "e => e.outerHTML"), Is.EqualTo("<span>nay</span>"));
        Assert.That(await page.EvalOnSelectorAsync<string>("text=o' >> span", "e => e.outerHTML"), Is.EqualTo("<span>nay</span>"));

        Assert.That(await page.EvalOnSelectorAsync<string>("text=ello`wor >> span", "e => e.outerHTML"), Is.EqualTo("<span>oh</span>"));
        await Expect(page.Locator("text=ello`wor").Locator("span").First).ToHaveTextAsync("oh");
        await Expect(page.Locator("text=ello`wor").Locator("span").Nth(1)).ToHaveTextAsync("oh2");

        Assert.That(await page.QuerySelectorAsync("text='wor >> span"), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=\" >> span"), Is.Null);
        Assert.That(await page.QuerySelectorAsync("text=` >> span"), Is.Null);
    }

    [UpstreamTest("SE-048 should work with paired quotes in the middle of selector")]
    public async Task ShouldWorkWithPairedQuotesInTheMiddleOfSelector(IPage page)
    {
        await page.SetContentAsync("<div>pattern \"^-?\\d+$\"</div>");
        // .NET: upstream calls expect() without a matcher here: the call is made, nothing is checked.
        await page.Locator("div >> text=pattern \"^-?\\d+$").IsVisibleAsync();
        await page.Locator("div >> text=pattern \"^-?\\d+$\"").IsVisibleAsync();
        // Should double escape inside quoted text.
        await page.Locator("div >> text='pattern \"^-?\\\\d+$\"'").IsVisibleAsync();
        await Expect(page.Locator("div >> text=pattern \"^-?\\d+$")).ToBeVisibleAsync();
        await Expect(page.Locator("div >> text=pattern \"^-?\\d+$\"")).ToBeVisibleAsync();
        // Should double escape inside quoted text.
        await Expect(page.Locator("div >> text='pattern \"^-?\\\\d+$\"'")).ToBeVisibleAsync();
    }

    [UpstreamTest("SE-049 hasText and internal:text should match full node text in strict mode")]
    public async Task HasTextAndInternalTextShouldMatchFullNodeTextInStrictMode(IPage page)
    {
        await page.SetContentAsync(@"
    <div id=div1>hello<span>world</span></div>
    <div id=div2>hello</div>
  ");
        await Expect(page.GetByText("helloworld", new() { Exact = true })).ToHaveIdAsync("div1");
        await Expect(page.GetByText("hello", new() { Exact = true })).ToHaveIdAsync("div2");
        await Expect(page.Locator("div", new() { HasTextRegex = new Regex("^helloworld$") })).ToHaveIdAsync("div1");
        await Expect(page.Locator("div", new() { HasTextRegex = new Regex("^hello$") })).ToHaveIdAsync("div2");

        await page.SetContentAsync(@"
    <div id=div1><span id=span1>hello</span>world</div>
    <div id=div2><span id=span2>hello</span></div>
  ");
        await Expect(page.GetByText("helloworld", new() { Exact = true })).ToHaveIdAsync("div1");
        Assert.That(await page.GetByText("hello", new() { Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.id)"), Is.EqualTo(new[] { "span1", "span2" }));
        await Expect(page.Locator("div", new() { HasTextRegex = new Regex("^helloworld$") })).ToHaveIdAsync("div1");
        await Expect(page.Locator("div", new() { HasTextRegex = new Regex("^hello$") })).ToHaveIdAsync("div2");
    }
}
