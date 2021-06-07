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
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.Helpers
{
    internal static class Paths
    {
        internal static string GetExecutablePath()
        {
            var assembly = typeof(Playwright).Assembly;
            string driversPath = new FileInfo(assembly.Location).Directory.FullName;

            string executableFile = GetPath(driversPath);
            if (File.Exists(executableFile))
            {
                return executableFile;
            }

            throw new PlaywrightException($@"Driver not found: {executableFile}");
        }

        private static string GetPath(string driversPath)
        {
            string platformId;
            string runnerName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformId = RuntimeInformation.OSArchitecture == Architecture.X64 ? "win32_x64" : "win";
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
                platformId = "linux";
            }
            else
            {
                throw new PlaywrightException("Unknown platform");
            }

            return Path.Combine(driversPath, ".playwright", platformId, runnerName);
        }
    }
}
