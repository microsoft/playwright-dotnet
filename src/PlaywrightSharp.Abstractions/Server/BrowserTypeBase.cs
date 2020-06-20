using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PlaywrightSharp.Server
{
    /// <summary>
    /// Base type for implementing <see cref="IBrowserType"/>.
    /// </summary>
    internal abstract class BrowserTypeBase : IBrowserType
    {
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

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public abstract IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null);

        /// <inheritdoc cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>
        public abstract Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null);

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            if (options != null && !string.IsNullOrEmpty(options.UserDataDir))
            {
                throw new ArgumentException("UserDataDir option is not supported in LauncheAsync. Use LaunchPersistentContextAsync instead");
            }

            if (options != null && options.Port.HasValue)
            {
                throw new ArgumentException("Cannot specify a port without launching as a server.");
            }

            options = ValidateLaunchOptions(options);
            const loggers = new Loggers(options.logger);
            const browser = await runAbortableTask(progress => this._innerLaunch(progress, options, loggers, undefined), loggers.browser, TimeoutSettings.timeout(options), `browserType.launch`);
            return browser;
        }

        /// <inheritdoc cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>
        public abstract string[] GetDefaultArgs(BrowserArgOptions options = null);

        /// <inheritdoc cref="IBrowserType.ConnectAsync(ConnectOptions)"/>
        public abstract Task<IBrowser> ConnectAsync(ConnectOptions options = null);

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

        internal string GetBrowserExecutablePath(LaunchOptions options)
        {
            string browserExecutable = options.ExecutablePath;
            if (string.IsNullOrEmpty(browserExecutable))
            {
                browserExecutable = ResolveExecutablePath();
            }

            return browserExecutable;
        }

        internal string ResolveExecutablePath()
        {
            var browserFetcher = CreateBrowserFetcher();
            var revisionInfo = browserFetcher.GetRevisionInfo();

            if (!revisionInfo.Local)
            {
                throw new FileNotFoundException($"{Name} revision is not downloaded. Run BrowserFetcher.DownloadAsync or download {Name} manually", revisionInfo.ExecutablePath);
            }

            return revisionInfo.ExecutablePath;
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
