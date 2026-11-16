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

/// <summary>Port of tests/page/page-click-timeout-2.spec.ts.</summary>
public class PageClickTimeout2Tests
{
    [UpstreamTest("CL-114 should timeout waiting for display:none to be gone")]
    [Fixme("dotnet: the message is 'Timeout 5000ms exceeded.', without the 'page.click: ' prefix of the call")]
    public async Task ShouldTimeoutWaitingForDisplayNoneToBeGone(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        var error = await Catch(() => page.ClickAsync("button", new() { Timeout = 5000 }));
        Assert.That(error.Message, Does.Contain("page.click: Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("waiting for element to be visible, enabled and stable"));
        Assert.That(error.Message, Does.Contain("element is not visible"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
    }

    [UpstreamTest("CL-115 should timeout waiting for visibility:hidden to be gone")]
    [Fixme("dotnet: the message is 'Timeout 5000ms exceeded.', without the 'page.click: ' prefix of the call")]
    public async Task ShouldTimeoutWaitingForVisibilityHiddenToBeGone(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
        var error = await Catch(() => page.ClickAsync("button", new() { Timeout = 5000 }));
        Assert.That(error.Message, Does.Contain("page.click: Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("waiting for element to be visible, enabled and stable"));
        Assert.That(error.Message, Does.Contain("element is not visible"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
    }
}
