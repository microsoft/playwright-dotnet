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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-add-init-script.spec.ts.</summary>
public class PageAddInitScriptTests
{
    [UpstreamTest("IS-001 should evaluate before anything else on the page")]
    public async Task ShouldEvaluateBeforeAnythingElseOnThePage(IPage page, Server server)
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-002 should work with a path")]
    public async Task ShouldWorkWithAPath(IPage page, Server server, Func<string, string> asset)
    {
        await page.AddInitScriptAsync(scriptPath: asset("injectedfile.js"));
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-003 should work with content @smoke")]
    public async Task ShouldWorkWithContent(IPage page, Server server)
    {
        await page.AddInitScriptAsync("window[\"injected\"] = 123");
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-005 should work with trailing comments")]
    public async Task ShouldWorkWithTrailingComments(IPage page, Func<string, string> asset)
    {
        await page.AddInitScriptAsync("// comment");
        await page.AddInitScriptAsync("window.secret = 42;");
        await page.GotoAsync("data:text/html,<html></html>");
        Assert.That(await page.EvaluateAsync<int>("secret"), Is.EqualTo(42));
    }

    [UpstreamTest("IS-006 should support multiple scripts")]
    public async Task ShouldSupportMultipleScripts(IPage page, Server server)
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync(@"(function() {
    window['script1'] = 1;
  })()");
        await page.AddInitScriptAsync(@"(function() {
    window['script2'] = 2;
  })()");
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['script1']"), Is.EqualTo(1));
        Assert.That(await page.EvaluateAsync<int>("() => window['script2']"), Is.EqualTo(2));
    }

    [UpstreamTest("IS-007 should work with CSP")]
    public async Task ShouldWorkWithCSP(IPage page, Server server)
    {
        server.SetCSP("/empty.html", "script-src " + server.Prefix);
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await page.GotoAsync(server.Prefix + "/empty.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['injected']"), Is.EqualTo(123));

        // Make sure CSP works.
        await Catch(() => page.AddScriptTagAsync(new() { Content = "window.e = 10;" }));
        Assert.That(await page.EvaluateAsync<object>("() => window['e']"), Is.Null);
    }

    [UpstreamTest("IS-008 should work after a cross origin navigation")]
    public async Task ShouldWorkAfterACrossOriginNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.CrossProcessPrefix);
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-009 should remove init script after dispose")]
    public async Task ShouldRemoveInitScriptAfterDispose(IPage page, Server server)
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        var disposable = await page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));

        await disposable.DisposeAsync();
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['result']"), Is.Null);
    }

    [UpstreamTest("IS-010 should remove one of multiple init scripts after dispose")]
    public async Task ShouldRemoveOneOfMultipleInitScriptsAfterDispose(IPage page, Server server)
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        var disposable1 = await page.AddInitScriptAsync(@"(function() {
    window['script1'] = 1;
  })()");
        await page.AddInitScriptAsync(@"(function() {
    window['script2'] = 2;
  })()");
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['script1']"), Is.EqualTo(1));
        Assert.That(await page.EvaluateAsync<int>("() => window['script2']"), Is.EqualTo(2));

        await disposable1.DisposeAsync();
        await page.GotoAsync(server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['script1']"), Is.Null);
        Assert.That(await page.EvaluateAsync<int>("() => window['script2']"), Is.EqualTo(2));
    }

    [UpstreamTest("IS-011 init script should run only once in iframe")]
    public async Task InitScriptShouldRunOnlyOnceInIframe(IPage page, Server server, string browserName, bool isBidi)
    {
        var messages = new List<string>();
        page.Console += (_, @event) =>
        {
            if (@event.Text.StartsWith("init script:", StringComparison.Ordinal))
            {
                messages.Add(@event.Text);
            }
        };
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync("(() => console.log('init script:', location.pathname || 'no url yet'))()");
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        Assert.That(messages, Is.EqualTo(new[]
        {
            "init script: /frames/one-frame.html",
            "init script: " + (browserName == "firefox" && !isBidi ? "no url yet" : "/frames/frame.html"),
        }));
    }
}
