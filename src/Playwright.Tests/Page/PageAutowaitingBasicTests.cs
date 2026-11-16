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
public class PageAutowaitingBasicTests
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
    public async Task ShouldAwaitNavigationWhenClickingAnchor(IPage page, Server server)
    {
        var messages = InitServer(server);
        await page.SetContentAsync($"<a id=\"anchor\" href=\"{server.EmptyPage}\">empty.html</a>");
        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await page.ClickAsync("a");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => page.FrameNavigated += h, h => page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-002 should not stall on JS navigation link")]
    public async Task ShouldNotStallOnJSNavigationLink(IPage page, string browserName)
    {
        await page.SetContentAsync("<a href=\"javascript:console.log(1)\">console.log</a>");
        await page.ClickAsync("a");
    }

    [UpstreamTest("AW-003 should await cross-process navigation when clicking anchor")]
    public async Task ShouldAwaitCrossProcessNavigationWhenClickingAnchor(IPage page, Server server)
    {
        var messages = InitServer(server);
        await page.SetContentAsync($"<a href=\"{server.CrossProcessPrefix + "/empty.html"}\">empty.html</a>");

        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await page.ClickAsync("a");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => page.FrameNavigated += h, h => page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-004 should await form-get on click")]
    public async Task ShouldAwaitFormGetOnClick(IPage page, Server server)
    {
        var messages = new List<string>();
        server.SetRoute("/empty.html?foo=bar", (req, res) =>
        {
            messages.Add("route");
            res.SetHeader("Content-Type", "text/html");
            res.End("<link rel='stylesheet' href='./one-style.css'>");
        });

        await page.SetContentAsync($@"
    <form action=""{server.EmptyPage}"" method=""get"">
      <input name=""foo"" value=""bar"">
      <input type=""submit"" value=""Submit"">
    </form>");

        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await page.ClickAsync("input[type=submit]");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => page.FrameNavigated += h, h => page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-005 should await form-post on click")]
    public async Task ShouldAwaitFormPostOnClick(IPage page, Server server)
    {
        var messages = InitServer(server);
        await page.SetContentAsync($@"
    <form action=""{server.EmptyPage}"" method=""post"">
      <input name=""foo"" value=""bar"">
      <input type=""submit"" value=""Submit"">
    </form>");

        // .NET: promise.then(() => messages.push('click')) is a local function that is called in its place.
        async Task Click()
        {
            await page.ClickAsync("input[type=submit]");
            messages.Add("click");
        }
        await PromiseAll(
            Click(),
            OnceThen<IFrame>(h => page.FrameNavigated += h, h => page.FrameNavigated -= h, _ => messages.Add("navigated")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|navigated|click"));
    }

    [UpstreamTest("AW-006 should work with noWaitAfter: true")]
    public async Task ShouldWorkWithNoWaitAfterTrue(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) => { });
        await page.SetContentAsync($"<a id=\"anchor\" href=\"{server.EmptyPage}\">empty.html</a>");
        await page.ClickAsync("a", new() { NoWaitAfter = true });
    }

    [UpstreamTest("AW-007 should work with dblclick without noWaitAfter when navigation is stalled")]
    public async Task ShouldWorkWithDblclickWithoutNoWaitAfterWhenNavigationIsStalled(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) => { });
        await page.SetContentAsync($"<a id=\"anchor\" href=\"{server.EmptyPage}\">empty.html</a>");
        await page.DblClickAsync("a");
    }

    [UpstreamTest("AW-008 should work with waitForLoadState(load)")]
    public async Task ShouldWorkWithWaitForLoadStateLoad(IPage page, Server server)
    {
        var messages = InitServer(server);
        await page.SetContentAsync($"<a id=\"anchor\" href=\"{server.EmptyPage}\">empty.html</a>");
        // .NET: promise.then(() => ...).then(() => messages.push('clickload')) is a local function that is called in its place.
        async Task Clickload()
        {
            await page.ClickAsync("a");
            await page.WaitForLoadStateAsync(LoadState.Load);
            messages.Add("clickload");
        }
        await PromiseAll(
            Clickload(),
            OnceThen<IPage>(h => page.Load += h, h => page.Load -= h, _ => messages.Add("load")));
        Assert.That(string.Join("|", messages), Is.EqualTo("route|load|clickload"));
    }

    [UpstreamTest("AW-009 should work with goto following click")]
    public async Task ShouldWorkWithGotoFollowingClick(IPage page, Server server)
    {
        server.SetRoute("/login.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("You are logged in");
        });

        await page.SetContentAsync($@"
    <form action=""{server.Prefix}/login.html"" method=""get"">
      <input type=""text"">
      <input type=""submit"" value=""Submit"">
    </form>");

        await page.FillAsync("input[type=text]", "admin");
        await page.ClickAsync("input[type=submit]");
        await page.GotoAsync(server.EmptyPage);
    }

    [UpstreamTest("AW-011 should report and collapse log in action")]
    public async Task ShouldReportAndCollapseLogInAction(IPage page, Server server, string mode)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox' style=\"visibility: hidden\"></input>");
        var error = await Catch(() => page.Locator("input").ClickAsync(new() { Timeout = 5000 }));
        var message = StripAnsi(error.Message);
        Assert.That(message, Does.Contain("Call log:"));
        Assert.That(message, Does.Match("\\d+ × waiting for"));
        var logLines = message.Substring(message.IndexOf("Call log:", StringComparison.Ordinal)).Split('\n');
        Assert.That(logLines.Length, Is.LessThan(30));
    }

    [UpstreamTest("AW-012 should report and collapse log in expect")]
    public async Task ShouldReportAndCollapseLogInExpect(IPage page, Server server, string mode)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox' style=\"visibility: hidden\"></input>");
        var error = await Catch(() => Expect(page.Locator("input")).ToBeVisibleAsync(new() { Timeout = 5000 }));
        var message = StripAnsi(error.Message);
        Assert.That(message, Does.Contain("Call log:"));
        Assert.That(message, Does.Match("\\d+ × locator resolved to"));
    }
}
