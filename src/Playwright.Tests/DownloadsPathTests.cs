using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class DownloadsPathTests : PlaywrightTestEx
    {
        private IBrowser _browser { get; set; }
        private TempDirectory _tmp = null;

        [PlaywrightTest("downloads-path.spec.ts", "should keep downloadsPath folder")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldKeepDownloadsPathFolder()
        {
            var page = await _browser.NewPageAsync();
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
            _tmp?.Dispose();
            await _browser.CloseAsync();
        }
    }
}
