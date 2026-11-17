/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-events.spec.ts.</summary>
public class BrowsercontextEventsTests
{
    [UpstreamTest("BC-033 console event should work with element handles")]
    public async Task ConsoleEventShouldWorkWithElementHandles(IPage page)
    {
        await page.SetContentAsync("<body>hello</body>");
        var (message, _) = await PromiseAll(
            page.Context.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => console.log(document.body)"));
        var body = message.Args[0];
        Assert.That(await body.EvaluateAsync<string>("x => x.nodeName"), Is.EqualTo("BODY"));
        await ((IElementHandle)body).ClickAsync();
    }

    [UpstreamTest("BC-034 console event should work in popup")]
    public async Task ConsoleEventShouldWorkInPopup(IPage page)
    {
        var (_, message, popup) = await PromiseAll(
            page.EvaluateAsync(@"() => {
      const win = window.open('');
      win.console.log('hello');
    }"),
            page.Context.WaitForConsoleMessageAsync(),
            page.WaitForPopupAsync());

        Assert.That(message.Text, Is.EqualTo("hello"));
        Assert.That(message.Page, Is.SameAs(popup));
    }

    [UpstreamTest("BC-035 console event should work in popup 2")]
    public async Task ConsoleEventShouldWorkInPopup2(IPage page, string browserName, bool isBidi)
    {
        Info.Fixme(browserName == "firefox" && !isBidi, "console message from javascript: url is not reported at all");

        var (_, message, popup) = await PromiseAll(
            page.EvaluateAsync(@"async () => {
      const win = window.open('javascript:console.log(""hello"")');
      await new Promise(f => window.builtins.setTimeout(f, 0));
      win.close();
    }"),
            page.Context.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Type == "log" }),
            page.Context.WaitForPageAsync());

