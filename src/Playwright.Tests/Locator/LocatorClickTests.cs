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


public class LocatorClickTests : PageTestEx
{
    [PlaywrightTest("locator-click.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = Page.Locator("button");
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => window['result']"));
    }

    [PlaywrightTest("locator-click.spec.ts", "should work with Node removed")]
    public async Task ShouldWorkWithNodeRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = Page.Locator("button");
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => window['result']"));
    }

    [PlaywrightTest("locator-click.spec.ts", "should work for TextNodes")]
    public async Task ShouldWorkForTextNodes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var buttonTextNode = await Page.EvaluateHandleAsync("() => document.querySelector('button').firstChild");
        await buttonTextNode.AsElement().ClickAsync();
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => window['result']"));
    }

    [PlaywrightTest("locator-click.spec.ts", "should double click the button")]
    public async Task ShouldDoubleClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() =>
{
  window['double'] = false;
  const button = document.querySelector('button');
  button.addEventListener('dblclick', event => {
    window['double'] = true;
  });
}");

        var button = Page.Locator("button");
        await button.DblClickAsync();
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window['double']"));
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => window['result']"));
    }
}
