using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="IBrowserType"/>
    public class FirefoxBrowserType : IBrowserType
    {
        /// <summary>
        /// Preferred revision.
        /// </summary>
        public const int PreferredRevision = 1021;

        private static readonly string[] DefaultArgs = { "-no-remote" };

        /// <inheritdoc cref="IBrowserType.Devices"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => null;

        /// <inheritdoc cref="IBrowserType.ExecutablePath"/>
        public string ExecutablePath => ResolveExecutablePath();

        /// <inheritdoc cref="IBrowserType.Name"/>
        public string Name => "firefox";

        /// <inheritdoc cref="IBrowserType.ConnectAsync(ConnectOptions)"/>
        public Task<IBrowser> ConnectAsync(ConnectOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null)
        {
            var downloadUrls = new Dictionary<Platform, string>
            {
                [Platform.Linux] = "{0}/builds/firefox/{1}/firefox-linux.zip",
                [Platform.MacOS] = "{0}/builds/firefox/{1}/firefox-mac.zip",
                [Platform.Win32] = "{0}/builds/firefox/{1}/firefox-win32.zip",
                [Platform.Win64] = "{0}/builds/firefox/{1}/firefox-win64.zip",
            };

            string path = options?.Path ?? Path.Combine(Directory.GetCurrentDirectory(), ".local-firefox");
            string host = options?.Host ?? "https://playwright.azureedge.net";
            var platform = options?.Platform ?? GetPlatform();

            Func<Platform, string, BrowserFetcherConfig> paramsGetter = (platform, revision) =>
            {
                string executablePath = string.Empty;
                switch (platform)
                {
                    case Platform.Linux:
                        executablePath = Path.Combine("firefox", "firefox");
                        break;
                    case Platform.MacOS:
                        executablePath = Path.Combine("firefox", "Nightly.app", "Contents", "MacOS", "firefox");
                        break;
                    case Platform.Win32:
                    case Platform.Win64:
                        executablePath = Path.Combine("firefox", "firefox.exe");
                        break;
                }

                return new BrowserFetcherConfig
                {
                    DownloadURL = string.Format(CultureInfo.InvariantCulture, downloadUrls[platform], host, revision),
                    ExecutablePath = executablePath,
                };
            };

            return new BrowserFetcher(path, platform, PreferredRevision.ToString(CultureInfo.InvariantCulture.NumberFormat), paramsGetter);
        }

        /// <inheritdoc cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>
        public string[] GetDefaultArgs(BrowserArgOptions options = null)
        {
            if (options?.Devtools == true)
            {
                throw new PlaywrightSharpException("Option \"devtools\" is not supported by Firefox");
            }

            var firefoxArguments = new List<string>(DefaultArgs);
            if (!string.IsNullOrEmpty(options?.UserDataDir))
            {
                firefoxArguments.Add("-profile");
                firefoxArguments.Add(options.UserDataDir);
            }

            if (options?.Headless == true)
            {
                firefoxArguments.Add("-headless");
            }

            if (options?.Args != null)
            {
                firefoxArguments.AddRange(options.Args);
            }

            if (firefoxArguments.TrueForAll(arg => arg.StartsWith("-")))
            {
                firefoxArguments.Add("about:blank");
            }

            return firefoxArguments.ToArray();
        }

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>
        public async Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            var firefoxArguments = new List<string>();
            if (!(options?.IgnoreDefaultArgs == true))
            {
                firefoxArguments.AddRange(GetDefaultArgs(options));
            }
            else if (options?.IgnoredDefaultArgs?.Length > 0)
            {
                firefoxArguments.AddRange(GetDefaultArgs(options).Except(options.IgnoredDefaultArgs));
            }
            else if (options?.Args?.Length > 0)
            {
                firefoxArguments.AddRange(options.Args);
            }

            if (!firefoxArguments.Contains("-juggler"))
            {
                firefoxArguments.Insert(0, "-juggler");
            }

            string temporaryProfileDir = null;
            if (!firefoxArguments.Contains("-profile") && !firefoxArguments.Contains("--profile"))
            {
                temporaryProfileDir = await CreateProfileAsync().ConfigureAwait(false);
                firefoxArguments.InsertRange(0, new[] { "-profile", temporaryProfileDir });
            }

            string firefoxExecutable = options?.ExecutablePath;
            if (firefoxExecutable == null)
            {
                firefoxExecutable = ResolveExecutablePath();
            }

            IBrowserApp browserApp = null;
            return browserApp;
        }

        private string ResolveExecutablePath()
        {
            var browserFetcher = CreateBrowserFetcher();
            var revisionInfo = browserFetcher.GetRevisionInfo();

            if (!revisionInfo.Local)
            {
                throw new FileNotFoundException("Firefox revision is not downloaded. Run BrowserFetcher.DownloadAsync or download Firefox manually", revisionInfo.ExecutablePath);
            }

            return revisionInfo.ExecutablePath;
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

        private Task<string> CreateProfileAsync()
        {
            return Task.FromResult("null");
        }
    }
}
