using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DriverDownloader.Linux;

namespace DriverDownloader
{
    class Program
    {
        private static readonly (string Platform, string Runtime)[] _platforms = new[]
        {
            ("mac", "osx"),
            ("linux", "unix"),
            ("win32_x64", "win-x64"),
            ("win32", "win-x86")
        };

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Missing destination path argument and driver version");
                return;
            }

            string basePath = args[0];
            var destinationDirectory = new DirectoryInfo(Path.Combine(basePath, "src", "PlaywrightSharp", "runtimes"));
            string driverVersion = args[1];

            if (!destinationDirectory.Exists)
            {
                destinationDirectory.Create();
            }

            var versionFile = new FileInfo(Path.Combine(destinationDirectory.FullName, driverVersion));

            if (!versionFile.Exists)
            {
                foreach (var file in destinationDirectory.GetFiles())
                {
                    file.Delete();
                }

                var tasks = new List<Task>();
                tasks.Add(UpdateBrowserVersionsAsync(basePath, driverVersion));
                foreach (var platform in _platforms)
                {
                    tasks.Add(DownloadDriverAsync(destinationDirectory, driverVersion, platform.Platform, platform.Runtime));
                }

                await Task.WhenAll(tasks);
                versionFile.CreateText();
            }
            else
            {
                Console.WriteLine("Drivers are up-to-date");
            }
        }

        private static async Task UpdateBrowserVersionsAsync(string basePath, string driverVersion)
        {
            string readmePath = Path.Combine(basePath, "readme.md");
            string readmeInDocsPath = Path.Combine(basePath, "docfx_project", "documentation", "index.md");
            string playwrightVersion = string.Join('.', driverVersion.Split('.')[1].ToCharArray());
            var regex = new Regex("<!-- GEN:(.*?) -->(.*?)<!-- GEN:stop -->", RegexOptions.Compiled);

            string readme = await GetUpstreamReadmeAsync(playwrightVersion);
            var browserMatches = regex.Matches(readme);
            File.WriteAllText(readmePath, ReplaceBrowserVersion(File.ReadAllText(readmePath), browserMatches));
            File.WriteAllText(readmeInDocsPath, ReplaceBrowserVersion(File.ReadAllText(readmeInDocsPath), browserMatches));
        }

        private static string ReplaceBrowserVersion(string content, MatchCollection browserMatches)
        {
            foreach (Match match in browserMatches)
            {
                content = new Regex($"<!-- GEN:{ match.Groups[1].Value } -->.*?<!-- GEN:stop -->")
                    .Replace(content, $"<!-- GEN:{ match.Groups[1].Value } -->{match.Groups[2].Value}<!-- GEN:stop -->");
            }

            return content;
        }

        private static Task<string> GetUpstreamReadmeAsync(string playwrightVersion)
        {
            var client = new HttpClient();
            string readmeUrl = $"https://raw.githubusercontent.com/microsoft/playwright/v{playwrightVersion}/README.md";
            return client.GetStringAsync(readmeUrl);
        }

        private static async Task DownloadDriverAsync(DirectoryInfo destinationDirectory, string driverVersion, string platform, string runtime)
        {
            Console.WriteLine("Downloading driver for " + platform);
            const string cdn = "https://playwright.azureedge.net/builds/cli";
            using var client = new HttpClient();
            var response = await client.GetAsync($"{cdn}/playwright-cli-{driverVersion}-{platform}.zip");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Unable to download driver for " + driverVersion);
                return;
            }

            var directory = new DirectoryInfo(Path.Combine(destinationDirectory.FullName, runtime));

            if (directory.Exists)
            {
                directory.Delete(true);
            }

            new ZipArchive(await response.Content.ReadAsStreamAsync()).ExtractToDirectory(directory.FullName);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var executable in directory.GetFiles().Where(f => f.Name == "playwright-cli" || f.Name.Contains("ffmpeg")))
                {
                    if (LinuxSysCall.Chmod(executable.FullName, LinuxSysCall.ExecutableFilePermissions) != 0)
                    {
                        throw new Exception($"Unable to chmod the driver ({Marshal.GetLastWin32Error()})");
                    }
                }
            }
            Console.WriteLine($"Driver for {platform} downloaded");
        }
    }
}
