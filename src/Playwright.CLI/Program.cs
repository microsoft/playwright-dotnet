using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.Playwright.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string pwPath = GetFullPath();
            if (string.IsNullOrEmpty(pwPath))
            {
                Console.WriteLine("Please install Playwright:");
                Console.WriteLine("   dotnet add package Microsoft.Playwright");
                return;
            }

            var playwrightStartInfo = new ProcessStartInfo(pwPath, string.Join(' ', args))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            var pwProcess = new Process()
            {
                StartInfo = playwrightStartInfo,
            };

            playwrightStartInfo.EnvironmentVariables.Add("PW_CLI_TARGET_LANG", "csharp");
            playwrightStartInfo.EnvironmentVariables.Add("PW_CLI_NAME ", "playwrightcli");

            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);

            pwProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) outputWaitHandle.Set();
                else
                {
                    Console.WriteLine(e.Data);
                }
            };

            pwProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null) errorWaitHandle.Set();
                else
                {
                    Console.Error.WriteLine(e.Data);
                }
            };

            pwProcess.Start();

            pwProcess.BeginOutputReadLine();
            pwProcess.BeginErrorReadLine();

            pwProcess.WaitForExit();
            outputWaitHandle.WaitOne(5000);
            errorWaitHandle.WaitOne(5000);
        }

        private static string GetFullPath()
        {
            string packagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget",
                    "packages");

            string sourcePath = Path.Combine(packagesPath, "microsoft.playwright");

            var packageDirectory = new DirectoryInfo(sourcePath);
            if (!packageDirectory.Exists)
            {
                return null;
            }

            var versions = packageDirectory.GetDirectories();
            int max = 0;
            string maxVersion = null;
            foreach (var version in versions)
            {
                var match = Regex.Match(version.Name, @"([\d]+)\.([\d]+)\.([\d]+)");
                if (!match.Success)
                    continue;
                var major = int.Parse(match.Groups[1].Value);
                var minor = int.Parse(match.Groups[2].Value);
                var patch = int.Parse(match.Groups[2].Value);
                var n = major * 10000 + minor * 100 + patch;
                if (n > max)
                {
                    max = n;
                    maxVersion = version.Name;
                }
            }
            if (string.IsNullOrEmpty(maxVersion))
            {
                return null;
            }

            return GetDriverPath(Path.Combine(packageDirectory.FullName, maxVersion, "Drivers"));
        }

        private static string GetDriverPath(string driversDirectory, string nativeComponent = "native")
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.OSArchitecture == Architecture.X64)
                {
                    return Path.Combine(driversDirectory, "win-x64", nativeComponent, "playwright.cmd");
                }
                else
                {
                    return Path.Combine(driversDirectory, "win-x86", nativeComponent, "playwright.cmd");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(driversDirectory, "osx", nativeComponent, "playwright.sh");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(driversDirectory, "unix", nativeComponent, "playwright.sh");
            }

            throw new Exception("Unknown platform");
        }
    }
}
