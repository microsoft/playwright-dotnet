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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-expose-function.spec.ts.</summary>
public class BrowsercontextExposeFunctionTests
{
    [UpstreamTest("EF-022 expose binding should work")]
    public async Task ExposeBindingShouldWork(IBrowserContext context)
    {
        BindingSource bindingSource = null;
        await context.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
        {
            bindingSource = source;
            return a + b;
        });
        var page = await context.NewPageAsync();
        var result = await page.EvaluateAsync<int>("add(5, 6)");
        Assert.That(bindingSource.Context, Is.SameAs(context));
        Assert.That(bindingSource.Page, Is.SameAs(page));
        Assert.That(bindingSource.Frame, Is.SameAs(page.MainFrame));
        Assert.That(result, Is.EqualTo(11));
    }

    [UpstreamTest("EF-023 should work")]
    public async Task ShouldWork(IBrowserContext context, Server server)
    {
        await context.ExposeFunctionAsync("add", (int a, int b) => a + b);
        var page = await context.NewPageAsync();
        await page.ExposeFunctionAsync("mul", (int a, int b) => a * b);
        await context.ExposeFunctionAsync("sub", (int a, int b) => a - b);
        // .NET: the binding source is not destructured: { frame } is source.Frame.
        await context.ExposeBindingAsync("addHandle", async (BindingSource source, int a, int b) =>
        {
            var handle = await source.Frame.EvaluateHandleAsync("([a, b]) => a + b", new[] { a, b });
            return handle;
        });
        var result = await page.EvaluateAsync<object>("(async () => ({ mul: await mul(9, 4), add: await add(9, 4), sub: await sub(9, 4), addHandle: await addHandle(5, 6) }))()");
        ExpectToEqual(result, new { mul = 36, add = 13, sub = 5, addHandle = 11 });
    }

    [UpstreamTest("EF-024 should dispose")]
    public async Task ShouldDispose(IBrowserContext context, Server server)
    {
        var binding = await context.ExposeFunctionAsync("compute", (int a, int b) =>
        {
            return a * b;
        });
        var page = await context.NewPageAsync();
        var result = await page.EvaluateAsync<int>(@"async function() {
    return await window['compute'](9, 4);
  }");
        Assert.That(result, Is.EqualTo(36));
        await binding.DisposeAsync();

        var e = await Catch(() => page.EvaluateAsync(@"async function() {
    return await window['compute'](9, 4);
  }"));
        Assert.That(e.Message, Does.Contain("is not a function"));
    }

    [UpstreamTest("EF-025 should throw for duplicate registrations")]
    public async Task ShouldThrowForDuplicateRegistrations(IBrowserContext context, Server server)
    {
        await context.ExposeFunctionAsync("foo", () => { });
        await context.ExposeFunctionAsync("bar", () => { });
        var error = await Catch(() => context.ExposeFunctionAsync("foo", () => { }));
        Assert.That(error.Message, Does.Contain("Function \"foo\" has been already registered"));
        var page = await context.NewPageAsync();
        error = await Catch(() => page.ExposeFunctionAsync("foo", () => { }));
        Assert.That(error.Message, Does.Contain("Function \"foo\" has been already registered in the browser context"));
        await page.ExposeFunctionAsync("baz", () => { });
        error = await Catch(() => context.ExposeFunctionAsync("baz", () => { }));
        Assert.That(error.Message, Does.Contain("Function \"baz\" has been already registered in one of the pages"));
    }

    [UpstreamTest("EF-026 should be callable from-inside addInitScript")]
    public async Task ShouldBeCallableFromInsideAddInitScript(IBrowserContext context, Server server)
    {
        List<string> args = [];
        await context.ExposeFunctionAsync("woof", (string arg) =>
        {
            args.Add(arg);
        });
        await context.AddInitScriptAsync("window[\"woof\"](\"context\")");
        var page = await context.NewPageAsync();
        await page.EvaluateAsync("undefined");
        await Expect(() => Assert.That(args, Is.EqualTo(new[] { "context" }))).ToPassAsync();
        args = [];
        await page.AddInitScriptAsync("window[\"woof\"](\"page\")");
        await page.ReloadAsync();
        await Expect(() => Assert.That(args, Is.EqualTo(new[] { "context", "page" }))).ToPassAsync();
    }

    [UpstreamTest("EF-027 should work with CSP")]
    [Fixme("dotnet: a call of a binding of ExposeBindingAsync(name, Action) fails with \"TargetParameterCountException: Parameter count mismatch.\": BindingCall.CallAsync passes the BindingSource to the callback, which has no parameters")]
    public async Task ShouldWorkWithCSP(IPage page, IBrowserContext context, Server server)
    {
        server.SetCSP("/empty.html", "default-src \"self\"");
        await page.GotoAsync(server.EmptyPage);
        var called = false;
        await context.ExposeBindingAsync("hi", () => called = true);
        await page.EvaluateAsync("() => window.hi()");
        Assert.That(called, Is.EqualTo(true));
    }

    [UpstreamTest("EF-028 should call binding from pagehide handler when navigating away")]
    public async Task ShouldCallBindingFromPagehideHandlerWhenNavigatingAway(IBrowserContext context, IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "chromium", "Chromium drops CDP events from the old document after the frame host swap");
        var calls = new List<string>();
        await context.ExposeBindingAsync("reportFromPage", (BindingSource _source, string message) => { calls.Add(message); });
        var html = (string body) => $"<!doctype html>{body}<script>addEventListener('pagehide', () => window.reportFromPage('pagehide of ' + location.pathname));</script>";
        server.SetRoute("/a.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(html("<a id=\"next\" href=\"/b.html\">next</a>"));
        });
        server.SetRoute("/b.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(html("<p>b</p>"));
        });
        await page.GotoAsync(server.Prefix + "/a.html");
        await page.ClickAsync("#next");
        await page.WaitForURLAsync("**/b.html");
        await Expect(() => Assert.That(calls, Does.Contain("pagehide of /a.html"))).ToPassAsync();
    }
}
