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

public class ElementHandleClickTests : PageTestEx
{
    [PlaywrightTest("elementhandle-click.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should work with Node removed")]
    public async Task ShouldWorkWithNodeRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = await Page.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should work for Shadow DOM v1")]
    public async Task ShouldWorkForShadowDOMV1()
    {
        await Page.GotoAsync(Server.Prefix + "/shadow.html");
        var buttonHandle = (IElementHandle)await Page.EvaluateHandleAsync("() => button");
        await buttonHandle.ClickAsync();
        Assert.True(await Page.EvaluateAsync<bool>("() => clicked"));
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should work for TextNodes")]
    public async Task ShouldWorkForTextNodes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var buttonTextNode = (IElementHandle)await Page.EvaluateHandleAsync("() => document.querySelector('button').firstChild");
        await buttonTextNode.ClickAsync();
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should throw for detached nodes")]
    public async Task ShouldThrowForDetachedNodes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("button => button.remove()", button);
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => button.ClickAsync());
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should throw for hidden nodes with force")]
    public async Task ShouldThrowForHiddenNodesWithForce()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("button => button.style.display = 'none'", button);
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => button.ClickAsync(new() { Force = true }));
        StringAssert.Contains("Element is not visible", exception.Message);
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should throw for recursively hidden nodes with force")]
    public async Task ShouldThrowForRecursivelyHiddenNodesWithForce()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("button => button.parentElement.style.display = 'none'", button);
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => button.ClickAsync(new() { Force = true }));
        StringAssert.Contains("Element is not visible", exception.Message);
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should throw for &lt;br&gt; elements with force")]
    public async Task ShouldThrowForBrElementsWithforce()
    {
        await Page.SetContentAsync("hello<br>goodbye");
        var br = await Page.QuerySelectorAsync("br");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => br.ClickAsync(new() { Force = true }));
        StringAssert.Contains("Element is outside of the viewport", exception.Message);
    }

    [PlaywrightTest("elementhandle-click.spec.ts", "should double click the button")]
    public async Task ShouldDoubleClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
              window.double = false;
              const button = document.querySelector('button');
              button.addEventListener('dblclick', event => {
                window.double = true;
              });
            }");

        var button = await Page.QuerySelectorAsync("button");
        await button.DblClickAsync();

        Assert.True(await Page.EvaluateAsync<bool>("double"));
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }
}
