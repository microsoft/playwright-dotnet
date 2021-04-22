using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DownloadsPathTests : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        private IBrowser _browser { get; set; }
        private TempDirectory _tmp = null;

        /// <inheritdoc/>
        public DownloadsPathTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("downloads-path.spec.ts", "should keep downloadsPath folder")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldKeepDownloadsPathFolder()
        {
            var page = await _browser.NewPageAsync();
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            Assert.Equal($"{TestConstants.ServerUrl}/download", download.Url);
            Assert.Equal("file.txt", download.SuggestedFilename);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => download.PathAsync());

            await page.CloseAsync();
            await _browser.CloseAsync();
            Assert.True(new DirectoryInfo(_tmp.Path).Exists);
        }

        [PlaywrightTest("downloads-path.spec.ts", "should delete downloads when context closes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteDownloadsWhenContextCloses()
        {
            var page = await _browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadsInDownloadsPathFolder()
        {
            var page = await _browser.NewPageAsync(acceptDownloads: true);
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result;
            string path = await download.PathAsync();
            Assert.StartsWith(_tmp.Path, path);
            await page.CloseAsync();
        }

        /// <inheritsdoc/>
        public async Task InitializeAsync()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
                return context.Response.WriteAsync("Hello world");
            });

            _tmp = new TempDirectory();
            _browser = await Playwright[TestConstants.Product].LaunchDefaultAsync(downloadsPath: _tmp.Path);
        }

        /// <inheritsdoc/>
        public async Task DisposeAsync()
        {
            _tmp?.Dispose();
            await _browser.CloseAsync();
        }
    }
}
