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

using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class BrowserContextEventsTests : PageTestEx
{
    [PlaywrightTest("browsercontext-events.spec.ts", "console event should work")]
    public async Task ConsoleEventShouldWork()
    {
        var (message, _) = await TaskUtils.WhenAll(
            Page.Context.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => console.log('hello')")
        );

        Assert.AreEqual("hello", message.Text);
        Assert.AreEqual(Page, message.Page);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "console event should work in popup")]
    public async Task ConsoleEventShouldWorkInPopup1()
    {
        var (message, popup, _) = await TaskUtils.WhenAll(
            Page.Context.WaitForConsoleMessageAsync(),
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync(@"() => {
                const win = window.open('');
                win.console.log('hello');
            }")
        );

        Assert.AreEqual("hello", message.Text);
        Assert.AreEqual(popup, message.Page);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "console event should work in popup 2")]
    [Skip(SkipAttribute.Targets.Firefox)] // console message from javascript: url is not reported at all
    public async Task ConsoleEventShouldWorkInPopup2()
    {
        var (message, popup, _) = await TaskUtils.WhenAll(
            Page.Context.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Type == "log" }),
            Page.Context.WaitForPageAsync(),
            Page.EvaluateAsync(@"async () => {
                const win = window.open('javascript:console.log(""hello"")');
                await new Promise(f => setTimeout(f, 0));
                win.close();
            }")
        );

        Assert.AreEqual("hello", message.Text);
        Assert.AreEqual(popup, message.Page);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "console event should work in immediately closed popup")]
    [Skip(SkipAttribute.Targets.Firefox)] // console message is not reported at all
    public async Task ConsoleEventShouldWorkInImmediatelyClosedPopup()
    {
        var (message, popup, _) = await TaskUtils.WhenAll(
            Page.Context.WaitForConsoleMessageAsync(),
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync(@"async () => {
                const win = window.open();
                win.console.log('hello');
                win.close();
            }")
        );

        Assert.AreEqual("hello", message.Text);
        Assert.AreEqual(popup, message.Page);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "dialog event should work")]
    public async Task DialogEventShouldWork()
    {
        var task = Page.EvaluateAsync<string>("() => prompt('hey?')");
        var (dialog1, dialog2) = await TaskUtils.WhenAll(
            Page.Context.WaitForDialogAsync(),
            Page.WaitForDialogAsync()
        );
        Assert.AreEqual(dialog1, dialog2);
        Assert.AreEqual("hey?", dialog1.Message);
        Assert.AreEqual(Page, dialog1.Page);
        await dialog1.AcceptAsync("hello");
        Assert.AreEqual("hello", await task);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "dialog event should work in popup")]
    public async Task DialogEventShouldWorkInPopup()
    {
        var task = Page.EvaluateAsync<string>(@"() => {
            const win = window.open('');
            return win.prompt('hey?');
        }");
        var (dialog, popup) = await TaskUtils.WhenAll(
            Page.Context.WaitForDialogAsync(),
            Page.WaitForPopupAsync()
        );
        Assert.AreEqual("hey?", dialog.Message);
        Assert.AreEqual(popup, dialog.Page);
        await dialog.AcceptAsync("hello");
        Assert.AreEqual("hello", await task);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "dialog event should work in popup 2")]
    [Skip(SkipAttribute.Targets.Firefox)] // dialog from javascript: url is not reported at all
    public async Task DialogEventShouldWorkInPopup2()
    {
        var task = Page.EvaluateAsync(@"() => {
            window.open('javascript:prompt(""hey?"")');
        }");
        var dialog = await Page.Context.WaitForDialogAsync();

        Assert.AreEqual("hey?", dialog.Message);
        Assert.AreEqual(null, dialog.Page);
        await dialog.AcceptAsync("hello");
        await task;
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "dialog event should work in immediately closed popup")]
    public async Task DialogEventShouldWorkInImmediatelyClosedPopup()
    {
        var task = Page.EvaluateAsync<string>(@"() => {
            const win = window.open();
            const result = win.prompt('hey?');
            win.close();
            return result;
        }");
        var (dialog, popup) = await TaskUtils.WhenAll(
            Page.Context.WaitForDialogAsync(),
            Page.WaitForPopupAsync()
        );

        Assert.AreEqual("hey?", dialog.Message);
        Assert.AreEqual(popup, dialog.Page);
        await dialog.AcceptAsync("hello");
        Assert.AreEqual("hello", await task);
    }

    [PlaywrightTest("browsercontext-events.spec.ts", "dialog event should work with inline script tag")]
    public async Task DialogEventShouldWorkWithInlineScriptTag()
    {
        Server.SetRoute("/popup.html", context =>
        {
            context.Response.Headers["content-type"] = "text/html";
            return context.Response.WriteAsync("<script>window.result = prompt('hey?')</script>");
        });

        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a href='popup.html' target=_blank>Click me</a>");

        var promise = Page.ClickAsync("a");
        var (dialog, popup) = await TaskUtils.WhenAll(
            Page.Context.WaitForDialogAsync(),
            Page.Context.WaitForPageAsync()
        );

        Assert.AreEqual("hey?", dialog.Message);
        Assert.AreEqual(popup, dialog.Page);
        await dialog.AcceptAsync("hello");
        await promise;
        Assert.AreEqual("hello", await popup.EvaluateAsync<string>("window.result"));
    }
}
