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

public class PageAddScriptTagTests : PageTestEx
{
    [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a url")]
    public async Task ShouldWorkWithAUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var scriptHandle = await Page.AddScriptTagAsync(new() { Url = "/injectedfile.js" });
        Assert.NotNull(scriptHandle);
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __injected"));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a url and type=module")]
    public async Task ShouldWorkWithAUrlAndTypeModule()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.AddScriptTagAsync(new() { Url = "/es6/es6import.js", Type = "module" });
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __es6injected"));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a path and type=module")]
    public async Task ShouldWorkWithAPathAndTypeModule()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.AddScriptTagAsync(new() { Path = TestUtils.GetAsset("es6/es6pathimport.js"), Type = "module" });
        await Page.WaitForFunctionAsync("window.__es6injected");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __es6injected"));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a content and type=module")]
    public async Task ShouldWorkWithAContentAndTypeModule()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.AddScriptTagAsync(new() { Content = "import num from '/es6/es6module.js'; window.__es6injected = num;", Type = "module" });
        await Page.WaitForFunctionAsync("window.__es6injected");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __es6injected"));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should throw an error if loading from url fail")]
    public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.AddScriptTagAsync(new() { Url = "/nonexistfile.js" }));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should work with a path")]
    public async Task ShouldWorkWithAPath()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var scriptHandle = await Page.AddScriptTagAsync(new() { Path = TestUtils.GetAsset("injectedfile.js") });
        Assert.NotNull(scriptHandle);
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => __injected"));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should include sourceURL when path is provided")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldIncludeSourceURLWhenPathIsProvided()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.AddScriptTagAsync(new() { Path = TestUtils.GetAsset("injectedfile.js") });
        string result = await Page.EvaluateAsync<string>("() => __injectedError.stack");
        StringAssert.Contains(TestUtils.GetAsset("injectedfile.js"), result);
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should work with content")]
    public async Task ShouldWorkWithContent()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var scriptHandle = await Page.AddScriptTagAsync(new() { Content = "window.__injected = 35;" });
        Assert.NotNull(scriptHandle);
        Assert.AreEqual(35, await Page.EvaluateAsync<int>("() => __injected"));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should throw when added with content to the CSP page")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
    {
        await Page.GotoAsync(Server.Prefix + "/csp.html");
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            Page.AddScriptTagAsync(new() { Content = "window.__injected = 35;" }));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should throw when added with URL to the CSP page")]
    public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
    {
        await Page.GotoAsync(Server.Prefix + "/csp.html");
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            Page.AddScriptTagAsync(new() { Url = Server.CrossProcessPrefix + "/injectedfile.js" }));
    }

    [PlaywrightTest("page-add-script-tag.spec.ts", "should throw a nice error when the request fails")]
    public async Task ShouldThrowANiceErrorWhenTheRequestFails()
    {
        await Page.GotoAsync(Server.EmptyPage);
        string url = Server.Prefix + "/this_does_not_exists.js";
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.AddScriptTagAsync(new() { Url = url }));
        StringAssert.Contains(url, exception.Message);
    }
}
