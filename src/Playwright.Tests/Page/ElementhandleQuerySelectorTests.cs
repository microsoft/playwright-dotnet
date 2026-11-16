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

/// <summary>Port of tests/page/elementhandle-query-selector.spec.ts.</summary>
public class ElementhandleQuerySelectorTests
{
    [UpstreamTest("EH-067 should query existing element")]
    public async Task ShouldQueryExistingElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/playground.html");
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = await page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAsync(".second");
        var inner = await second.QuerySelectorAsync(".inner");
        var content = await page.EvaluateAsync<string>("e => e.textContent", inner);
        Assert.That(content, Is.EqualTo("A"));
    }

    [UpstreamTest("EH-068 should return null for non-existing element")]
    public async Task ShouldReturnNullForNonExistingElement(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
        var html = await page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAsync(".third");
        Assert.That(second, Is.Null);
    }

    [UpstreamTest("EH-069 should work for adopted elements")]
    public async Task ShouldWorkForAdoptedElements(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window['__popup'] = window.open(url)", server.EmptyPage));
        var divHandle = (IElementHandle)await page.EvaluateHandleAsync(@"() => {
    const div = document.createElement('div');
    document.body.appendChild(div);
    const span = document.createElement('span');
    span.textContent = 'hello';
    div.appendChild(span);
    return div;
  }");
        Assert.That(await divHandle.QuerySelectorAsync("span"), Is.Not.Null);
        Assert.That(await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"), Is.EqualTo("hello"));

        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.EvaluateAsync(@"() => {
    const div = document.querySelector('div');
    window['__popup'].document.body.appendChild(div);
  }");
        Assert.That(await divHandle.QuerySelectorAsync("span"), Is.Not.Null);
        Assert.That(await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"), Is.EqualTo("hello"));
    }

    [UpstreamTest("EH-070 should query existing elements")]
    public async Task ShouldQueryExistingElements(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div>A</div><br/><div>B</div></body></html>");
        var html = await page.QuerySelectorAsync("html");
        var elements = await html.QuerySelectorAllAsync("div");
        Assert.That(elements.Count, Is.EqualTo(2));
        var promises = elements.Select(element => page.EvaluateAsync<string>("e => e.textContent", element));
        Assert.That(await PromiseAll(promises), Is.EqualTo(new[] { "A", "B" }));
    }

    [UpstreamTest("EH-071 should return empty array for non-existing elements")]
    public async Task ShouldReturnEmptyArrayForNonExistingElements(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><span>A</span><br/><span>B</span></body></html>");
        var html = await page.QuerySelectorAsync("html");
        var elements = await html.QuerySelectorAllAsync("div");
        Assert.That(elements.Count, Is.EqualTo(0));
    }

    [UpstreamTest("EH-072 xpath should query existing element")]
    public async Task XpathShouldQueryExistingElement(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/playground.html");
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = await page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAllAsync("xpath=./body/div[contains(@class, 'second')]");
        var inner = await second[0].QuerySelectorAllAsync("xpath=./div[contains(@class, 'inner')]");
        var content = await page.EvaluateAsync<string>("e => e.textContent", inner[0]);
        Assert.That(content, Is.EqualTo("A"));
    }

    [UpstreamTest("EH-073 xpath should return null for non-existing element")]
    public async Task XpathShouldReturnNullForNonExistingElement(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
        var html = await page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAllAsync("xpath=/div[contains(@class, 'third')]");
        Assert.That(second, Is.EqualTo(Array.Empty<IElementHandle>()));
    }
}
