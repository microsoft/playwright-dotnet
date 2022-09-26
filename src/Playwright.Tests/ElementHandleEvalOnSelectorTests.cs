/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

public class ElementHandleEvalOnSelectorTests : PageTestEx
{
    [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should work for all")]
    public async Task ShouldWorkForAll()
    {
        await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
        var tweet = await Page.QuerySelectorAsync(".tweet");
        string[] content = await tweet.EvalOnSelectorAllAsync<string[]>(".like", "nodes => nodes.map(n => n.innerText)");
        Assert.AreEqual(new[] { "100", "10" }, content);
    }

    [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should retrieve content from subtree for all")]
    public async Task ShouldRetrieveContentFromSubtreeForAll()
    {
        string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>";
        await Page.SetContentAsync(htmlContent);
        var elementHandle = await Page.QuerySelectorAsync("#myId");
        string[] content = await elementHandle.EvalOnSelectorAllAsync<string[]>(".a", "nodes => nodes.map(n => n.innerText)");
        Assert.AreEqual(new[] { "a1-child-div", "a2-child-div" }, content);
    }

    [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should not throw in case of missing selector for all")]
    public async Task ShouldNotThrowInCaseOfMissingSelectorForAll()
    {
        string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
        await Page.SetContentAsync(htmlContent);
        var elementHandle = await Page.QuerySelectorAsync("#myId");
        int nodesLength = await elementHandle.EvalOnSelectorAllAsync<int>(".a", "nodes => nodes.length");
        Assert.AreEqual(0, nodesLength);
    }

    [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
        var tweet = await Page.QuerySelectorAsync(".tweet");
        string content = await tweet.EvalOnSelectorAsync<string>(".like", "node => node.innerText");
        Assert.AreEqual("100", content);
    }

    [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should retrieve content from subtree")]
    public async Task ShouldRetrieveContentFromSubtree()
    {
        string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
        await Page.SetContentAsync(htmlContent);
        var elementHandle = await Page.QuerySelectorAsync("#myId");
        string content = await elementHandle.EvalOnSelectorAsync<string>(".a", "node => node.innerText");
        Assert.AreEqual("a-child-div", content);
    }

    [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should throw in case of missing selector")]
    public async Task ShouldThrowInCaseOfMissingSelector()
    {
        string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
        await Page.SetContentAsync(htmlContent);
        var elementHandle = await Page.QuerySelectorAsync("#myId");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => elementHandle.EvalOnSelectorAsync(".a", "node => node.innerText"));
        StringAssert.Contains("failed to find element matching selector \".a\"", exception.Message);
    }
}
