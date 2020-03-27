using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Messaging;
using PlaywrightSharp.Chromium.Protocol.Browser;
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
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => null;

        /// <inheritdoc cref="IBrowserType"/>
        public string ExecutablePath => ResolveExecutablePath();

        /// <inheritdoc cref="IBrowserType"/>
        public string Name => "chromium";

        /// <inheritdoc cref="IBrowserType"/>
        public async Task<IBrowser> ConnectAsync(ConnectOptions options = null)
        {
            options = options == null ? new ConnectOptions() : options.Clone();

            if (string.IsNullOrEmpty(options.BrowserURL))
            {
                return await ChromiumBrowser.ConnectAsync(options).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(options.BrowserWSEndpoint) && options.TransportFactory != null)
            {
                throw new ArgumentException("Exactly one of BrowserWSEndpoint or TransportFactory must be passed to connect");
            }

            options.BrowserWSEndpoint = await GetWsEndpointAsync(options.BrowserURL).ConfigureAwait(false);

            return await ChromiumBrowser.ConnectAsync(options).ConfigureAwait(false);
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

            BrowserFetcherConfig ParamsGetter(Platform platformParam, string revision)
            {
                string archiveName = string.Empty;
                string executablePath = string.Empty;

                switch (platformParam)
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
                    DownloadURL = string.Format(CultureInfo.InvariantCulture, downloadUrls[platformParam], host, revision, archiveName),
                    ExecutablePath = executablePath,
                };
            }

            return new BrowserFetcher(path, platform, PreferredRevision.ToString(CultureInfo.InvariantCulture.NumberFormat), ParamsGetter);
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
            var connectOptions = app.ConnectOptions;
            connectOptions.EnqueueTransportMessages = options?.EnqueueTransportMessages ?? false;
            return await ChromiumBrowser.ConnectAsync(app, connectOptions).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IBrowserType"/>
        public async Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            options ??= new LaunchOptions();

            var (chromiumArgs, tempUserDataDir) = PrepareChromiumArgs(options);
            string chromiumExecutable = GetChromeExecutablePath(options);
            BrowserApp browserApp = null;

            var process = new ChromiumProcessManager(
                chromiumExecutable,
                chromiumArgs,
                tempUserDataDir,
                options.Timeout,
                async () =>
                {
                    if (browserApp == null)
                    {
                        return;
                    }

                    var transport = await BrowserHelper.CreateTransportAsync(browserApp.ConnectOptions).ConfigureAwait(false);
                    await transport.SendAsync(new BrowserCloseRequest().Command).ConfigureAwait(false);
                },
                (exitCode) =>
                {
                    browserApp?.ProcessKilled(exitCode);
                });

            try
            {
                SetEnvVariables(process.Process.StartInfo.Environment, options.Env, Environment.GetEnvironmentVariables());

                if (options.DumpIO)
                {
                    process.Process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
                }

                await process.StartAsync().ConfigureAwait(false);
                var connectOptions = new ConnectOptions()
                {
                    BrowserWSEndpoint = process.Endpoint,
                    SlowMo = options.SlowMo,
                };

                return new BrowserApp(process, () => Task.CompletedTask, connectOptions);
            }
            catch
            {
                await process.KillAsync().ConfigureAwait(false);
                throw;
            }
        }

        private static void SetEnvVariables(IDictionary<string, string> environment, IDictionary<string, string> customEnv, IDictionary realEnv)
        {
            foreach (DictionaryEntry item in realEnv)
            {
                environment[item.Key.ToString()] = item.Value.ToString();
            }

            if (customEnv != null)
            {
                foreach (var item in customEnv)
                {
                    environment[item.Key] = item.Value;
                }
            }
        }

        private static async Task<string> GetWsEndpointAsync(string browserUrl)
        {
            try
            {
                if (Uri.TryCreate(new Uri(browserUrl), "/json/version", out var endpointUrl))
                {
                    string data;
                    using (var client = new HttpClient())
                    {
                        data = await client.GetStringAsync(endpointUrl).ConfigureAwait(false);
                    }

                    return JsonSerializer.Deserialize<WSEndpointResponse>(data).WebSocketDebuggerUrl;
                }

                throw new MessageException($"Invalid URL {browserUrl}");
            }
            catch (Exception ex)
            {
                throw new MessageException($"Failed to fetch browser webSocket url from {browserUrl}.", ex);
            }
        }

        private string GetChromeExecutablePath(LaunchOptions options)
        {
            string chromeExecutable = options.ExecutablePath;
            if (string.IsNullOrEmpty(chromeExecutable))
            {
                chromeExecutable = ResolveExecutablePath();
            }

            if (!File.Exists(chromeExecutable))
            {
                throw new FileNotFoundException("Failed to launch chrome! path to executable does not exist", chromeExecutable);
            }

            return chromeExecutable;
        }

        private string ResolveExecutablePath()
        {
            var browserFetcher = CreateBrowserFetcher();
            var revisionInfo = browserFetcher.GetRevisionInfo();

            if (!revisionInfo.Local)
            {
                throw new FileNotFoundException("Chromium revision is not downloaded. Run BrowserFetcher.DownloadAsync or download Chromium manually", revisionInfo.ExecutablePath);
            }

            return revisionInfo.ExecutablePath;
        }

        private (List<string> chromiumArgs, TempDirectory tempUserDataDir) PrepareChromiumArgs(LaunchOptions options)
        {
            var chromiumArgs = new List<string>();

            if (!options.IgnoreDefaultArgs)
            {
                chromiumArgs.AddRange(GetDefaultArgs(options));
            }
            else if (options.IgnoredDefaultArgs?.Length > 0)
            {
                chromiumArgs.AddRange(GetDefaultArgs(options).Except(options.IgnoredDefaultArgs));
            }
            else
            {
                chromiumArgs.AddRange(options.Args);
            }

            TempDirectory tempUserDataDir = null;

            if (!chromiumArgs.Any(argument => argument.StartsWith("--remote-debugging-", StringComparison.Ordinal)))
            {
                chromiumArgs.Add("--remote-debugging-port=0");
            }

            string userDataDirOption = chromiumArgs.FirstOrDefault(i => i.StartsWith(UserDataDirArgument, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(userDataDirOption))
            {
                tempUserDataDir = new TempDirectory();
                chromiumArgs.Add($"{UserDataDirArgument}={tempUserDataDir.Path.Quote()}");
            }

            return (chromiumArgs, tempUserDataDir);
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
