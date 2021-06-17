using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using DriverDownloader.Linux;
using Playwright.Tooling.Options;

namespace Playwright.Tooling
{
    internal class DriverDownloader
    {
        private static readonly string[] _platforms = new[]
        {
            "mac",
            "linux",
            "win32_x64",
            "win32",
        };

        public string BasePath { get; set; }

        public string DriverVersion { get; set; }

        internal static Task RunAsync(DownloadDriversOptions o)
        {
            var props = new XmlDocument();
            props.Load(Path.Combine(o.BasePath, "src", "Common", "Version.props"));
            string driverVersion = props.DocumentElement.SelectSingleNode("/Project/PropertyGroup/DriverVersion").FirstChild.Value;

            return new DriverDownloader()
            {
                BasePath = o.BasePath,
                DriverVersion = driverVersion,
            }.ExecuteAsync();
        }

        private static async Task UpdateBrowserVersionsAsync(string basePath, string driverVersion)
        {
            try
            {
                string readmePath = Path.Combine(basePath, "README.md");
                string playwrightVersion = driverVersion.Contains("-") ? driverVersion.Substring(0, driverVersion.IndexOf("-")) : driverVersion;

                var regex = new Regex("<!-- GEN:(.*?) -->(.*?)<!-- GEN:stop -->", RegexOptions.Compiled);
                string readme;
                try
                {
                    readme = await GetUpstreamReadmeAsync(playwrightVersion).ConfigureAwait(false);
                }
                catch
                {
                    if (playwrightVersion.EndsWith(".0"))
                    {
                        throw new FileNotFoundException("Could not find a suitable README file with browser version, nor fallback.");
                    }

                    // fallback to a x.y.0 revision
                    playwrightVersion = $"{playwrightVersion.Substring(0, playwrightVersion.LastIndexOf('.') + 1)}.0";
                    readme = await GetUpstreamReadmeAsync(playwrightVersion).ConfigureAwait(false);
                }

                var browserMatches = regex.Matches(readme);
                string readmeText = await File.ReadAllTextAsync(readmePath).ConfigureAwait(false);
                await File.WriteAllTextAsync(readmePath, ReplaceBrowserVersion(readmeText, browserMatches)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARNING: Could not update the browser versions in the README file.");
                Console.WriteLine($"This is usually due to the readme file not yet existing for {driverVersion}.");
                Console.WriteLine(e.Message);
            }
        }

        private static string ReplaceBrowserVersion(string content, MatchCollection browserMatches)
        {
            foreach (Match match in browserMatches)
            {
                content = new Regex($"<!-- GEN:{match.Groups[1].Value} -->.*?<!-- GEN:stop -->")
                    .Replace(content, $"<!-- GEN:{match.Groups[1].Value} -->{match.Groups[2].Value}<!-- GEN:stop -->");
            }

            return content;
        }

        private static Task<string> GetUpstreamReadmeAsync(string playwrightVersion)
        {
            var client = new HttpClient();
            string readmeUrl = $"https://raw.githubusercontent.com/microsoft/playwright/v{playwrightVersion}/README.md";
            return client.GetStringAsync(readmeUrl);
        }

        private async Task DownloadDriverAsync(DirectoryInfo destinationDirectory, string driverVersion, string platform)
        {
            Console.WriteLine("Downloading driver for " + platform);
            string cdn = "https://playwright.azureedge.net/builds/driver/next";

            using var client = new HttpClient();
            string url = $"{cdn}/playwright-{driverVersion}-{platform}.zip";

            try
            {
                var response = await client.GetAsync(url).ConfigureAwait(false);

                var directory = new DirectoryInfo(Path.Combine(destinationDirectory.FullName, platform));

                if (directory.Exists)
                {
                    directory.Delete(true);
                }

                new ZipArchive(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)).ExtractToDirectory(directory.FullName);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var executables = new[] { "playwright.sh", "node", "package/third_party/ffmpeg/ffmpeg-linux", "package/third_party/ffmpeg/ffmpeg-mac" }
                        .Select(f => Path.Combine(directory.FullName, f));

                    foreach (string executable in executables)
                    {
                        if (new FileInfo(executable).Exists && LinuxSysCall.Chmod(executable, LinuxSysCall.ExecutableFilePermissions) != 0)
                        {
                            throw new Exception($"Unable to chmod {executable} ({Marshal.GetLastWin32Error()})");
                        }
                    }
                }

                Console.WriteLine($"Driver for {platform} downloaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to download driver for {driverVersion} using url {url}");
                throw new Exception($"Unable to download driver for {driverVersion} using url {url}", ex);
            }
        }

        private async Task<bool> ExecuteAsync()
        {
            var destinationDirectory = new DirectoryInfo(Path.Combine(BasePath, "src", "Playwright", ".drivers"));
            var dedupeDirectory = new DirectoryInfo(Path.Combine(BasePath, "src", "Playwright", ".playwright"));
            string driverVersion = DriverVersion;

            var versionFile = new FileInfo(Path.Combine(destinationDirectory.FullName, driverVersion));

            if (!versionFile.Exists)
            {
                if (destinationDirectory.Exists)
                {
                    Directory.Delete(destinationDirectory.FullName, true);
                }

                if (dedupeDirectory.Exists)
                {
                    Directory.Delete(dedupeDirectory.FullName, true);
                }

                destinationDirectory.Create();

                var tasks = new List<Task>();

                if (!driverVersion.Contains("next"))
                {
                    tasks.Add(UpdateBrowserVersionsAsync(BasePath, driverVersion));
                }

                foreach (var platform in _platforms)
                {
                    tasks.Add(DownloadDriverAsync(destinationDirectory, driverVersion, platform));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
                versionFile.CreateText();
            }
            else
            {
                Console.WriteLine("Drivers are up-to-date");
            }

            return true;
        }
    }
}
