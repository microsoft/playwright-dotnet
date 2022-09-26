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

public class ElementHandleSelectTextTests : PageTestEx
{
    [PlaywrightTest("elementhandle-select-text.spec.ts", "should select textarea")]
    public async Task ShouldSelectTextarea()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
        await textarea.SelectTextAsync();

        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual(0, await textarea.EvaluateAsync<int>("el => el.selectionStart"));
            Assert.AreEqual(10, await textarea.EvaluateAsync<int>("el => el.selectionEnd"));
        }
        else
        {
            Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
        }
    }

    [PlaywrightTest("elementhandle-select-text.spec.ts", "should select input")]
    public async Task ShouldSelectInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var input = await Page.QuerySelectorAsync("input");
        await input.EvaluateAsync("input => input.value = 'some value'");
        await input.SelectTextAsync();

        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual(0, await input.EvaluateAsync<int>("el => el.selectionStart"));
            Assert.AreEqual(10, await input.EvaluateAsync<int>("el => el.selectionEnd"));
        }
        else
        {
            Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
        }
    }

    [PlaywrightTest("elementhandle-select-text.spec.ts", "should select plain div")]
    public async Task ShouldSelectPlainDiv()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var div = await Page.QuerySelectorAsync("div.plain");
        await div.EvaluateAsync("input => input.value = 'some value'");
        await div.SelectTextAsync();

        Assert.AreEqual("Plain div", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
    }

    [PlaywrightTest("elementhandle-select-text.spec.ts", "should timeout waiting for invisible element")]
    public async Task ShouldTimeoutWaitingForInvisibleElement()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.EvaluateAsync("e => e.style.display = 'none'");

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => textarea.SelectTextAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("element is not visible", exception.Message);
    }

    [PlaywrightTest("elementhandle-select-text.spec.ts", "should wait for visible")]
    public async Task ShouldWaitForVisible()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = await Page.QuerySelectorAsync("textarea");
        await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
        await textarea.EvaluateAsync("e => e.style.display = 'none'");

        var task = textarea.SelectTextAsync(new() { Timeout = 3000 });
        await Page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
        Assert.False(task.IsCompleted);
        await textarea.EvaluateAsync("e => e.style.display = 'block'");
        await task;
    }
}
