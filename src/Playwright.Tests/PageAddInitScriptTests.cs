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

public class PageAddInitScriptTests : PageTestEx
{
    [PlaywrightTest("page-add-init-script.spec.ts", "should evaluate before anything else on the page")]
    public async Task ShouldEvaluateBeforeAnythingElseOnThePage()
    {
        await Page.AddInitScriptAsync("window.injected = 123;");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work with a path")]
    public async Task ShouldWorkWithAPath()
    {
        await Page.AddInitScriptAsync(scriptPath: TestUtils.GetAsset("injectedfile.js"));

        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work with a path")]
    public async Task ShouldWorkWithContents()
    {
        await Page.AddInitScriptAsync("window.injected = 123;");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should throw without path and content")]
    public Task ShouldThrowWithoutPathAndContent()
    {
        return PlaywrightAssert.ThrowsAsync<ArgumentException>(() => Page.AddInitScriptAsync());
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts")]
    public async Task ShouldWorkWithBrowserContextScripts()
    {
        await using var context = await Browser.NewContextAsync();
        await context.AddInitScriptAsync("window.temp = 123;");

        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync("window.injected = window.temp;");
        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await page.EvaluateAsync<int>("() => window.result"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts with path")]
    public async Task ShouldWorkWithBrowserContextScriptsWithPath()
    {
        await using var context = await Browser.NewContextAsync();
        await context.AddInitScriptAsync(scriptPath: TestUtils.GetAsset("injectedfile.js"));

        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await page.EvaluateAsync<int>("() => window.result"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work with browser context scripts for already created pages")]
    public async Task ShouldWorkWithBrowserContextScriptsForAlreadyCreatedPages()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await context.AddInitScriptAsync("window.temp = 123;");

        await page.AddInitScriptAsync(script: "window.injected = window.temp;");

        await page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await page.EvaluateAsync<int>("() => window.result"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should support multiple scripts")]
    public async Task ShouldSupportMultipleScripts()
    {
        await Page.AddInitScriptAsync("window.script1 = 1;");
        await Page.AddInitScriptAsync("window.script2 = 2;");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(1, await Page.EvaluateAsync<int>("() => window.script1"));
        Assert.AreEqual(2, await Page.EvaluateAsync<int>("() => window.script2"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work with CSP")]
    public async Task ShouldWorkWithCSP()
    {
        Server.SetCSP("/empty.html", "script-src " + Server.Prefix);
        await Page.AddInitScriptAsync("window.injected = 123;");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.injected"));

        // Make sure CSP works.
        try
        {
            await Page.AddScriptTagAsync(new() { Content = "window.e = 10;" });
        }
        catch
        {
            //Silent exception
        }

        Assert.Null(await Page.EvaluateAsync("() => window.e"));
    }

    [PlaywrightTest("page-add-init-script.spec.ts", "should work after a cross origin navigation")]
    public async Task ShouldWorkAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.CrossProcessPrefix);
        await Page.AddInitScriptAsync("window.injected = 123;");
        await Page.GotoAsync(Server.Prefix + "/tamperable.html");
        Assert.AreEqual(123, await Page.EvaluateAsync<int>("() => window.result"));
    }
}