        Assert.That(message.Text, Is.EqualTo("hello"));
        Assert.That(message.Page, Is.SameAs(popup));
    }

    [UpstreamTest("BC-036 console event should work in immediately closed popup")]
    public async Task ConsoleEventShouldWorkInImmediatelyClosedPopup(IPage page, string browserName, bool isBidi)
    {
        Info.Fixme(browserName == "firefox" && !isBidi, "console message is not reported at all");

        var (_, message, popup) = await PromiseAll(
            page.EvaluateAsync(@"async () => {
      const win = window.open();
      win.console.log('hello');
      win.close();
    }"),
            page.Context.WaitForConsoleMessageAsync(),
            page.WaitForPopupAsync());

        Assert.That(message.Text, Is.EqualTo("hello"));
        Assert.That(message.Page, Is.SameAs(popup));
    }

    [UpstreamTest("BC-037 dialog event should work @smoke")]
    public async Task DialogEventShouldWork(IPage page)
    {
        var promise = page.EvaluateAsync<string>("() => prompt('hey?')");
        var (dialog1, dialog2) = await PromiseAll(
            Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h),
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h));

        Assert.That(dialog1, Is.SameAs(dialog2));
        Assert.That(dialog1.Message, Is.EqualTo("hey?"));
        Assert.That(dialog1.Page, Is.SameAs(page));
        await dialog1.AcceptAsync("hello");
        Assert.That(await promise, Is.EqualTo("hello"));
    }

    [UpstreamTest("BC-038 dialogclosed event should work")]
    public async Task DialogclosedEventShouldWork(IPage page)
    {
        var promise = page.EvaluateAsync<string>("() => prompt('hey?')");
        var dialog = await Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h);
        var (closed1, closed2) = await PromiseAll(
            Once<IDialog>(h => page.Context.DialogClosed += h, h => page.Context.DialogClosed -= h),
            Once<IDialog>(h => page.DialogClosed += h, h => page.DialogClosed -= h),
            dialog.AcceptAsync("hello"));
        Assert.That(closed1, Is.SameAs(dialog));
        Assert.That(closed2, Is.SameAs(dialog));
        Assert.That(await promise, Is.EqualTo("hello"));
    }

    [UpstreamTest("BC-039 dialog event should work in popup")]
    public async Task DialogEventShouldWorkInPopup(IPage page)
    {
        var promise = page.EvaluateAsync<string>(@"() => {
    const win = window.open('');
    return win.prompt('hey?');
  }");

        var (dialog, popup) = await PromiseAll(
            Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h),
            page.WaitForPopupAsync());

        Assert.That(dialog.Message, Is.EqualTo("hey?"));
        Assert.That(dialog.Page, Is.SameAs(popup));
        await dialog.AcceptAsync("hello");
        Assert.That(await promise, Is.EqualTo("hello"));
    }

    [UpstreamTest("BC-040 dialog event should work in popup 2")]
    public async Task DialogEventShouldWorkInPopup2(IPage page, string browserName)
    {
        Info.Fixme(browserName == "firefox", "dialog from javascript: url is not reported at all");

        var promise = page.EvaluateAsync(@"async () => {
    window.open('javascript:prompt(""hey?"")');
  }");

        var dialog = await Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h);

        Assert.That(dialog.Message, Is.EqualTo("hey?"));
        Assert.That(dialog.Page, Is.Null);
        await dialog.AcceptAsync("hello");
        await promise;
    }

    [UpstreamTest("BC-041 dialog event should work in immediately closed popup")]
    public async Task DialogEventShouldWorkInImmediatelyClosedPopup(IPage page)
    {
        var promise = page.EvaluateAsync<string>(@"async () => {
    const win = window.open();
    const result = win.prompt('hey?');
    win.close();
    return result;
  }");

        var (dialog, popup) = await PromiseAll(
            Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h),
            page.WaitForPopupAsync());

        Assert.That(dialog.Message, Is.EqualTo("hey?"));
        Assert.That(dialog.Page, Is.SameAs(popup));
        await dialog.AcceptAsync("hello");
        Assert.That(await promise, Is.EqualTo("hello"));
    }

    [UpstreamTest("BC-042 dialog event should work with inline script tag")]
    public async Task DialogEventShouldWorkWithInlineScriptTag(IPage page, Server server)
    {
        server.SetRoute("/popup.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<script>window.result = prompt('hey?')</script>");
        });

        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a href='popup.html' target=_blank>Click me</a>");

        var promise = page.ClickAsync("a");
        var (dialog, popup) = await PromiseAll(
            Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h),
            page.Context.WaitForPageAsync());

        Assert.That(dialog.Message, Is.EqualTo("hey?"));
        Assert.That(dialog.Page, Is.SameAs(popup));
        await dialog.AcceptAsync("hello");
        await promise;
        await Expect(async () => Assert.That(await popup.EvaluateAsync<string>("window.result"), Is.EqualTo("hello"))).ToPassAsync();
    }

    [UpstreamTest("BC-043 weberror event should work")]
    public async Task WeberrorEventShouldWork(IPage page)
    {
        var webError = await PromiseAll(
            Once<IWebError>(h => page.Context.WebError += h, h => page.Context.WebError -= h),
            page.SetContentAsync("<script>throw new Error(\"boom\")</script>"));
        Assert.That(webError.Page, Is.SameAs(page));
        // .NET: webError.error().stack is webError.Error, a string.
        Assert.That(webError.Error, Does.Contain("boom"));
    }

    [UpstreamTest("BC-044 weberror event should include location")]
    public async Task WeberrorEventShouldIncludeLocation(IPage page, Server server)
    {
        server.SetRoute("/error.js", (req, res) =>
        {
            res.SetHeader("content-type", "application/javascript");
            res.End(@"
      function foo() {
        throw new Error('boom');
      }
      foo();
    ");
        });

        server.SetRoute("/error.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<script src=\"/error.js\"></script>");
        });

        var (webError, _) = await PromiseAll(
            Once<IWebError>(h => page.Context.WebError += h, h => page.Context.WebError -= h),
            page.GotoAsync(server.Prefix + "/error.html"));

        var location = webError.Location;
        Assert.That(location.Url, Is.EqualTo($"{server.Prefix}/error.js"));
        Assert.That(location.Line, Is.EqualTo(2));
        Assert.That(location.Column, Is.GreaterThan(0)); // column is not consistent across browsers
    }

    [UpstreamTest("BC-045 pageload event should work @smoke")]
    public async Task PageloadEventShouldWork(IPage page, Server server)
    {
        var (eventPage, _) = await PromiseAll(
            Once<IPage>(h => page.Context.PageLoad += h, h => page.Context.PageLoad -= h),
            page.GotoAsync(server.EmptyPage));
        Assert.That(eventPage, Is.SameAs(page));
    }

    [UpstreamTest("BC-046 framenavigated event should work @smoke")]
    public async Task FramenavigatedEventShouldWork(IPage page, Server server)
    {
        var (frame, _) = await PromiseAll(
            Once<IFrame>(h => page.Context.FrameNavigated += h, h => page.Context.FrameNavigated -= h),
            page.GotoAsync(server.EmptyPage));
        Assert.That(frame, Is.SameAs(page.MainFrame));
        Assert.That(frame.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("BC-047 pageclose event should work @smoke")]
    public async Task PagecloseEventShouldWork(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        var closed = await PromiseAll(
            Once<IPage>(h => context.PageClose += h, h => context.PageClose -= h),
            page.CloseAsync());
        Assert.That(closed, Is.SameAs(page));
    }

    [UpstreamTest("BC-048 frameattached event should work @smoke")]
    public async Task FrameattachedEventShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (frame, _) = await PromiseAll(
            Once<IFrame>(h => page.Context.FrameAttached += h, h => page.Context.FrameAttached -= h),
            page.EvaluateAsync(@"() => {
      const iframe = document.createElement('iframe');
      iframe.src = 'about:blank';
      document.body.appendChild(iframe);
    }"));
        Assert.That(frame.ParentFrame, Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("BC-049 framedetached event should work @smoke")]
    public async Task FramedetachedEventShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.id = 'x';
    iframe.src = 'about:blank';
    document.body.appendChild(iframe);
  }");
        await page.WaitForSelectorAsync("iframe");
        var (frame, _) = await PromiseAll(
            Once<IFrame>(h => page.Context.FrameDetached += h, h => page.Context.FrameDetached -= h),
            page.EvaluateAsync("() => document.getElementById('x').remove()"));
        Assert.That(frame.ParentFrame, Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("BC-050 download event should work @smoke")]
    public async Task DownloadEventShouldWork(IPage page, Server server)
    {
        server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.SetHeader("Content-Disposition", "attachment; filename=file.txt");
            res.End("Hello world");
        });
        await page.SetContentAsync($"<a href=\"{server.Prefix}/download\">download</a>");
        var download = await PromiseAll(
            Once<IDownload>(h => page.Context.Download += h, h => page.Context.Download -= h),
            page.ClickAsync("a"));
        Assert.That(download.SuggestedFilename, Is.EqualTo("file.txt"));
        Assert.That(download.Page, Is.SameAs(page));
    }
}
