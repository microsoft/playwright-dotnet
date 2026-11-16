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

/// <summary>Port of tests/page/elementhandle-eval-on-selector.spec.ts.</summary>
public class ElementhandleEvalOnSelectorTests
{
    [UpstreamTest("EH-039 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
        var tweet = await page.QuerySelectorAsync(".tweet");
        var content = await tweet.EvalOnSelectorAsync<string>(".like", "node => node.innerText");
        Assert.That(content, Is.EqualTo("100"));
    }

    [UpstreamTest("EH-040 should retrieve content from subtree")]
    public async Task ShouldRetrieveContentFromSubtree(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
        await page.SetContentAsync(htmlContent);
        var elementHandle = await page.QuerySelectorAsync("#myId");
        var content = await elementHandle.EvalOnSelectorAsync<string>(".a", "node => node.innerText");
        Assert.That(content, Is.EqualTo("a-child-div"));
    }

    [UpstreamTest("EH-041 should throw in case of missing selector")]
    [Fixme("dotnet: the message is 'Failed to find element matching selector \".a\"', without the 'elementHandle.$eval: ' prefix of the call")]
    public async Task ShouldThrowInCaseOfMissingSelector(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
        await page.SetContentAsync(htmlContent);
        var elementHandle = await page.QuerySelectorAsync("#myId");
        var errorMessage = (await Catch(() => elementHandle.EvalOnSelectorAsync<string>(".a", "node => node.innerText"))).Message;
        Assert.That(errorMessage, Does.Contain("elementHandle.$eval: Failed to find element matching selector \".a\""));
    }

    [UpstreamTest("EH-042 should work for all")]
    public async Task ShouldWorkForAll(IPage page, Server server)
    {
        await page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
        var tweet = await page.QuerySelectorAsync(".tweet");
        var content = await tweet.EvalOnSelectorAllAsync<string[]>(".like", "nodes => nodes.map(n => n.innerText)");
        Assert.That(content, Is.EqualTo(new[] { "100", "10" }));
    }

    [UpstreamTest("EH-043 should retrieve content from subtree for all")]
    public async Task ShouldRetrieveContentFromSubtreeForAll(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>";
        await page.SetContentAsync(htmlContent);
        var elementHandle = await page.QuerySelectorAsync("#myId");
        var content = await elementHandle.EvalOnSelectorAllAsync<string[]>(".a", "nodes => nodes.map(n => n.innerText)");
        Assert.That(content, Is.EqualTo(new[] { "a1-child-div", "a2-child-div" }));
    }

    [UpstreamTest("EH-044 should not throw in case of missing selector for all")]
    public async Task ShouldNotThrowInCaseOfMissingSelectorForAll(IPage page, Server server)
    {
        const string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
        await page.SetContentAsync(htmlContent);
        var elementHandle = await page.QuerySelectorAsync("#myId");
        var nodesLength = await elementHandle.EvalOnSelectorAllAsync<int>(".a", "nodes => nodes.length");
        Assert.That(nodesLength, Is.EqualTo(0));
    }
}
