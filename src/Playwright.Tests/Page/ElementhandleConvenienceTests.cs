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

/// <summary>Port of tests/page/elementhandle-convenience.spec.ts.</summary>
public class ElementhandleConvenienceTests
{
    [UpstreamTest("EH-024 should have a nice preview")]
    public async Task ShouldHaveANicePreview(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var outer = await page.QuerySelectorAsync("#outer");
        var inner = await page.QuerySelectorAsync("#inner");
        var check = await page.QuerySelectorAsync("#check");
        var text = await inner.EvaluateHandleAsync("e => e.firstChild");
        await page.EvaluateAsync("() => 1");  // Give them a chance to calculate the preview.
        Assert.That(outer.ToString(), Is.EqualTo("JSHandle@<div id=\"outer\" name=\"value\">…</div>"));
        Assert.That(inner.ToString(), Is.EqualTo("JSHandle@<div id=\"inner\">Text,↵more text</div>"));
        Assert.That(text.ToString(), Is.EqualTo("JSHandle@#text=Text,↵more text"));
        Assert.That(check.ToString(), Is.EqualTo("JSHandle@<input checked id=\"check\" foo=\"bar\"\" type=\"checkbox\"/>"));
    }

    [UpstreamTest("EH-025 should have a nice preview for non-ascii attributes/children")]
    public async Task ShouldHaveANicePreviewForNonAsciiAttributesChildren(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<div title=\"{string.Concat(Enumerable.Repeat("😛", 100))}\">{string.Concat(Enumerable.Repeat("😛", 100))}");
        var handle = await page.QuerySelectorAsync("div");
        await Expect(() => Assert.That(handle.ToString(), Is.EqualTo("JSHandle@<div title=\"😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛\">😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛…</div>"))).ToPassAsync();
    }

    [UpstreamTest("EH-026 getAttribute should work")]
    public async Task GetAttributeShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var handle = await page.QuerySelectorAsync("#outer");
        Assert.That(await handle.GetAttributeAsync("name"), Is.EqualTo("value"));
        Assert.That(await handle.GetAttributeAsync("foo"), Is.Null);
        Assert.That(await page.GetAttributeAsync("#outer", "name"), Is.EqualTo("value"));
        Assert.That(await page.GetAttributeAsync("#outer", "foo"), Is.Null);
    }

    [UpstreamTest("EH-027 inputValue should work")]
    public async Task InputValueShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");

        await page.SelectOptionAsync("#select", "foo");
        Assert.That(await page.InputValueAsync("#select"), Is.EqualTo("foo"));

        await page.FillAsync("#textarea", "text value");
        Assert.That(await page.InputValueAsync("#textarea"), Is.EqualTo("text value"));

        await page.FillAsync("#input", "input value");
        Assert.That(await page.InputValueAsync("#input"), Is.EqualTo("input value"));
        var handle = await page.QuerySelectorAsync("#input");
        Assert.That(await handle.InputValueAsync(), Is.EqualTo("input value"));

        Assert.That((await Catch(() => page.InputValueAsync("#inner"))).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
        var handle2 = await page.QuerySelectorAsync("#inner");
        Assert.That((await Catch(() => handle2.InputValueAsync())).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
    }

    [UpstreamTest("EH-028 innerHTML should work")]
    public async Task InnerHTMLShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var handle = await page.QuerySelectorAsync("#outer");
        Assert.That(await handle.InnerHTMLAsync(), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
        Assert.That(await page.InnerHTMLAsync("#outer"), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
    }

    [UpstreamTest("EH-029 innerText should work")]
    public async Task InnerTextShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var handle = await page.QuerySelectorAsync("#inner");
        Assert.That(await handle.InnerTextAsync(), Is.EqualTo("Text, more text"));
        Assert.That(await page.InnerTextAsync("#inner"), Is.EqualTo("Text, more text"));
    }

    [UpstreamTest("EH-030 innerText should throw")]
    public async Task InnerTextShouldThrow(IPage page, Server server)
    {
        await page.SetContentAsync("<svg>text</svg>");
        var error1 = await Catch(() => page.InnerTextAsync("svg"));
        Assert.That(error1.Message, Does.Contain("Node is not an HTMLElement"));
        var handle = await page.QuerySelectorAsync("svg");
        var error2 = await Catch(() => handle.InnerTextAsync());
        Assert.That(error2.Message, Does.Contain("Node is not an HTMLElement"));
    }

    [UpstreamTest("EH-031 textContent should work")]
    public async Task TextContentShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var handle = await page.QuerySelectorAsync("#inner");
        Assert.That(await handle.TextContentAsync(), Is.EqualTo("Text,\nmore text"));
        Assert.That(await page.TextContentAsync("#inner"), Is.EqualTo("Text,\nmore text"));
    }

    [UpstreamTest("EH-032 textContent should work on ShadowRoot")]
    public async Task TextContentShouldWorkOnShadowRoot(IPage page, Server server)
    {
        await page.SetContentAsync(@"
    <div></div>
    <script>
      document.querySelector('div').attachShadow({ mode: 'open' }).innerHTML = '<div>hello</div>';
    </script>
  ");
        var div = await page.QuerySelectorAsync("div");
        var root = (IElementHandle)await div.EvaluateHandleAsync("div => div.shadowRoot");
        Assert.That(await root.TextContentAsync(), Is.EqualTo("hello"));
        // We do not match ShadowRoot as ":scope".
        Assert.That(await root.QuerySelectorAllAsync(":scope div"), Is.EqualTo(Array.Empty<IElementHandle>()));
    }

    [UpstreamTest("EH-033 isVisible and isHidden should work")]
    public async Task IsVisibleAndIsHiddenShouldWork(IPage page)
    {
        await page.SetContentAsync("<div>Hi</div><span></span>");

        var div = await page.QuerySelectorAsync("div");
        Assert.That(await div.IsVisibleAsync(), Is.EqualTo(true));
        Assert.That(await div.IsHiddenAsync(), Is.EqualTo(false));
        Assert.That(await page.IsVisibleAsync("div"), Is.EqualTo(true));
        Assert.That(await page.IsHiddenAsync("div"), Is.EqualTo(false));

        var span = await page.QuerySelectorAsync("span");
        Assert.That(await span.IsVisibleAsync(), Is.EqualTo(false));
        Assert.That(await span.IsHiddenAsync(), Is.EqualTo(true));
        Assert.That(await page.IsVisibleAsync("span"), Is.EqualTo(false));
        Assert.That(await page.IsHiddenAsync("span"), Is.EqualTo(true));

        Assert.That(await page.IsVisibleAsync("no-such-element"), Is.EqualTo(false));
        Assert.That(await page.IsHiddenAsync("no-such-element"), Is.EqualTo(true));
    }

    [UpstreamTest("EH-034 isVisible should not throw when the DOM element is not connected")]
    public async Task IsVisibleShouldNotThrowWhenTheDOMElementIsNotConnected(IPage page)
    {
        await page.SetContentAsync("<div id=\"root\"></div>");
        await page.EvaluateAsync(@"() => {
    function insert() {
      document.getElementById('root').innerHTML = '<div id=""problem"">Problem</div>';
      window.requestAnimationFrame(remove);
    }
    function remove() {
      const node = document.getElementById('problem');
      node?.parentNode?.removeChild(node);
      window.requestAnimationFrame(insert);
    }
    window.requestAnimationFrame(insert);
  }");

        for (var i = 0; i < 10; i++)
        {
            await page.IsVisibleAsync("#problem");
        }
    }

    [UpstreamTest("EH-035 isEnabled and isDisabled should work")]
    public async Task IsEnabledAndIsDisabledShouldWork(IPage page)
    {
        await page.SetContentAsync(@"
    <button disabled>button1</button>
    <button>button2</button>
    <div>div</div>
  ");
        var div = await page.QuerySelectorAsync("div");
        Assert.That(await div.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await div.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEnabledAsync("div"), Is.EqualTo(true));
        Assert.That(await page.IsDisabledAsync("div"), Is.EqualTo(false));
        var button1 = await page.QuerySelectorAsync(":text(\"button1\")");
        Assert.That(await button1.IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await button1.IsDisabledAsync(), Is.EqualTo(true));
        Assert.That(await page.IsEnabledAsync(":text(\"button1\")"), Is.EqualTo(false));
        Assert.That(await page.IsDisabledAsync(":text(\"button1\")"), Is.EqualTo(true));
        var button2 = await page.QuerySelectorAsync(":text(\"button2\")");
        Assert.That(await button2.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await button2.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEnabledAsync(":text(\"button2\")"), Is.EqualTo(true));
        Assert.That(await page.IsDisabledAsync(":text(\"button2\")"), Is.EqualTo(false));
    }

    [UpstreamTest("EH-036 isEnabled and isDisabled should work with <select/> option/optgroup correctly")]
    public async Task IsEnabledAndIsDisabledShouldWorkWithSelectOptionOptgroupCorrectly(IPage page)
    {
        await page.SetContentAsync(@"
    <select name=""select"">
      <option id=""enabled1"" value=""1"">Enabled</option>
      <option id=""disabled1"" value=""2"" disabled>Disabled</option>
      <optgroup label=""Foo1"">
        <option value=""mercedes"">Mercedes</option>
      </optgroup>
      <optgroup label=""Foo2"" disabled>
        <option value=""mercedes"">Mercedes</option>
      </optgroup>
    </select>
  ");
        Assert.That(await (await page.QuerySelectorAsync("#enabled1")).IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await (await page.QuerySelectorAsync("#enabled1")).IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await (await page.QuerySelectorAsync("#disabled1")).IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await (await page.QuerySelectorAsync("#disabled1")).IsDisabledAsync(), Is.EqualTo(true));
        Assert.That(await (await page.QuerySelectorAsync("optgroup >> nth=0")).IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await (await page.QuerySelectorAsync("optgroup >> nth=0")).IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await (await page.QuerySelectorAsync("optgroup >> nth=1")).IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await (await page.QuerySelectorAsync("optgroup >> nth=1")).IsDisabledAsync(), Is.EqualTo(true));
    }

    [UpstreamTest("EH-037 isEditable should work")]
    public async Task IsEditableShouldWork(IPage page)
    {
        await page.SetContentAsync("<input id=input1 disabled><textarea></textarea><input id=input2>");
        await page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");
        var input1 = await page.QuerySelectorAsync("#input1");
        Assert.That(await input1.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEditableAsync("#input1"), Is.EqualTo(false));
        var input2 = await page.QuerySelectorAsync("#input2");
        Assert.That(await input2.IsEditableAsync(), Is.EqualTo(true));
        Assert.That(await page.IsEditableAsync("#input2"), Is.EqualTo(true));
        var textarea = await page.QuerySelectorAsync("textarea");
        Assert.That(await textarea.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEditableAsync("textarea"), Is.EqualTo(false));
    }

    [UpstreamTest("EH-038 isChecked should work")]
    public async Task IsCheckedShouldWork(IPage page)
    {
        await page.SetContentAsync("<input type='checkbox' checked><div>Not a checkbox</div>");
        var handle = await page.QuerySelectorAsync("input");
        Assert.That(await handle.IsCheckedAsync(), Is.EqualTo(true));
        Assert.That(await page.IsCheckedAsync("input"), Is.EqualTo(true));
        await handle.EvaluateAsync("input => input.checked = false");
        Assert.That(await handle.IsCheckedAsync(), Is.EqualTo(false));
        Assert.That(await page.IsCheckedAsync("input"), Is.EqualTo(false));
        var error = await Catch(() => page.IsCheckedAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }
}
