using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>download.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class DownloadTests : PageTestEx
    {
        [SetUp]
        public void Setup()
        {
            HttpServer.Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment";
                return context.Response.WriteAsync("Hello world");
            });

            HttpServer.Server.SetRoute("/downloadWithFilename", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
                return context.Response.WriteAsync("Hello world");
            });
        }

        [PlaywrightTest("download.spec.ts", "should report downloads with acceptDownloads: false")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadsWithAcceptDownloadsFalse()
        {
            await Page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/downloadWithFilename\">download</a>");
            var downloadTask = Page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                Page.ClickAsync("a"));

            var download = downloadTask.Result;
            Assert.AreEqual($"{TestConstants.ServerUrl}/downloadWithFilename", download.Url);
            Assert.AreEqual("file.txt", download.SuggestedFilename);

            var exception = await AssertThrowsAsync<PlaywrightException>(() => download.PathAsync());
            StringAssert.Contains("acceptDownloads", await download.FailureAsync());
            StringAssert.Contains("acceptDownloads: true", exception.Message);
        }

        [PlaywrightTest("download.spec.ts", "should report downloads with acceptDownloads: true")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadsWithAcceptDownloadsTrue()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var download = await page.RunAndWaitForDownloadAsync(async () =>
            {
                await page.ClickAsync("a");
            });
            string path = await download.PathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.AreEqual("Hello world", File.ReadAllText(path));
        }

        [PlaywrightTest("download.spec.ts", "should save to user-specified path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToUserSpecifiedPath()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToUserSpecifiedPathWithoutUpdatingOriginalPath()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");

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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToTwoDifferentPathsWithMultipleSaveAsCalls()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");

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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSaveToOverwrittenFilepath()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCreateSubdirectoriesWhenSavingToNonExistentUserSpecifiedPath()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
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

        [PlaywrightTest("download.spec.ts", "should save when connected remotely")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldSaveWhenConnectedRemotely()
        {
        }

        [PlaywrightTest("download.spec.ts", "should error when saving with downloads disabled")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldErrorWhenSavingWithDownloadsDisabled()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = false });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;

            var exception = await AssertThrowsAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
            StringAssert.Contains("Pass { acceptDownloads: true } when you are creating your browser context", exception.Message);
        }

        [PlaywrightTest("download.spec.ts", "should error when saving after deletion")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldErrorWhenSavingAfterDeletion()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            using var tmpDir = new TempDirectory();
            string userPath = Path.Combine(tmpDir.Path, "download.txt");
            var download = downloadTask.Result;
            await download.DeleteAsync();
            var exception = await AssertThrowsAsync<PlaywrightException>(() => download.SaveAsAsync(userPath));
            StringAssert.Contains("Target page, context or browser has been closed", exception.Message);
        }

        [PlaywrightTest("download.spec.ts", "should error when saving after deletion when connected remotely")]
        [Test, Ignore("SKIP WIRE")]
        public void ShouldErrorWhenSavingAfterDeletionWhenConnectedRemotely()
        {
        }

        [PlaywrightTest("download.spec.ts", "should report non-navigation downloads")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNonNavigationDownloads()
        {
            HttpServer.Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                return context.Response.WriteAsync("Hello world");
            });

            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.SetContentAsync($"<a download=\"file.txt\" href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForFiles()
        {
            var downloadPathTcs = new TaskCompletionSource<string>();
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            page.Download += async (_, e) =>
            {
                downloadPathTcs.TrySetResult(await e.PathAsync());
            };

            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            await page.ClickAsync("a");
            string path = await downloadPathTcs.Task;

            Assert.AreEqual("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should report download path within page.on('download', …) handler for Blobs")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadPathWithinPageOnDownloadHandlerForBlobs()
        {
            var downloadPathTcs = new TaskCompletionSource<string>();
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            page.Download += async (_, e) =>
            {
                downloadPathTcs.TrySetResult(await e.PathAsync());
            };

            await page.GotoAsync(TestConstants.ServerUrl + "/download-blob.html");
            await page.ClickAsync("a");
            string path = await downloadPathTcs.Task;

            Assert.AreEqual("Hello world", File.ReadAllText(path));
            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should report alt-click downloads")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportAltClickDownloads()
        {
            HttpServer.Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                return context.Response.WriteAsync("Hello world");
            });

            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a", new PageClickOptions { Modifiers = new[] { KeyboardModifier.Alt } }));

            var download = downloadTask.Result;
            string path = await download.PathAsync();

            Assert.True(new FileInfo(path).Exists);
            Assert.AreEqual("Hello world", File.ReadAllText(path));
        }

        [PlaywrightTest("download.spec.ts", "should report new window downloads")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNewWindowDownloads()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteFile()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldExposeStream()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a target=_blank href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForDownloadAsync();

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            using var stream = await download.CreateReadStreamAsync();
            Assert.AreEqual("Hello world", await new StreamReader(stream).ReadToEndAsync());

            await page.CloseAsync();
        }

        [PlaywrightTest("download.spec.ts", "should delete downloads on context destruction")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteDownloadsOnContextDestruction()
        {
            var page = await Browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteDownloadsOnBrowserGone()
        {
            var browser = await BrowserType.LaunchAsync();
            var page = await browser.NewPageAsync(new BrowserNewPageOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
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
    }
}
