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
using System.Diagnostics;
using System.Threading;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var p = new Program();
            return p.Run(args);
        }

        public int Run(string[] args)
        {
            // we need to use this logic, because .NET by design does some weird magic with the quotes
            // so, we'll use a bit of our own magic... We'll start by calling the same base mathod, however
            // we know it'll return the executable & path in the first argument. We'll use this to replace
            // the entire string, which we can access from the "original" command line that we were called with
            // which doesn't have the quotes stripped, but does include the full path
            // see https://github.com/microsoft/playwright-dotnet/issues/1653
            args = Environment.GetCommandLineArgs();
            var lineArguments = Environment.CommandLine.Replace(args[0], string.Empty);

            string pwPath = null;
            try
            {
                pwPath = Paths.GetExecutablePath();
            }
            catch
            {
                return PrintError("Microsoft.Playwright assembly was found, but is missing required assets. Please ensure to build your project before running Playwright tool.");
            }

            var playwrightStartInfo = new ProcessStartInfo(pwPath, lineArguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

            using var pwProcess = new Process()
            {
                StartInfo = playwrightStartInfo,
            };

            playwrightStartInfo.EnvironmentVariables.Add("PW_CLI_TARGET_LANG", "csharp");
            playwrightStartInfo.EnvironmentVariables.Add("PW_CLI_NAME ", "playwright");

            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);

            pwProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    Console.WriteLine(e.Data);
                }
            };

            pwProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    errorWaitHandle.Set();
                }
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
            return 0;
        }

        private static int PrintError(string error)
        {
            Console.Error.WriteLine("\x1b[91m" + error + "\x1b[0m");
            return 1;
        }
    }
}
