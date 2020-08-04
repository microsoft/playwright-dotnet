using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.DownloadsPath
{
    ///<playwright-file>downloadspath.spec.js</playwright-file>
    ///<playwright-describe>browserType.launchPersistent({acceptDownloads})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DownloadsPathLaunchPersistentTests : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        private IBrowserContext _context = null;
        private IPage _page = null;
        private TempDirectory _userDataDir = null;
        private TempDirectory _downloadsPath = null;

        /// <inheritdoc/>
        public DownloadsPathLaunchPersistentTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>downloadspath.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchPersistent({acceptDownloads})</playwright-describe>
        ///<playwright-it>should accept downloads</playwright-it>
        [Retry]
        public async Task ShouldAcceptDownloads()
        {
            var downloadTask = _page.WaitForEvent<DownloadEventArgs>(PageEvent.Download);

            await Task.WhenAll(
                downloadTask,
                _page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            Assert.Equal($"{TestConstants.ServerUrl}/download", download.Url);
            Assert.Equal("file.txt", download.SuggestedFilename);

            string path = await download.GetPathAsync();
            Assert.True(new FileInfo(path).Exists);
        }

        ///<playwright-file>downloadspath.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchPersist   qent({acceptDownloads})</playwright-describe>
        ///<playwright-it>should not delete downloads when the context closes</playwright-it>
        [Retry]
        public async Task ShouldNotDeleteDownloadsWhenTheContextCloses()
        {
            var downloadTask = _page.WaitForEvent<DownloadEventArgs>(PageEvent.Download);

            await Task.WhenAll(
                downloadTask,
                _page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            string path = await download.GetPathAsync();
            await _context.CloseAsync();
            Assert.True(new FileInfo(path).Exists);
        }

        public async Task InitializeAsync()
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
                return context.Response.WriteAsync("Hello world");
            });

            _downloadsPath = new TempDirectory();
            _userDataDir = new TempDirectory();

            var options = (LaunchPersistentOptions)TestConstants.GetDefaultBrowserOptions();
            options.DownloadsPath = _downloadsPath.Path;
            options.AcceptDownloads = true;
            _context = await Playwright[TestConstants.Product].LaunchPersistenContextAsync(_userDataDir.Path, options);
            _page = _context.Pages[0];
            await _page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/download\">download</a>");
        }

        public async Task DisposeAsync()
        {
            _downloadsPath?.Dispose();
            _userDataDir?.Dispose();
            await _context.CloseAsync();
        }
    }
}
