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

///<playwright-file>selectors-register.spec.ts</playwright-file>
public class SelectorsRegisterTests : PageTestEx
{
    [PlaywrightTest("selector-register.spec.ts", "textContent should be atomic")]
    public async Task TextContentShouldBeAtomic()
    {
        string script = @"
({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        Promise.resolve().then(() => result.textContent = 'modified');
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        Promise.resolve().then(() => e.textContent = 'modified');
      return result;
    }
 })";
        await Playwright.Selectors.RegisterAsync("textContentFromLocators", new() { Script = script });
        await Page.SetContentAsync("<div>Hello</div>");
        var tc = await Page.TextContentAsync("textContentFromLocators=div");
        Assert.AreEqual("Hello", tc);
        Assert.AreEqual("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').innerText"));
    }

    [PlaywrightTest("selectors-register.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        const string createTagSelector = @"({
                create(root, target) {
                  return target.nodeName;
                },
                query(root, selector) {
                  return root.querySelector(selector);
                },
                queryAll(root, selector) {
                  return Array.from(root.querySelectorAll(selector));
                }
            })";

        await TestUtils.RegisterEngineAsync(Playwright, "tag", createTagSelector);
        var context = await Browser.NewContextAsync();
        await TestUtils.RegisterEngineAsync(Playwright, "tag2", createTagSelector);
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<div><span></span></div><div></div>");

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.QuerySelectorAsync("tAG=DIV"));
        StringAssert.Contains("Unknown engine \"tAG\" while parsing selector tAG=DIV", exception.Message);
    }

    [PlaywrightTest("selectors-register.spec.ts", "should work with path")]
    public async Task ShouldWorkWithPath()
    {
        await TestUtils.RegisterEngineWithPathAsync(Playwright, "foo", TestUtils.GetAsset("sectionselectorengine.js"));
        await Page.SetContentAsync("<section></section>");
        Assert.AreEqual("SECTION", await Page.EvalOnSelectorAsync<string>("foo=whatever", "e => e.nodeName"));
    }

    [PlaywrightTest("selectors-register.spec.ts", "should work in main and isolated world")]
    public async Task ShouldWorkInMainAndIsolatedWorld()
    {
        const string createTagSelector = @"({
                create(root, target) { },
                query(root, selector) {
                  return window['__answer'];
                },
                queryAll(root, selector) {
                  return window['__answer'] ? [window['__answer'], document.body, document.documentElement] : [];
                }
            })";

        await TestUtils.RegisterEngineAsync(Playwright, "main", createTagSelector);
        await TestUtils.RegisterEngineAsync(Playwright, "isolated", createTagSelector, true);
        await Page.SetContentAsync("<div><span><section></section></span></div>");
        await Page.EvaluateAsync("() => window['__answer'] = document.querySelector('span')");

        Assert.AreEqual("SPAN", await Page.EvalOnSelectorAsync<string>("main=ignored", "e => e.nodeName"));
        Assert.AreEqual("SPAN", await Page.EvalOnSelectorAsync<string>("css=div >> main=ignored", "e => e.nodeName"));
        Assert.True(await Page.EvalOnSelectorAllAsync<bool>("main=ignored", "es => window['__answer'] !== undefined"));
        Assert.AreEqual(3, await Page.EvalOnSelectorAllAsync<int>("main=ignored", "es => es.filter(e => e).length"));

        Assert.Null(await Page.QuerySelectorAsync("isolated=ignored"));
        Assert.Null(await Page.QuerySelectorAsync("css=div >> isolated=ignored"));
        Assert.True(await Page.EvalOnSelectorAllAsync<bool>("isolated=ignored", "es => window['__answer'] !== undefined"));
        Assert.AreEqual(3, await Page.EvalOnSelectorAllAsync<int>("isolated=ignored", "es => es.filter(e => e).length"));

        Assert.AreEqual("SPAN", await Page.EvalOnSelectorAsync<string>("main=ignored >> isolated=ignored", "e => e.nodeName"));
        Assert.AreEqual("SPAN", await Page.EvalOnSelectorAsync<string>("isolated=ignored >> main=ignored", "e => e.nodeName"));

        Assert.AreEqual("SECTION", await Page.EvalOnSelectorAsync<string>("main=ignored >> css=section", "e => e.nodeName"));
    }

    [PlaywrightTest("selectors-register.spec.ts", "should handle errors")]
    public async Task ShouldHandleErrors()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("neverregister=ignored"));
        StringAssert.Contains("Unknown engine \"neverregister\" while parsing selector neverregister=ignored", exception.Message);

        const string createDummySelector = @"({
                create(root, target) {
                    return target.nodeName;
                },
                query(root, selector) {
                    return root.querySelector('dummy');
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll('dummy'));
                }
            })";

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Playwright.Selectors.RegisterAsync("$", new() { Script = createDummySelector }));
        StringAssert.Contains("Selector engine name may only contain [a-zA-Z0-9_] characters", exception.Message);

        await TestUtils.RegisterEngineAsync(Playwright, "dummy", createDummySelector);
        await TestUtils.RegisterEngineAsync(Playwright, "duMMy", createDummySelector);

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Playwright.Selectors.RegisterAsync("dummy", new() { Script = createDummySelector }));
        StringAssert.Contains("\"dummy\" selector engine has been already registered", exception.Message);

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Playwright.Selectors.RegisterAsync("css", new() { Script = createDummySelector }));
        StringAssert.Contains("\"css\" is a predefined selector engine", exception.Message);
    }
}
