using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DriverDownloader.Linux;

namespace PlaywrightSharp.BuildTasks
{
    public class DriverDownloader : Microsoft.Build.Utilities.Task
    {
        private static readonly (string Platform, string Runtime)[] _platforms = new[]
        {
            ("mac", "osx"),
            ("linux", "unix"),
            ("win32_x64", "win-x64"),
            ("win32", "win-x86")
        };

        public string BasePath { get; set; }

        public string DriverVersion { get; set; }

        public override bool Execute()
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }

        private async Task<bool> ExecuteAsync()
        {
            var destinationDirectory = new DirectoryInfo(Path.Combine(BasePath, "src", "PlaywrightSharp", "runtimes"));
            string driverVersion = DriverVersion;

            if (!destinationDirectory.Exists)
            {
                destinationDirectory.Create();
            }

            var versionFile = new FileInfo(Path.Combine(destinationDirectory.FullName, driverVersion));

            if (!versionFile.Exists)
            {
                foreach (var file in destinationDirectory.GetFiles().Where(f => f.Name != "expected_api_mismatch.json"))
                {
                    file.Delete();
                }

                var tasks = new List<Task>();

                if (!driverVersion.Contains("next"))
                {
                    tasks.Add(UpdateBrowserVersionsAsync(BasePath, driverVersion));
                }

                foreach (var (platform, runtime) in _platforms)
                {
                    tasks.Add(DownloadDriverAsync(destinationDirectory, driverVersion, platform, runtime));
                }

                await Task.WhenAll(tasks);
                GenerateApiFile(destinationDirectory.FullName);
                versionFile.CreateText();
            }
            else
            {
                Log.LogMessage("Drivers are up-to-date");
            }

            return true;
        }

        private void GenerateApiFile(string driversDirectory)
        {
            string executablePath = GetDriverPath(driversDirectory);
            var process = GetProcess(executablePath);
            process.Start();

            using StreamWriter file = new(Path.Combine(driversDirectory, "api.json"));
            process.StandardOutput.BaseStream.CopyTo(file.BaseStream);

            process.WaitForExit();
        }

        private string GetDriverPath(string driversDirectory)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    return Path.Combine(driversDirectory, "win-x64", "playwright.cmd");
                }
                else
                {
                    return Path.Combine(driversDirectory, "win-x86", "playwright.cmd");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(driversDirectory, "osx", "playwright.sh");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(driversDirectory, "unix", "playwright.sh");
            }

            throw new Exception("Unknown platform");
        }

        private static Process GetProcess(string driverExecutablePath)
            => new()
        {
            StartInfo =
                {
                    FileName = driverExecutablePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = "print-api-json"
                },
        };

        private static async Task UpdateBrowserVersionsAsync(string basePath, string driverVersion)
        {
            string readmePath = Path.Combine(basePath, "README.md");
            string readmeInDocsPath = Path.Combine(basePath, "docfx_project", "documentation", "index.md");
            string playwrightVersion = driverVersion;
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

        private async Task DownloadDriverAsync(DirectoryInfo destinationDirectory, string driverVersion, string platform, string runtime)
        {
            Log.LogMessage("Downloading driver for " + platform);
            string cdn = "https://playwright.azureedge.net/builds/driver";

            if (driverVersion.Contains("next"))
            {
                cdn += "/next";
            }

            using var client = new HttpClient();
            string url = $"{cdn}/playwright-{driverVersion}-{platform}.zip";

            try
            {
                var response = await client.GetAsync(url);

                var directory = new DirectoryInfo(Path.Combine(destinationDirectory.FullName, runtime));

                if (directory.Exists)
                {
                    directory.Delete(true);
                }

                new ZipArchive(await response.Content.ReadAsStreamAsync()).ExtractToDirectory(directory.FullName);

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
                Log.LogMessage($"Driver for {platform} downloaded");
            }
            catch (Exception ex)
            {
                Log.LogMessage($"Unable to download driver for {driverVersion} using url {url}");
                throw new Exception($"Unable to download driver for {driverVersion} using url {url}", ex);
            }
        }
    }
}
