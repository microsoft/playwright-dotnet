using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
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

    internal static string ValidatePathSegment(string value, string purpose)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"Path segment for {purpose} must not be null or empty.", nameof(value));
        }

        if (value == "." || value == ".." ||
            value.Contains(Path.DirectorySeparatorChar) ||
            value.Contains(Path.AltDirectorySeparatorChar) ||
            value.Contains('\0') ||
            (value.Length >= 2 && value[1] == ':') ||
            value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new PlaywrightException($"Invalid path segment for {purpose}: {value}");
        }

        return value;
    }

    internal static Uri ValidateHttpsUri(string url, string purpose)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(uri.Host))
        {
            throw new PlaywrightException($"Invalid HTTPS URL for {purpose}: {url}");
        }

        return uri;
    }

    internal static void ExtractZipToDirectorySafely(string archivePath, string destination, bool overwriteFiles)
    {
        var destinationRoot = Path.GetFullPath(destination);
        Directory.CreateDirectory(destinationRoot);

        if (!destinationRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            destinationRoot += Path.DirectorySeparatorChar;
        }

        using var archive = ZipFile.OpenRead(archivePath);
        var entries = new List<ValidatedZipEntry>(archive.Entries.Count);
        foreach (var entry in archive.Entries)
        {
            var entryName = entry.FullName.Replace('\\', '/');
            if (string.IsNullOrEmpty(entryName))
            {
                continue;
            }

            ValidateArchiveEntryName(entryName, "ZIP");
            if (IsZipEntrySymlink(entry))
            {
                throw new InvalidDataException($"Refusing to extract symbolic link from ZIP archive: {entry.FullName}");
            }

            var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, entryName.Replace('/', Path.DirectorySeparatorChar)));
            if (!destinationPath.StartsWith(destinationRoot, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                throw new InvalidDataException($"ZIP entry escapes destination directory: {entry.FullName}");
            }

            entries.Add(new(
                entry,
                entry.FullName,
                destinationPath,
                NormalizeExtractionPathForComparison(destinationPath),
                entryName.EndsWith("/", StringComparison.Ordinal)));
        }

        ValidateZipExtractionPlan(destinationRoot, entries, overwriteFiles);

        foreach (var entry in entries)
        {
            if (entry.IsDirectory)
            {
                Directory.CreateDirectory(entry.DestinationPath);
                continue;
            }

            var directory = Path.GetDirectoryName(entry.DestinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            entry.Entry.ExtractToFile(entry.DestinationPath, overwriteFiles);
        }
    }

    private static void ValidateZipExtractionPlan(string destinationRoot, IReadOnlyList<ValidatedZipEntry> entries, bool overwriteFiles)
    {
        var comparer = GetPathComparer();
        var destinationRootPath = NormalizeExtractionPathForComparison(destinationRoot);
        var filePaths = new HashSet<string>(comparer);
        var directoryPaths = new HashSet<string>(comparer);

        foreach (var entry in entries)
        {
            if (entry.IsDirectory)
            {
                directoryPaths.Add(entry.NormalizedDestinationPath);
                continue;
            }

            if (!filePaths.Add(entry.NormalizedDestinationPath) && !overwriteFiles)
            {
                throw new InvalidDataException($"Duplicate ZIP entry destination: {entry.ArchiveName}");
            }
        }

        foreach (var entry in entries)
        {
            if (entry.IsDirectory)
            {
                if (filePaths.Contains(entry.NormalizedDestinationPath) || File.Exists(entry.NormalizedDestinationPath))
                {
                    throw new InvalidDataException($"ZIP directory entry conflicts with a file: {entry.ArchiveName}");
                }
            }
            else
            {
                if (directoryPaths.Contains(entry.NormalizedDestinationPath) || Directory.Exists(entry.NormalizedDestinationPath))
                {
                    throw new InvalidDataException($"ZIP file entry conflicts with a directory: {entry.ArchiveName}");
                }

                if (!overwriteFiles && File.Exists(entry.NormalizedDestinationPath))
                {
                    throw new InvalidDataException($"ZIP entry would overwrite an existing file: {entry.ArchiveName}");
                }
            }

            var parent = Path.GetDirectoryName(entry.NormalizedDestinationPath);
            while (!string.IsNullOrEmpty(parent))
            {
                parent = NormalizeExtractionPathForComparison(parent);
                if (comparer.Equals(parent, destinationRootPath))
                {
                    break;
                }

                if (filePaths.Contains(parent) || File.Exists(parent))
                {
                    throw new InvalidDataException($"ZIP entry parent path conflicts with a file: {entry.ArchiveName}");
                }

                parent = Path.GetDirectoryName(parent);
            }
        }
    }

    private static string NormalizeExtractionPathForComparison(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static StringComparer GetPathComparer()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal static void ValidateArchiveEntryName(string entryName, string archiveKind)
    {
        var normalizedEntryName = entryName.Replace('\\', '/');
        if (string.IsNullOrEmpty(normalizedEntryName) ||
            normalizedEntryName.StartsWith("/", StringComparison.Ordinal) ||
            normalizedEntryName.Contains('\0') ||
            (normalizedEntryName.Length >= 2 && normalizedEntryName[1] == ':'))
        {
            throw new InvalidDataException($"Unsafe {archiveKind} entry path: {entryName}");
        }

        var segments = normalizedEntryName.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0)
            {
                if (i == segments.Length - 1 && normalizedEntryName.EndsWith("/", StringComparison.Ordinal))
                {
                    continue;
                }

                throw new InvalidDataException($"Unsafe {archiveKind} entry path: {entryName}");
            }

            if (segment == "." || segment == ".." || segment.Contains(':') || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidDataException($"Unsafe {archiveKind} entry path: {entryName}");
            }
        }
    }

    internal static void ValidateTarVerboseEntryType(string verboseListingLine, string archiveKind)
    {
        if (string.IsNullOrEmpty(verboseListingLine))
        {
            throw new InvalidDataException($"Unsafe {archiveKind} entry: empty verbose listing line.");
        }

        var type = verboseListingLine[0];
        if (type != '-' && type != 'd')
        {
            throw new InvalidDataException($"Unsafe {archiveKind} entry type '{type}' in: {verboseListingLine}");
        }
    }

    private static bool IsZipEntrySymlink(ZipArchiveEntry entry)
    {
        const int UnixFileTypeMask = 0xF000;
        const int UnixSymlinkType = 0xA000;
        return ((entry.ExternalAttributes >> 16) & UnixFileTypeMask) == UnixSymlinkType;
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

        if (trimmed.Contains(' ') || trimmed.Contains('"') || trimmed.Contains('\'') || trimmed.IndexOfAny(new[] { '\r', '\n', '\t', '\0' }) >= 0)
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

            if (!string.IsNullOrEmpty(uri.UserInfo) ||
                (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/") ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new PlaywrightException($"Invalid proxy server: only scheme, host, and port are allowed in {proxyServer}");
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
        var safeToolName = ValidatePathSegment(toolName, "tool name");
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
            var fullPath = Path.Combine(basePath, safeToolName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        var (exitCode, stdout, _) = RunProcessWhich(safeToolName);
        if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
        {
            var resolved = ResolveToolPathFromSearchOutput(stdout);
            if (!string.IsNullOrEmpty(resolved))
            {
                return resolved;
            }
        }

        throw new PlaywrightException($"Required tool '{safeToolName}' not found. Install it and ensure it is available via PATH.");
    }

    internal static string? ResolveToolPathFromSearchOutput(string stdout)
    {
        foreach (var line in stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = line.Trim();
            if (candidate.Length == 0 || candidate.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                continue;
            }

            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static (int ExitCode, string Stdout, string Stderr) RunProcessWhich(string toolName)
    {
        try
        {
            var whichCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var psi = new ProcessStartInfo(whichCmd)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add(toolName);

            using var process = Process.Start(psi) ?? throw new PlaywrightException($"Could not start {whichCmd}.");
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stderr.AppendLine(e.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit(3000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                process.WaitForExit();
                return (-1, stdout.ToString(), stderr.ToString());
            }

            process.WaitForExit();
            return (process.ExitCode, stdout.ToString(), stderr.ToString());
        }
        catch
        {
            return (-1, string.Empty, string.Empty);
        }
    }

    private readonly struct ValidatedZipEntry
    {
        internal ValidatedZipEntry(
            ZipArchiveEntry entry,
            string archiveName,
            string destinationPath,
            string normalizedDestinationPath,
            bool isDirectory)
        {
            Entry = entry;
            ArchiveName = archiveName;
            DestinationPath = destinationPath;
            NormalizedDestinationPath = normalizedDestinationPath;
            IsDirectory = isDirectory;
        }

        internal ZipArchiveEntry Entry { get; }

        internal string ArchiveName { get; }

        internal string DestinationPath { get; }

        internal string NormalizedDestinationPath { get; }

        internal bool IsDirectory { get; }
    }
}
