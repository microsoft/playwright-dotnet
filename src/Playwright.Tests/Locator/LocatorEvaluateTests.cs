/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

namespace Microsoft.Playwright.Tests.Locator;

public class LocatorEvaluateTests : PageTestEx
{
    [PlaywrightTest("locator-element-handle.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
        var tweet = Page.Locator(".tweet .like");
        var content = await tweet.EvaluateAsync<string>("node => node.innerText");
        Assert.AreEqual("100", content);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "should retrieve content from subtree")]
    public async Task ShouldRetrieveContentFromSubtree()
    {
        await Page.SetContentAsync("<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>");
        var elementHandle = Page.Locator("#myId .a");
        var content = await elementHandle.EvaluateAsync<string>("node => node.innerText");
        Assert.AreEqual("a-child-div", content);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "should work for all")]
    public async Task ShouldWorkForAll()
    {
        await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
        var element = Page.Locator(".tweet .like");
        var content = await element.EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.innerText)");
        CollectionAssert.AreEqual(new string[] { "100", "10" }, content);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "should retrieve content from subtree for all")]
    public async Task ShouldRetrieveContentFromSubtreeForAll()
    {
        await Page.SetContentAsync("<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>");
        var elementHandle = Page.Locator("#myId .a");
        var content = await elementHandle.EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.innerText)");
        CollectionAssert.AreEqual(new string[] { "a1-child-div", "a2-child-div" }, content);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "should not throw in case of missing selector for all")]
    public async Task ShouldNotThrowInCaseOfMissingSelectorForAll()
    {
        await Page.SetContentAsync("<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>");
        var element = Page.Locator("#myId .a");
        var nodesLength = await element.EvaluateAllAsync<int>("nodes => nodes.length");
        Assert.AreEqual(0, nodesLength);
    }
}
