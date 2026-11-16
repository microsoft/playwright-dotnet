/**
 * Copyright 2017 Google Inc. All rights reserved.
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

/// <summary>Port of tests/page/page-fill.spec.ts.</summary>
public class PageFillTests
{
    private static Task GiveItAChanceToFill(IPage page) => Rafraf(page, 5);

    [UpstreamTest("FI-001 should fill textarea @smoke")]
    public async Task ShouldFillTextarea(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FillAsync("textarea", "some value");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-002 should fill input")]
    public async Task ShouldFillInput(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FillAsync("input", "some value");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-003 should throw on unsupported inputs")]
    public async Task ShouldThrowOnUnsupportedInputs(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        foreach (var type in new[] { "button", "checkbox", "file", "image", "radio", "reset", "submit" })
        {
            await page.EvalOnSelectorAsync("input", "(input, type) => input.setAttribute('type', type)", type);
            Exception error = null;
            error = await Catch(() => page.FillAsync("input", ""));
            Assert.That(error.Message, Does.Contain($"Input of type \"{type}\" cannot be filled"));
        }
    }

    [UpstreamTest("FI-004 should fill different input types")]
    public async Task ShouldFillDifferentInputTypes(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        foreach (var type in new[] { "password", "search", "tel", "text", "url", "invalid-type" })
        {
            await page.EvalOnSelectorAsync("input", "(input, type) => input.setAttribute('type', type)", type);
            await page.FillAsync("input", "text " + type);
            Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("text " + type));
        }
    }

    [UpstreamTest("FI-005 should fill range input")]
    public async Task ShouldFillRangeInput(IPage page)
    {
        await page.SetContentAsync("<input type=range min=0 max=100 value=50>");
        await page.FillAsync("input", "42");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("42"));
        // Valid numbers that the browser may serialize differently.
        foreach (var (value, expected) in new[] { ("42.0", 42), ("1e1", 10) })
        {
            await page.FillAsync("input", value);
            Assert.That(await page.EvalOnSelectorAsync<int>("input", "input => input.valueAsNumber"), Is.EqualTo(expected));
        }
    }

    [UpstreamTest("FI-006 should fill color input")]
    public async Task ShouldFillColorInput(IPage page)
    {
        await page.SetContentAsync("<input type=color value=\"#e66465\">");
        await page.FillAsync("input", "#aaaaaa");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("#aaaaaa"));
    }

    [UpstreamTest("FI-007 should fill color input case insensitive")]
    public async Task ShouldFillColorInputCaseInsensitive(IPage page, string browserName, bool isWindows, string channel)
    {
        await page.SetContentAsync("<input type=color value=\"#e66465\">");
        await page.FillAsync("input", "#AbCd00");
        if (browserName == "webkit" && isWindows && channel != "webkit-wsl")
        {
            Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("#AbCd00"));
        }
        else
        {
            Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("#abcd00"));
        }
    }

    [UpstreamTest("FI-008 should fill date input after clicking")]
    public async Task ShouldFillDateInputAfterClicking(IPage page, Server server)
    {
        await page.SetContentAsync("<input type=date>");
        await page.ClickAsync("input");
        await page.FillAsync("input", "2020-03-02");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("2020-03-02"));
    }

    [UpstreamTest("FI-009 input event.composed should be true and cross shadow dom boundary - ${type}")]
    [Case("color", "#aaaaaa")]
    [Case("date", "2020-03-02")]
    [Case("time", "13:15")]
    [Case("datetime-local", "2020-03-02T13:15:30")]
    [Case("month", "2020-03")]
    [Case("range", "42")]
    [Case("week", "2020-W50")]
    public async Task InputEventComposedShouldBeTrueAndCrossShadowDomBoundary(string type, string value, IPage page, Server server, string browserName, bool isWindows)
    {
        It.Skip(browserName != "chromium" && new[] { "month", "week" }.Contains(type), "Some browser/platforms do not implement certain input types");
        It.Skip(browserName == "webkit" && isWindows && new[] { "color", "date", "time", "datetime-local" }.Contains(type), "Some browser/platforms do not implement certain input types");
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($@"<body><script>
    const div = document.createElement('div');
    const shadowRoot = div.attachShadow({{mode: 'open'}});
    shadowRoot.innerHTML = '<input type={type}></input>';
    document.body.appendChild(div);
  </script></body>");
        await page.Locator("body").EvaluateAsync(@"select => {
      window.firedBodyEvents = [];
      for (const event of ['input', 'change']) {
        select.addEventListener(event, e => {
          window.firedBodyEvents.push(e.type + ':' + e.composed);
        }, false);
      }
    }");

        await page.Locator("input").EvaluateAsync(@"select => {
      window.firedEvents = [];
      for (const event of ['input', 'change']) {
        select.addEventListener(event, e => {
          window.firedEvents.push(e.type + ':' + e.composed);
        }, false);
      }
    }");
        await page.Locator("input").FillAsync(value);

        Assert.That(await page.EvaluateAsync<string[]>("() => window['firedEvents']"), Is.EqualTo(new[] { "input:true", "change:false" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['firedBodyEvents']"), Is.EqualTo(new[] { "input:true" }));
    }

    [UpstreamTest("FI-010 should fill time input")]
    public async Task ShouldFillTimeInput(IPage page)
    {
        await page.SetContentAsync("<input type=time>");
        await page.FillAsync("input", "13:15");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("13:15"));
    }

    [UpstreamTest("FI-011 should fill month input")]
    public async Task ShouldFillMonthInput(IPage page)
    {
        await page.SetContentAsync("<input type=month>");
        await page.FillAsync("input", "2020-07");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("2020-07"));
    }

    [UpstreamTest("FI-012 should fill week input")]
    public async Task ShouldFillWeekInput(IPage page)
    {
        await page.SetContentAsync("<input type=week>");
        await page.FillAsync("input", "2020-W50");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("2020-W50"));
    }

    [UpstreamTest("FI-013 should fill datetime-local input")]
    public async Task ShouldFillDatetimeLocalInput(IPage page, Server server, string browserName, bool isWindows, string channel)
    {
        await page.SetContentAsync("<input type=datetime-local>");
        await page.FillAsync("input", "2020-03-02T05:15");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("2020-03-02T05:15"));
        // The browser omits zero seconds. WebKit on Windows does not implement datetime-local inputs and keeps the value as is.
        await page.FillAsync("input", "2020-03-02T06:20:00");
        if (browserName == "webkit" && isWindows && channel != "webkit-wsl")
        {
            Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("2020-03-02T06:20:00"));
        }
        else
        {
            Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("2020-03-02T06:20"));
        }
    }

    [UpstreamTest("FI-014 should fill contenteditable")]
    public async Task ShouldFillContenteditable(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FillAsync("div[contenteditable]", "some value");
        Assert.That(await page.EvalOnSelectorAsync<string>("div[contenteditable]", "div => div.textContent"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-015 should fill contenteditable with new lines")]
    public async Task ShouldFillContenteditableWithNewLines(IPage page, Server server, string browserName, bool isBidi)
    {
        It.Fixme(browserName == "firefox" && !isBidi, "Firefox does not handle new lines in contenteditable");

        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<div contenteditable=\"true\"></div>");
        await page.Locator("div[contenteditable]").FillAsync("John\nDoe");
        Assert.That(await page.Locator("div[contenteditable]").InnerTextAsync(), Is.EqualTo("John\nDoe"));
    }

    [UpstreamTest("FI-016 should not double-fill in contenteditable with beforeinput handler in Firefox")]
    public async Task ShouldNotDoubleFillInContenteditableWithBeforeinputHandlerInFirefox(IPage page, string browserName, bool isBidi)
    {
        It.Fixme(browserName == "firefox" && !isBidi, "https://github.com/microsoft/playwright/issues/36715");

        await page.SetContentAsync(@"
    <div id=""editor"" contenteditable=""true""></div>
    <script>
      const editor = document.getElementById('editor');
      editor.addEventListener('beforeinput', (event) => {
        event.preventDefault();
        editor.textContent = event.data;
      });
    </script>
  ");

        var locator = page.Locator("#editor");
        var testValue = "Playwright";
        await locator.FillAsync(testValue);
        await Expect(locator).ToHaveTextAsync(testValue);
    }

    [UpstreamTest("FI-017 should fill elements with existing value and selection")]
    public async Task ShouldFillElementsWithExistingValueAndSelection(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");

        await page.EvalOnSelectorAsync("input", "input => input.value = 'value one'");
        await page.FillAsync("input", "another value");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("another value"));

        await page.EvalOnSelectorAsync("input", @"input => {
    input.selectionStart = 1;
    input.selectionEnd = 2;
  }");
        await page.FillAsync("input", "maybe this one");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("maybe this one"));

        await page.EvalOnSelectorAsync("div[contenteditable]", @"div => {
    div.innerHTML = 'some text <span>some more text<span> and even more text';
    const range = document.createRange();
    range.selectNodeContents(div.querySelector('span'));
    const selection = window.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
  }");
        await page.FillAsync("div[contenteditable]", "replace with this");
        Assert.That(await page.EvalOnSelectorAsync<string>("div[contenteditable]", "div => div.textContent"), Is.EqualTo("replace with this"));
    }

    [UpstreamTest("FI-018 should throw nice error without injected script stack when element is not an <input>")]
    [Fixme("dotnet: the message is 'Error: Element is not an <input>, <textarea> or [contenteditable] element\\nCall log: ...', without the 'page.fill: ' prefix of the call")]
    public async Task ShouldThrowNiceErrorWithoutInjectedScriptStackWhenElementIsNotAnInput(IPage page, Server server)
    {
        Exception error = null;
        await page.SetContentAsync("<select><option>value1</option></select>");
        error = await Catch(() => page.FillAsync("select", ""));
        Assert.That(error.Message, Does.Contain("page.fill: Error: Element is not an <input>, <textarea> or [contenteditable] element\nCall log:"));
    }

    [UpstreamTest("FI-020 should retry on disabled element")]
    public async Task ShouldRetryOnDisabledElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.EvalOnSelectorAsync("input", "i => i.disabled = true");
        var done = false;

        // .NET: page.fill('input', 'some value').then(() => done = true) is a local function that is called in its place.
        async Task Then()
        {
            await page.FillAsync("input", "some value");
            done = true;
        }
        var promise = Then();
        await GiveItAChanceToFill(page);
        Assert.That(done, Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo(""));

        await page.EvalOnSelectorAsync("input", "i => i.disabled = false");
        await promise;
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-021 should retry on readonly element")]
    public async Task ShouldRetryOnReadonlyElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.EvalOnSelectorAsync("textarea", "i => i.readOnly = true");
        var done = false;

        // .NET: page.fill('textarea', 'some value').then(() => done = true) is a local function that is called in its place.
        async Task Then()
        {
            await page.FillAsync("textarea", "some value");
            done = true;
        }
        var promise = Then();
        await GiveItAChanceToFill(page);
        Assert.That(done, Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo(""));

        await page.EvalOnSelectorAsync("textarea", "i => i.readOnly = false");
        await promise;
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-022 should retry on invisible element")]
    public async Task ShouldRetryOnInvisibleElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.EvalOnSelectorAsync("input", "i => i.style.display = 'none'");
        var done = false;

        // .NET: page.fill('input', 'some value').then(() => done = true) is a local function that is called in its place.
        async Task Then()
        {
            await page.FillAsync("input", "some value");
            done = true;
        }
        var promise = Then();
        await GiveItAChanceToFill(page);
        Assert.That(done, Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo(""));

        await page.EvalOnSelectorAsync("input", "i => i.style.display = 'inline'");
        await promise;
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-023 should be able to fill the body")]
    public async Task ShouldBeAbleToFillTheBody(IPage page)
    {
        await page.SetContentAsync("<body contentEditable=\"true\"></body>");
        await page.FillAsync("body", "some value");
        Assert.That(await page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-024 should fill fixed position input")]
    public async Task ShouldFillFixedPositionInput(IPage page)
    {
        await page.SetContentAsync("<input style='position: fixed;' />");
        await page.FillAsync("input", "some value");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('input').value"), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-025 should be able to fill when focus is in the wrong frame")]
    public async Task ShouldBeAbleToFillWhenFocusIsInTheWrongFrame(IPage page)
    {
        await page.SetContentAsync(@"
    <div contentEditable=""true""></div>
    <iframe srcdoc=""<body contenteditable><p>old</p></body>""></iframe>
  ");
        await page.FocusAsync("iframe");
        await page.FillAsync("div", "some value");
        Assert.That(await page.EvalOnSelectorAsync<string>("div", "d => d.textContent"), Is.EqualTo("some value"));
        var paragraph = page.FrameLocator("iframe").Locator("p");
        await paragraph.FillAsync("new");
        await Expect(paragraph).ToHaveTextAsync("new");
    }

    [UpstreamTest("FI-026 should be able to fill the input[type=number]")]
    public async Task ShouldBeAbleToFillTheInputTypeNumber(IPage page)
    {
        await page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        await page.FillAsync("input", "42");
        Assert.That(await page.EvaluateAsync<string>("() => window['input'].value"), Is.EqualTo("42"));
    }

    [UpstreamTest("FI-027 should be able to fill exponent into the input[type=number]")]
    public async Task ShouldBeAbleToFillExponentIntoTheInputTypeNumber(IPage page)
    {
        await page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        await page.FillAsync("input", "-10e5");
        Assert.That(await page.EvaluateAsync<string>("() => window['input'].value"), Is.EqualTo("-10e5"));
    }

    [UpstreamTest("FI-028 should be able to fill input[type=number] with empty string")]
    public async Task ShouldBeAbleToFillInputTypeNumberWithEmptyString(IPage page)
    {
        await page.SetContentAsync("<input id=\"input\" type=\"number\" value=\"123\"></input>");
        await page.FillAsync("input", "");
        Assert.That(await page.EvaluateAsync<string>("() => window['input'].value"), Is.EqualTo(""));
    }

    [UpstreamTest("FI-029 should not be able to fill text into the input[type=number]")]
    public async Task ShouldNotBeAbleToFillTextIntoTheInputTypeNumber(IPage page)
    {
        await page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        Exception error = null;
        error = await Catch(() => page.FillAsync("input", "abc"));
        Assert.That(error.Message, Does.Contain("Cannot type text into input[type=number]"));
    }

    [UpstreamTest("FI-030 should be able to clear using fill()")]
    public async Task ShouldBeAbleToClearUsingFill(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FillAsync("input", "some value");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
        await page.FillAsync("input", "");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo(""));
    }

    [UpstreamTest("FI-031 should not throw when fill causes navigation")]
    public async Task ShouldNotThrowWhenFillCausesNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.SetContentAsync("<input type=date>");
        await page.EvalOnSelectorAsync("input", "select => select.addEventListener('input', () => window.location.href = '/empty.html')");
        await PromiseAll(
            page.FillAsync("input", "2020-03-02"),
            page.WaitForNavigationAsync());
        Assert.That(page.Url, Does.Contain("empty.html"));
    }

    [UpstreamTest("FI-032 fill back to back")]
    public async Task FillBackToBack(IPage page)
    {
        await page.SetContentAsync("<input id=\"one\"></input><input id=\"two\"></input>");
        await page.FillAsync("id=one", "first value");
        await page.FillAsync("id=two", "second value");
        await Expect(page.Locator("id=one")).ToHaveValueAsync("first value");
        await Expect(page.Locator("id=two")).ToHaveValueAsync("second value");
    }

    [UpstreamTest("FI-033 should fill contenteditable with focus handler that collapses selection")]
    public async Task ShouldFillContenteditableWithFocusHandlerThatCollapsesSelection(IPage page)
    {
        await page.SetContentAsync(@"
    <div contenteditable=""true"">initial text</div>
    <script>
      const editor = document.querySelector('[contenteditable]');
      editor.addEventListener('focus', () => {
        const selection = window.getSelection();
        if (selection.rangeCount > 0)
          selection.collapseToEnd();
      });
    </script>
  ");

        await page.FillAsync("div[contenteditable]", "some value");
        Assert.That(await page.Locator("div[contenteditable]").TextContentAsync(), Is.EqualTo("some value"));
    }

    [UpstreamTest("FI-034 should retry on inert element")]
    public async Task ShouldRetryOnInertElement(IPage page)
    {
        await page.SetContentAsync(@"
    <div inert id=container>
      <input id=target>
    </div>
    <input id=outside>
  ");
        await page.FocusAsync("#outside");
        var done = false;

        // .NET: page.fill('#target', 'some value').then(() => done = true) is a local function that is called in its place.
        async Task Then()
        {
            await page.FillAsync("#target", "some value");
            done = true;
        }
        var promise = Then();
        await GiveItAChanceToFill(page);
        Assert.That(done, Is.EqualTo(false));
        Assert.That(await page.EvalOnSelectorAsync<string>("#target", "i => i.value"), Is.EqualTo(""));
        Assert.That(await page.EvalOnSelectorAsync<string>("#outside", "i => i.value"), Is.EqualTo(""));

        await page.EvalOnSelectorAsync("#container", "e => e.removeAttribute('inert')");
        await promise;
        Assert.That(await page.EvalOnSelectorAsync<string>("#target", "i => i.value"), Is.EqualTo("some value"));
        Assert.That(await page.EvalOnSelectorAsync<string>("#outside", "i => i.value"), Is.EqualTo(""));
    }

    [UpstreamTest("FI-036 should fail with a nice error on inert element")]
    public async Task ShouldFailWithANiceErrorOnInertElement(IPage page)
    {
        await page.SetContentAsync("<div inert><input></div>");
        var error = await Catch(() => page.FillAsync("input", "some value", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("element is inert"));
    }
}
