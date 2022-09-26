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

///<playwright-file>download.spec.ts</playwright-file>
public class DownloadTests : PageTestEx
{
    [SetUp]
    public void Setup()
    {
        Server.SetRoute("/download", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            context.Response.Headers["Content-Disposition"] = "attachment";
            return context.Response.WriteAsync("Hello world");
        });

        Server.SetRoute("/downloadWithFilename", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
            return context.Response.WriteAsync("Hello world");
        });

        Server.SetRoute("/downloadWithDelay", async context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            context.Response.Headers["Content-Disposition"] = "attachment;";
            // Chromium requires a large enough payload to trigger the download event soon enough
            await context.Response.WriteAsync("a".PadLeft(4096, 'a'));
            await Task.Delay(3000);
            await context.Response.WriteAsync("foo hello world");
        });

        Server.SetRoute("/downloadLarge", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            context.Response.Headers["Content-Disposition"] = "attachment";
            var payload = string.Empty;
            for (var i = 0; i < 10_000; i++)
            {
                payload += $"a{i}";
            }
            return context.Response.WriteAsync(payload);
        });
    }

    [PlaywrightTest("download.spec.ts", "should report downloads with acceptDownloads: false")]
    public async Task ShouldReportDownloadsWithAcceptDownloadsFalse()
    {
        await Page.SetContentAsync($"<a href=\"{Server.Prefix}/downloadWithFilename\">download</a>");
        var downloadTask = Page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            Page.ClickAsync("a"));

        var download = downloadTask.Result;
        Assert.AreEqual($"{Server.Prefix}/downloadWithFilename", download.Url);
        Assert.AreEqual("file.txt", download.SuggestedFilename);

        string path = await download.PathAsync();
        Assert.True(new FileInfo(path).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(path));
    }

    [PlaywrightTest("download.spec.ts", "should report downloads with acceptDownloads: true")]
    public async Task ShouldReportDownloadsWithAcceptDownloadsTrue()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.ClickAsync("a");
        });
        string path = await download.PathAsync();

        Assert.True(new FileInfo(path).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(path));
    }

    [PlaywrightTest("download.spec.ts", "should save to user-specified path")]
    public async Task ShouldSaveToUserSpecifiedPath()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.ClickAsync("a");
        });

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        await download.SaveAsAsync(userPath);

        Assert.True(new FileInfo(userPath).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(userPath));
        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should save to user-specified path without updating original path")]
    public async Task ShouldSaveToUserSpecifiedPathWithoutUpdatingOriginalPath()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");

        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.ClickAsync("a");
        });

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        await download.SaveAsAsync(userPath);

        Assert.True(new FileInfo(userPath).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(userPath));

        string originalPath = await download.PathAsync();
        Assert.True(new FileInfo(originalPath).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(originalPath));

        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should save to two different paths with multiple saveAs calls")]
    public async Task ShouldSaveToTwoDifferentPathsWithMultipleSaveAsCalls()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");

        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await page.ClickAsync("a");
        });

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        await download.SaveAsAsync(userPath);
        Assert.True(new FileInfo(userPath).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(userPath));

        string anotherUserPath = Path.Combine(tmpDir.Path, "download (2).txt");
        await download.SaveAsAsync(anotherUserPath);
        Assert.True(new FileInfo(anotherUserPath).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(anotherUserPath));

        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should save to overwritten filepath")]
    public async Task ShouldSaveToOverwrittenFilepath()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        var download = downloadTask.Result;
        await download.SaveAsAsync(userPath);
        Assert.AreEqual(1, new DirectoryInfo(tmpDir.Path).GetFiles().Length);
        await download.SaveAsAsync(userPath);
        Assert.AreEqual(1, new DirectoryInfo(tmpDir.Path).GetFiles().Length);

        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should create subdirectories when saving to non-existent user-specified path")]
    public async Task ShouldCreateSubdirectoriesWhenSavingToNonExistentUserSpecifiedPath()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "download.txt");
        var download = downloadTask.Result;
        await download.SaveAsAsync(userPath);
        Assert.True(new FileInfo(userPath).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(userPath));

        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should error when saving with downloads disabled")]
    public async Task ShouldErrorWhenSavingWithDownloadsDisabled()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = false });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        var download = downloadTask.Result;

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
        StringAssert.Contains("Pass { acceptDownloads: true } when you are creating your browser context", exception.Message);
    }

    [PlaywrightTest("download.spec.ts", "should error when saving after deletion")]
    public async Task ShouldErrorWhenSavingAfterDeletion()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        var download = downloadTask.Result;
        await download.DeleteAsync();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
        StringAssert.Contains("Target page, context or browser has been closed", exception.Message);
    }

    [PlaywrightTest("download.spec.ts", "should report non-navigation downloads")]
    public async Task ShouldReportNonNavigationDownloads()
    {
        Server.SetRoute("/download", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            return context.Response.WriteAsync("Hello world");
        });

        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync($"<a download=\"file.txt\" href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        Assert.AreEqual("file.txt", download.SuggestedFilename);
        string path = await download.PathAsync();

        Assert.True(new FileInfo(path).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(path));
        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should report download path within page.on('download', …) handler for Files")]
    public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForFiles()
    {
        var downloadPathTcs = new TaskCompletionSource<string>();
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        page.Download += async (_, e) =>
        {
            downloadPathTcs.TrySetResult(await e.PathAsync());
        };

        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        await page.ClickAsync("a");
        string path = await downloadPathTcs.Task;

        Assert.AreEqual("Hello world", File.ReadAllText(path));
        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should report download path within page.on('download', …) handler for Blobs")]
    public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForBlobs()
    {
        var downloadPathTcs = new TaskCompletionSource<string>();
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        page.Download += async (_, e) =>
        {
            downloadPathTcs.TrySetResult(await e.PathAsync());
        };

        await page.GotoAsync(Server.Prefix + "/download-blob.html");
        await page.ClickAsync("a");
        string path = await downloadPathTcs.Task;

        Assert.AreEqual("Hello world", File.ReadAllText(path));
        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should report alt-click downloads")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportAltClickDownloads()
    {
        Server.SetRoute("/download", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            return context.Response.WriteAsync("Hello world");
        });

        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a", new() { Modifiers = new[] { KeyboardModifier.Alt } }));

        var download = downloadTask.Result;
        string path = await download.PathAsync();

        Assert.True(new FileInfo(path).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(path));
    }

    [PlaywrightTest("download.spec.ts", "should report new window downloads")]
    public async Task ShouldReportNewWindowDownloads()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a target=_blank href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        string path = await download.PathAsync();

        Assert.True(new FileInfo(path).Exists);
        Assert.AreEqual("Hello world", File.ReadAllText(path));
    }

    [PlaywrightTest("download.spec.ts", "should delete file")]
    public async Task ShouldDeleteFile()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a target=_blank href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        string path = await download.PathAsync();

        Assert.True(new FileInfo(path).Exists);
        await download.DeleteAsync();
        Assert.False(new FileInfo(path).Exists);
        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should expose stream")]
    public async Task ShouldExposeStream()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a target=_blank href=\"{Server.Prefix}/downloadLarge\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));

        var download = downloadTask.Result;
        var expected = string.Empty;
        for (var i = 0; i < 10_000; i++)
        {
            expected += $"a{i}";
        }
        using (var stream = await download.CreateReadStreamAsync())
        {
            Assert.AreEqual(expected, await new StreamReader(stream).ReadToEndAsync());
        }

        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should delete downloads on context destruction")]
    public async Task ShouldDeleteDownloadsOnContextDestruction()
    {
        var page = await Browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download1Task = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            download1Task,
            page.ClickAsync("a"));

        var download2Task = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            download2Task,
            page.ClickAsync("a"));

        string path1 = await download1Task.Result.PathAsync();
        string path2 = await download2Task.Result.PathAsync();
        Assert.True(new FileInfo(path1).Exists);
        Assert.True(new FileInfo(path2).Exists);
        await page.Context.CloseAsync();
        Assert.False(new FileInfo(path1).Exists);
        Assert.False(new FileInfo(path2).Exists);
    }

    [PlaywrightTest("download.spec.ts", "should delete downloads on browser gone")]
    public async Task ShouldDeleteDownloadsOnBrowserGone()
    {
        var browser = await BrowserType.LaunchAsync();
        var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download1Task = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            download1Task,
            page.ClickAsync("a"));

        var download2Task = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            download2Task,
            page.ClickAsync("a"));

        string path1 = await download1Task.Result.PathAsync();
        string path2 = await download2Task.Result.PathAsync();
        Assert.True(new FileInfo(path1).Exists);
        Assert.True(new FileInfo(path2).Exists);
        await browser.CloseAsync();
        Assert.False(new FileInfo(path1).Exists);
        Assert.False(new FileInfo(path2).Exists);
        Assert.False(new FileInfo(Path.Combine(path1, "..")).Exists);
    }

    [PlaywrightTest("download.spec.ts", "should be able to cancel pending downloads")]
    public async Task ShouldBeAbleToCancelPendingDownload()
    {
        var browser = await BrowserType.LaunchAsync();
        var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/downloadWithDelay\">download</a>");

        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("a"));
        await download.CancelAsync();

        var failure = await download.FailureAsync();
        Assert.AreEqual("canceled", failure);

        await page.CloseAsync();
    }

    [PlaywrightTest("download.spec.ts", "should not fail explicitly to cancel a download even if that is already finished")]
    public async Task ShouldNotFailWhenCancellingACompletedDownload()
    {
        var browser = await BrowserType.LaunchAsync();
        var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("a"));

        using var tmpDir = new TempDirectory();
        string userPath = Path.Combine(tmpDir.Path, "download.txt");
        await download.SaveAsAsync(userPath);

        Assert.IsTrue(File.Exists(userPath));

        await download.CancelAsync();

        var failure = await download.FailureAsync();
        Assert.IsNull(failure);

        await page.CloseAsync();
    }


    [PlaywrightTest("download.spec.ts", "should report downloads with interception")]
    public async Task ShouldReportDownloadsWithInterception()
    {
        var browser = await BrowserType.LaunchAsync();
        var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
        await page.RouteAsync("*", r => r.ContinueAsync());
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("a"));

        var path = await download.PathAsync();
        Assert.IsTrue(File.Exists(path));

        await page.CloseAsync();
    }

}
