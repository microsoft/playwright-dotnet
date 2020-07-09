using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Unix;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>BrowserFetcher</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class BrowserFetcherTests : PlaywrightSharpBaseTest, IDisposable
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

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>BrowserFetcher</playwright-describe>
        ///<playwright-it>should download and extract linux binary</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldDownloadAndExtractLinuxBinary()
        {
            var browserFetcher = BrowserType.CreateBrowserFetcher(new BrowserFetcherOptions
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
        private Mono.Unix.FileAccessPermissions ConvertPermissions(PlaywrightSharp.Helpers.Linux.FileAccessPermissions executableFilePermissions)
        {
            Mono.Unix.FileAccessPermissions output = 0;

            var map = new Dictionary<PlaywrightSharp.Helpers.Linux.FileAccessPermissions, Mono.Unix.FileAccessPermissions>()
            {
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.OtherExecute, Mono.Unix.FileAccessPermissions.OtherExecute},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.OtherWrite, Mono.Unix.FileAccessPermissions.OtherWrite},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.OtherRead, Mono.Unix.FileAccessPermissions.OtherRead},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.GroupExecute, Mono.Unix.FileAccessPermissions.GroupExecute},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.GroupWrite, Mono.Unix.FileAccessPermissions.GroupWrite},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.GroupRead, Mono.Unix.FileAccessPermissions.GroupRead},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.UserExecute, Mono.Unix.FileAccessPermissions.UserExecute},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.UserWrite, Mono.Unix.FileAccessPermissions.UserWrite},
                {PlaywrightSharp.Helpers.Linux.FileAccessPermissions.UserRead, Mono.Unix.FileAccessPermissions.UserRead}
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
