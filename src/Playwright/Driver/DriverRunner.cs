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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Playwright.Driver
{
    public static class DriverRunner
    {
        public static void Run(string driverPath, string[] args)
        {
            string allArgs = args != null && args.Length > 0 ? "\"" + string.Join("\" \"", args) + "\"" : string.Empty;
            var playwrightStartInfo = new ProcessStartInfo(driverPath, allArgs)
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
        }
    }
}
