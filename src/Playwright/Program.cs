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
using System.Text;
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
        Process pwProcess;
        try
        {
            pwProcess = CreateDriverProcess(args, captureOutput: false);
        }
        catch
        {
            return PrintError("Microsoft.Playwright assembly was found, but is missing required assets. Please ensure to build your project before running Playwright tool.");
        }
        using (pwProcess)
        {
            pwProcess.Start();
            pwProcess.WaitForExit();
            return pwProcess.ExitCode;
        }
    }

    /// <summary>
    /// Runs the Playwright driver with the given arguments and returns the captured stdout, stderr, and exit code.
    /// Unlike <see cref="Run(string[])"/>, this does not inherit the calling process's stdio, so the output is
    /// available to callers running under environments (e.g. xUnit) that don't surface inherited console output.
    /// </summary>
    /// <param name="args">The arguments to pass to the Playwright driver (e.g. <c>install</c>, <c>codegen</c>).</param>
    /// <returns>The captured <see cref="RunResult"/>.</returns>
    public RunResult RunWithResult(string[] args)
    {
        using var pwProcess = CreateDriverProcess(args, captureOutput: true);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        pwProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdout.AppendLine(e.Data);
            }
        };
        pwProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderr.AppendLine(e.Data);
            }
        };

        pwProcess.Start();
        pwProcess.BeginOutputReadLine();
        pwProcess.BeginErrorReadLine();
        pwProcess.WaitForExit();

        return new RunResult(pwProcess.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static Process CreateDriverProcess(string[] args, bool captureOutput)
    {
        var (executablePath, getArgs) = Driver.GetExecutablePath();
        var startInfo = new ProcessStartInfo(executablePath, getArgs(args.Length > 0 ? "\"" + string.Join("\" \"", args) + "\"" : null))
        {
            UseShellExecute = false,
            // This works after net8.0-preview-4
            // https://github.com/dotnet/runtime/pull/82662
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
        };
        foreach (var pair in Driver.EnvironmentVariables)
        {
            startInfo.EnvironmentVariables[pair.Key] = pair.Value;
        }
        return new Process() { StartInfo = startInfo };
    }

    private static int PrintError(string error)
    {
        Console.Error.WriteLine("\x1b[91m" + error + "\x1b[0m");
        return 1;
    }

    /// <summary>
    /// Captured result of <see cref="RunWithResult(string[])"/>.
    /// </summary>
    public class RunResult
    {
        internal RunResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public int ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }
    }
}
