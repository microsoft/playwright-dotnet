using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Server
{
    /// <summary>
    /// Base type for implementing <see cref="IBrowserType"/>.
    /// </summary>
    internal abstract class BrowserTypeBase : IBrowserType
    {
        private static readonly string _downloadsFolder = Path.Combine(Path.GetTempPath(), "playwright_downloads-");
        private readonly string _browserPath;
        private string _executablePath;
        private ILoggerFactory _loggerFactory;

        protected BrowserTypeBase(string executablePath, BrowserDescriptor browser, ILoggerFactory loggerFactory = null)
        {
            Name = browser.Browser;
            string browserPath = BrowserPaths.GetBrowsersPath(executablePath);
            _browserPath = BrowserPaths.GetBrowserDirectory(browserPath, browser);
            _executablePath = BrowserPaths.GetExecutablePath(_browserPath, browser);
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

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public abstract IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null);

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null)
            => TaskProgress.RunAbortableTask(
                    progress => InnerLaunchAsync(progress, options),
                    _loggerFactory,
                    options?.Timeout ?? Playwright.DefaultTimeout,
                    "BrowserType.LaunchAsync");

        /// <inheritdoc cref="IBrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentOptions)"/>
        public async Task<IBrowser> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options = null)
        {
            options = ValidateBrowserContextOptions(options);

            var browser = await TaskProgress.RunAbortableTask(
                progress => InnerLaunchAsync(progress, options, options.ContextOptions, userDataDir),
                _loggerFactory,
                options?.Timeout ?? Playwright.DefaultTimeout,
                "BrowserType.LaunchPersistentContextAsync").ConfigureAwait(false);

            return browser.DefaultContext;
        }

        /// <inheritdoc cref="IBrowserType.LaunchServerAsync(LaunchServerOptions)"/>
        public Task<IBrowserServer> LaunchServerAsync(LaunchServerOptions options = null)
            => TaskProgress.RunAbortableTask(
                progress => LaunchServerAsync(progress, options, false, _loggerFactory),
                _loggerFactory,
                options?.Timeout ?? Playwright.DefaultTimeout,
                "BrowserType.LaunchServerAsync");

        /// <inheritdoc cref="IBrowserType.ConnectAsync(ConnectOptions)"/>
        public Task<IBrowser> ConnectAsync(ConnectOptions options = null)
            => TaskProgress.RunAbortableTask(
                async progress =>
                {
                    options ??= new ConnectOptions();
                    var transport = await WebSocketTransport.CreateAsync(progress, options.WSEndpoint).ConfigureAwait(false);

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
                "BrowserType.ConnectAsync");

        private async Task<(BrowserServer browserServer, string downloadsPath, IConnectionTransport transport)> LaunchServerAsync(
            TaskProgress progress,
            LaunchServerOptions options,
            bool isPersistent,
            ILoggerFactory loggerFactory,
            string userDataDir)
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
                OnExit = (exitCode) => browserServer.OnClose(exitCode),
            });

            browserServer = new BrowserServer(launchResult);
            progress.CleanupWhenAborted = () => browserServer?.CloseOrKillAsync(progress.TimeUntilDeadline);

            if (!SupportsPipes)
            {
                string innerEndpoint = await ProcessLauncher.WaitForLineAsync(progress, launchResult.Process, WebSocketInfoStreamSource, WebSocketRegEx).ConfigureAwait(false);
                transport = await WebSocketTransport.CreateAsync(progress, innerEndpoint);
            }

            return (browserServer, downloadsPath, transport);
        }


        protected abstract string[] GetDefaultArgs(LaunchServerOptions options, bool isPersistent, string userDataDir);

        protected abstract Task<IBrowser> ConnectToTransportAsync(object transport, BrowserOptions browserOptions);

        protected abstract Dictionary<string, string> AmmendEnvironment(IDictionary<string, string> startInfoEnvironment, string userDataDir, string executable, List<string> browserArguments);

        protected abstract void AttemptToGracefullyCloseBrowser(IConnectionTransport transport);

        private LaunchPersistentOptions ValidateBrowserContextOptions(LaunchPersistentOptions options)
        {
            throw new NotImplementedException();
        }

        private async Task<BrowserBase> InnerLaunchAsync(
            TaskProgress progress,
            LaunchOptions options,
            PersistentContextOptions persistent = null,
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
                await browser.DefaultContext.LoadDefaultContextAsync().ConfigureAwait(false);
            }

            return browser;
        }

        private ProxySettings VerifyProxySettings(ProxySettings proxy)
        {
            throw new NotImplementedException();
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
