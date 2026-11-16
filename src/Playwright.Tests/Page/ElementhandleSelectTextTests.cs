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

/// <summary>Port of tests/page/elementhandle-select-text.spec.ts.</summary>
public class ElementhandleSelectTextTests
{
    [UpstreamTest("EH-102 should select textarea")]
    public async Task ShouldSelectTextarea(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
        await textarea.SelectTextAsync();
        Assert.That(await page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("some value"));
    }

    [UpstreamTest("EH-103 should select input")]
    public async Task ShouldSelectInput(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var input = await page.QuerySelectorAsync("input");
        await input.EvaluateAsync("input => input.value = 'some value'");
        await input.SelectTextAsync();
        Assert.That(await page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("some value"));
    }

    [UpstreamTest("EH-104 should select plain div")]
    public async Task ShouldSelectPlainDiv(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var div = await page.QuerySelectorAsync("div.plain");
        await div.SelectTextAsync();
        Assert.That(await page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("Plain div"));
    }

    [UpstreamTest("EH-105 should timeout waiting for invisible element")]
    public async Task ShouldTimeoutWaitingForInvisibleElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.EvaluateAsync("e => e.style.display = 'none'");
        var error = await Catch(() => textarea.SelectTextAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("element is not visible"));
    }

    [UpstreamTest("EH-106 should wait for visible")]
    public async Task ShouldWaitForVisible(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        var textarea = await page.QuerySelectorAsync("textarea");
        await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
        await textarea.EvaluateAsync("e => e.style.display = 'none'");
        var done = false;
        // .NET: const promise = textarea.selectText({ timeout: 3000 }).then(() => done = true);
        async Task Promise()
        {
            await textarea.SelectTextAsync(new() { Timeout = 3000 });
            done = true;
        }
        var promise = Promise();
        await page.WaitForTimeoutAsync(1000);
        Assert.That(done, Is.EqualTo(false));
        await textarea.EvaluateAsync("e => e.style.display = 'block'");
        await promise;
    }
}
