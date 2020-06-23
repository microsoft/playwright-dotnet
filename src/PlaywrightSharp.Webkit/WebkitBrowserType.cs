using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Server;

namespace PlaywrightSharp.Webkit
{
    /// <inheritdoc cref="IBrowserType"/>
    public class WebkitBrowserType : BrowserTypeBase
    {
        /// <summary>
        /// Preferred revision.
        /// </summary>
        public const int PreferredRevision = 1127;

        private static string _cachedMacVersion;

        /// <inheritdoc cref="IBrowserType.ConnectAsync(ConnectOptions)"/>
        public override Task<IBrowser> ConnectAsync(ConnectOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public override IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null)
        {
            var downloadUrls = new Dictionary<Platform, string>
            {
                [Platform.Linux] = "{0}/builds/webkit/{1}/minibrowser-gtk-wpe.zip",
                [Platform.MacOS] = "{0}/builds/webkit/{1}/minibrowser-mac-{2}.zip",
                [Platform.Win64] = "{0}/builds/webkit/{1}/minibrowser-win64.zip",
            };

            string path = options?.Path ?? Path.Combine(Directory.GetCurrentDirectory(), ".local-webkit");
            string host = options?.Host ?? "https://playwright.azureedge.net";
            var platform = options?.Platform ?? GetPlatform();

            BrowserFetcherConfig ParamsGetter(Platform platformParam, string revision)
            {
                string archiveName = string.Empty;
                string executablePath = string.Empty;

                return new BrowserFetcherConfig
                {
                    DownloadURL = platformParam == Platform.MacOS
                        ? string.Format(downloadUrls[platformParam], host, revision, GetMacVersion())
                        : string.Format(downloadUrls[platformParam], host, revision),
                    ExecutablePath = platformParam == Platform.Win32 || platformParam == Platform.Win64 ? "MiniBrowser.exe" : "pw_run.sh",
                };
            }

            return new BrowserFetcher(path, platform, PreferredRevision.ToString(CultureInfo.InvariantCulture.NumberFormat), ParamsGetter);
        }

        /// <inheritdoc cref="IBrowserType.GetDefaultArgs"/>
        public override string[] GetDefaultArgs(LaunchOptionsBase options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public override Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>
        public override Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        internal override Platform GetPlatform()
        {
            var platform = base.GetPlatform();
            return platform == Platform.Win32 ? Platform.Win64 : platform;
        }

        private string GetMacVersion()
        {
            if (_cachedMacVersion == null)
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "sw_vers",
                    Arguments = "-productVersion",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                });
                process.WaitForExit();
                string[] parts = process.StandardOutput.ReadToEnd().Trim().Split('.');
                int major = int.Parse(parts[0]);
                int minor = int.Parse(parts[1]);
                if (!(major == 10 && minor >= 14))
                {
                    throw new PlatformNotSupportedException("Unsupported macOS version, macOS 10.14 and newer are supported");
                }

                _cachedMacVersion = major + "." + minor;
            }

            return _cachedMacVersion;
        }

        private (List<string> webkitArgs, TempDirectory tempUserDataDir) PrepareWebkitArgs(LaunchOptions options)
        {
            var webkitArgs = new List<string>();
            var tempUserDataDir = new TempDirectory();

            return (webkitArgs, tempUserDataDir);
        }
    }
}
