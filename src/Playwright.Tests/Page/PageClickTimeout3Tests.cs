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

/// <summary>Port of tests/page/page-click-timeout-3.spec.ts.</summary>
public class PageClickTimeout3Tests : UpstreamPageTest
{
    [UpstreamTest("CL-117 should timeout waiting for hit target")]
    public async Task ShouldTimeoutWaitingForHitTarget()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync(@"() => {
    document.body.style.position = 'relative';
    const blocker = document.createElement('div');
    blocker.id = 'blocker';
    blocker.style.position = 'absolute';
    blocker.style.width = '400px';
    blocker.style.height = '20px';
    blocker.style.left = '0';
    blocker.style.top = '0';
    document.body.appendChild(blocker);
  }");
        var error = await Catch(() => button.ClickAsync(new() { Timeout = 5000 }));
        Assert.That(error.Message, Does.Contain("Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("<div id=\"blocker\"></div> intercepts pointer events"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
        Assert.That(error.Message, Does.Contain("waiting 500ms"));
    }

    [UpstreamTest("CL-118 should still click when force but hit target is obscured")]
    public async Task ShouldStillClickWhenForceButHitTargetIsObscured()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync(@"() => {
    document.body.style.position = 'relative';
    const blocker = document.createElement('div');
    blocker.id = 'blocker';
    blocker.style.position = 'absolute';
    blocker.style.width = '400px';
    blocker.style.height = '200px';
    blocker.style.left = '0';
    blocker.style.top = '0';
    document.body.appendChild(blocker);
  }");
        await button.ClickAsync(new() { Force = true });
    }

    [UpstreamTest("CL-119 should report wrong hit target subtree")]
    public async Task ShouldReportWrongHitTargetSubtree()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync(@"() => {
    document.body.style.position = 'relative';

    const blocker = document.createElement('div');
    blocker.id = 'blocker';
    blocker.style.position = 'absolute';
    blocker.style.width = '400px';
    blocker.style.height = '20px';
    blocker.style.left = '0';
    blocker.style.top = '0';
    document.body.appendChild(blocker);

    const inner = document.createElement('div');
    inner.id = 'inner';
    inner.style.position = 'absolute';
    inner.style.left = '0';
    inner.style.top = '0';
    inner.style.right = '0';
    inner.style.bottom = '0';
    blocker.appendChild(inner);
  }");
        var error = await Catch(() => button.ClickAsync(new() { Timeout = 5000 }));
        Assert.That(error.Message, Does.Contain("Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("<div id=\"inner\"></div> from <div id=\"blocker\">…</div> subtree intercepts pointer events"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
    }
}
