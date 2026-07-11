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
        ["PW_CLI_DISPLAY_VERSION"] = "1.0.0",
    };

    internal static (string ExecutablePath, string CliEntrypoint) GetExecutablePath()
    {
        var baseDir = AppContext.BaseDirectory;
        var assemblyDirectory = new DirectoryInfo(baseDir);

        string executableFile;
        string cliEntrypoint;

        var driverSearchPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        if (!string.IsNullOrEmpty(driverSearchPath))
        {
            var safeSearchPath = ResolveEnvironmentPath(driverSearchPath, "PLAYWRIGHT_DRIVER_SEARCH_PATH");
            if (!Directory.Exists(safeSearchPath))
            {
                throw new PlaywrightException($"PLAYWRIGHT_DRIVER_SEARCH_PATH points to non-existent directory: {safeSearchPath}");
            }

            Console.Error.WriteLine("[playwright] WARNING: Using PLAYWRIGHT_DRIVER_SEARCH_PATH override. This bypasses built-in driver resolution.");
            (executableFile, cliEntrypoint) = GetPath(safeSearchPath);
            ValidateResolvedDriverPath(executableFile, cliEntrypoint, "PLAYWRIGHT_DRIVER_SEARCH_PATH");
            return (executableFile, cliEntrypoint);
        }

        (executableFile, cliEntrypoint) = GetPath(assemblyDirectory.FullName);
        if (File.Exists(executableFile) && File.Exists(cliEntrypoint))
        {
            return (executableFile, cliEntrypoint);
        }

        if (assemblyDirectory.Parent?.Parent != null)
        {
            (executableFile, cliEntrypoint) = GetPath(assemblyDirectory.Parent.Parent.FullName);
            if (File.Exists(executableFile) && File.Exists(cliEntrypoint))
            {
                return (executableFile, cliEntrypoint);
            }
        }

        throw new PlaywrightException($"Driver not found: {executableFile}");
    }

    private static (string ExecutablePath, string CliEntrypoint) GetPath(string driversPath)
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
        var envNodePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH");
        string resolvedNodePath;
        if (!string.IsNullOrEmpty(envNodePath))
        {
            resolvedNodePath = ResolveEnvironmentPath(envNodePath, "PLAYWRIGHT_NODEJS_PATH");
            if (!File.Exists(resolvedNodePath))
            {
                throw new PlaywrightException($"PLAYWRIGHT_NODEJS_PATH points to non-existent file: {resolvedNodePath}");
            }

            var fileName = Path.GetFileName(resolvedNodePath);
            if (!string.Equals(fileName, "node", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fileName, "node.exe", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"[playwright] WARNING: PLAYWRIGHT_NODEJS_PATH points to '{fileName}', expected 'node' or 'node.exe'.");
            }

            Console.Error.WriteLine("[playwright] WARNING: Using PLAYWRIGHT_NODEJS_PATH override. This bypasses built-in driver resolution.");
        }
        else
        {
            resolvedNodePath = Path.GetFullPath(Path.Combine(driversPath, ".playwright", "node", platformId, nodeExecutable));
        }

        return (resolvedNodePath, cliEntrypoint);
    }

    private static string ResolveEnvironmentPath(string path, string purpose)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            throw new PlaywrightException($"{purpose} must be a fully-qualified path: {path}");
        }

        return SecurityHelpers.ResolveAndValidatePath(path, purpose);
    }

    private static void ValidateResolvedDriverPath(string executableFile, string cliEntrypoint, string source)
    {
        if (!File.Exists(executableFile))
        {
            throw new PlaywrightException($"Couldn't find Node.js executable from {source}: {executableFile}");
        }

        if (!File.Exists(cliEntrypoint))
        {
            throw new PlaywrightException($"Couldn't find Playwright driver entrypoint from {source}: {cliEntrypoint}");
        }
    }
}
