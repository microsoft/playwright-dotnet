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
public class PageAddInitScriptTests : UpstreamPageTest
{
    [UpstreamTest("IS-001 should evaluate before anything else on the page")]
    public async Task ShouldEvaluateBeforeAnythingElseOnThePage()
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await Page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-002 should work with a path")]
    public async Task ShouldWorkWithAPath()
    {
        await Page.AddInitScriptAsync(scriptPath: Asset("injectedfile.js"));
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-003 should work with content @smoke")]
    public async Task ShouldWorkWithContent()
    {
        await Page.AddInitScriptAsync("window[\"injected\"] = 123");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-005 should work with trailing comments")]
    public async Task ShouldWorkWithTrailingComments()
    {
        await Page.AddInitScriptAsync("// comment");
        await Page.AddInitScriptAsync("window.secret = 42;");
        await Page.GotoAsync("data:text/html,<html></html>");
        Assert.That(await Page.EvaluateAsync<int>("secret"), Is.EqualTo(42));
    }

    [UpstreamTest("IS-006 should support multiple scripts")]
    public async Task ShouldSupportMultipleScripts()
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await Page.AddInitScriptAsync(@"(function() {
    window['script1'] = 1;
  })()");
        await Page.AddInitScriptAsync(@"(function() {
    window['script2'] = 2;
  })()");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['script1']"), Is.EqualTo(1));
        Assert.That(await Page.EvaluateAsync<int>("() => window['script2']"), Is.EqualTo(2));
    }

    [UpstreamTest("IS-007 should work with CSP")]
    public async Task ShouldWorkWithCSP()
    {
        Server.SetCSP("/empty.html", "script-src " + Server.Prefix);
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await Page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['injected']"), Is.EqualTo(123));

        // Make sure CSP works.
        await Catch(() => Page.AddScriptTagAsync(new() { Content = "window.e = 10;" }));
        Assert.That(await Page.EvaluateAsync<object>("() => window['e']"), Is.Null);
    }

    [UpstreamTest("IS-008 should work after a cross origin navigation")]
    public async Task ShouldWorkAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.CrossProcessPrefix);
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await Page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-009 should remove init script after dispose")]
    public async Task ShouldRemoveInitScriptAfterDispose()
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        var disposable = await Page.AddInitScriptAsync(@"(function() {
    window['injected'] = 123;
  })()");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));

        await disposable.DisposeAsync();
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<object>("() => window['result']"), Is.Null);
    }

    [UpstreamTest("IS-010 should remove one of multiple init scripts after dispose")]
    public async Task ShouldRemoveOneOfMultipleInitScriptsAfterDispose()
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        var disposable1 = await Page.AddInitScriptAsync(@"(function() {
    window['script1'] = 1;
  })()");
        await Page.AddInitScriptAsync(@"(function() {
    window['script2'] = 2;
  })()");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<int>("() => window['script1']"), Is.EqualTo(1));
        Assert.That(await Page.EvaluateAsync<int>("() => window['script2']"), Is.EqualTo(2));

        await disposable1.DisposeAsync();
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await Page.EvaluateAsync<object>("() => window['script1']"), Is.Null);
        Assert.That(await Page.EvaluateAsync<int>("() => window['script2']"), Is.EqualTo(2));
    }

    [UpstreamTest("IS-011 init script should run only once in iframe")]
    public async Task InitScriptShouldRunOnlyOnceInIframe()
    {
        var messages = new List<string>();
        Page.Console += (_, @event) =>
        {
            if (@event.Text.StartsWith("init script:", StringComparison.Ordinal))
            {
                messages.Add(@event.Text);
            }
        };
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await Page.AddInitScriptAsync("(() => console.log('init script:', location.pathname || 'no url yet'))()");
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        Assert.That(messages, Is.EqualTo(new[]
        {
            "init script: /frames/one-frame.html",
            "init script: " + (BrowserName == "firefox" && !IsBidi ? "no url yet" : "/frames/frame.html"),
        }));
    }
}
