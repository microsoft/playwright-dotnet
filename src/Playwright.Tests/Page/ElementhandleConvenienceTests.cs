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
public class ElementhandleConvenienceTests : UpstreamPageTest
{
    [UpstreamTest("EH-024 should have a nice preview")]
    public async Task ShouldHaveANicePreview()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var outer = await Page.QuerySelectorAsync("#outer");
        var inner = await Page.QuerySelectorAsync("#inner");
        var check = await Page.QuerySelectorAsync("#check");
        var text = await inner.EvaluateHandleAsync("e => e.firstChild");
        await Page.EvaluateAsync("() => 1");  // Give them a chance to calculate the preview.
        Assert.That(outer.ToString(), Is.EqualTo("JSHandle@<div id=\"outer\" name=\"value\">…</div>"));
        Assert.That(inner.ToString(), Is.EqualTo("JSHandle@<div id=\"inner\">Text,↵more text</div>"));
        Assert.That(text.ToString(), Is.EqualTo("JSHandle@#text=Text,↵more text"));
        Assert.That(check.ToString(), Is.EqualTo("JSHandle@<input checked id=\"check\" foo=\"bar\"\" type=\"checkbox\"/>"));
    }

    [UpstreamTest("EH-025 should have a nice preview for non-ascii attributes/children")]
    public async Task ShouldHaveANicePreviewForNonAsciiAttributesChildren()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<div title=\"{string.Concat(Enumerable.Repeat("😛", 100))}\">{string.Concat(Enumerable.Repeat("😛", 100))}");
        var handle = await Page.QuerySelectorAsync("div");
        await Expect(() => Assert.That(handle.ToString(), Is.EqualTo("JSHandle@<div title=\"😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛\">😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛😛…</div>"))).ToPassAsync();
    }

    [UpstreamTest("EH-026 getAttribute should work")]
    public async Task GetAttributeShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var handle = await Page.QuerySelectorAsync("#outer");
        Assert.That(await handle.GetAttributeAsync("name"), Is.EqualTo("value"));
        Assert.That(await handle.GetAttributeAsync("foo"), Is.Null);
        Assert.That(await Page.GetAttributeAsync("#outer", "name"), Is.EqualTo("value"));
        Assert.That(await Page.GetAttributeAsync("#outer", "foo"), Is.Null);
    }

    [UpstreamTest("EH-027 inputValue should work")]
    public async Task InputValueShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");

        await Page.SelectOptionAsync("#select", "foo");
        Assert.That(await Page.InputValueAsync("#select"), Is.EqualTo("foo"));

        await Page.FillAsync("#textarea", "text value");
        Assert.That(await Page.InputValueAsync("#textarea"), Is.EqualTo("text value"));

        await Page.FillAsync("#input", "input value");
        Assert.That(await Page.InputValueAsync("#input"), Is.EqualTo("input value"));
        var handle = await Page.QuerySelectorAsync("#input");
        Assert.That(await handle.InputValueAsync(), Is.EqualTo("input value"));

        Assert.That((await Catch(() => Page.InputValueAsync("#inner"))).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
        var handle2 = await Page.QuerySelectorAsync("#inner");
        Assert.That((await Catch(() => handle2.InputValueAsync())).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
    }

    [UpstreamTest("EH-028 innerHTML should work")]
    public async Task InnerHTMLShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var handle = await Page.QuerySelectorAsync("#outer");
        Assert.That(await handle.InnerHTMLAsync(), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
        Assert.That(await Page.InnerHTMLAsync("#outer"), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
    }

    [UpstreamTest("EH-029 innerText should work")]
    public async Task InnerTextShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var handle = await Page.QuerySelectorAsync("#inner");
        Assert.That(await handle.InnerTextAsync(), Is.EqualTo("Text, more text"));
        Assert.That(await Page.InnerTextAsync("#inner"), Is.EqualTo("Text, more text"));
    }

    [UpstreamTest("EH-030 innerText should throw")]
    public async Task InnerTextShouldThrow()
    {
        await Page.SetContentAsync("<svg>text</svg>");
        var error1 = await Catch(() => Page.InnerTextAsync("svg"));
        Assert.That(error1.Message, Does.Contain("Node is not an HTMLElement"));
        var handle = await Page.QuerySelectorAsync("svg");
        var error2 = await Catch(() => handle.InnerTextAsync());
        Assert.That(error2.Message, Does.Contain("Node is not an HTMLElement"));
    }

    [UpstreamTest("EH-031 textContent should work")]
    public async Task TextContentShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var handle = await Page.QuerySelectorAsync("#inner");
        Assert.That(await handle.TextContentAsync(), Is.EqualTo("Text,\nmore text"));
        Assert.That(await Page.TextContentAsync("#inner"), Is.EqualTo("Text,\nmore text"));
    }

    [UpstreamTest("EH-032 textContent should work on ShadowRoot")]
    public async Task TextContentShouldWorkOnShadowRoot()
    {
        await Page.SetContentAsync(@"
    <div></div>
    <script>
      document.querySelector('div').attachShadow({ mode: 'open' }).innerHTML = '<div>hello</div>';
    </script>
  ");
        var div = await Page.QuerySelectorAsync("div");
        var root = (IElementHandle)await div.EvaluateHandleAsync("div => div.shadowRoot");
        Assert.That(await root.TextContentAsync(), Is.EqualTo("hello"));
        // We do not match ShadowRoot as ":scope".
        Assert.That(await root.QuerySelectorAllAsync(":scope div"), Is.EqualTo(Array.Empty<IElementHandle>()));
    }

    [UpstreamTest("EH-033 isVisible and isHidden should work")]
    public async Task IsVisibleAndIsHiddenShouldWork()
    {
        await Page.SetContentAsync("<div>Hi</div><span></span>");

        var div = await Page.QuerySelectorAsync("div");
        Assert.That(await div.IsVisibleAsync(), Is.EqualTo(true));
        Assert.That(await div.IsHiddenAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsVisibleAsync("div"), Is.EqualTo(true));
        Assert.That(await Page.IsHiddenAsync("div"), Is.EqualTo(false));

        var span = await Page.QuerySelectorAsync("span");
        Assert.That(await span.IsVisibleAsync(), Is.EqualTo(false));
        Assert.That(await span.IsHiddenAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsVisibleAsync("span"), Is.EqualTo(false));
        Assert.That(await Page.IsHiddenAsync("span"), Is.EqualTo(true));

        Assert.That(await Page.IsVisibleAsync("no-such-element"), Is.EqualTo(false));
        Assert.That(await Page.IsHiddenAsync("no-such-element"), Is.EqualTo(true));
    }

    [UpstreamTest("EH-034 isVisible should not throw when the DOM element is not connected")]
    public async Task IsVisibleShouldNotThrowWhenTheDOMElementIsNotConnected()
    {
        await Page.SetContentAsync("<div id=\"root\"></div>");
        await Page.EvaluateAsync(@"() => {
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
            await Page.IsVisibleAsync("#problem");
        }
    }

    [UpstreamTest("EH-035 isEnabled and isDisabled should work")]
    public async Task IsEnabledAndIsDisabledShouldWork()
    {
        await Page.SetContentAsync(@"
    <button disabled>button1</button>
    <button>button2</button>
    <div>div</div>
  ");
        var div = await Page.QuerySelectorAsync("div");
        Assert.That(await div.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await div.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEnabledAsync("div"), Is.EqualTo(true));
        Assert.That(await Page.IsDisabledAsync("div"), Is.EqualTo(false));
        var button1 = await Page.QuerySelectorAsync(":text(\"button1\")");
        Assert.That(await button1.IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await button1.IsDisabledAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsEnabledAsync(":text(\"button1\")"), Is.EqualTo(false));
        Assert.That(await Page.IsDisabledAsync(":text(\"button1\")"), Is.EqualTo(true));
        var button2 = await Page.QuerySelectorAsync(":text(\"button2\")");
        Assert.That(await button2.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await button2.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEnabledAsync(":text(\"button2\")"), Is.EqualTo(true));
        Assert.That(await Page.IsDisabledAsync(":text(\"button2\")"), Is.EqualTo(false));
    }

    [UpstreamTest("EH-036 isEnabled and isDisabled should work with <select/> option/optgroup correctly")]
    public async Task IsEnabledAndIsDisabledShouldWorkWithSelectOptionOptgroupCorrectly()
    {
        await Page.SetContentAsync(@"
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
        Assert.That(await (await Page.QuerySelectorAsync("#enabled1")).IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await (await Page.QuerySelectorAsync("#enabled1")).IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await (await Page.QuerySelectorAsync("#disabled1")).IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await (await Page.QuerySelectorAsync("#disabled1")).IsDisabledAsync(), Is.EqualTo(true));
        Assert.That(await (await Page.QuerySelectorAsync("optgroup >> nth=0")).IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await (await Page.QuerySelectorAsync("optgroup >> nth=0")).IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await (await Page.QuerySelectorAsync("optgroup >> nth=1")).IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await (await Page.QuerySelectorAsync("optgroup >> nth=1")).IsDisabledAsync(), Is.EqualTo(true));
    }

    [UpstreamTest("EH-037 isEditable should work")]
    public async Task IsEditableShouldWork()
    {
        await Page.SetContentAsync("<input id=input1 disabled><textarea></textarea><input id=input2>");
        await Page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");
        var input1 = await Page.QuerySelectorAsync("#input1");
        Assert.That(await input1.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEditableAsync("#input1"), Is.EqualTo(false));
        var input2 = await Page.QuerySelectorAsync("#input2");
        Assert.That(await input2.IsEditableAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsEditableAsync("#input2"), Is.EqualTo(true));
        var textarea = await Page.QuerySelectorAsync("textarea");
        Assert.That(await textarea.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEditableAsync("textarea"), Is.EqualTo(false));
    }

    [UpstreamTest("EH-038 isChecked should work")]
    public async Task IsCheckedShouldWork()
    {
        await Page.SetContentAsync("<input type='checkbox' checked><div>Not a checkbox</div>");
        var handle = await Page.QuerySelectorAsync("input");
        Assert.That(await handle.IsCheckedAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsCheckedAsync("input"), Is.EqualTo(true));
        await handle.EvaluateAsync("input => input.checked = false");
        Assert.That(await handle.IsCheckedAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsCheckedAsync("input"), Is.EqualTo(false));
        var error = await Catch(() => Page.IsCheckedAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }
}
