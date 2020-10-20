using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DriverDownloader
{
    class Program
    {
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
                Console.WriteLine("Invalid destination directory");
                return;
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
                foreach (string platform in new[] { "mac", "linux", "win32_x64" })
                {
                    tasks.Add(DownloadDriverAsync(destinationDirectory, driverVersion, platform));
                }

                await Task.WhenAll(tasks);
            }
            else
            {
                Console.WriteLine("Drivers are up-to-date");
            }
        }

        private static async Task DownloadDriverAsync(DirectoryInfo destinationDirectory, string driverVersion, string platform)
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

            using var fs = new FileStream(Path.Combine(destinationDirectory.FullName, $"playwright-cli-{platform}.zip"), FileMode.CreateNew);
            await response.Content.CopyToAsync(fs);
            Console.WriteLine($"Driver for {platform} downloaded");
        }
    }
}
