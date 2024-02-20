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
    internal static Dictionary<string, string> EnvironmentVariables { get; } = new()
    {
        ["PW_LANG_NAME"] = "csharp",
        ["PW_LANG_NAME_VERSION"] = $"{Environment.Version.Major}.{Environment.Version.Minor}",
        ["PW_CLI_DISPLAY_VERSION"] = typeof(Driver).Assembly.GetName().Version.ToString(3),
    };

    internal static (string ExecutablePath, Func<string, string> GetArgs) GetExecutablePath()
    {
        DirectoryInfo assemblyDirectory = null;
        if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
        {
            assemblyDirectory = new(AppContext.BaseDirectory);
        }
        if (assemblyDirectory?.Exists != true || !File.Exists(Path.Combine(assemblyDirectory.FullName, "Microsoft.Playwright.dll")))
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

        string executableFile;
        Func<string, string> getArgs;

        // When loading the Assembly via the memory we don't have any references where the driver might be located.
        // To workaround this we pass this env from the .ps1 wrapper over to the Assembly.
        var driverSearchPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        if (!string.IsNullOrEmpty(driverSearchPath))
        {
            (executableFile, getArgs) = GetPath(driverSearchPath);
            if (!File.Exists(executableFile))
            {
                throw new PlaywrightException("Couldn't find driver in \"PLAYWRIGHT_DRIVER_SEARCH_PATH\"");
            }
            return (executableFile, getArgs);
        }

        (executableFile, getArgs) = GetPath(assemblyDirectory.FullName);
        if (File.Exists(executableFile))
        {
            return (executableFile, getArgs);
        }

        // if the above fails, we can assume we're in the nuget registry
        (executableFile, getArgs) = GetPath(assemblyDirectory.Parent.Parent.FullName);
        if (File.Exists(executableFile))
        {
            return (executableFile, getArgs);
        }

        throw new PlaywrightException($"Driver not found: {executableFile}");
    }

    private static (string ExecutablePath, Func<string, string> GetArgs) GetPath(string driversPath)
    {
        string platformId;
        string nodeExecutable;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platformId = "win32_x64";
            nodeExecutable = "node.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            nodeExecutable = "node";
            platformId = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "darwin-arm64" : "darwin-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            nodeExecutable = "node";
            platformId = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        }
        else
        {
            throw new PlaywrightException("Unknown platform");
        }

        var cliEntrypoint = Path.Combine(driversPath, ".playwright", "package", "cli.js");
        string getArgs(string args)
        {
            return !string.IsNullOrEmpty(args) ? $"\"{cliEntrypoint}\" {args}" : cliEntrypoint;
        }
        return (
            Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH") ?? Path.GetFullPath(Path.Combine(driversPath, ".playwright", "node", platformId, nodeExecutable)),
            getArgs);
    }
}
