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
            /*
             * Scenarios this has to cover:
             * 1) The simple one, where the .playwright folder is next to the BaseDirectory, which is almost always
             * 2) Scenarios where we have to rely on the assembly location of Microsoft.Playwright.dll, but can't be
             *      sure that's always correct, because it can be empty for some cases (hence 1. is first)
             * 3) Scenarios where we get loaded from NuGet and **NOT** copied, which is rare, but implies we're loaded
             *      from the NuGet cache.
             */
            var location = typeof(Playwright).Assembly.Location;
            var potentialPaths = new string[]
            {
                GetPath(AppContext.BaseDirectory),
                GetPath(Path.GetDirectoryName(location)),
                !string.IsNullOrEmpty(location) ? GetPath(new FileInfo(location)?.Directory?.Parent?.Parent?.FullName) : null,
            };

            var path = Array.Find(potentialPaths, x => File.Exists(x));
            if (path != null)
            {
                return path;
            }

            throw new PlaywrightException($"Driver not found in any location: {string.Join(Environment.NewLine, potentialPaths)}");
        }

        private static string GetPath(string driversPath)
        {
            if (string.IsNullOrEmpty(driversPath))
            {
                return null;
            }

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

            return Path.Combine(driversPath, ".playwright", "node", platformId, runnerName);
        }
    }
}
