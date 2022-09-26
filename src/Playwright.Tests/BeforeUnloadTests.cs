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

public class BeforeUnloadTests : PageTestEx
{
    [PlaywrightTest("beforeunload.spec.ts", "should close browser with beforeunload page")]
    public async Task ShouldCloseBrowserWithBeforeUnloadPage()
    {
        var browser = await BrowserType.LaunchAsync();
        var page = await browser.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // we have to interact with the page
        await page.ClickAsync("body");
        await browser.CloseAsync();
    }

    [PlaywrightTest("beforeunload.spec.ts", "should access page after beforeunload")]
    public async Task ShouldAccessPageAfterBeforeUnload()
    {
        await Page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // we have to interact with the page
        await Page.ClickAsync("body");
        var dialogT = new TaskCompletionSource<bool>();
        Page.Dialog += async (_, dialog) =>
        {
            await dialog.DismissAsync();
            dialogT.SetResult(true);
        };

        await Page.CloseAsync(new() { RunBeforeUnload = true });
        await dialogT.Task;

        await Page.EvaluateAsync("() => document.title");
    }

    [PlaywrightTest("beforeunload.spec.ts", "should run beforeunload if asked for")]
    public async Task ShouldRunBeforeunloadIfAskedFor()
    {
        var newPage = await Context.NewPageAsync();
        await newPage.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await newPage.ClickAsync("body");

        var dialogEvent = new TaskCompletionSource<IDialog>();
        newPage.Dialog += (_, dialog) => dialogEvent.TrySetResult(dialog);

        var pageClosingTask = newPage.CloseAsync(new() { RunBeforeUnload = true });
        var dialog = await dialogEvent.Task;
        Assert.AreEqual(DialogType.BeforeUnload, dialog.Type);
        Assert.IsEmpty(dialog.DefaultValue);
        if (TestConstants.IsChromium)
        {
            Assert.IsEmpty(dialog.Message);
        }
        else if (TestConstants.IsWebKit)
        {
            Assert.AreEqual("Leave?", dialog.Message);
        }
        else
        {
            StringAssert.Contains("This page is asking you to confirm that you want to leave", dialog.Message);
        }

        await dialog.AcceptAsync();
        await pageClosingTask;
    }

    [PlaywrightTest("beforeunload.spec.ts", "should *not* run beforeunload by default")]
    public async Task ShouldNotRunBeforeunloadByDefault()
    {
        var newPage = await Context.NewPageAsync();
        await newPage.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await newPage.ClickAsync("body");
        await newPage.CloseAsync();
    }

}
