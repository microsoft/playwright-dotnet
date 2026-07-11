/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Playwright.CLI;

public static class Program
{
    static int Main(string[] args)
    {
        var path = Directory.GetCurrentDirectory();
        if (args.Length > 1 && args[0] == "-p")
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), args[1]);
            var isFile = File.Exists(path);
            if (!isFile && !Directory.Exists(path))
            {
                return PrintError($"Couldn't find project using Playwright. Ensure a project or a solution exists in {path}, or provide another path using -p.");
            }

            if (isFile)
            {
                path = Path.GetDirectoryName(path)!;
            }

            var argsCloned = new string[args.Length - 2];
            Array.Copy(args, 2, argsCloned, 0, args.Length - 2);
            args = argsCloned;
        }

        // Locating project is important, otherwise we are at risk
        // of traversing entire fs from root.
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
        {
            var solutions = Directory.GetFiles(path, "*.sln");
            var projects = Directory.GetFiles(path, "*.*proj");
            if (solutions.Length == 0 && projects.Length == 0)
            {
                return PrintError($"Couldn't find project using Playwright. Ensure a project or a solution exists in {path}, or provide another path using -p.");
            }
        }

        var file = FindPlaywrightAssembly(new(Path.GetFullPath(path)));

        if (string.IsNullOrEmpty(file))
        {
            return PrintError(@"Please make sure Playwright is installed and built prior to using Playwright tool:
   dotnet add package Microsoft.Playwright
   dotnet build");
        }

        return RunPlaywrightProgram(file, args);
    }

    private static int RunPlaywrightProgram(string file, string[] args)
    {
        var startInfo = new ProcessStartInfo(DotnetHostPath())
        {
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(file);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return PrintError("Could not start the dotnet host to run Playwright.");
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    public static string DotnetHostPath()
    {
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(hostPath) && TryResolveDotnetHostPath(hostPath, out var resolvedHostPath))
        {
            return resolvedHostPath;
        }

        return "dotnet";
    }

    private static bool TryResolveDotnetHostPath(string hostPath, out string resolvedHostPath)
    {
        resolvedHostPath = string.Empty;
        try
        {
            if (hostPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || !Path.IsPathFullyQualified(hostPath))
            {
                return false;
            }

            var fullPath = Path.GetFullPath(hostPath);
            var fileName = Path.GetFileName(fullPath);
            if (!string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!File.Exists(fullPath))
            {
                return false;
            }

            resolvedHostPath = fullPath;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string FindPlaywrightAssembly(DirectoryInfo root)
    {
        string best = null;
        var bestTfm = -1;
        var bestWriteTime = DateTime.MinValue;
        Traverse(root, ref best, ref bestTfm, ref bestWriteTime);
        return best;
    }

    private static void Traverse(DirectoryInfo root, ref string best, ref int bestTfm, ref DateTime bestWriteTime)
    {
        IEnumerable<DirectoryInfo> directories;
        try
        {
            directories = root.EnumerateDirectories();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var dir in directories)
        {
            if ((dir.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            var candidate = Path.Combine(dir.ToString(), "Microsoft.Playwright.dll");
            if (File.Exists(candidate))
            {
                ConsiderCandidate(candidate, ref best, ref bestTfm, ref bestWriteTime);
            }
            Traverse(dir, ref best, ref bestTfm, ref bestWriteTime);
        }
    }

    private static void ConsiderCandidate(string candidate, ref string best, ref int bestTfm, ref DateTime bestWriteTime)
    {
        var tfm = TargetFrameworkScore(candidate);
        var writeTime = File.GetLastWriteTimeUtc(candidate);
        if (best == null || tfm > bestTfm || (tfm == bestTfm && writeTime > bestWriteTime))
        {
            best = candidate;
            bestTfm = tfm;
            bestWriteTime = writeTime;
        }
    }

    private static int TargetFrameworkScore(string candidate)
    {
        var tfm = Path.GetFileName(Path.GetDirectoryName(candidate));
        if (tfm == null || !tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var version = tfm.Substring(3);
        var dot = version.IndexOf('.');
        var majorText = dot >= 0 ? version.Substring(0, dot) : version;
        var minorText = dot >= 0 ? version.Substring(dot + 1) : "0";
        return int.TryParse(majorText, out var major) && int.TryParse(minorText, out var minor)
            ? checked((major * 100) + minor)
            : 0;
    }

    private static int PrintError(string error)
    {
        Console.Error.WriteLine("\x1b[91m" + error + "\x1b[0m");
        return 1;
    }
}
