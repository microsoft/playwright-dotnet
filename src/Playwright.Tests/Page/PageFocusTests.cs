/**
 * Copyright Microsoft Corporation. All rights reserved.
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

/// <summary>Port of tests/page/page-focus.spec.ts.</summary>
public class PageFocusTests : UpstreamPageTest
{
    [UpstreamTest("FO-001 should work @smoke")]
    public async Task ShouldWork()
    {
        Info.Skip(BrowserName == "firefox" && !IsBidi);

        await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.nodeName"), Is.EqualTo("BODY"));
        await Page.FocusAsync("#d1");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("d1"));
    }

    [UpstreamTest("FO-002 should emit focus event")]
    public async Task ShouldEmitFocusEvent()
    {
        await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
        var focused = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.EvalOnSelectorAsync("#d1", "d1 => d1.addEventListener('focus', window['focusEvent'])");
        await Page.FocusAsync("#d1");
        Assert.That(focused, Is.EqualTo(true));
    }

    [UpstreamTest("FO-003 should emit blur event")]
    public async Task ShouldEmitBlurEvent()
    {
        await Page.SetContentAsync("<div id=d1 tabIndex=0>DIV1</div><div id=d2 tabIndex=0>DIV2</div>");
        await Page.FocusAsync("#d1");
        var focused = false;
        var blurred = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.ExposeFunctionAsync("blurEvent", () => blurred = true);
        await Page.EvalOnSelectorAsync("#d1", "d1 => d1.addEventListener('blur', window['blurEvent'])");
        await Page.EvalOnSelectorAsync("#d2", "d2 => d2.addEventListener('focus', window['focusEvent'])");
        await Page.FocusAsync("#d2");
        Assert.That(focused, Is.EqualTo(true));
        Assert.That(blurred, Is.EqualTo(true));
    }

    [UpstreamTest("FO-004 should traverse focus")]
    public async Task ShouldTraverseFocus()
    {
        await Page.SetContentAsync("<input id=\"i1\"><input id=\"i2\">");
        var focused = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.EvalOnSelectorAsync("#i2", "i2 => i2.addEventListener('focus', window['focusEvent'])");

        await Page.FocusAsync("#i1");
        await Page.Keyboard.TypeAsync("First");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.TypeAsync("Last");

        Assert.That(focused, Is.EqualTo(true));
        Assert.That(await Page.EvalOnSelectorAsync<string>("#i1", "e => e.value"), Is.EqualTo("First"));
        Assert.That(await Page.EvalOnSelectorAsync<string>("#i2", "e => e.value"), Is.EqualTo("Last"));
    }

    [UpstreamTest("FO-005 should traverse focus in all directions")]
    public async Task ShouldTraverseFocusInAllDirections()
    {
        await Page.SetContentAsync("<input value=\"1\"><input value=\"2\"><input value=\"3\">");
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.value"), Is.EqualTo("1"));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.value"), Is.EqualTo("2"));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.value"), Is.EqualTo("3"));
        await Page.Keyboard.PressAsync("Shift+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.value"), Is.EqualTo("2"));
        await Page.Keyboard.PressAsync("Shift+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.value"), Is.EqualTo("1"));
    }

    [UpstreamTest("FO-006 should traverse only form elements")]
    public async Task ShouldTraverseOnlyFormElements()
    {
        Info.Skip(
            Platform != "darwin" || BrowserName != "webkit",
            "Chromium and WebKit both have settings for tab traversing all links, but it is only on by default in WebKit.");

        await Page.SetContentAsync(@"
    <input id=""input-1"">
    <button id=""button"">button</button>
    <a href id=""link"">link</a>
    <input id=""input-2"">
  ");
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input-1"));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input-2"));
        await Page.Keyboard.PressAsync("Shift+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input-1"));
        await Page.Keyboard.PressAsync("Alt+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("button"));
        await Page.Keyboard.PressAsync("Alt+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("link"));
        await Page.Keyboard.PressAsync("Alt+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input-2"));
        await Page.Keyboard.PressAsync("Alt+Shift+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("link"));
        await Page.Keyboard.PressAsync("Alt+Shift+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("button"));
        await Page.Keyboard.PressAsync("Alt+Shift+Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input-1"));
    }

    [UpstreamTest("FO-007 clicking checkbox should activate it")]
    public async Task ClickingCheckboxShouldActivateIt()
    {
        Info.Skip(BrowserName == "webkit", "Safari does not focus on click");

        await Page.SetContentAsync("<input type=checkbox></input>");
        await Page.ClickAsync("input");
        var nodeName = await Page.EvaluateAsync<string>("() => document.activeElement.nodeName");
        Assert.That(nodeName, Is.EqualTo("INPUT"));
    }

    [UpstreamTest("FO-008 tab should cycle between single input and browser")]
    public async Task TabShouldCycleBetweenSingleInputAndBrowser()
    {
        Info.Skip(BrowserName == "chromium" && !IsHeadlessShell, "Chromium keeps input focused.");
        Info.Fixme(BrowserName == "firefox");
        await Page.SetContentAsync(@"<label for=""input1"">input1</label>
    <input id=""input1"">
    <script>
    {
      window.focusEvents = [];
      const input = document.getElementById('input1');
      input.addEventListener('blur', () => focusEvents.push('blur'));
      input.addEventListener('focus', () => focusEvents.push('focus'));
    }
    </script>");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.tagName"), Is.EqualTo("BODY"));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input1"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus" }));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.tagName"), Is.EqualTo("BODY"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus", "blur" }));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input1"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus", "blur", "focus" }));
    }

    [UpstreamTest("FO-009 tab should cycle between document elements and browser")]
    public async Task TabShouldCycleBetweenDocumentElementsAndBrowser()
    {
        Info.Skip(BrowserName == "chromium" && !IsHeadlessShell, "Chromium keeps last input focused.");
        Info.Fixme(BrowserName == "firefox");
        await Page.SetContentAsync(@"
    <input id=""input1"">
    <input id=""input2"">
    <script>
      window.focusEvents = [];
      {
        const input = document.getElementById('input1');
        input.addEventListener('blur', () => focusEvents.push('blur1'));
        input.addEventListener('focus', () => focusEvents.push('focus1'));
      }
      {
        const input = document.getElementById('input2');
        input.addEventListener('blur', () => focusEvents.push('blur2'));
        input.addEventListener('focus', () => focusEvents.push('focus2'));
      }
    </script>");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.tagName"), Is.EqualTo("BODY"));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input1"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus1" }));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input2"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus1", "blur1", "focus2" }));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.tagName"), Is.EqualTo("BODY"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus1", "blur1", "focus2", "blur2" }));
        await Page.Keyboard.PressAsync("Tab");
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement.id"), Is.EqualTo("input1"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.focusEvents"), Is.EqualTo(new[] { "focus1", "blur1", "focus2", "blur2", "focus1" }));
    }

    [UpstreamTest("FO-010 keeps focus on element when attempting to focus a non-focusable element")]
    public async Task KeepsFocusOnElementWhenAttemptingToFocusANonFocusableElement()
    {
        await Page.SetContentAsync(@"
      <div id=""focusable"" tabindex=""0"">focusable</div>
      <div id=""non-focusable"">not focusable</div>
      <script>
        window.eventLog = [];

        const focusable = document.getElementById(""focusable"");

        focusable.addEventListener('blur', () => window.eventLog.push('blur focusable'));
        focusable.addEventListener('focus', () => window.eventLog.push('focus focusable'));

        const nonFocusable = document.getElementById(""non-focusable"");
        nonFocusable.addEventListener('blur', () => window.eventLog.push('blur non-focusable'));
        nonFocusable.addEventListener('focus', () => window.eventLog.push('focus non-focusable'));
      </script>
    ");
        await Page.Locator("#focusable").ClickAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement?.id"), Is.EqualTo("focusable"));
        await Page.Locator("#non-focusable").FocusAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.activeElement?.id"), Is.EqualTo("focusable"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['eventLog']"), Is.EqualTo(new[]
        {
            "focus focusable",
        }));
    }

    [UpstreamTest("FO-011 should move focus from an iframe to its parent")]
    public async Task ShouldMoveFocusFromAnIframeToItsParent()
    {
        Server.SetRoute("/frame.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<input id=inner>");
        });
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<iframe src=\"{Server.Prefix}/frame.html\"></iframe><input id=outer>");
        await Page.FrameLocator("iframe").Locator("#inner").FocusAsync();
        Assert.That(await Page.FrameLocator("iframe").Locator("#inner").EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        await Page.EvaluateAsync("() => document.getElementById('outer').focus()");
        await Expect(Page.Locator("#outer")).ToBeFocusedAsync();
        Assert.That(await Page.FrameLocator("iframe").Locator("#inner").EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(false));
    }

    [UpstreamTest("FO-012 should tab from an iframe to its parent")]
    public async Task ShouldTabFromAnIframeToItsParent()
    {
        Server.SetRoute("/frame.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<input id=inner>");
        });
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<iframe src=\"{Server.Prefix}/frame.html\"></iframe><input id=outer>");
        await Page.FrameLocator("iframe").Locator("#inner").ClickAsync();
        Assert.That(await Page.FrameLocator("iframe").Locator("#inner").EvaluateAsync<bool>("e => e === document.activeElement"), Is.EqualTo(true));
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#outer")).ToBeFocusedAsync();
    }

    [UpstreamTest("FO-013 should move focus from the parent into its iframe")]
    public async Task ShouldMoveFocusFromTheParentIntoItsIframe()
    {
        Server.SetRoute("/frame.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<input id=inner>");
        });
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<iframe src=\"{Server.Prefix}/frame.html\"></iframe><input id=outer>");
        await Page.Locator("#outer").FocusAsync();
        await Expect(Page.Locator("#outer")).ToBeFocusedAsync();
        // Call native focus() exactly once, locator.focus() would hide the bug by calling it twice.
        await Page.EvaluateAsync("() => document.querySelector('iframe').contentDocument.querySelector('input').focus()");
        await Expect(Page.Locator("iframe")).ToBeFocusedAsync();
        await Expect(Page.FrameLocator("iframe").Locator("#inner")).ToBeFocusedAsync();
    }
}
