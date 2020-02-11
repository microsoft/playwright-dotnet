using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Unix;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>BrowserFetcher</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class BrowserFetcherTests : PlaywrightSharpBrowserBaseTest, IDisposable
    {
        private readonly TempDirectory _downloadsFolder;

        /// <inheritdoc/>
        public BrowserFetcherTests(ITestOutputHelper output) : base(output)
        {
            _downloadsFolder = new TempDirectory();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _downloadsFolder.Dispose();
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>BrowserFetcher</playwright-describe>
        ///<playwright-it>should download and extract linux binary</playwright-it>
        [Fact]
        public async Task ShouldDownloadAndExtractLinuxBinary()
        {
            var browserFetcher = Playwright.CreateBrowserFetcher(new BrowserFetcherOptions
            {
                Platform = Platform.Linux,
                Path = _downloadsFolder.Path,
                Host = TestConstants.ServerUrl
            });
            var revisionInfo = browserFetcher.GetRevisionInfo("123456");

            Server.SetRedirect(revisionInfo.Url.Substring(TestConstants.ServerUrl.Length), "/chromium-linux.zip");
            Assert.False(revisionInfo.Local);
            Assert.Equal(Platform.Linux, revisionInfo.Platform);
            Assert.False(await browserFetcher.CanDownloadAsync("100000"));
            Assert.True(await browserFetcher.CanDownloadAsync("123456"));

            revisionInfo = await browserFetcher.DownloadAsync("123456");
            Assert.True(revisionInfo.Local);
            Assert.Equal("LINUX BINARY\n", File.ReadAllText(revisionInfo.ExecutablePath));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
#if NETCOREAPP //This will not be run on net4x anyway.
                Mono.Unix.FileAccessPermissions permissions = ConvertPermissions(LinuxSysCall.ExecutableFilePermissions);

                Assert.Equal(permissions, UnixFileSystemInfo.GetFileSystemEntry(revisionInfo.ExecutablePath).FileAccessPermissions & permissions);
#endif
            }
            Assert.Equal(new[] { "123456" }, browserFetcher.GetLocalRevisions());
            browserFetcher.Remove("123456");
            Assert.Empty(browserFetcher.GetLocalRevisions());

            //Download should return data from a downloaded version
            //This section is not in the Playwright test.
            await browserFetcher.DownloadAsync("123456");
            Server.Reset();
            revisionInfo = await browserFetcher.DownloadAsync("123456");
            Assert.True(revisionInfo.Local);
            Assert.Equal("LINUX BINARY\n", File.ReadAllText(revisionInfo.ExecutablePath));
        }

#if NETCOREAPP
        private Mono.Unix.FileAccessPermissions ConvertPermissions(Helpers.Linux.FileAccessPermissions executableFilePermissions)
        {
            Mono.Unix.FileAccessPermissions output = 0;

            var map = new Dictionary<Helpers.Linux.FileAccessPermissions, Mono.Unix.FileAccessPermissions>()
            {
                {Helpers.Linux.FileAccessPermissions.OtherExecute, Mono.Unix.FileAccessPermissions.OtherExecute},
                {Helpers.Linux.FileAccessPermissions.OtherWrite, Mono.Unix.FileAccessPermissions.OtherWrite},
                {Helpers.Linux.FileAccessPermissions.OtherRead, Mono.Unix.FileAccessPermissions.OtherRead},
                {Helpers.Linux.FileAccessPermissions.GroupExecute, Mono.Unix.FileAccessPermissions.GroupExecute},
                {Helpers.Linux.FileAccessPermissions.GroupWrite, Mono.Unix.FileAccessPermissions.GroupWrite},
                {Helpers.Linux.FileAccessPermissions.GroupRead, Mono.Unix.FileAccessPermissions.GroupRead},
                {Helpers.Linux.FileAccessPermissions.UserExecute, Mono.Unix.FileAccessPermissions.UserExecute},
                {Helpers.Linux.FileAccessPermissions.UserWrite, Mono.Unix.FileAccessPermissions.UserWrite},
                {Helpers.Linux.FileAccessPermissions.UserRead, Mono.Unix.FileAccessPermissions.UserRead}
            };

            foreach (var item in map.Keys)
            {
                if ((executableFilePermissions & item) == item)
                {
                    output |= map[item];
                }
            }

            return output;
        }
#endif
    }
}
