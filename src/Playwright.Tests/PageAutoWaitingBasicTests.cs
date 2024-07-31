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

public class PageAutoWaitingBasicTests : PageTestEx
{
    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigation when clicking anchor")]
    [Ignore("Flacky")]
    public async Task ShouldAwaitNavigationWhenClickingAnchor()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">empty.html</a>");
        await TaskUtils.WhenAll(
            Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
            Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

        Assert.AreEqual("route|navigated|click", string.Join("|", messages));
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await cross-process navigation when clicking anchor")]
    [Ignore("Flacky")]
    public async Task ShouldAwaitCrossProcessNavigationWhenClickingAnchor()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($"<a href=\"{Server.CrossProcessPrefix}/empty.html\">empty.html</a>");
        await TaskUtils.WhenAll(
            Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
            Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

        Assert.AreEqual("route|navigated|click", string.Join("|", messages));
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await form-get on click")]
    [Ignore("Flacky")]
    public async Task ShouldAwaitFormGetOnClick()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html?foo=bar", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($@"
                <form action=""{Server.EmptyPage}"" method=""get"">
                    <input name=""foo"" value=""bar"">
                    <input type=""submit"" value=""Submit"">
                </form>");

        await TaskUtils.WhenAll(
            Page.ClickAsync("input[type=submit]").ContinueWith(_ => messages.Add("click")),
            Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

        Assert.AreEqual("route|navigated|click", string.Join("|", messages));
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await form-post on click")]
    [Ignore("Flacky")]
    public async Task ShouldAwaitFormPostOnClick()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($@"
                <form action=""{Server.EmptyPage}"" method=""post"">
                    <input name=""foo"" value=""bar"">
                    <input type=""submit"" value=""Submit"">
                </form>");

        await TaskUtils.WhenAll(
            Page.ClickAsync("input[type=submit]").ContinueWith(_ => messages.Add("click")),
            Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

        Assert.AreEqual("route|navigated|click", string.Join("|", messages));
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should await navigating specified target")]
    [Ignore("Flacky")]
    public async Task ShouldAwaitNavigatingSpecifiedTarget()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($@"
                <a href=""{Server.EmptyPage}"" target=target>empty.html</a>
                <iframe name=target></iframe>");

        var frame = Page.Frame("target");

        await TaskUtils.WhenAll(
            Page.ClickAsync("a").ContinueWith(_ => messages.Add("click")),
            Page.WaitForNavigationAsync().ContinueWith(_ => messages.Add("navigated")));

        Assert.AreEqual(Server.EmptyPage, frame.Url);
        Assert.AreEqual("route|navigated|click", string.Join("|", messages));
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with dblclick without noWaitAfter when navigation is stalled")]
    public async Task ShouldWorkWithDblClickWithoutNoWaitAfterWhenNavigaionIsStalled()
    {
        Server.SetRoute("/empty.html", _ => Task.Delay(10000));
        await Page.SetContentAsync($"<a id=anchor href='{Server.EmptyPage}'>empty.html</a>");
        await Page.ClickAsync("a");
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with waitForLoadState(load)")]
    [Ignore("Flacky")]
    public async Task ShouldWorkWithWaitForLoadStateLoad()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">empty.html</a>");
        var clickLoaded = new TaskCompletionSource<bool>();

        await TaskUtils.WhenAll(
            Page.ClickAsync("a").ContinueWith(_ => Page.WaitForLoadStateAsync(LoadState.Load).ContinueWith(_ =>
            {
                messages.Add("clickload");
                clickLoaded.TrySetResult(true);
            })),
            clickLoaded.Task,
            Page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded }).ContinueWith(_ => messages.Add("domcontentloaded")));

        Assert.AreEqual("route|domcontentloaded|clickload", string.Join("|", messages));
    }

    [PlaywrightTest("page-autowaiting-basic.spec.ts", "should work with goto following click")]
    public async Task ShouldWorkWithGotoFollowingClick()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html", context =>
        {
            messages.Add("route");
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("You are logged in");
        });

        await Page.SetContentAsync($@"
                <form action=""{Server.EmptyPage}/login.html"" method=""get"">
                    <input type=""text"">
                    <input type=""submit"" value=""Submit"">
                </form>");

        await Page.FillAsync("input[type=text]", "admin");
        await Page.ClickAsync("input[type=submit]");
        await Page.GotoAsync(Server.EmptyPage);
    }
}
