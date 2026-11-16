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

/// <summary>Port of tests/page/locator-evaluate.spec.ts.</summary>
public class LocatorEvaluateTests
{
    [UpstreamTest("LO-084 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
        var tweet = page.Locator(".tweet .like");
        var content = await tweet.EvaluateAsync<string>("node => node.innerText");
        Assert.That(content, Is.EqualTo("100"));
    }

    [UpstreamTest("LO-085 should retrieve content from subtree")]
    public async Task ShouldRetrieveContentFromSubtree(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
        await page.SetContentAsync(htmlContent);
        var elementHandle = page.Locator("#myId .a");
        var content = await elementHandle.EvaluateAsync<string>("node => node.innerText");
        Assert.That(content, Is.EqualTo("a-child-div"));
    }

    [UpstreamTest("LO-086 should work for all")]
    public async Task ShouldWorkForAll(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
        var tweet = page.Locator(".tweet .like");
        var content = await tweet.EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.innerText)");
        Assert.That(content, Is.EqualTo(new[] { "100", "10" }));
    }

    [UpstreamTest("LO-087 should retrieve content from subtree for all")]
    public async Task ShouldRetrieveContentFromSubtreeForAll(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>";
        await page.SetContentAsync(htmlContent);
        var element = page.Locator("#myId .a");
        var content = await element.EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.innerText)");
        Assert.That(content, Is.EqualTo(new[] { "a1-child-div", "a2-child-div" }));
    }

    [UpstreamTest("LO-088 should not throw in case of missing selector for all")]
    public async Task ShouldNotThrowInCaseOfMissingSelectorForAll(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
        await page.SetContentAsync(htmlContent);
        var element = page.Locator("#myId .a");
        var nodesLength = await element.EvaluateAllAsync<int>("nodes => nodes.length");
        Assert.That(nodesLength, Is.EqualTo(0));
    }
}
