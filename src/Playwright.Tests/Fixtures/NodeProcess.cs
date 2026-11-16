/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// What the upstream tests take from <c>process</c> of Node.js: the tests are not a Node.js process,
/// so the Node.js is the one of the driver that they run with.
/// </summary>
public static class NodeProcess
{
    private static readonly Lazy<string> _driverDir = new(FindDriverDir);

    /// <summary><c>process.execPath</c>, and <c>'node'</c> as the first item of a command.</summary>
    public static string ExecPath
    {
        get
        {
            var nodePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_NODEJS_PATH");
            if (!string.IsNullOrEmpty(nodePath))
            {
                return nodePath;
            }
            var arm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            var (platformId, executable) = Env.Platform switch
            {
                "win32" => ("win32_x64", "node.exe"),
                "darwin" => (arm64 ? "darwin-arm64" : "darwin-x64", "node"),
                _ => (arm64 ? "linux-arm64" : "linux-x64", "node"),
            };
            return Path.Combine(_driverDir.Value, "node", platformId, executable);
        }
    }

    /// <summary><c>packages/playwright-core/cli.js</c>: the CLI of the driver.</summary>
    public static string CliPath => Path.Combine(_driverDir.Value, "package", "cli.js");

    /// <summary>
    /// <c>process.kill(pid, signal)</c> with 'SIGHUP', 'SIGINT', 'SIGKILL' or 'SIGTERM'. A negative pid is a
    /// process group. On Windows every signal terminates the process, as in Node.js.
    /// </summary>
    public static void Kill(int pid, string signal = "SIGTERM")
    {
        var number = signal switch
        {
            "SIGHUP" => 1,
            "SIGINT" => 2,
            "SIGKILL" => 9,
            "SIGTERM" => 15,
            _ => throw new ArgumentException($"Unknown signal: {signal}", nameof(signal)),
        };
        if (Env.IsWindows)
        {
            using var process = Process.GetProcessById(pid);
            process.Kill();
            return;
        }
        if (SysKill(pid, number) != 0)
        {
            throw new InvalidOperationException($"kill({pid}, {signal}) failed: {new Win32Exception(Marshal.GetLastPInvokeError()).Message}");
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int SysKill(int pid, int sig);

    private static string FindDriverDir()
    {
        var searchPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        var directory = string.IsNullOrEmpty(searchPath) ? Path.GetDirectoryName(typeof(IPlaywright).Assembly.Location) : searchPath;
        var driverDir = Path.Combine(directory, ".playwright");
        if (!File.Exists(Path.Combine(driverDir, "package", "cli.js")))
        {
            throw new InvalidOperationException($"Could not find the driver in {driverDir}");
        }
        return driverDir;
    }
}
