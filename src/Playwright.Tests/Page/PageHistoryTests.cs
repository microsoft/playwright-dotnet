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
public class PageHistoryTests : UpstreamPageTest
{
    [UpstreamTest("NA-071 page.goBack should work @smoke")]
    public async Task PageGoBackShouldWork()
    {
        Assert.That(await Page.GoBackAsync(), Is.Null);

        await Page.GotoAsync(Server.EmptyPage);
        await Page.GotoAsync(Server.Prefix + "/grid.html");

        var response = await Page.GoBackAsync();
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Does.Contain(Server.EmptyPage));

        response = await Page.GoForwardAsync();
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Does.Contain("/grid.html"));

        response = await Page.GoForwardAsync();
        Assert.That(response, Is.Null);
    }

    [UpstreamTest("NA-072 page.goBack should work with HistoryAPI")]
    public async Task PageGoBackShouldWorkWithHistoryAPI()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/first.html');
    history.pushState({}, '', '/second.html');
  }");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/second.html"));

        await Page.GoBackAsync();
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/first.html"));
        await Page.GoBackAsync();
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await Page.GoForwardAsync();
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/first.html"));
    }

    [UpstreamTest("NA-073 page.goBack should work for file urls")]
    public async Task PageGoBackShouldWorkForFileUrls()
    {
        Info.Skip(IsAndroid, "No files on Android");
        Info.Skip(Channel == "webkit-wsl");

        var url1 = new Uri(Asset("consolelog.html")).AbsoluteUri;
        var url2 = Server.Prefix + "/consolelog.html";
        await PromiseAll(
            Page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url1 }),
            Page.GotoAsync(url1));
        await Page.SetContentAsync($"<a href='{url2}'>url2</a>");
        Assert.That(Page.Url.ToLowerInvariant(), Is.EqualTo(url1.ToLowerInvariant()));

        await PromiseAll(
            Page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url2 }),
            Page.ClickAsync("a"));
        Assert.That(Page.Url, Is.EqualTo(url2));

        await PromiseAll(
            Page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url1 }),
            Page.GoBackAsync());
        Assert.That(Page.Url.ToLowerInvariant(), Is.EqualTo(url1.ToLowerInvariant()));
        // Should be able to evaluate in the new context, and
        // not reach for the old cross-process one.
        Assert.That(await Page.EvaluateAsync<int>("() => window.scrollX"), Is.EqualTo(0));
        // Should be able to screenshot.
        await Page.ScreenshotAsync();

        await PromiseAll(
            Page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "here:" + url2 }),
            Page.GoForwardAsync());
        Assert.That(Page.Url, Is.EqualTo(url2));
        Assert.That(await Page.EvaluateAsync<int>("() => window.scrollX"), Is.EqualTo(0));
        await Page.ScreenshotAsync();
    }

    [UpstreamTest("NA-074 goBack/goForward should work with bfcache-able pages")]
    public async Task GoBackGoForwardShouldWorkWithBfcacheAblePages()
    {
        await Page.GotoAsync(Server.Prefix + "/cached/bfcached.html");
        await Page.SetContentAsync($"<a href={JsonSerializer.Serialize(Server.Prefix + "/cached/bfcached.html?foo")}>click me</a>");
        await Page.ClickAsync("a");

        var response = await Page.GoBackAsync();
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/cached/bfcached.html"));
        // BFCache should be disabled.
        ExpectToEqual(await Page.EvaluateAsync<object>("window.didShow"), new { persisted = false });

        response = await Page.GoForwardAsync();
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/cached/bfcached.html?foo"));
    }

    [UpstreamTest("NA-075 page.reload should work")]
    public async Task PageReloadShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => window['_foo'] = 10");
        await Page.ReloadAsync();
        Assert.That(await Page.EvaluateAsync<object>("() => window['_foo']"), Is.Null);
    }

    [UpstreamTest("NA-076 page.reload should work with data url")]
    public async Task PageReloadShouldWorkWithDataUrl()
    {
        await Page.GotoAsync("data:text/html,hello");
        Assert.That(await Page.ContentAsync(), Does.Contain("hello"));
        Assert.That(await Page.ReloadAsync(), Is.Null);
        Assert.That(await Page.ContentAsync(), Does.Contain("hello"));
    }

    [UpstreamTest("NA-077 page.reload during renderer-initiated navigation")]
    public async Task PageReloadDuringRendererInitiatedNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        await Page.SetContentAsync("<form method='POST' action='/post'>Form is here<input type='submit'></form>");
        Server.SetRoute("/post", (req, res) => { });

        var reloadFailedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IRequest> handler = null;
        handler = async (_, _) =>
        {
            Page.Request -= handler;
            await Catch(() => Page.ReloadAsync());
            reloadFailedPromise.TrySetResult();
        };
        Page.Request += handler;
        var clickPromise = Catch(() => Page.ClickAsync("input[type=submit]"));
        await reloadFailedPromise.Task;
        await clickPromise;

        // Form submit should be canceled, and reload should eventually arrive
        // to the original one-style.html.
        await Page.WaitForSelectorAsync("text=hello");
    }

    [UpstreamTest("NA-078 page.reload should not resolve with same-document navigation")]
    public async Task PageReloadShouldNotResolveWithSameDocumentNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        // 1. Make sure execution contexts are ready for fast evaluate.
        await Page.EvaluateAsync("1");

        // 2. Stall the reload request.
        ServerResponse response = null;
        Server.SetRoute("/empty.html", (req, res) => { response = res; });
        var requestPromise = Server.WaitForRequest("/empty.html");

        // 3. Trigger push state that could resolve the reload.
        IgnoreErrors(Page.EvaluateAsync(@"() => {
    window.history.pushState({}, '');
  }"));

        // 4. Trigger the reload, it should not resolve.
        var reloadPromise = Page.ReloadAsync();

        // 5. Trigger push state again, for the good measure :)
        IgnoreErrors(Page.EvaluateAsync(@"() => {
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
    public async Task PageReloadShouldWorkWithSameOriginRedirect()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRedirect("/empty.html", Server.Prefix + "/title.html");
        await Page.ReloadAsync();
        await Expect(Page).ToHaveURLAsync(Server.Prefix + "/title.html");
    }

    [UpstreamTest("NA-080 page.reload should work with cross-origin redirect")]
    public async Task PageReloadShouldWorkWithCrossOriginRedirect()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRedirect("/empty.html", Server.CrossProcessPrefix + "/title.html");
        await Page.ReloadAsync();
        await Expect(Page).ToHaveURLAsync(Server.CrossProcessPrefix + "/title.html");
    }

    [UpstreamTest("NA-081 page.reload should work on a page with a hash")]
    public async Task PageReloadShouldWorkOnAPageWithAHash()
    {
        await Page.GotoAsync(Server.EmptyPage + "#hash");
        await Page.ReloadAsync();
        await Expect(Page).ToHaveURLAsync(Server.EmptyPage + "#hash");
    }

    [UpstreamTest("NA-082 page.reload should work on a page with a hash at the end")]
    public async Task PageReloadShouldWorkOnAPageWithAHashAtTheEnd()
    {
        await Page.GotoAsync(Server.EmptyPage + "#");
        await Page.ReloadAsync();
        await Expect(Page).ToHaveURLAsync(Server.EmptyPage + "#");
    }

    [UpstreamTest("NA-083 page.goBack during renderer-initiated navigation")]
    public async Task PageGoBackDuringRendererInitiatedNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<form method='POST' action='/post'>Form is here<input type='submit'></form>");
        Server.SetRoute("/post", (req, res) => { });

        var reloadFailedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IRequest> handler = null;
        handler = async (_, _) =>
        {
            Page.Request -= handler;
            await Catch(() => Page.GoBackAsync());
            reloadFailedPromise.TrySetResult();
        };
        Page.Request += handler;
        var clickPromise = Catch(() => Page.ClickAsync("input[type=submit]"));
        await reloadFailedPromise.Task;
        await clickPromise;

        // Form submit should be canceled, and goBack should eventually arrive
        // to the original one-style.html.
        await Page.WaitForSelectorAsync("text=hello");
    }

    [UpstreamTest("NA-084 page.goForward during renderer-initiated navigation")]
    public async Task PageGoForwardDuringRendererInitiatedNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        await Page.GoBackAsync();

        await Page.SetContentAsync("<form method='POST' action='/post'>Form is here<input type='submit'></form>");
        Server.SetRoute("/post", (req, res) => { });

        var reloadFailedPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<IRequest> handler = null;
        handler = async (_, _) =>
        {
            Page.Request -= handler;
            await Catch(() => Page.GoForwardAsync());
            reloadFailedPromise.TrySetResult();
        };
        Page.Request += handler;
        var clickPromise = Catch(() => Page.ClickAsync("input[type=submit]"));
        await reloadFailedPromise.Task;
        await clickPromise;

        // Form submit should be canceled, and goForward should eventually arrive
        // to the original one-style.html.
        await Page.WaitForSelectorAsync("text=hello");
    }

    [UpstreamTest("NA-085 regression test for issue 20791")]
    public async Task RegressionTestForIssue20791()
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        Server.SetRoute("/iframe.html", (req, res) =>
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
        Server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            res.End($@"
      <!doctype html>
      <iframe id=myframe src=""about:blank""></iframe>
      <script type=""text/javascript"">
        setTimeout(() => window.foo = 'foo', 0);
        setTimeout(() => myframe.contentDocument.location.href = '{Server.Prefix}/iframe.html', 0);
      </script>
    ");
        });
        var messages = new List<string>();
        Page.Console += (_, msg) => messages.Add(msg.Text);
        await Page.GotoAsync(Server.Prefix + "/main.html");
        await Expect(() => Assert.That(messages, Is.EqualTo(new[] { "foo" }))).ToPassAsync();
        await Page.ReloadAsync();
        await Expect(() => Assert.That(messages, Is.EqualTo(new[] { "foo", "foo" }))).ToPassAsync();
    }

    [UpstreamTest("NA-086 should reload proper page")]
    public async Task ShouldReloadProperPage()
    {
        var mainRequest = 0;
        var popupRequest = 0;
        Server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            res.End($"<!doctype html><h1>main: {++mainRequest}</h1>");
        });
        Server.SetRoute("/popup.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html; charset=utf-8" });
            res.End($"<!doctype html><h1>popup: {++popupRequest}</h1>");
        });
        await Page.GotoAsync(Server.Prefix + "/main.html");
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('/popup.html')"));
        await Expect(Page.Locator("h1")).ToHaveTextAsync("main: 1");
        await Expect(popup.Locator("h1")).ToHaveTextAsync("popup: 1");

        await Page.ReloadAsync();
        await Expect(Page.Locator("h1")).ToHaveTextAsync("main: 2");
        await Expect(popup.Locator("h1")).ToHaveTextAsync("popup: 1");

        await popup.ReloadAsync();
        await Expect(Page.Locator("h1")).ToHaveTextAsync("main: 2");
        await Expect(popup.Locator("h1")).ToHaveTextAsync("popup: 2");
    }
}
