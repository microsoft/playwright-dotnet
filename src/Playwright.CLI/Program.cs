using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Assumed path: {GetFullPath()}");
            Console.WriteLine("Hello World!");
        }

        private static string GetFullPath()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;

            var assumedRootDirectory = new DirectoryInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages",
                "microsoft.playwright"));

            if (!assumedRootDirectory.Exists)
            {
                // figure out what to fallback to?
                return string.Empty;
            }

            string path = Path.Combine(assumedRootDirectory.FullName, version.ToString());
            if (!Directory.Exists(path))
            {
                // fallback to the first one with the best version match#
                var targetPath = assumedRootDirectory.GetDirectories().Where(x => x.Name.Contains(version.ToString())).FirstOrDefault();
                if (targetPath == null)
                {
                    // fallback to the first folder we find
                    path = assumedRootDirectory.GetDirectories().FirstOrDefault()?.FullName;
                    if(string.IsNullOrEmpty(path))
                    {
                        throw new Exception("not found");
                    }
                }
            }


            var assumedPath = new DirectoryInfo(Path.Combine(path, "Drivers"));
            return GetDriverPath(assumedPath.FullName);
        }

        // TODO: Potentially move this to a shared file between Playwright and the CLI
        private static string GetDriverPath(string driversDirectory)
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
    }
}
