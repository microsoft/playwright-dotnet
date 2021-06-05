using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Graph;

namespace Microsoft.Playwright.CLI
{
    static class Program
    {
        static void Main(string[] args)
        {
            string path = Directory.GetCurrentDirectory();

            if (args.Length > 1 && args[0] == "-p")
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), args[1]);
                args = args[2..];
            }

            string version = null;
            try
            {
                version = ParsePlaywrightVersion(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("\x1b[91m" + e.Message + "\x1b[0m");
                return;
            }

            string pwPath = GetPathToDriver(version);

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
            playwrightStartInfo.EnvironmentVariables.Add("PW_CLI_NAME ", "playwright");

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

        private static string ParsePlaywrightVersion(string path)
        {
            if (!(File.Exists(path) || Directory.Exists(path)))
            {
                throw new Exception($"Project path does not exist. Ensure a project exists in {path}, or pass the path to the project using -p.");
            }

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                var solutions = Directory.GetFiles(path, "*.sln");
                if (solutions.Length == 1)
                {
                    return ParseSolution(Path.GetFullPath(solutions[0]));
                }

                if (solutions.Length > 1)
                {
                    throw new Exception($"More than one solution file found in {path}, pass the path to the project using -p.");
                }

                var projects = Directory.GetFiles(path, "*.csproj");

                if (projects.Length > 1)
                {
                    throw new Exception($"More than one project file found in {path}, pass the path to the project using -p.");
                }

                if (projects.Length == 1)
                {
                    return ParseProject(Path.GetFullPath(projects[0]));
                }

                throw new Exception($"Couldn't find a project that uses Playwright. Ensure a project exists in {path}, or pass the path to the project using -p.");
            }

            if (path.EndsWith(".sln"))
            {
                return ParseSolution(path);
            }

            if (path.EndsWith(".csproj"))
            {
                return ParseProject(path);
            }

            throw new Exception($"Specified file is neither project nor solution. Ensure a project exists in {path}, or pass the path to the project using -p.");
        }

        private static string ParseSolution(string path)
        {
            var solution = SolutionFile.Parse(path);
            if (solution.ProjectsInOrder.Count > 1)
            {
                throw new Exception($"More than one project file found in {path}, pass the path to the project using -p.");
            }
            return ParseProject(solution.ProjectsInOrder[0].AbsolutePath);
        }

        private static string ParseProject(string path)
        {
            var document = XDocument.Load(path);
            var references = GetDescendants(document, "PackageReference");
            string version = null;
            foreach (var item in references)
            {
                if (GetAttributeValue(item, "Include") == "Microsoft.Playwright")
                    version = GetAttributeValue(item, "Version");
                if (GetAttributeValue(item, "Include") == "Microsoft.Playwright.NUnit")
                    version = GetAttributeValue(item, "Version");
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception("Unable to detect version: project does not depend on Microsoft.Playwright");
            }
            return version;
        }

        private static string GetPathToDriver(string version)
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

            return GetDriverPlatformPath(Path.Combine(packageDirectory.FullName, version, "Drivers"));
        }

        private static string GetDriverPlatformPath(string driversDirectory, string nativeComponent = "native")
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

        private static IEnumerable<XElement> GetDescendants(XDocument document, string name) =>
            document.Descendants().Where(x => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));

        private static string GetAttributeValue(XElement element, string name) =>
            element.Attributes().FirstOrDefault(x => string.Equals(x.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
