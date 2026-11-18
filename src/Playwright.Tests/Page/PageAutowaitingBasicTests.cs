/**
 * Copyright 2018 Google Inc. All rights reserved.
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

/// <summary>Port of tests/page/page-autowaiting-basic.spec.ts.</summary>
public class PageAutowaitingBasicTests : UpstreamPageTest
{
    private static List<string> InitServer(Server server)
    {
        var messages = new List<string>();
        server.SetRoute("/empty.html", (req, res) =>
        {
            messages.Add("route");
            res.SetHeader("Content-Type", "text/html");
            res.End("<link rel='stylesheet' href='./one-style.css'>");
        });
        return messages;
    }

    [UpstreamTest("AW-001 should await navigation when clicking anchor")]
    public async Task ShouldAwaitNavigationWhenClickingAnchor()
    {
        var messages = InitServer(Server);
        await Page.SetContentAsync($"<a id=\"anchor\" href=\"{Server.EmptyPage}\">empty.html</a>");
        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await Page.ClickAsync("a");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => Page.FrameNavigated += h, h => Page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-002 should not stall on JS navigation link")]
    public async Task ShouldNotStallOnJSNavigationLink()
    {
        await Page.SetContentAsync("<a href=\"javascript:console.log(1)\">console.log</a>");
        await Page.ClickAsync("a");
    }

    [UpstreamTest("AW-003 should await cross-process navigation when clicking anchor")]
    public async Task ShouldAwaitCrossProcessNavigationWhenClickingAnchor()
    {
        var messages = InitServer(Server);
        await Page.SetContentAsync($"<a href=\"{Server.CrossProcessPrefix + "/empty.html"}\">empty.html</a>");

        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await Page.ClickAsync("a");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => Page.FrameNavigated += h, h => Page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-004 should await form-get on click")]
    public async Task ShouldAwaitFormGetOnClick()
    {
        var messages = new List<string>();
        Server.SetRoute("/empty.html?foo=bar", (req, res) =>
        {
            messages.Add("route");
            res.SetHeader("Content-Type", "text/html");
            res.End("<link rel='stylesheet' href='./one-style.css'>");
        });

        await Page.SetContentAsync($@"
    <form action=""{Server.EmptyPage}"" method=""get"">
      <input name=""foo"" value=""bar"">
      <input type=""submit"" value=""Submit"">
    </form>");

        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await Page.ClickAsync("input[type=submit]");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => Page.FrameNavigated += h, h => Page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-005 should await form-post on click")]
    public async Task ShouldAwaitFormPostOnClick()
    {
        var messages = InitServer(Server);
        await Page.SetContentAsync($@"
    <form action=""{Server.EmptyPage}"" method=""post"">
      <input name=""foo"" value=""bar"">
      <input type=""submit"" value=""Submit"">
    </form>");

        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await Page.ClickAsync("input[type=submit]");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => Page.FrameNavigated += h, h => Page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-006 should work with noWaitAfter: true")]
    public async Task ShouldWorkWithNoWaitAfterTrue()
    {
        Server.SetRoute("/empty.html", (req, res) => { });
        await Page.SetContentAsync($"<a id=\"anchor\" href=\"{Server.EmptyPage}\">empty.html</a>");
        await Page.ClickAsync("a", new() { NoWaitAfter = true });
    }

    [UpstreamTest("AW-007 should work with dblclick without noWaitAfter when navigation is stalled")]
    public async Task ShouldWorkWithDblclickWithoutNoWaitAfterWhenNavigationIsStalled()
    {
        Server.SetRoute("/empty.html", (req, res) => { });
        await Page.SetContentAsync($"<a id=\"anchor\" href=\"{Server.EmptyPage}\">empty.html</a>");
        await Page.DblClickAsync("a");
    }

    [UpstreamTest("AW-008 should work with waitForLoadState(load)")]
    public async Task ShouldWorkWithWaitForLoadStateLoad()
    {
        var messages = InitServer(Server);
        await Page.SetContentAsync($"<a id=\"anchor\" href=\"{Server.EmptyPage}\">empty.html</a>");
        // .NET: promise.then(() => ...).then(() => messages.push('clickload')) is a local function that is called in its place.
        async Task Clickload()
        {
            await Page.ClickAsync("a");
            await Page.WaitForLoadStateAsync(LoadState.Load);
            messages.Add("clickload");
        }
        await PromiseAll(
            Clickload(),
            OnceThen<IPage>(h => Page.Load += h, h => Page.Load -= h, _ => messages.Add("load")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|load|clickload"));
    }

    [UpstreamTest("AW-009 should work with goto following click")]
    public async Task ShouldWorkWithGotoFollowingClick()
    {
        Server.SetRoute("/login.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("You are logged in");
        });

        await Page.SetContentAsync($@"
    <form action=""{Server.Prefix}/login.html"" method=""get"">
      <input type=""text"">
      <input type=""submit"" value=""Submit"">
    </form>");

        await Page.FillAsync("input[type=text]", "admin");
        await Page.ClickAsync("input[type=submit]");
        await Page.GotoAsync(Server.EmptyPage);
    }

    [UpstreamTest("AW-011 should report and collapse log in action")]
    public async Task ShouldReportAndCollapseLogInAction()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' style=\"visibility: hidden\"></input>");
        var error = await Catch(() => Page.Locator("input").ClickAsync(new() { Timeout = 5000 }));
        var message = StripAnsi(error.Message);
        Assert.That(message, Does.Contain("Call log:"));
        Assert.That(message, Does.Match("\\d+ × waiting for"));
        var logLines = message.Substring(message.IndexOf("Call log:", StringComparison.Ordinal)).Split('\n');
        Assert.That(logLines.Length, Is.LessThan(30));
    }

    [UpstreamTest("AW-012 should report and collapse log in expect")]
    public async Task ShouldReportAndCollapseLogInExpect()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' style=\"visibility: hidden\"></input>");
        var error = await Catch(() => Expect(Page.Locator("input")).ToBeVisibleAsync(new() { Timeout = 5000 }));
        var message = StripAnsi(error.Message);
        Assert.That(message, Does.Contain("Call log:"));
        Assert.That(message, Does.Match("\\d+ × locator resolved to"));
    }
}
