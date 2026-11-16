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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-click.spec.ts.</summary>
public class LocatorClickTests
{
    [UpstreamTest("LO-052 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        var button = page.Locator("button");
        await button.ClickAsync();
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("LO-053 should work with Node removed")]
    public async Task ShouldWorkWithNodeRemoved(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync("() => delete window['Node']");
        var button = page.Locator("button");
        await button.ClickAsync();
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("LO-054 should double click the button")]
    public async Task ShouldDoubleClickTheButton(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync(@"() => {
    window['double'] = false;
    const button = document.querySelector('button');
    button.addEventListener('dblclick', event => {
      window['double'] = true;
    });
  }");
        var button = page.Locator("button");
        await button.DblClickAsync();
        Assert.That(await page.EvaluateAsync<bool>("double"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("LO-055 should click if the target element is removed in pointerup event")]
    public async Task ShouldClickIfTheTargetElementIsRemovedInPointerupEvent(IPage page)
    {
        await page.SetContentAsync("<button id=clickme>Clickable</button>");
        await page.EvalOnSelectorAsync("#clickme", "element => element.addEventListener('pointerup', () => element.remove(), false)");
        await page.Locator("#clickme").ClickAsync();
    }

    [UpstreamTest("LO-056 should click if the target element is removed in pointerdown event")]
    public async Task ShouldClickIfTheTargetElementIsRemovedInPointerdownEvent(IPage page)
    {
        await page.SetContentAsync("<button id=clickme>Clickable</button>");
        await page.EvalOnSelectorAsync("#clickme", "element => element.addEventListener('pointerdown', () => element.remove(), false)");
        await page.Locator("#clickme").ClickAsync();
    }
}
