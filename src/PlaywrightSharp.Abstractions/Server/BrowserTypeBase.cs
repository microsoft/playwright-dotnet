using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Server
{
    /// <summary>
    /// Base type for implementing <see cref="IBrowserType"/>.
    /// </summary>
    internal abstract class BrowserTypeBase : IBrowserType
    {
        private static readonly string _downloadsFolder = Path.Combine(Path.GetTempPath(), "playwright_downloads-");
        private readonly string _executablePath;
        private readonly ILoggerFactory _loggerFactory;

        protected BrowserTypeBase(string executablePath, BrowserDescriptor browser, ILoggerFactory loggerFactory = null)
        {
            Name = browser.Browser;
            string browserPath = BrowserPaths.GetBrowsersPath(executablePath);
            BrowserPath = BrowserPaths.GetBrowserDirectory(browserPath, browser);
            _executablePath = BrowserPaths.GetExecutablePath(BrowserPath, browser);
            _loggerFactory = loggerFactory ?? new LoggerFactory();
        }

        /// <inheritdoc cref="IBrowserType.Devices"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => DeviceDescriptors.ToReadOnly();

        /// <inheritdoc cref="IBrowserType.ExecutablePath"/>
        public string ExecutablePath
        {
            get
            {
                if (string.IsNullOrEmpty(_executablePath))
                {
                    throw new PlaywrightSharpException("Browser is not supported on current platform");
                }

                return _executablePath;
            }
        }

        /// <inheritdoc cref="IBrowserType.Name"/>
        public Browser Name { get; }

        protected abstract bool SupportsPipes { get; }

        protected abstract ProcessStream WebSocketInfoStreamSource { get; }

        protected abstract string WebSocketRegEx { get; }

        protected string BrowserPath { get; }

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public abstract IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null);

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public async Task<IBrowser> LaunchAsync(LaunchOptions options = null)
            => await TaskProgress.RunAbortableTaskAsync(
                    progress => InnerLaunchAsync(progress, options),
                    _loggerFactory,
                    options?.Timeout ?? Playwright.DefaultTimeout,
                    "BrowserType.LaunchAsync").ConfigureAwait(false);

        /// <inheritdoc cref="IBrowserType.LaunchPersistentContextAsync(string,LaunchPersistentOptions)"/>
        public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options = null)
        {
            options ??= new LaunchPersistentOptions();
            options.ContextOptions = ValidateBrowserContextOptions(options.ContextOptions);

            var browser = await TaskProgress.RunAbortableTaskAsync(
                progress => InnerLaunchAsync(progress, options, options.ContextOptions, userDataDir),
                _loggerFactory,
                options?.Timeout ?? Playwright.DefaultTimeout,
                "BrowserType.LaunchPersistentContextAsync").ConfigureAwait(false);

            return browser.DefaultContext;
        }

        /// <inheritdoc cref="IBrowserType.LaunchServerAsync(LaunchServerOptions)"/>
        public async Task<IBrowserServer> LaunchServerAsync(LaunchServerOptions options = null)
            => (await TaskProgress.RunAbortableTaskAsync(
                progress => LaunchServerAsync(progress, options, false, _loggerFactory),
                _loggerFactory,
                options?.Timeout ?? Playwright.DefaultTimeout,
                "BrowserType.LaunchServerAsync").ConfigureAwait(false)).BrowserServer;

        /// <inheritdoc cref="IBrowserType.ConnectAsync(ConnectOptions)"/>
        public async Task<IBrowser> ConnectAsync(ConnectOptions options = null)
            => await TaskProgress.RunAbortableTaskAsync(
                async progress =>
                {
                    options ??= new ConnectOptions();
                    var transport = await WebSocketTransport.CreateAsync(progress, options).ConfigureAwait(false);

                    if (options.Async != null)
                    {
                        await options.Async().ConfigureAwait(false);
                    }

                    return await ConnectToTransportAsync(
                        transport,
                        new BrowserOptions
                        {
                            SlowMo = options.SlowMo,
                        }).ConfigureAwait(false);
                },
                _loggerFactory,
                options?.Timeout ?? Playwright.DefaultTimeout,
                "BrowserType.ConnectAsync").ConfigureAwait(false);


        protected abstract string[] GetDefaultArgs(LaunchServerOptions options, bool isPersistent, string userDataDir);

        protected abstract Task<BrowserBase> ConnectToTransportAsync(object transport, BrowserOptions browserOptions);

        protected abstract Dictionary<string, string> AmmendEnvironment(IDictionary<string, string> startInfoEnvironment, string userDataDir, string executable, List<string> browserArguments);

        protected abstract void AttemptToGracefullyCloseBrowser(IConnectionTransport transport);

        private async Task<LaunchServerResult> LaunchServerAsync(
            TaskProgress progress,
            LaunchServerOptions options,
            bool isPersistent,
            ILoggerFactory loggerFactory,
            string userDataDir = null)
        {
            options ??= new LaunchServerOptions();

            List<string> tempDirectories = new List<string>();
            string downloadsPath;

            if (!string.IsNullOrEmpty(options.DownloadsPath))
            {
                downloadsPath = options.DownloadsPath;
                new DirectoryInfo(downloadsPath).CreateIfNotExists();
            }
            else
            {
                downloadsPath = _downloadsFolder;
                new DirectoryInfo(downloadsPath).CreateIfNotExists();
                tempDirectories.Add(downloadsPath);
            }

            if (string.IsNullOrEmpty(userDataDir))
            {
                userDataDir = Path.Combine(Path.GetTempPath(), $"playwright_{Name.ToString().ToLower()}dev_profile-");
                new DirectoryInfo(userDataDir).CreateIfNotExists();
                tempDirectories.Add(userDataDir);
            }

            var browserArguments = new List<string>();
            if (!options.IgnoreDefaultArgs)
            {
                browserArguments.AddRange(GetDefaultArgs(options, isPersistent, userDataDir));
            }
            else if (options.IgnoredDefaultArgs.Length > 0)
            {
                browserArguments.AddRange(GetDefaultArgs(options, isPersistent, userDataDir).Where(arg => !options.IgnoredDefaultArgs.Contains(arg)));
            }
            else
            {
                browserArguments.AddRange(options.Args);
            }

            string executable = string.IsNullOrEmpty(options.ExecutablePath) ? ExecutablePath : options.ExecutablePath;
            if (string.IsNullOrEmpty(executable))
            {
                throw new PlaywrightSharpException("No executable path is specified. Pass ExecutablePath option directly.");
            }

            IConnectionTransport transport = null;
            BrowserServer browserServer = null;

            var launchResult = ProcessLauncher.LaunchProcess(new LaunchProcessOptions
            {
                ExecutablePath = executable,
                Args = browserArguments,
                Env = AmmendEnvironment(Process.GetCurrentProcess().StartInfo.Environment, userDataDir, executable, browserArguments),
                Pipe = SupportsPipes,
                Progress = progress,
                TempDirectories = tempDirectories,
                AttemptToGracefullyClose = async () =>
                {
                    if (options.TestHookGracefullyCloseAsync != null)
                    {
                        await options.TestHookGracefullyCloseAsync().ConfigureAwait(false);
                    }

                    AttemptToGracefullyCloseBrowser(transport);
                },
                OnExit = exitCode => browserServer?.OnClose(exitCode),
            });

            browserServer = new BrowserServer(launchResult);
            progress.CleanupWhenAborted = () => browserServer?.CloseOrKillAsync(progress.TimeUntilDeadline);

            if (!SupportsPipes)
            {
                var match = await ProcessLauncher.WaitForLineAsync(progress, launchResult.Process, WebSocketInfoStreamSource, WebSocketRegEx).ConfigureAwait(false);
                string innerEndpoint = match.Groups[1].Value;
                transport = await WebSocketTransport.CreateAsync(progress, innerEndpoint).ConfigureAwait(false);
            }

            return new LaunchServerResult
            {
                BrowserServer = browserServer,
                DownloadsPath = downloadsPath,
                Transport = transport,
            };
        }

        private BrowserContextOptions ValidateBrowserContextOptions(BrowserContextOptions options)
        {
            var result = options?.Clone() ?? new BrowserContextOptions();

            if (result.Viewport == null && result.DeviceScaleFactor != 1)
            {
                throw new PlaywrightSharpException("DeviceScaleFactor option is not supported with null viewport");
            }

            if (result.Viewport == null && result.IsMobile)
            {
                throw new PlaywrightSharpException("IsMobile option is not supported with null viewport");
            }

            if (result.Geolocation != null)
            {
                VerifyGeolocation(result.Geolocation);
            }

            return result;
        }

        private void VerifyGeolocation(GeolocationOption geolocation)
        {
            if (geolocation.Longitude < -180 || geolocation.Longitude > 180)
            {
                throw new ArgumentException($"Invalid longitude '{geolocation.Longitude}': precondition -180 <= LONGITUDE <= 180 failed.");
            }

            if (geolocation.Latitude < -90 || geolocation.Latitude > 90)
            {
                throw new ArgumentException($"Invalid latitude '{geolocation.Latitude}': precondition -90 <= LONGITUDE <= 90 failed.");
            }

            if (geolocation.Accuracy < 0)
            {
                throw new ArgumentException($"Invalid accuracy '{geolocation.Accuracy}': precondition 0 <= LONGITUDE failed.");
            }
        }

        private async Task<BrowserBase> InnerLaunchAsync(
            TaskProgress progress,
            LaunchOptions options,
            BrowserContextOptions persistent = null,
            string userDataDir = null)
        {
            options ??= new LaunchOptions();
            options.Proxy = options.Proxy != null ? VerifyProxySettings(options.Proxy) : null;
            var (browserServer, downloadsPath, transport) = await LaunchServerAsync(progress, options.ToLaunchServerOptions(), persistent != null, _loggerFactory, userDataDir).ConfigureAwait(false);

            options.TestHook();

            var browserOptions = new BrowserOptions
            {
                SlowMo = options.SlowMo,
                Persistent = persistent,
                Headful = !options.Headless,
                DownloadsPath = downloadsPath,
                OwnedServer = browserServer,
                Proxy = options.Proxy,
                TestHook = options.TestHook,
            };

            var browser = await ConnectToTransportAsync(transport, browserOptions).ConfigureAwait(false);

            // We assume no control when using custom arguments, and do not prepare the default context in that case.
            bool hasCustomArguments = options.IgnoreDefaultArgs && options.IgnoredDefaultArgs?.Any() == false;
            if (persistent != null && !hasCustomArguments)
            {
                await ((BrowserContext)browser.DefaultContext).LoadDefaultContextAsync().ConfigureAwait(false);
            }

            return browser;
        }

        private ProxySettings VerifyProxySettings(ProxySettings proxy)
        {
            var url = new Url(proxy.Server);
            if(url.P)
            if (!['http:', 'https:', 'socks5:'].includes(url.protocol)) {
                url = new URL('http://' + server);
                server = `${url.protocol}//${url.host}`;
            }
            if (bypass)
                bypass = bypass.split(',').map(t => t.trim()).join(',');
            return { ...proxy, server, bypass };
        }

        internal static void SetEnvVariables(IDictionary<string, string> environment, IDictionary<string, string> customEnv, IDictionary realEnv)
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

        internal virtual Platform GetPlatform()
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
