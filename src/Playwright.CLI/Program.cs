using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Playwright.CLI
{
    class Program
    {
        private const string DriverEnvironmentPath = "PW_CLI_DRIVERPATH";

        static void Main(string[] args)
        {
            string pwPath = GetFullPath();
            Console.WriteLine(pwPath);
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
            string envPath = Environment.GetEnvironmentVariable(DriverEnvironmentPath);
            if (!string.IsNullOrEmpty(envPath))
            {
                envPath = Path.Join(envPath, "Drivers");
                if (!Directory.Exists(envPath))
                {
                    Console.Error.WriteLine($"The path specified in the environment variable {DriverEnvironmentPath} ({envPath}) does not contain the Drivers folder.");
                }

                return GetDriverPath(envPath);
            }

            var version = Assembly.GetEntryAssembly().GetName().Version;

            string sourcePath = TraverseAndFindFolder(new DirectoryInfo("."));
            if (!string.IsNullOrEmpty(sourcePath))
            {
                return GetDriverPath(sourcePath, string.Empty);
            }

            sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages",
                "microsoft.playwright");

            var assumedRootDirectory = new DirectoryInfo(sourcePath);

            if (!assumedRootDirectory.Exists)
            {
                throw new Exception("Driver Not found");
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
                    if (string.IsNullOrEmpty(path))
                    {
                        throw new Exception("Driver Not found");
                    }
                }
            }

            var assumedPath = new DirectoryInfo(Path.Combine(path, "Drivers"));
            return GetDriverPath(assumedPath.FullName);
        }

        private static string TraverseAndFindFolder(DirectoryInfo root)
        {
            foreach (var subdir in root.EnumerateDirectories())
            {
                if (subdir.Name == ".playwright")
                {
                    return subdir.FullName;
                }

                string attempt = TraverseAndFindFolder(subdir);
                if (!string.IsNullOrEmpty(attempt))
                    return attempt;
            }

            return null;
        }

        // TODO: Potentially move this to a shared file between Playwright and the CLI
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
