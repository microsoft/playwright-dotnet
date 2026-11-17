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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-add-init-script.spec.ts.</summary>
public class BrowsercontextAddInitScriptTests : UpstreamBrowserTest
{
    [UpstreamTest("IS-024 should work with browser context scripts @smoke")]
    public async Task ShouldWorkWithBrowserContextScripts()
    {
        var context = await ContextFixture();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await context.AddInitScriptAsync("(() => window['temp'] = 123)()");
        var page = await context.NewPageAsync();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync("(() => window['injected'] = window['temp'])()");
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-025 should work without navigation, after all bindings")]
    public async Task ShouldWorkWithoutNavigationAfterAllBindings()
    {
        var context = await ContextFixture();
        var promise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await context.ExposeFunctionAsync("woof", (string arg) =>
        {
            promise.TrySetResult(arg);
        });

        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await context.AddInitScriptAsync(@"(() => {
    window['woof']('hey');
    window['temp'] = 123;
  })()");
        var page = await context.NewPageAsync();

        Assert.That(await page.EvaluateAsync<int>("() => window['temp']"), Is.EqualTo(123));
        Assert.That(await promise.Task, Is.EqualTo("hey"));
    }

    [UpstreamTest("IS-026 should work without navigation in popup")]
    public async Task ShouldWorkWithoutNavigationInPopup()
    {
        var context = await ContextFixture();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await context.AddInitScriptAsync("(() => window['temp'] = 123)()");
        var page = await context.NewPageAsync();
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window['win'] = window.open()"));
        Assert.That(await popup.EvaluateAsync<int>("() => window['temp']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-027 should work with browser context scripts with a path")]
    public async Task ShouldWorkWithBrowserContextScriptsWithAPath()
    {
        var context = await ContextFixture();
        await context.AddInitScriptAsync(scriptPath: Asset("injectedfile.js"));
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-028 should work with browser context scripts for already created pages")]
    public async Task ShouldWorkWithBrowserContextScriptsForAlreadyCreatedPages()
    {
        var context = await ContextFixture();
        var page = await context.NewPageAsync();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await context.AddInitScriptAsync("(() => window['temp'] = 123)()");
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync("(() => window['injected'] = window['temp'])()");
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(123));
    }

    [UpstreamTest("IS-029 should remove context init script after dispose")]
    public async Task ShouldRemoveContextInitScriptAfterDispose()
    {
        var context = await ContextFixture();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        var disposable = await context.AddInitScriptAsync("(() => window['temp'] = 123)()");
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<int>("() => window['temp']"), Is.EqualTo(123));

        await disposable.DisposeAsync();
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['temp']"), Is.Null);
    }

    [UpstreamTest("IS-030 should remove context init script and keep working in new pages")]
    public async Task ShouldRemoveContextInitScriptAndKeepWorkingInNewPages()
    {
        var context = await ContextFixture();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        var disposable = await context.AddInitScriptAsync("(() => window['temp'] = 123)()");
        await disposable.DisposeAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['temp']"), Is.Null);
    }

    [UpstreamTest("IS-035 init script should run only once in popup")]
    public async Task InitScriptShouldRunOnlyOnceInPopup()
    {
        var context = await ContextFixture();
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await context.AddInitScriptAsync(@"(() => {
    window['callCount'] = (window['callCount'] || 0) + 1;
  })()");
        var page = await context.NewPageAsync();
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open('about:blank')"));
        Assert.That(await popup.EvaluateAsync<int>("callCount"), Is.EqualTo(1));
    }
}
