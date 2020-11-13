using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
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

            var destinationDirectory = new DirectoryInfo(args[0]);
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

                versionFile.CreateText();
                var tasks = new List<Task>();
                foreach (var platform in _platforms)
                {
                    tasks.Add(DownloadDriverAsync(destinationDirectory, driverVersion, platform.Platform, platform.Runtime));
                }

                await Task.WhenAll(tasks);
                UpdateApiJson(destinationDirectory);
            }
            else
            {
                Console.WriteLine("Drivers are up-to-date");
            }
        }

        private static void UpdateApiJson(DirectoryInfo destinationDirectory)
        {
            StringBuilder output = new StringBuilder();
            var process = new Process()
            {
                StartInfo =
                {
                    FileName = GetDriverPath(destinationDirectory),
                    Arguments = $"print-api-json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            process.OutputDataReceived += (_, e) => output.Append(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            File.WriteAllText(
                Path.Combine(destinationDirectory.FullName, "..", "..", "tools", "ApiChecker", "api.json"),
                output.ToString());
        }

        private static string GetDriverPath(DirectoryInfo directory)
        {
            string driversPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                driversPath = Path.Combine(directory.FullName, "win-x64", "playwright-cli.exe");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                driversPath = Path.Combine(directory.FullName, "osx", "playwright-cli");
            }
            else
            {
                driversPath = Path.Combine(directory.FullName, "unix", "playwright-cli");
            }

            return driversPath;
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
