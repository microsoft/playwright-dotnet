using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.DownloadsPath
{
    ///<playwright-file>downloadspath.spec.js</playwright-file>
    ///<playwright-describe>browserType.launch({downloadsPath})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DownloadsPathLaunchTests : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        private IBrowser _browser { get; set; }
        private TempDirectory _tmp = null;

        /// <inheritdoc/>
        public DownloadsPathLaunchTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("downloadspath.spec.js", "browserType.launch({downloadsPath})", "should keep downloadsPath folder")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldKeepDownloadsPathFolder()
        {
            var page = await _browser.NewPageAsync();
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            Assert.Equal($"{TestConstants.ServerUrl}/download", download.Url);
            Assert.Equal("file.txt", download.SuggestedFilename);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => download.GetPathAsync());

            await page.CloseAsync();
            await _browser.CloseAsync();
            Assert.True(new DirectoryInfo(_tmp.Path).Exists);
        }

        [PlaywrightTest("downloadspath.spec.js", "browserType.launch({downloadsPath})", "should delete downloads when context closes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeleteDownloadsWhenContextCloses()
        {
            var page = await _browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();
            Assert.True(new FileInfo(path).Exists);
            await page.CloseAsync();
            Assert.False(new FileInfo(path).Exists);
        }

        [PlaywrightTest("downloadspath.spec.js", "browserType.launch({downloadsPath})", "should report downloads in downloadsPath folder")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDownloadsInDownloadsPathFolder()
        {
            var page = await _browser.NewPageAsync(new BrowserContextOptions { AcceptDownloads = true });
            await page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
            var downloadTask = page.WaitForEventAsync(PageEvent.Download);

            await TaskUtils.WhenAll(
                downloadTask,
                page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();
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

            var options = TestConstants.GetDefaultBrowserOptions();
            options.DownloadsPath = _tmp.Path;
            _browser = await Playwright[TestConstants.Product].LaunchAsync(options);
        }

        /// <inheritsdoc/>
        public async Task DisposeAsync()
        {
            _tmp?.Dispose();
            await _browser.CloseAsync();
        }
    }
}
