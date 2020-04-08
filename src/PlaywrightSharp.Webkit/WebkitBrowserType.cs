using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Webkit
{
    /// <inheritdoc cref="IBrowserType"/>
    public class WebkitBrowserType : IBrowserType
    {
        /// <summary>
        /// Preferred revision.
        /// </summary>
        public const int PreferredRevision = 1127;

        private static string _cachedMacVersion;

        /// <inheritdoc cref="IBrowserType.Devices"/>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices => null;

        /// <inheritdoc cref="IBrowserType.ExecutablePath"/>
        public string ExecutablePath => null;

        /// <inheritdoc cref="IBrowserType.Name"/>
        public string Name => "webkit";

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

        /// <inheritdoc cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>
        public string[] GetDefaultArgs(BrowserArgOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>
        public Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            throw new NotImplementedException();
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

        private string GetWebkitExecutablePath(LaunchOptions options)
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
                throw new FileNotFoundException("Webkit revision is not downloaded. Run BrowserFetcher.DownloadAsync or download Webkit manually", revisionInfo.ExecutablePath);
            }

            return revisionInfo.ExecutablePath;
        }

        private (List<string> webkitArgs, TempDirectory tempUserDataDir) PrepareWebkitArgs(LaunchOptions options)
        {
            var webkitArgs = new List<string>();
            var tempUserDataDir = new TempDirectory();

            return (webkitArgs, tempUserDataDir);
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
                return Platform.Win64;
            }

            return Platform.Unknown;
        }
    }
}
