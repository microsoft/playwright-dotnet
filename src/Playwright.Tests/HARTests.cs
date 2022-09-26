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

using System.Text.Json;

namespace Microsoft.Playwright.Tests;

public class HARTests : BrowserTestEx
{
    [PlaywrightTest("har.spec.ts", "should have version and creator")]
    public async Task ShouldWork()
    {
        var (page, context, getContent) = await PageWithHAR();
        await page.GotoAsync(HttpsServer.EmptyPage);
        JsonElement log = await getContent();
        Assert.AreEqual("1.2", log.GetProperty("log").GetProperty("version").ToString());
        Assert.AreEqual("Playwright", log.GetProperty("log").GetProperty("creator").GetProperty("name").ToString());
    }

    [PlaywrightTest("har.spec.ts", "should have pages in persistent context")]
    public async Task ShouldWorkWithPersistentContext()
    {
        using var harFolder = new TempDirectory();
        var harPath = Path.Combine(harFolder.Path, "har.json");
        using var userDataDir = new TempDirectory();

        var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, new()
        {
            RecordHarPath = harPath,
        });
        var page = browserContext.Pages[0];
        await page.GotoAsync("data:text/html,<title>Hello</title>");
        // For data: load comes before domcontentloaded...
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await browserContext.CloseAsync();

        var content = await File.ReadAllTextAsync(harPath);
        var log = JsonSerializer.Deserialize<dynamic>(content);
        Assert.AreEqual(1, log.GetProperty("log").GetProperty("pages").GetArrayLength());
        var pageEntry = log.GetProperty("log").GetProperty("pages")[0];
        Assert.AreEqual("Hello", pageEntry.GetProperty("title").ToString());
    }

    private async Task<(IPage, IBrowserContext, System.Func<Task<dynamic>>)> PageWithHAR()
    {

        var tmp = new TempDirectory();
        var harPath = Path.Combine(tmp.Path, "har.json");
        IBrowserContext context = await Browser.NewContextAsync(new() { RecordHarPath = harPath, IgnoreHTTPSErrors = true });
        IPage page = await context.NewPageAsync();

        async Task<dynamic> getContent()
        {
            await context.CloseAsync();
            var content = await File.ReadAllTextAsync(harPath);
            tmp.Dispose();
            return JsonSerializer.Deserialize<dynamic>(content);
        };

        return (page, context, getContent);
    }
}
