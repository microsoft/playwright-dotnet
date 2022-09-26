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

using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class DownloadsPathTests : PlaywrightTestEx
{
    private IBrowser _browser { get; set; }
    private TempDirectory _tmp = null;

    [PlaywrightTest("downloads-path.spec.ts", "should keep downloadsPath folder")]
    public async Task ShouldKeepDownloadsPathFolder()
    {
        var page = await _browser.NewPageAsync(new() { AcceptDownloads = false });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        Assert.AreEqual($"{Server.Prefix}/download", download.Url);
        Assert.AreEqual("file.txt", download.SuggestedFilename);

        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => download.PathAsync());

        await page.CloseAsync();
        await _browser.CloseAsync();
        Assert.True(new DirectoryInfo(_tmp.Path).Exists);
    }

    [PlaywrightTest("downloads-path.spec.ts", "should delete downloads when context closes")]
    public async Task ShouldDeleteDownloadsWhenContextCloses()
    {
        var page = await _browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        string path = await download.PathAsync();
        Assert.True(new FileInfo(path).Exists);
        await page.CloseAsync();
        Assert.False(new FileInfo(path).Exists);
    }

    [PlaywrightTest("downloads-path.spec.ts", "should report downloads in downloadsPath folder")]
    public async Task ShouldReportDownloadsInDownloadsPathFolder()
    {
        var page = await _browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        string path = await download.PathAsync();
        Assert.That(path, Does.StartWith(_tmp.Path));
        await page.CloseAsync();
    }


    [PlaywrightTest("downloads-path.spec.ts", "should report downloads in downloadsPath folder with a relative path")]
    public async Task ShouldReportDownloadsInDownloadsPathFolderWithARelativePath()
    {
        var browser = await Playwright[TestConstants.BrowserName]
            .LaunchAsync(new()
            {
                DownloadsPath = "."
            });

        var page = await browser.NewPageAsync(new()
        {
            AcceptDownloads = true
        });

        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("a"));
        string path = await download.PathAsync();
        Assert.That(path, Does.StartWith(Directory.GetCurrentDirectory()));
        await page.CloseAsync();
    }

    [PlaywrightTest("downloads-path.spec.ts", "should accept downloads in persistent context")]
    public async Task ShouldAcceptDownloadsInPersistentContext()
    {
        var userProfile = new TempDirectory();
        var browser = await Playwright[TestConstants.BrowserName]
            .LaunchPersistentContextAsync(userProfile.Path, new()
            {
                AcceptDownloads = true,
                DownloadsPath = _tmp.Path
            });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("a"));

        Assert.AreEqual($"{Server.Prefix}/download", download.Url);
        Assert.AreEqual("file.txt", download.SuggestedFilename);
        Assert.That(await download.PathAsync(), Does.StartWith(_tmp.Path));
        await page.CloseAsync();
    }

    [PlaywrightTest("downloads-path.spec.ts", "should delete downloads when persistent context closes")]
    public async Task ShouldDeleteDownloadsWhenPersistentContextCloses()
    {
        var userProfile = new TempDirectory();
        var browser = await Playwright[TestConstants.BrowserName]
            .LaunchPersistentContextAsync(userProfile.Path, new()
            {
                AcceptDownloads = true,
                DownloadsPath = _tmp.Path
            });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("a"));
        var path = await download.PathAsync();
        Assert.IsTrue(File.Exists(path));
        await browser.CloseAsync();
        Assert.IsFalse(File.Exists(path));
    }

    [SetUp]
    public async Task InitializeAsync()
    {
        Server.SetRoute("/download", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
            return context.Response.WriteAsync("Hello world");
        });

        _tmp = new();
        _browser = await Playwright[TestConstants.BrowserName].LaunchAsync(new() { DownloadsPath = _tmp.Path });
    }

    [TearDown]
    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _tmp.Dispose();
    }
}
