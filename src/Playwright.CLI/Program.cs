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
using System.IO;

namespace Microsoft.Playwright.CLI
{
    static class Program
    {
        static int Main(string[] args)
        {
            var pathCandidate = Directory.GetCurrentDirectory();

            static string getDriverDirectory(string path)
            {
                try
                {
                    if (Directory.Exists(Path.Combine(path, ".playwright")))
                        return path;

                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var candidate = getDriverDirectory(dir);
                        if (!string.IsNullOrEmpty(candidate))
                            return candidate;
                    }

                    return null;
                }
                catch (Exception) { return null; }
            }

            // we need to use the original command line to avoid getting quotes stripped by the runtime
            // see https://github.com/microsoft/playwright-dotnet/issues/1653
            args = Environment.GetCommandLineArgs();
            var argumentsLine = Environment.CommandLine.Replace(args[0], string.Empty).Trim(); // replaces the original caller name
            if (args.Length > 2 && args[1] == "-p")
            {
                if (Path.IsPathFullyQualified(args[2]))
                {
                    pathCandidate = args[2];
                }
                else
                {
                    pathCandidate = Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(args[2]));
                }

                argumentsLine = argumentsLine.Replace($"-p {args[2]}", string.Empty).Trim();
            }

            var driverPath = getDriverDirectory(pathCandidate);
            if (driverPath == null)
                return PrintError($"Couldn't find project using Playwright. Ensure a project or a solution exists in {pathCandidate}, or provide another path using -p.");

            var pwHelper = new Microsoft.Playwright.Program();
            driverPath = Helpers.Paths.GetPath(driverPath);
            return pwHelper.Run(driverPath, string.Join(" ", argumentsLine));
        }

        private static int PrintError(string error)
        {
            Console.Error.WriteLine("\x1b[91m" + error + "\x1b[0m");
            return 1;
        }
    }
}
