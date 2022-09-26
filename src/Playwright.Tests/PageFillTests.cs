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

public class PageFillTests : PageTestEx
{
    [PlaywrightTest("page-fill.spec.ts", "should fill textarea")]
    public async Task ShouldFillTextarea()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FillAsync("textarea", "some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill input")]
    public async Task ShouldFillInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FillAsync("input", "some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should throw on unsupported inputs")]
    public async Task ShouldThrowOnUnsupportedInputs()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        foreach (string type in new[] { "button", "checkbox", "file", "image", "radio", "reset", "submit" })
        {
            await Page.EvalOnSelectorAsync("input", "(input, type) => input.setAttribute('type', type)", type);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FillAsync("input", string.Empty));
            StringAssert.Contains($"input of type \"{type}\" cannot be filled", exception.Message);
        }
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill different input types")]
    public async Task ShouldFillDifferentInputTypes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        foreach (string type in new[] { "password", "search", "tel", "text", "url" })
        {
            await Page.EvalOnSelectorAsync("input", "(input, type) => input.setAttribute('type', type)", type);
            await Page.FillAsync("input", "text " + type);
            Assert.AreEqual("text " + type, await Page.EvaluateAsync<string>("() => result"));
        }
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill date input after clicking")]
    public async Task ShouldFillDateInputAfterClicking()
    {
        await Page.SetContentAsync("<input type=date>");
        await Page.ClickAsync("input");
        await Page.FillAsync("input", "2020-03-02");
        Assert.AreEqual("2020-03-02", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should throw on incorrect date")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowOnIncorrectDate()
    {
        await Page.SetContentAsync("<input type=date>");
        await Page.ClickAsync("input");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FillAsync("input", "2020-13-02"));
        StringAssert.Contains("Malformed value", exception.Message);
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill time input after clicking")]
    public async Task ShouldFillTimeInputAfterClicking()
    {
        await Page.SetContentAsync("<input type=time>");
        await Page.ClickAsync("input");
        await Page.FillAsync("input", "13:15");
        Assert.AreEqual("13:15", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should throw on incorrect time")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowOnIncorrectTime()
    {
        await Page.SetContentAsync("<input type=time>");
        await Page.ClickAsync("input");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FillAsync("input", "25:05"));
        StringAssert.Contains("Malformed value", exception.Message);
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill datetime-local input")]
    public async Task ShouldFillDatetimeLocalInput()
    {
        await Page.SetContentAsync("<input type=datetime-local>");
        await Page.ClickAsync("input");
        await Page.FillAsync("input", "2020-03-02T05:15");
        Assert.AreEqual("2020-03-02T05:15", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should throw on incorrect datetime-local")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowOnIncorrectDateTimeLocal()
    {
        await Page.SetContentAsync("<input type=datetime-local>");
        await Page.ClickAsync("input");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FillAsync("input", "abc"));
        StringAssert.Contains("Malformed value", exception.Message);
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill contenteditable")]
    public async Task ShouldFillContenteditable()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FillAsync("div[contenteditable]", "some value");
        Assert.AreEqual("some value", await Page.EvalOnSelectorAsync<string>("div[contenteditable]", "div => div.textContent"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill elements with existing value and selection")]
    public async Task ShouldFillElementsWithExistingValueAndSelection()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");

        await Page.EvalOnSelectorAsync("input", "input => input.value = 'value one'");
        await Page.FillAsync("input", "another value");
        Assert.AreEqual("another value", await Page.EvaluateAsync<string>("() => result"));

        await Page.EvalOnSelectorAsync("input", @"input => {
                input.selectionStart = 1;
                input.selectionEnd = 2;
            }");
        await Page.FillAsync("input", "maybe this one");
        Assert.AreEqual("maybe this one", await Page.EvaluateAsync<string>("() => result"));

        await Page.EvalOnSelectorAsync("div[contenteditable]", @"div => {
                div.innerHTML = 'some text <span>some more text<span> and even more text';
                var range = document.createRange();
                range.selectNodeContents(div.querySelector('span'));
                var selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
            }");
        await Page.FillAsync("div[contenteditable]", "replace with this");
        Assert.AreEqual("replace with this", await Page.EvalOnSelectorAsync<string>("div[contenteditable]", "div => div.textContent"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should throw when element is not an &lt;input&gt;, &lt;textarea&gt; or [contenteditable]")]
    public async Task ShouldThrowWhenElementIsNotAnInputOrTextareaOrContenteditable()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FillAsync("body", string.Empty));
        StringAssert.Contains("Element is not an <input>", exception.Message);
    }

    [PlaywrightTest("page-fill.spec.ts", "should retry on disabled element")]
    public async Task ShouldRetryOnDisabledElement()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvalOnSelectorAsync("input", "i => i.disabled = true");

        var task = Page.FillAsync("input", "some value");
        await GiveItAChanceToFillAsync(Page);
        Assert.False(task.IsCompleted);
        Assert.IsEmpty(await Page.EvaluateAsync<string>("() => result"));

        await Page.EvalOnSelectorAsync("input", "i => i.disabled = false");
        await task;
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should retry on readonly element")]
    public async Task ShouldRetryOnReadonlyElement()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvalOnSelectorAsync("textarea", "i => i.readOnly = true");
        var task = Page.FillAsync("textarea", "some value");
        await GiveItAChanceToFillAsync(Page);
        Assert.False(task.IsCompleted);
        Assert.IsEmpty(await Page.EvaluateAsync<string>("() => result"));

        await Page.EvalOnSelectorAsync("textarea", "i => i.readOnly = false");
        await task;
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should retry on invisible element")]
    public async Task ShouldRetryOnInvisibleElement()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvalOnSelectorAsync("input", "i => i.style.display = 'none'");

        var task = Page.FillAsync("input", "some value");
        await GiveItAChanceToFillAsync(Page);
        Assert.False(task.IsCompleted);
        Assert.IsEmpty(await Page.EvaluateAsync<string>("() => result"));

        await Page.EvalOnSelectorAsync("input", "i => i.style.display = 'inline'");
        await task;
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should be able to fill the body")]
    public async Task ShouldBeAbleToFillTheBody()
    {
        await Page.SetContentAsync("<body contentEditable=\"true\"></body>");
        await Page.FillAsync("body", "some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => document.body.textContent"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should fill fixed position input")]
    public async Task ShouldFillFixedPositionInput()
    {
        await Page.SetContentAsync("<input style='position: fixed;' />");
        await Page.FillAsync("input", "some value");
        Assert.AreEqual("some value", await Page.EvalOnSelectorAsync<string>("input", "i => i.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should be able to fill when focus is in the wrong frame")]
    public async Task ShouldBeAbleToFillWhenFocusIsInTheWrongFrame()
    {
        await Page.SetContentAsync("<div contentEditable=\"true\"></div><iframe></iframe>");
        await Page.FocusAsync("iframe");
        await Page.FillAsync("div", "some value");
        Assert.AreEqual("some value", await Page.EvalOnSelectorAsync<string>("div", "d => d.textContent"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should be able to fill the input[type=number]")]
    public async Task ShouldBeAbleToFillTheInputTypeNumber()
    {
        await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        await Page.FillAsync("input", "42");
        Assert.AreEqual("42", await Page.EvaluateAsync<string>("() => input.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should be able to fill exponent into the input[type=number]")]
    public async Task ShouldBeAbleToFillTheInputExponentIntoTypeNumber()
    {
        await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        await Page.FillAsync("input", "-10e5");
        Assert.AreEqual("-10e5", await Page.EvaluateAsync<string>("() => input.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should be able to fill the input[type=number] with empty string")]
    public async Task ShouldBeAbleToFillTheInputTypeNumberWithEmptyString()
    {
        await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        await Page.FillAsync("input", "");
        Assert.IsEmpty(await Page.EvaluateAsync<string>("() => input.value"));
    }

    [PlaywrightTest("page-fill.spec.ts", "should not be able to fill text into the input[type=number]")]
    public async Task ShouldNotBeAbleToFillTextIntoTheInputTypeNumber()
    {
        await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FillAsync("input", "abc"));
        StringAssert.Contains("Cannot type text into input[type=number]", exception.Message);
    }

    [PlaywrightTest("page-fill.spec.ts", "should be able to clear")]
    public async Task ShouldBeAbleToClear()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FillAsync("input", "some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
        await Page.FillAsync("input", "");
        Assert.IsEmpty(await Page.EvaluateAsync<string>("() => result"));
    }

    private async Task GiveItAChanceToFillAsync(IPage page)
    {
        for (int i = 0; i < 5; i++)
        {
            await page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
        }
    }
}
