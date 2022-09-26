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

public class PageSetContentTests : PageTestEx
{
    const string _expectedOutput = "<html><head></head><body><div>hello</div></body></html>";

    /// <inheritdoc />
    [PlaywrightTest("page-set-content.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<div>hello</div>");
        string result = await Page.ContentAsync();
        Assert.AreEqual(_expectedOutput, result);
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with domcontentloaded")]
    public async Task ShouldWorkWithDomcontentloaded()
    {
        await Page.SetContentAsync("<div>hello</div>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        string result = await Page.ContentAsync();
        Assert.AreEqual(_expectedOutput, result);
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with doctype")]
    public async Task ShouldWorkWithDoctype()
    {
        string doctype = "<!DOCTYPE html>";
        await Page.SetContentAsync($"{doctype}<div>hello</div>");
        string result = await Page.ContentAsync();
        Assert.AreEqual($"{doctype}{_expectedOutput}", result);
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with HTML 4 doctype")]
    public async Task ShouldWorkWithHTML4Doctype()
    {
        string doctype = "<!DOCTYPE html PUBLIC \" -//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">";
        await Page.SetContentAsync($"{doctype}<div>hello</div>");
        string result = await Page.ContentAsync();
        Assert.AreEqual($"{doctype}{_expectedOutput}", result);
    }

    [PlaywrightTest("page-set-content.spec.ts", "should respect timeout")]
    public Task ShouldRespectTimeout()
    {
        string imgPath = "/img.png";
        // stall for image
        Server.SetRoute(imgPath, _ => Task.Delay(Timeout.Infinite));
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(() =>
             Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>", new() { Timeout = 1 })
        );
    }

    [PlaywrightTest("page-set-content.spec.ts", "should respect default navigation timeout")]
    public async Task ShouldRespectDefaultNavigationTimeout()
    {
        Page.SetDefaultNavigationTimeout(1);
        string imgPath = "/img.png";
        // stall for image
        Server.SetRoute(imgPath, _ => Task.Delay(Timeout.Infinite));
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() =>
            Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>", new() { Timeout = 1 })
        );

        StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
    }

    [PlaywrightTest("page-set-content.spec.ts", "should await resources to load")]
    public async Task ShouldAwaitResourcesToLoad()
    {
        string imgPath = "/img.png";
        var imgResponse = new TaskCompletionSource<bool>();
        Server.SetRoute(imgPath, _ => imgResponse.Task);
        bool loaded = false;
        var contentTask = Page.SetContentAsync($"<img src=\"{Server.Prefix + imgPath}\"></img>").ContinueWith(_ => loaded = true);
        await Server.WaitForRequest(imgPath);
        Assert.False(loaded);
        imgResponse.SetResult(true);
        await contentTask;
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work fast enough")]
    public async Task ShouldWorkFastEnough()
    {
        for (int i = 0; i < 20; ++i)
        {
            await Page.SetContentAsync("<div>yo</div>");
        }
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with tricky content")]
    public async Task ShouldWorkWithTrickyContent()
    {
        await Page.SetContentAsync("<div>hello world</div>" + "\x7F");
        Assert.AreEqual("hello world", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with accents")]
    public async Task ShouldWorkWithAccents()
    {
        await Page.SetContentAsync("<div>aberración</div>");
        Assert.AreEqual("aberración", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with emojis")]
    public async Task ShouldWorkWithEmojis()
    {
        await Page.SetContentAsync("<div>🐥</div>");
        Assert.AreEqual("🐥", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
    }

    [PlaywrightTest("page-set-content.spec.ts", "should work with newline")]
    public async Task ShouldWorkWithNewline()
    {
        await Page.SetContentAsync("<div>\n</div>");
        Assert.AreEqual("\n", await Page.EvalOnSelectorAsync<string>("div", "div => div.textContent"));
    }
}
