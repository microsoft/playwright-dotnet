/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
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
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.Helpers;

internal static class Driver
{
    internal static string GetExecutablePath()
    {
        DirectoryInfo assemblyDirectory = new(AppContext.BaseDirectory);
        if (!assemblyDirectory.Exists || !File.Exists(Path.Combine(assemblyDirectory.FullName, "Microsoft.Playwright.dll")))
        {
            string assemblyLocation;
            var assembly = typeof(Playwright).Assembly;
#pragma warning disable SYSLIB0012 // 'Assembly.CodeBase' is obsolete: 'Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility.
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out var codeBase) && codeBase.IsFile)
#pragma warning restore SYSLIB0012 // 'Assembly.CodeBase' is obsolete: 'Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility.
            {
                assemblyLocation = codeBase.LocalPath;
            }
            else
            {
                assemblyLocation = assembly.Location;
            }
            assemblyDirectory = new FileInfo(assemblyLocation).Directory;
        }

        string executableFile = GetPath(assemblyDirectory.FullName);
        if (File.Exists(executableFile))
        {
            return executableFile;
        }

        // if the above fails, we can assume we're in the nuget registry
        executableFile = GetPath(assemblyDirectory.Parent.Parent.FullName);
        if (File.Exists(executableFile))
        {
            return executableFile;
        }

        throw new PlaywrightException($"Driver not found: {executableFile}");
    }

    private static string GetPath(string driversPath)
    {
        string platformId;
        string runnerName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platformId = "win32_x64";
            runnerName = "playwright.cmd";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            runnerName = "playwright.sh";
            platformId = "mac";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            runnerName = "playwright.sh";
            platformId = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }
        else
        {
            throw new PlaywrightException("Unknown platform");
        }

        return Path.Combine(driversPath, ".playwright", "node", platformId, runnerName);
    }

    internal static Dictionary<string, string> GetEnvironmentVariables()
    {
        var environmentVariables = new Dictionary<string, string>();
        environmentVariables.Add("PW_LANG_NAME", "csharp");
        environmentVariables.Add("PW_LANG_NAME_VERSION", $"{Environment.Version.Major}.{Environment.Version.Minor}");
        environmentVariables.Add("PW_CLI_DISPLAY_VERSION", GetSemVerPackageVersion());
        return environmentVariables;
    }

    private static string GetSemVerPackageVersion()
    {
        // AssemblyName.Version returns a 4 digit version number, this method
        // drops the last number which represents the build revision.
        string version = typeof(Driver).Assembly.GetName().Version.ToString();
        string[] versionParts = version.Split('.');
        return $"{versionParts[0]}.{versionParts[1]}.{versionParts[2]}";
    }
}
