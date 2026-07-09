using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Helpers;

internal static class SecurityHelpers
{
    private static readonly string[] _dangerousChromiumFlags = new string[]
    {
        "--disable-web-security",
        "--disable-security",
        "--allow-file-access-from-files",
        "--allow-running-insecure-content",
        "--reduce-security-for-testing",
        "--unsafely-treat-insecure-origin-as-secure",
        "--explicitly-allowed-ports",
        "--disable-features",
        "--enable-features",
    };

    internal static string ResolveAndValidatePath(string path, string purpose)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException($"Path for {purpose} must not be null or empty.", nameof(path));
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new PlaywrightException($"Invalid characters in path for {purpose}: {path}");
        }

        var fullPath = Path.GetFullPath(path);
        var fileName = Path.GetFileName(fullPath);

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new PlaywrightException($"Invalid characters in file name for {purpose}: {fileName}");
        }

        return fullPath;
    }

    internal static void ValidateStorageStatePath(string path)
    {
        ResolveAndValidatePath(path, "storage state");
    }

    internal static string[] FilterChromiumArgs(string[]? userArgs)
    {
        if (userArgs == null || userArgs.Length == 0)
        {
            return Array.Empty<string>();
        }

        var filtered = new List<string>(userArgs.Length);
        foreach (var arg in userArgs)
        {
            if (string.IsNullOrEmpty(arg))
            {
                continue;
            }

            bool isDangerous = false;
            foreach (var flag in _dangerousChromiumFlags)
            {
                if (arg.StartsWith(flag, StringComparison.OrdinalIgnoreCase))
                {
                    isDangerous = true;
                    break;
                }
            }

            if (isDangerous)
            {
                Console.Error.WriteLine($"[clearcote] WARNING: Blocked dangerous Chromium flag: {arg.Split('=')[0]}");
                continue;
            }

            filtered.Add(arg);
        }
        return filtered.ToArray();
    }

    internal static string ValidateProxyServer(string proxyServer)
    {
        if (string.IsNullOrEmpty(proxyServer))
        {
            return string.Empty;
        }

        var trimmed = proxyServer.Trim();

        if (trimmed.Contains(' ') || trimmed.Contains('"') || trimmed.Contains('\''))
        {
            throw new PlaywrightException($"Invalid proxy server format: {trimmed}");
        }

        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("socks4://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "http://" + trimmed;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (string.IsNullOrEmpty(uri.Host))
            {
                throw new PlaywrightException($"Invalid proxy server: no host in {proxyServer}");
            }
            return trimmed;
        }

        throw new PlaywrightException($"Invalid proxy server format: {proxyServer}");
    }

    internal static Regex CreateSafeRegex(string pattern, RegexOptions options = RegexOptions.None, TimeSpan? timeout = null)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException("Regex pattern must not be null or empty.", nameof(pattern));
        }

        if (pattern.Length > 1000)
        {
            throw new PlaywrightException("Regex pattern too long (max 1000 characters).");
        }

        timeout ??= TimeSpan.FromSeconds(1);

        return new Regex(pattern, options | RegexOptions.None, timeout.Value);
    }

    internal static void SetSecureDirectoryPermissions(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Directory.CreateDirectory(path);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var dirInfo = new DirectoryInfo(path);
                    if (dirInfo.Exists)
                    {
                        File.SetUnixFileMode(
                            path,
                            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    }
                }
            }
            catch
            {
            }
        }
        else
        {
            Directory.CreateDirectory(path);
        }
    }

    internal static string GetAbsoluteToolPath(string toolName)
    {
        string[] commonPaths;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            commonPaths = new string[] { "C:\\Windows\\System32\\", "C:\\Program Files\\Git\\usr\\bin\\" };
        }
        else
        {
            commonPaths = new string[] { "/usr/bin/", "/usr/local/bin/" };
        }

        foreach (var basePath in commonPaths)
        {
            var fullPath = Path.Combine(basePath, toolName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        var (exitCode, stdout, _) = RunProcessWhich(toolName);
        if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
        {
            var resolved = stdout.TrimEnd('\r', '\n');
            if (File.Exists(resolved))
            {
                return resolved;
            }
        }

        throw new PlaywrightException($"Required tool '{toolName}' not found. Install it and ensure it is available via PATH.");
    }

    private static (int ExitCode, string Stdout, string Stderr) RunProcessWhich(string toolName)
    {
        try
        {
            var whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var psi = new ProcessStartInfo(whichCmd, toolName)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(psi) ?? throw new PlaywrightException($"Could not start {whichCmd}.");
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(3000);
            return (process.ExitCode, stdout, stderr);
        }
        catch
        {
            return (-1, string.Empty, string.Empty);
        }
    }
}
