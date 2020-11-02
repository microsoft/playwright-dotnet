using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>download.spec.js</playwright-file>
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

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should report downloads with acceptDownloads: false</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportDownloadsWithAcceptDownloadsFalse()
        {
            await Page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/downloadWithFilename\">download</a>");
            var downloadTask = Page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                Page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            Assert.Equal($"{TestConstants.ServerUrl}/downloadWithFilename", download.Url);
            Assert.Equal("file.txt", download.SuggestedFilename);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => download.GetPathAsync());
            Assert.Contains("acceptDownloads", await download.GetFailureAsync());
            Assert.Contains("acceptDownloads: true", exception.Message);
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should report downloads with acceptDownloads: true</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportDownloadsWithAcceptDownloadsTrue()
        {
            var page = await Browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should save to user-specified path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;
            await download.SaveAsAsync(userPath);

            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));
            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should save to user-specified path without updating original path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;
            await download.SaveAsAsync(userPath);

            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));

            string originalPath = await download.GetPathAsync();
            Assert.True(new FileInfo(originalPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(originalPath));

            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should save to two different paths with multiple saveAs calls</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;
            await download.SaveAsAsync(userPath);
            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));

            string anotherUserPath = Path.Combine(tmpDir.Path, "download (2).txt");
            await download.SaveAsAsync(anotherUserPath);
            Assert.True(new FileInfo(anotherUserPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(anotherUserPath));

            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should save to overwritten filepath</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;
            await download.SaveAsAsync(userPath);
            Assert.Single(new DirectoryInfo(tmpDir.Path).GetFiles());
            await download.SaveAsAsync(userPath);
            Assert.Single(new DirectoryInfo(tmpDir.Path).GetFiles());

            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should create subdirectories when saving to non-existent user-specified path</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;
            await download.SaveAsAsync(userPath);
            Assert.True(new FileInfo(userPath).Exists);
            Assert.Equal("Hello world", File.ReadAllText(userPath));

            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should save when connected remotely</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldSaveWhenConnectedRemotely()
        {
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should error when saving with downloads disabled</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => download.SaveAsAsync(userPath));
            Assert.Contains("Pass { acceptDownloads: true } when you are creating your browser context", exception.Message);
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should error when saving after deletion</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            var download = downloadTask.Result.Download;
            await download.DeleteAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => download.SaveAsAsync(userPath));
            Assert.Contains("Download already deleted. Save before deleting.", exception.Message);
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should error when saving after deletion when connected remotely</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldErrorWhenSavingAfterDeletionWhenConnectedRemotely()
        {
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should report non-navigation downloads</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportNonNavigationDownloads()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                return context.Response.WriteAsync("Hello world");
            });

            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.SetContentAsync($"<a download=\"file.txt\" href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            Assert.Equal("file.txt", download.SuggestedFilename);
            string path = await download.GetPathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>

        ///<playwright-it>should report download path within page.on('download', …) handler for Files</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForFiles()
        {
            var downloadPathTcs = new TaskCompletionSource<string>();
            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            page.Download += async (sender, e) =>
            {
                downloadPathTcs.TrySetResult(await e.Download.GetPathAsync());
            };

            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            await page.ClickAsync("a");
            string path = await downloadPathTcs.Task;

            Assert.Equal("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should report download path within page.on('download', …) handler for Blobs</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForBlobs()
        {
            var downloadPathTcs = new TaskCompletionSource<string>();
            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            page.Download += async (sender, e) =>
            {
                downloadPathTcs.TrySetResult(await e.Download.GetPathAsync());
            };

            await page.GoToAsync(TestConstants.ServerUrl + "/download-blob.html");
            await page.ClickAsync("a");
            string path = await downloadPathTcs.Task;

            Assert.Equal("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should report alt-click downloads</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportAltClickDownloads()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                return context.Response.WriteAsync("Hello world");
            });

            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a", modifiers: new[] { Modifier.Alt }));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should report new window downloads</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportNewWindowDownloads()
        {
            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.Equal("Hello world", File.ReadAllText(path));
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should delete file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDeleteFile()
        {
            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();

            Assert.True(new FileInfo(path).Exists);
            await download.DeleteAsync();
            Assert.False(new FileInfo(path).Exists);
            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should expose stream</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldExposeStream()
        {
            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            using var stream = await download.CreateReadStreamAsync();
            Assert.Equal("Hello world", await new StreamReader(stream).ReadToEndAsync());

            await page.CloseAsync();
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should delete downloads on context destruction</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDeleteDownloadsOnContextDestruction()
        {
            var page = await Browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var download1Task = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                download1Task,
                page.ClickAsync("a"));

            var download2Task = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                download2Task,
                page.ClickAsync("a"));

            string path1 = await download1Task.Result.Download.GetPathAsync();
            string path2 = await download2Task.Result.Download.GetPathAsync();
            Assert.True(new FileInfo(path1).Exists);
            Assert.True(new FileInfo(path2).Exists);
            await page.Context.CloseAsync();
            Assert.False(new FileInfo(path1).Exists);
            Assert.False(new FileInfo(path2).Exists);
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-it>should delete downloads on browser gone</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDeleteDownloadsOnBrowserGone()
        {
            var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
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

            string path1 = await download1Task.Result.Download.GetPathAsync();
            string path2 = await download2Task.Result.Download.GetPathAsync();
            Assert.True(new FileInfo(path1).Exists);
            Assert.True(new FileInfo(path2).Exists);
            await browser.CloseAsync();
            Assert.False(new FileInfo(path1).Exists);
            Assert.False(new FileInfo(path2).Exists);
            Assert.False(new FileInfo(Path.Combine(path1, "..")).Exists);
        }
    }
}
