/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

namespace Microsoft.Playwright.Tests.Locator;

public class LocatorMisc1Tests : PageTestEx
{
    [PlaywrightTest("locator-misc-1.spec.ts", "should hover")]
    public async Task ShouldHover()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        var button = Page.Locator("#button-6");
        await button.HoverAsync();
        Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should hover when Node is removed")]
    public async Task ShouldHoverWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = Page.Locator("#button-6");
        await button.HoverAsync();
        Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should fill input")]
    public async Task ShouldFillInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var handle = Page.Locator("input");
        await handle.FillAsync("some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window['result']"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should fill input when Node is removed")]
    public async Task ShouldFillInputWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var handle = Page.Locator("input");
        await handle.FillAsync("some value");
        Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window['result']"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should check the box")]
    public async Task ShouldCheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = Page.Locator("input");
        await input.CheckAsync();
        Assert.IsTrue(await Page.EvaluateAsync<bool>("checkbox.checked"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = Page.Locator("input");
        await input.SetCheckedAsync(true);
        Assert.IsTrue(await Page.EvaluateAsync<bool>("checkbox.checked"));
        await input.SetCheckedAsync(false);
        Assert.IsFalse(await Page.EvaluateAsync<bool>("checkbox.checked"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should uncheck the box")]
    public async Task ShouldUncheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        var input = Page.Locator("input");
        await input.UncheckAsync();
        Assert.IsFalse(await Page.EvaluateAsync<bool>("checkbox.checked"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should select single option")]
    public async Task ShouldSelectSingleOption()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var select = Page.Locator("select");
        await select.SelectOptionAsync("blue");
        CollectionAssert.AreEqual(new string[] { "blue" }, await Page.EvaluateAsync<string[]>("() => window['result'].onInput"));
        CollectionAssert.AreEqual(new string[] { "blue" }, await Page.EvaluateAsync<string[]>("() => window['result'].onChange"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should focus a button")]
    public async Task ShouldFocusAButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = Page.Locator("button");
        Assert.IsFalse(await button.EvaluateAsync<bool>("button => document.activeElement === button"));
        await button.FocusAsync();
        Assert.IsTrue(await button.EvaluateAsync<bool>("button => document.activeElement === button"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "focus should respect strictness")]
    public async Task FocusShouldRespectStrictness()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("div").FocusAsync());
        StringAssert.Contains("strict mode violation", exception.Message);
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should dispatch click event via ElementHandles")]
    public async Task ShouldDispatchClickEventViaElementhandles()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = Page.Locator("button");
        await button.DispatchEventAsync("click");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => window['result']"));
    }

    [PlaywrightTest("locator-misc-1.spec.ts", "should upload the file")]
    public async Task ShouldUploadTheFile()
    {
        await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
        var input = Page.Locator("input[type=file]");
        await input.SetInputFilesAsync(TestUtils.GetAsset("file-to-upload.txt"));
        Assert.AreEqual("file-to-upload.txt", await Page.EvaluateAsync<string>("e => e.files[0].name", await input.ElementHandleAsync()));
    }
}
