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

/// <summary>Port of tests/page/page-click-timeout-1.spec.ts.</summary>
public class PageClickTimeout1Tests
{
    [UpstreamTest("CL-111 should timeout waiting for button to be enabled")]
    [Fixme("dotnet: the message is 'Timeout 3000ms exceeded.', without the 'page.click: ' prefix of the call")]
    public async Task ShouldTimeoutWaitingForButtonToBeEnabled(IPage page)
    {
        await page.SetContentAsync("<button onclick=\"javascript:window.__CLICKED=true;\" disabled><span>Click target</span></button>");
        var error = await Catch(() => page.ClickAsync("text=Click target", new() { Timeout = 3000 }));
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(error.Message, Does.Contain("page.click: Timeout 3000ms exceeded."));
        Assert.That(error.Message, Does.Contain("element is not enabled"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
    }

    [UpstreamTest("CL-112 should timeout waiting for inert element to become non-inert")]
    [Fixme("dotnet: the message is 'Timeout 3000ms exceeded.', without the 'page.click: ' prefix of the call")]
    public async Task ShouldTimeoutWaitingForInertElementToBecomeNonInert(IPage page)
    {
        await page.SetContentAsync("<div inert><button onclick=\"javascript:window.__CLICKED=true;\">Click target</button></div>");
        var error = await Catch(() => page.ClickAsync("text=Click target", new() { Timeout = 3000 }));
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(error.Message, Does.Contain("page.click: Timeout 3000ms exceeded."));
        Assert.That(error.Message, Does.Contain("element is inert"));
        Assert.That(error.Message, Does.Contain("retrying click action"));
    }
}
