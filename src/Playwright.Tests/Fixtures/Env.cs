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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The configuration of a run, the port of <c>tests/library/playwright.config.ts</c>: one project
/// (browser) per run, chosen with the same environment variables as the old tests and upstream.
/// </summary>
internal static class Env
{
    /// <summary><c>BROWSER</c>: chromium (default), firefox or webkit.</summary>
    public static string BrowserName { get; } = ReadBrowserName();

    /// <summary><c>HEADED=1</c>, as <c>--headed</c> upstream.</summary>
    public static bool Headed { get; } = Environment.GetEnvironmentVariable("HEADED") == "1";

    /// <summary><c>PWTEST_CHANNEL</c>.</summary>
    public static string Channel { get; } = NullIfEmpty(Environment.GetEnvironmentVariable("PWTEST_CHANNEL"));

    /// <summary><c>CRPATH</c>, <c>FFPATH</c> or <c>WKPATH</c>.</summary>
    public static string ExecutablePath { get; } = NullIfEmpty(Environment.GetEnvironmentVariable(BrowserName switch
    {
        "firefox" => "FFPATH",
        "webkit" => "WKPATH",
        _ => "CRPATH",
    }));

    public static bool CI { get; } = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

    /// <summary>A failed test runs again up to 3 times on CI, as upstream.</summary>
    public static int Retries { get; } = CI ? 3 : 0;

    /// <summary>The timeout of a test in milliseconds. <c>PWTEST_TIMEOUT=0</c> turns it off, for debugging.</summary>
    public static int Timeout { get; } = int.TryParse(Environment.GetEnvironmentVariable("PWTEST_TIMEOUT"), out var timeout)
        ? timeout
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.X64 ? 60_000 : 30_000;

    /// <summary><c>PWTEST_LOG</c>: a file that gets a line when a test starts and when it ends.</summary>
    public static string LogFile { get; } = NullIfEmpty(Environment.GetEnvironmentVariable("PWTEST_LOG"));

    /// <summary>The timeout of <c>expect</c>, <c>expect: { timeout: 10000 }</c> upstream.</summary>
    public const float ExpectTimeout = 10_000;

    /// <summary>'win32', 'darwin' or 'linux', as <c>process.platform</c>.</summary>
    public static string Platform { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win32"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" : "linux";

    public static bool IsWindows => Platform == "win32";

    public static bool IsMac => Platform == "darwin";

    public static bool IsLinux => Platform == "linux";

    /// <summary>The major version of macOS, 0 on the other platforms.</summary>
    public static int MacVersion { get; } = ReadMacVersion();

    /// <summary>The root of the repository.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary><c>.upstream/tests</c>: the upstream tests at <c>DriverVersion</c>, with the assets and the snapshots.</summary>
    public static string UpstreamTestsDir { get; } = Path.Combine(RepoRoot, ".upstream", "tests");

    public static string AssetsDir { get; } = Path.Combine(UpstreamTestsDir, "assets");

    /// <summary>The directory of the outputs of the tests, <c>test-results</c> upstream.</summary>
    public static string OutputDir { get; } = Path.Combine(AppContext.BaseDirectory, "test-results");

    /// <summary><c>PWTEST_SERVER_REGISTRY</c> of the driver.</summary>
    public static string ServerRegistry { get; } = Path.Combine(OutputDir, $"registry-{Environment.ProcessId}");

    private static string ReadBrowserName()
    {
        var name = Environment.GetEnvironmentVariable("BROWSER")?.ToLowerInvariant();
        // GitHub Codespaces and DevContainers set BROWSER to a path.
        return name is "chromium" or "firefox" or "webkit" ? name : "chromium";
    }

    private static int ReadMacVersion()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return 0;
        }
        return Environment.OSVersion.Version.Major;
    }

    private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

    private static string FindRepoRoot([CallerFilePath] string sourceFile = null)
    {
        // src/Playwright.Tests/Fixtures/Env.cs
        var fromSource = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile) ?? ".", "..", "..", ".."));
        if (File.Exists(Path.Combine(fromSource, "src", "Common", "Version.props")))
        {
            return fromSource;
        }
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Common", "Version.props")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("Could not find the root of the repository, with src/Common/Version.props");
    }
}
