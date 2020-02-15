using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserType"/>
    public class ChromiumBrowserType : IBrowserType
    {
        /// <summary>
        /// Preferred revision.
        /// </summary>
        public const int PreferredRevision = 733125;

        private const string UserDataDirArgument = "--user-data-dir";
        private static readonly string[] DefaultArgs = new[]
        {
            "--disable-background-networking",
            "--enable-features=NetworkService,NetworkServiceInProcess",
            "--disable-background-timer-throttling",
            "--disable-backgrounding-occluded-windows",
            "--disable-breakpad",
            "--disable-client-side-phishing-detection",
            "--disable-component-extensions-with-background-pages",
            "--disable-default-apps",
            "--disable-dev-shm-usage",
            "--disable-extensions",
            "--disable-features=TranslateUI,BlinkGenPropertyTrees",
            "--disable-hang-monitor",
            "--disable-ipc-flooding-protection",
            "--disable-popup-blocking",
            "--disable-prompt-on-repost",
            "--disable-renderer-backgrounding",
            "--disable-sync",
            "--force-color-profile=srgb",
            "--metrics-recording-only",
            "--no-first-run",
            "--enable-automation",
            "--password-store=basic",
            "--use-mock-keychain",
        };

        /// <inheritdoc cref="IBrowserType"/>
        public ChromiumBrowserType()
        {
        }

        /// <inheritdoc cref="IBrowserType"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => null;

        /// <inheritdoc cref="IBrowserType"/>
        public string ExecutablePath => null;

        /// <inheritdoc cref="IBrowserType"/>
        public string Name => "chromium";

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

            string path = options?.Path ?? Path.Combine(Directory.GetCurrentDirectory(), ".local-chromium");
            string host = options?.Host ?? "https://storage.googleapis.com";
            var platform = options?.Platform ?? GetPlatform();

            Func<Platform, string, BrowserFetcherConfig> paramsGetter = (platform, revision) =>
            {
                string archiveName = string.Empty;
                string executablePath = string.Empty;

                switch (platform)
                {
                    case Platform.Linux:
                        archiveName = "chrome-linux";
                        executablePath = Path.Combine(archiveName, "chrome");
                        break;
                    case Platform.MacOS:
                        archiveName = "chrome-mac";
                        executablePath = Path.Combine(archiveName, "Chromium.app", "Contents", "MacOS", "Chromium");
                        break;
                    case Platform.Win32:
                    case Platform.Win64:
                        {
                            // Windows archive name changed at r591479.
                            archiveName = int.TryParse(revision, out int revisionNumber) && revisionNumber > 591479 ? "chrome-win" : "chrome-win32";
                            executablePath = Path.Combine(archiveName, "chrome.exe");
                            break;
                        }
                }

                return new BrowserFetcherConfig
                {
                    DownloadURL = string.Format(CultureInfo.InvariantCulture, downloadUrls[platform], host, revision, archiveName),
                    ExecutablePath = executablePath,
                };
            };

            return new BrowserFetcher(path, platform, PreferredRevision.ToString(CultureInfo.InvariantCulture.NumberFormat), paramsGetter);
        }

        /// <inheritdoc cref="IBrowserType"/>
        public string[] GetDefaultArgs(BrowserArgOptions options = null)
        {
            bool devtools = options?.Devtools ?? false;
            bool headless = options?.Headless ?? !devtools;
            string userDataDir = options?.UserDataDir;
            string[] args = options?.Args ?? Array.Empty<string>();

            var chromeArguments = new List<string>(DefaultArgs);
            if (userDataDir != null)
            {
                chromeArguments.Add($"{UserDataDirArgument}={options.UserDataDir.Quote()}");
            }

            if (devtools)
            {
                chromeArguments.Add("--auto-open-devtools-for-tabs");
            }

            if (headless)
            {
                chromeArguments.AddRange(new[]
                {
                    "--headless",
                    "--hide-scrollbars",
                    "--mute-audio",
                });
            }

            if (args.All(arg => arg.StartsWith("-", StringComparison.Ordinal)))
            {
                chromeArguments.Add("about:blank");
            }

            chromeArguments.AddRange(args);
            return chromeArguments.ToArray();
        }

        /// <inheritdoc cref="IBrowserType"/>
        public async Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            var app = await LaunchBrowserAppAsync(options).ConfigureAwait(false);
            return await ChromiumBrowser.ConnectAsync(app).ConfigureAwait(false);
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
