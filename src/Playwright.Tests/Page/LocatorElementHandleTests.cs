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

/// <summary>Port of tests/page/locator-element-handle.spec.ts.</summary>
public class LocatorElementHandleTests
{
    [UpstreamTest("LO-079 should query existing element @smoke")]
    public async Task ShouldQueryExistingElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/playground.html");
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = page.Locator("html");
        var second = html.Locator(".second");
        var inner = second.Locator(".inner");
        var content = await page.EvaluateAsync<string>("e => e.textContent", await inner.ElementHandleAsync());
        Assert.That(content, Is.EqualTo("A"));
    }

    [UpstreamTest("LO-080 should query existing elements")]
    public async Task ShouldQueryExistingElements(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div>A</div><br/><div>B</div></body></html>");
        var html = page.Locator("html");
        var elements = await html.Locator("div").ElementHandlesAsync();
        Assert.That(elements.Count, Is.EqualTo(2));
        var promises = elements.Select(element => page.EvaluateAsync<string>("e => e.textContent", element));
        Assert.That(await PromiseAll(promises), Is.EqualTo(new[] { "A", "B" }));
    }

    [UpstreamTest("LO-081 should return empty array for non-existing elements")]
    public async Task ShouldReturnEmptyArrayForNonExistingElements(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><span>A</span><br/><span>B</span></body></html>");
        var html = page.Locator("html");
        var elements = await html.Locator("div").ElementHandlesAsync();
        Assert.That(elements.Count, Is.EqualTo(0));
    }

    [UpstreamTest("LO-082 xpath should query existing element")]
    public async Task XpathShouldQueryExistingElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/playground.html");
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = page.Locator("html");
        var second = html.Locator("xpath=./body/div[contains(@class, 'second')]");
        var inner = second.Locator("xpath=./div[contains(@class, 'inner')]");
        var content = await page.EvaluateAsync<string>("e => e.textContent", await inner.ElementHandleAsync());
        Assert.That(content, Is.EqualTo("A"));
    }

    [UpstreamTest("LO-083 xpath should return null for non-existing element")]
    public async Task XpathShouldReturnNullForNonExistingElement(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
        var html = page.Locator("html");
        var second = await html.Locator("xpath=/div[contains(@class, 'third')]").ElementHandlesAsync();
        Assert.That(second, Is.EqualTo(Array.Empty<IElementHandle>()));
    }
}
