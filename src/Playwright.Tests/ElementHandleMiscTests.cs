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

public class ElementHandleMiscTests : PageTestEx
{
    [PlaywrightTest("elementhandle-misc.spec.ts", "should hover")]
    public async Task ShouldHover()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        var button = await Page.QuerySelectorAsync("#button-6");
        await button.HoverAsync();
        Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should hover when Node is removed")]
    public async Task ShouldHoverWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = await Page.QuerySelectorAsync("#button-6");
        await button.HoverAsync();
        Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should fill input")]
    public async Task ShouldFillInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var handle = await Page.QuerySelectorAsync("input");
        await handle.FillAsync("some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should fill input when Node is removed")]
    public async Task ShouldFillInputWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var handle = await Page.QuerySelectorAsync("input");
        await handle.FillAsync("some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should check the box")]
    public async Task ShouldCheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = await Page.QuerySelectorAsync("input");
        await input.CheckAsync();
        Assert.True(await Page.EvaluateAsync<bool>("() => checkbox.checked"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should uncheck the box")]
    public async Task ShouldUncheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = await Page.QuerySelectorAsync("input");
        await input.UncheckAsync();
        Assert.False(await Page.EvaluateAsync<bool>("() => checkbox.checked"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should focus a button")]
    public async Task ShouldFocusAButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");

        Assert.False(await button.EvaluateAsync<bool?>("button => document.activeElement === button"));
        await button.FocusAsync();
        Assert.True(await button.EvaluateAsync<bool?>("button => document.activeElement === button"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should select single option")]
    public async Task ShouldSelectSingleOption()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var select = await Page.QuerySelectorAsync("select");
        await select.SelectOptionAsync("blue");

        Assert.AreEqual(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
        Assert.AreEqual(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
    }

    [PlaywrightTest("elementhandle-misc.spec.ts", "should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = await Page.QuerySelectorAsync("input");
        await input.SetCheckedAsync(true);
        Assert.IsTrue(await Page.EvaluateAsync<bool>("checkbox.checked"));
        await input.SetCheckedAsync(false);
        Assert.IsFalse(await Page.EvaluateAsync<bool>("checkbox.checked"));
    }

}
