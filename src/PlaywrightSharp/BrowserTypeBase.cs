using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Base type for implementing <see cref="IBrowserType"/>.
    /// </summary>
    public abstract class BrowserTypeBase : IBrowserType
    {
        /// <inheritdoc cref="IBrowserType.Devices"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => DeviceDescriptors.ToReadOnly();

        /// <inheritdoc cref="IBrowserType.ExecutablePath"/>
        public string ExecutablePath => ResolveExecutablePath();

        /// <inheritdoc cref="IBrowserType.Name"/>
        public abstract string Name { get; }

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public abstract IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null);

        /// <inheritdoc cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>
        public abstract Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null);

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public abstract Task<IBrowser> LaunchAsync(LaunchOptions options = null);

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

            if (!File.Exists(browserExecutable))
            {
                throw new FileNotFoundException($"Failed to launch {Name}! path to executable does not exist", browserExecutable);
            }

            return browserExecutable;
        }

        internal string ResolveExecutablePath()
        {
            var browserFetcher = CreateBrowserFetcher();
            var revisionInfo = browserFetcher.GetRevisionInfo();

            if (!revisionInfo.Local)
            {
                throw new FileNotFoundException("Chromium revision is not downloaded. Run BrowserFetcher.DownloadAsync or download Chromium manually", revisionInfo.ExecutablePath);
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
