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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-history.spec.ts.</summary>
public class PageHistoryTests
{
    [UpstreamTest("NA-071 page.goBack should work @smoke")]
    public async Task PageGoBackShouldWork(IPage page, Server server)
    {
        Assert.That(await page.GoBackAsync(), Is.Null);

        await page.GotoAsync(server.EmptyPage);
        await page.GotoAsync(server.Prefix + "/grid.html");

        var response = await page.GoBackAsync();
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Does.Contain(server.EmptyPage));

        response = await page.GoForwardAsync();
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Does.Contain("/grid.html"));

        response = await page.GoForwardAsync();
        Assert.That(response, Is.Null);
    }

    [UpstreamTest("NA-072 page.goBack should work with HistoryAPI")]
    public async Task PageGoBackShouldWorkWithHistoryAPI(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/first.html');
    history.pushState({}, '', '/second.html');
  }");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/second.html"));

        await page.GoBackAsync();
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/first.html"));
        await page.GoBackAsync();
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await page.GoForwardAsync();
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/first.html"));
    }

    [UpstreamTest("NA-073 page.goBack should work for file urls")]
    public async Task PageGoBackShouldWorkForFileUrls(IPage page, Server server, Func<string, string> asset, string channel, bool isAndroid, string mode)
    {
        Info.Skip(isAndroid, "No files on Android");
        Info.Skip(channel == "webkit-wsl");

        var url1 = new Uri(asset("consolelog.html")).AbsoluteUri;
        var url2 = server.Prefix + "/consolelog.html";
        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url1 }),
            page.GotoAsync(url1));
        await page.SetContentAsync($"<a href='{url2}'>url2</a>");
        Assert.That(page.Url.ToLowerInvariant(), Is.EqualTo(url1.ToLowerInvariant()));

        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url2 }),
            page.ClickAsync("a"));
        Assert.That(page.Url, Is.EqualTo(url2));

        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url1 }),
            page.GoBackAsync());
        Assert.That(page.Url.ToLowerInvariant(), Is.EqualTo(url1.ToLowerInvariant()));
        // Should be able to evaluate in the new context, and
        // not reach for the old cross-process one.
        Assert.That(await page.EvaluateAsync<int>("() => window.scrollX"), Is.EqualTo(0));
        // Should be able to screenshot.
        await page.ScreenshotAsync();

        await PromiseAll(
            page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url2 }),
            page.GoForwardAsync());
        Assert.That(page.Url, Is.EqualTo(url2));
        Assert.That(await page.EvaluateAsync<int>("() => window.scrollX"), Is.EqualTo(0));
        await page.ScreenshotAsync();
    }

    [UpstreamTest("NA-074 goBack/goForward should work with bfcache-able pages")]
    public async Task GoBackGoForwardShouldWorkWithBfcacheAblePages(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/cached/bfcached.html");
        await page.SetContentAsync($"<a href={JsonSerializer.Serialize(server.Prefix + "/cached/bfcached.html?foo")}>click me</a>");
        await page.ClickAsync("a");

        var response = await page.GoBackAsync();
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/cached/bfcached.html"));
        // BFCache should be disabled.
        ExpectToEqual(await page.EvaluateAsync<object>("window.didShow"), new { persisted = false });

        response = await page.GoForwardAsync();
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/cached/bfcached.html?foo"));
    }

    [UpstreamTest("NA-075 page.reload should work")]
    public async Task PageReloadShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => window['_foo'] = 10");
        await page.ReloadAsync();
        Assert.That(await page.EvaluateAsync<object>("() => window['_foo']"), Is.Null);
    }

    [UpstreamTest("NA-076 page.reload should work with data url")]
    public async Task PageReloadShouldWorkWithDataUrl(IPage page, Server server)
    {
        await page.GotoAsync("data:text/html,hello");
        Assert.That(await page.ContentAsync(), Does.Contain("hello"));
        Assert.That(await page.ReloadAsync(), Is.Null);
        Assert.That(await page.ContentAsync(), Does.Contain("hello"));
    }

    [UpstreamTest("NA-077 page.reload during renderer-initiated navigation")]
    public async Task PageReloadDuringRendererInitiatedNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await page.SetContentAsync("<form method='POST' action='/post'>Form is here<input type='submit'></form>");
        server.SetRoute("/post", (req, res) => { });

        var reloadFailedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IRequest> handler = null;
        handler = async (_, _) =>
        {
            page.Request -= handler;
            await Catch(() => page.ReloadAsync());
            reloadFailedPromise.TrySetResult();
        };
        page.Request += handler;
        var clickPromise = Catch(() => page.ClickAsync("input[type=submit]"));
        await reloadFailedPromise.Task;
        await clickPromise;

        // Form submit should be canceled, and reload should eventually arrive
        // to the original one-style.html.
        await page.WaitForSelectorAsync("text=hello");
    }

    [UpstreamTest("NA-078 page.reload should not resolve with same-document navigation")]
    public async Task PageReloadShouldNotResolveWithSameDocumentNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        // 1. Make sure execution contexts are ready for fast evaluate.
        await page.EvaluateAsync("1");

        // 2. Stall the reload request.
        ServerResponse response = null;
        server.SetRoute("/empty.html", (req, res) => { response = res; });
        var requestPromise = server.WaitForRequest("/empty.html");

        // 3. Trigger push state that could resolve the reload.
        IgnoreErrors(page.EvaluateAsync(@"() => {
    window.history.pushState({}, '');
  }"));

        // 4. Trigger the reload, it should not resolve.
        var reloadPromise = page.ReloadAsync();

        // 5. Trigger push state again, for the good measure :)
        IgnoreErrors(page.EvaluateAsync(@"() => {
    window.history.pushState({}, '');
  }"));

        // 5. Serve the request, it should resolve the reload.
        await requestPromise;
        response.End("hello");

        // 6. Check the reload response.
        var gotResponse = await reloadPromise;
        Assert.That(await gotResponse.TextAsync(), Is.EqualTo("hello"));
    }

    [UpstreamTest("NA-079 page.reload should work with same origin redirect")]
    public async Task PageReloadShouldWorkWithSameOriginRedirect(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRedirect("/empty.html", server.Prefix + "/title.html");
        await page.ReloadAsync();
        await Expect(page).ToHaveURLAsync(server.Prefix + "/title.html");
    }

    [UpstreamTest("NA-080 page.reload should work with cross-origin redirect")]
    public async Task PageReloadShouldWorkWithCrossOriginRedirect(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRedirect("/empty.html", server.CrossProcessPrefix + "/title.html");
        await page.ReloadAsync();
        await Expect(page).ToHaveURLAsync(server.CrossProcessPrefix + "/title.html");
    }

    [UpstreamTest("NA-081 page.reload should work on a page with a hash")]
    public async Task PageReloadShouldWorkOnAPageWithAHash(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage + "#hash");
        await page.ReloadAsync();
        await Expect(page).ToHaveURLAsync(server.EmptyPage + "#hash");
    }

    [UpstreamTest("NA-082 page.reload should work on a page with a hash at the end")]
    public async Task PageReloadShouldWorkOnAPageWithAHashAtTheEnd(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage + "#");
        await page.ReloadAsync();
        await Expect(page).ToHaveURLAsync(server.EmptyPage + "#");
    }

    [UpstreamTest("NA-083 page.goBack during renderer-initiated navigation")]
    public async Task PageGoBackDuringRendererInitiatedNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<form method='POST' action='/post'>Form is here<input type='submit'></form>");
        server.SetRoute("/post", (req, res) => { });

        var reloadFailedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IRequest> handler = null;
        handler = async (_, _) =>
        {
            page.Request -= handler;
            await Catch(() => page.GoBackAsync());
            reloadFailedPromise.TrySetResult();
        };
        page.Request += handler;
        var clickPromise = Catch(() => page.ClickAsync("input[type=submit]"));
        await reloadFailedPromise.Task;
        await clickPromise;

        // Form submit should be canceled, and goBack should eventually arrive
        // to the original one-style.html.
        await page.WaitForSelectorAsync("text=hello");
    }

    [UpstreamTest("NA-084 page.goForward during renderer-initiated navigation")]
    public async Task PageGoForwardDuringRendererInitiatedNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await page.GoBackAsync();

        await page.SetContentAsync("<form method='POST' action='/post'>Form is here<input type='submit'></form>");
        server.SetRoute("/post", (req, res) => { });

        var reloadFailedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IRequest> handler = null;
        handler = async (_, _) =>
        {
            page.Request -= handler;
            await Catch(() => page.GoForwardAsync());
            reloadFailedPromise.TrySetResult();
        };
        page.Request += handler;
        var clickPromise = Catch(() => page.ClickAsync("input[type=submit]"));
        await reloadFailedPromise.Task;
        await clickPromise;

        // Form submit should be canceled, and goForward should eventually arrive
        // to the original one-style.html.
        await page.WaitForSelectorAsync("text=hello");
    }

    [UpstreamTest("NA-085 regression test for issue 20791")]
    public async Task RegressionTestForIssue20791(IPage page, Server server)
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        server.SetRoute("/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            // iframe access parent frame to log a value from it.
            res.End(@"
      <!doctype html>
      <script type=""text/javascript"">
        console.log(window.parent.foo);
      </script>
    ");
        });
        server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            res.End($@"
      <!doctype html>
      <iframe id=myframe src=""about:blank""></iframe>
      <script type=""text/javascript"">
        setTimeout(() => window.foo = 'foo', 0);
        setTimeout(() => myframe.contentDocument.location.href = '{server.Prefix}/iframe.html', 0);
      </script>
    ");
        });
        var messages = new List<string>();
        page.Console += (_, msg) => messages.Add(msg.Text);
        await page.GotoAsync(server.Prefix + "/main.html");
        await Expect(() => Assert.That(messages, Is.EqualTo(new[] { "foo" }))).ToPassAsync();
        await page.ReloadAsync();
        await Expect(() => Assert.That(messages, Is.EqualTo(new[] { "foo", "foo" }))).ToPassAsync();
    }

    [UpstreamTest("NA-086 should reload proper page")]
    public async Task ShouldReloadProperPage(IPage page, Server server)
    {
        var mainRequest = 0;
        var popupRequest = 0;
        server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            res.End($"<!doctype html><h1>main: {++mainRequest}</h1>");
        });
        server.SetRoute("/popup.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            res.End($"<!doctype html><h1>popup: {++popupRequest}</h1>");
        });
        await page.GotoAsync(server.Prefix + "/main.html");
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open('/popup.html')"));
        await Expect(page.Locator("h1")).ToHaveTextAsync("main: 1");
        await Expect(popup.Locator("h1")).ToHaveTextAsync("popup: 1");

        await page.ReloadAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("main: 2");
        await Expect(popup.Locator("h1")).ToHaveTextAsync("popup: 1");

        await popup.ReloadAsync();
        await Expect(page.Locator("h1")).ToHaveTextAsync("main: 2");
        await Expect(popup.Locator("h1")).ToHaveTextAsync("popup: 2");
    }
}
