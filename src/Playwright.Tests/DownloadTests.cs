using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>download.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DownloadTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public DownloadTests(ITestOutputHelper output) : base(output)
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
        }

        [PlaywrightTest("download.spec.ts", "should report downloads with acceptDownloads: false")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadsWithAcceptDownloadsFalse()
        {
            await Page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/downloadWithFilename\">download</a>");
            var downloadTask = Page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                Page.ClickAsync("a"));

            var download = downloadTask.Result;
            Assert.Equal($"{TestConstants.ServerUrl}/downloadWithFilename", download.Url);
            Assert.Equal("file.txt", download.SuggestedFilename);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => download.PathAsync());
            Assert.Contains("acceptDownloads", await download.FailureAsync());
            Assert.Contains("acceptDownloads: true", exception.Message);
        }

        [PlaywrightTest("download.spec.ts", "should report downloads with acceptDownloads: true")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadsWithAcceptDownloadsTrue()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            string path = await download.PathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
        }

        [PlaywrightTest("download.spec.ts", "should save to user-specified path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToUserSpecifiedPath()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.SaveAsAsync(userPath);

            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));
            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should save to user-specified path without updating original path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToUserSpecifiedPathWithoutUpdatingOriginalPath()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.SaveAsAsync(userPath);

            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));

            string originalPath = await download.PathAsync();
            Assert.True(new FileInfo(originalPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(originalPath));

            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should save to two different paths with multiple saveAs calls")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToTwoDifferentPathsWithMultipleSaveAsCalls()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.SaveAsAsync(userPath);
            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));

            string anotherUserPath = Path.Combine(tmpDir.Path, "download (2).txt");
            await download.SaveAsAsync(anotherUserPath);
            Assert.True(new FileInfo(anotherUserPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(anotherUserPath));

            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should save to overwritten filepath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToOverwrittenFilepath()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.SaveAsAsync(userPath);
            Assert.Single(new DirectoryInfo(tmpDir.Path).GetFiles());
            await download.SaveAsAsync(userPath);
            Assert.Single(new DirectoryInfo(tmpDir.Path).GetFiles());

            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should create subdirectories when saving to non-existent user-specified path")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCreateSubdirectoriesWhenSavingToNonExistentUserSpecifiedPath()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "download.txt");
            var download = downloadTask.Result;
            await download.SaveAsAsync(userPath);
            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));

            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should save when connected remotely")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldSaveWhenConnectedRemotely()
        {
        }

        [PlaywrightTest("download.spec.ts", "should error when saving with downloads disabled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldErrorWhenSavingWithDownloadsDisabled()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: false);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;

            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
            Assert.Contains("Pass { acceptDownloads: true } when you are creating your browser context", exception.Message);
        }

        [PlaywrightTest("download.spec.ts", "should error when saving after deletion")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldErrorWhenSavingAfterDeletion()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.DeleteAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
            Assert.Contains("Target page, context or browser has been closed", exception.Message);
        }

        [PlaywrightTest("download.spec.ts", "should error when saving after deletion when connected remotely")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldErrorWhenSavingAfterDeletionWhenConnectedRemotely()
        {
        }

        [PlaywrightTest("download.spec.ts", "should report non-navigation downloads")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNonNavigationDownloads()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                return context.Response.WriteAsync("Hello world");
            });

            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.SetContentAsync($"<a download=\"file.txt\" href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            Assert.Equal("file.txt", download.SuggestedFilename);
            string path = await download.PathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should report download path within page.on('download', …) handler for Files")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForFiles()
        {
            var downloadPathTcs = new TaskCompletionSource<string>();
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            page.Download += async (_, e) =>
            {
                downloadPathTcs.TrySetResult(await e.PathAsync());
            };

            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            await page.ClickAsync("a");
            string path = await downloadPathTcs.Task;

            Assert.Equal("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should report download path within page.on('download', …) handler for Blobs")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForBlobs()
        {
            var downloadPathTcs = new TaskCompletionSource<string>();
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            page.Download += async (_, e) =>
            {
                downloadPathTcs.TrySetResult(await e.PathAsync());
            };

            await page.GoToAsync(TestConstants.ServerUrl + "/download-blob.html");
            await page.ClickAsync("a");
            string path = await downloadPathTcs.Task;

            Assert.Equal("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should report alt-click downloads")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportAltClickDownloads()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                return context.Response.WriteAsync("Hello world");
            });

            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a", modifiers: new[] { KeyboardModifier.Alt }));

            var download = downloadTask.Result;
            string path = await download.PathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
        }

        [PlaywrightTest("download.spec.ts", "should report new window downloads")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNewWindowDownloads()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            string path = await download.PathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
        }

        [PlaywrightTest("download.spec.ts", "should delete file")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteFile()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeStream()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            using var stream = await download.CreateReadStreamAsync();
            Assert.Equal("Hello world", await new StreamReader(stream).ReadToEndAsync());

            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should delete downloads on context destruction")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteDownloadsOnContextDestruction()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var download1Task = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                download1Task,
                page.ClickAsync("a"));

            var download2Task = page.WaitForEventAsync(PageEvent.Download);

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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteDownloadsOnBrowserGone()
        {
            var browser = await BrowserType.LaunchDefaultAsync();
            var page = await browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var download1Task = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                download1Task,
                page.ClickAsync("a"));

            var download2Task = page.WaitForEventAsync(PageEvent.Download);

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
    }
}
