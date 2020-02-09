using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserType"/>
    public class ChromiumBrowserType : IBrowserType
    {
        private readonly string _revision = "733125";

        /// <inheritdoc cref="IBrowserType"/>
        public ChromiumBrowserType()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChromiumBrowserType"/> class.
        /// </summary>
        /// <param name="preferredRevision">Revision to use.</param>
        public ChromiumBrowserType(string preferredRevision)
        {
            _revision = preferredRevision;
        }

        /// <inheritdoc cref="IBrowserType"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => throw new NotImplementedException();

        /// <inheritdoc cref="IBrowserType"/>
        public string ExecutablePath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc cref="IBrowserType"/>
        public Task<IBrowser> ConnectAsync(ConnectOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null)
        {
            var downloadUrls = new Dictionary<Platform, string>
            {
                [Platform.Linux] = "{0}/chromium-browser-snapshots/Linux_x64/{1}/{2}.zip",
                [Platform.MacOS] = "{0}/chromium-browser-snapshots/Mac/{1}/{2}.zip",
                [Platform.Win32] = "{0}/chromium-browser-snapshots/Win/{1}/{2}.zip",
                [Platform.Win64] = "{0}/chromium-browser-snapshots/Win_x64/{1}/{2}.zip",
            };

            var path = options?.Path ?? Path.Combine(Directory.GetCurrentDirectory(), ".local-chromium");
            string host = options?.Host ?? "https://storage.googleapis.com";
            var platform = options.Platform ?? GetPlatform();

            Func<Platform, string, BrowserFetcherConfig> paramsGetter = (platform, revision) =>
            {
                var archiveName = string.Empty;
                var executablePath = string.Empty;
                if (platform == Platform.Linux)
                {
                    archiveName = "chrome-linux";
                    executablePath = Path.Combine(archiveName, "chrome");
                }
                else if (platform == Platform.MacOS)
                {
                    archiveName = "chrome-mac";
                    executablePath = Path.Combine(archiveName, "Chromium.app", "Contents", "MacOS", "Chromium");
                }
                else if (platform == Platform.Win32 || platform == Platform.Win64)
                {
                    // Windows archive name changed at r591479.
                    archiveName = int.TryParse(_revision, out int revisionNumber) && revisionNumber > 591479 ? "chrome-win" : "chrome-win32";
                    executablePath = Path.Combine(archiveName, "chrome.exe");
                }

                return new BrowserFetcherConfig
                {
                    DownloadURL = string.Format(downloadUrls[platform], host, revision, archiveName),
                    ExecutablePath = executablePath,
                };
            };

            return new BrowserFetcher(path, platform, _revision, paramsGetter);
        }

        /// <inheritdoc cref="IBrowserType"/>
        public string[] GetDefaultArgs(BrowserArgOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        private Platform GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Platform.MacOS;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Platform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimeInformation.OSArchitecture == Architecture.X64 ? Platform.Win64 : Platform.Win32;
            }

            return Platform.Unknown;
        }
    }
}
