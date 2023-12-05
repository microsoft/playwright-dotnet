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
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright;

public class Program
{
    public static int Main(string[] args)
    {
        var p = new Program();
        return p.Run(args);
    }

    public int Run(string[] args)
    {
        Func<string, string> getArgs;
        string executablePath;
        try
        {
            (executablePath, getArgs) = Driver.GetExecutablePath();
        }
        catch
        {
            return PrintError("Microsoft.Playwright assembly was found, but is missing required assets. Please ensure to build your project before running Playwright tool.");
        }

        var playwrightStartInfo = new ProcessStartInfo(executablePath, getArgs(string.Join("\" \"", args)))
        {
            UseShellExecute = false,
            // This works after net8.0-preview-4
            // https://github.com/dotnet/runtime/pull/82662
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var pair in Driver.EnvironmentVariables)
        {
            playwrightStartInfo.EnvironmentVariables[pair.Key] = pair.Value;
        }

        using var pwProcess = new Process()
        {
            StartInfo = playwrightStartInfo,
        };

        pwProcess.Start();

        pwProcess.WaitForExit();
        return pwProcess.ExitCode;
    }

    private static int PrintError(string error)
    {
        Console.Error.WriteLine("\x1b[91m" + error + "\x1b[0m");
        return 1;
    }
}
