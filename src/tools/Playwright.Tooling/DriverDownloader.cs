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

using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CommandLine;

[assembly: InternalsVisibleTo("Microsoft.Playwright.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]

namespace Playwright.Tooling;

/// <summary>
/// Assembles the Playwright driver into src/Playwright/.drivers from the
/// playwright-core npm package and the official Node.js builds, mirroring what
/// upstream's utils/build/build-playwright-driver.sh bundles into the prebuilt
/// driver archives. The resulting directory layout matches the .playwright
/// folder shipped in the NuGet package:
///   .drivers/package/**                  - playwright-core npm package contents
///   .drivers/node/LICENSE                - Node.js license
///   .drivers/node/&lt;platform&gt;/node[.exe]  - per-platform Node.js binary.
/// </summary>
internal class DriverDownloader
{
    // Directory names under .drivers/node/ are the platform ids that Driver.cs
    // resolves at runtime; the suffixes name the Node.js download for that platform.
    private static readonly (string PlatformId, string NodeSuffix)[] _platforms = new[]
    {
            ("darwin-x64", "darwin-x64"),
            ("darwin-arm64", "darwin-arm64"),
            ("linux-x64", "linux-x64"),
            ("linux-arm64", "linux-arm64"),
            ("win32_x64", "win-x64"),
    };

    public string BasePath { get; set; }

    public string DriverVersion { get; set; }

    public string NodeVersion { get; set; }

    internal static Task RunAsync(DownloadDriversOptions o)
    {
        var props = new XmlDocument();
        props.Load(Path.Combine(o.BasePath, "src", "Common", "Version.props"));
        string driverVersion = props.DocumentElement.SelectSingleNode("/Project/PropertyGroup/DriverVersion").FirstChild.Value;
        string nodeVersion = props.DocumentElement.SelectSingleNode("/Project/PropertyGroup/DriverNodeVersion").FirstChild.Value;

        return new DriverDownloader()
        {
            BasePath = o.BasePath,
            DriverVersion = driverVersion,
            NodeVersion = nodeVersion,
        }.ExecuteAsync();
    }

    private static async Task ExtractEntryAsync(TarEntry entry, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        // Write the file instead of TarEntry.ExtractToFile to give it a current
        // modification time. npm publishes tarballs with an mtime far in the past,
        // which breaks "newer file wins" update checks down the line, see
        // https://github.com/microsoft/playwright-dotnet/issues/2069.
        using (var output = File.Create(destination))
        {
            if (entry.DataStream != null)
            {
                await entry.DataStream.CopyToAsync(output).ConfigureAwait(false);
            }
        }
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(destination, entry.Mode);
        }
    }

    private static string GetSafeArchiveDestination(string destinationRoot, string entryName)
    {
        var normalizedEntryName = entryName.Replace('\\', '/');
        if (Path.IsPathRooted(normalizedEntryName))
        {
            throw new InvalidDataException($"Archive entry '{entryName}' is rooted.");
        }

        foreach (var segment in normalizedEntryName.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == ".." || segment.Contains(':'))
            {
                throw new InvalidDataException($"Archive entry '{entryName}' contains an invalid path segment.");
            }
        }

        var root = Path.GetFullPath(destinationRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            root += Path.DirectorySeparatorChar;
        }

        var destination = Path.GetFullPath(Path.Combine(root, normalizedEntryName.Replace('/', Path.DirectorySeparatorChar)));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!destination.StartsWith(root, comparison))
        {
            throw new InvalidDataException($"Archive entry '{entryName}' escapes the destination directory.");
        }

        return destination;
    }

    private static void ExtractZipEntry(ZipArchive zip, string entryName, string destination)
    {
        var entry = zip.GetEntry(entryName) ?? throw new Exception($"Could not find {entryName} in the archive");
        if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || string.IsNullOrEmpty(entry.Name))
        {
            throw new InvalidDataException($"Archive entry '{entryName}' is not a regular file.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var output = File.Create(destination);
        using var input = entry.Open();
        input.CopyTo(output);
    }

    private static string CreateStagingDirectory(string driversDirectory, string purpose)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(driversDirectory)) ?? Path.GetTempPath();
        var staging = Path.Combine(parent, $".drivers-{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        return staging;
    }

    private static void MoveFileIntoPlace(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            File.Delete(destination);
        }

        File.Move(source, destination);
    }

    internal static async Task ExtractPlaywrightPackageAsync(Stream stream, string driversDirectory)
    {
        var stagingRoot = CreateStagingDirectory(driversDirectory, "package");
        var extractedDestinations = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        try
        {
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var tar = new TarReader(gzip);
            bool extractedAnything = false;
            while (await tar.GetNextEntryAsync().ConfigureAwait(false) is { } entry)
            {
                // npm tarballs nest all package contents under a top-level "package/" directory,
                // which conveniently matches the layout we want under .drivers/.
                var normalizedEntryName = entry.Name.Replace('\\', '/');
                if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) || !normalizedEntryName.StartsWith("package/", StringComparison.Ordinal))
                {
                    continue;
                }

                var destination = GetSafeArchiveDestination(stagingRoot, normalizedEntryName);
                var normalizedDestination = Path.GetFullPath(destination);
                if (!extractedDestinations.Add(normalizedDestination))
                {
                    throw new InvalidDataException($"Duplicate archive entry destination: {entry.Name}");
                }

                await ExtractEntryAsync(entry, destination).ConfigureAwait(false);
                extractedAnything = true;
            }

            if (!extractedAnything)
            {
                throw new Exception("No files were extracted from the playwright-core archive.");
            }

            var stagingPackage = Path.Combine(stagingRoot, "package");
            if (!Directory.Exists(stagingPackage))
            {
                throw new Exception("The playwright-core archive did not contain a package directory.");
            }

            Directory.CreateDirectory(driversDirectory);
            var destinationPackage = Path.Combine(driversDirectory, "package");
            if (Directory.Exists(destinationPackage))
            {
                Directory.Delete(destinationPackage, recursive: true);
            }

            Directory.Move(stagingPackage, destinationPackage);
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    private static async Task<Stream> GetStreamAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.Dispose();
            throw new Exception($"Failed to download {url} with status {response.StatusCode} and content {content}.");
        }
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    private static async Task WithRetriesAsync(string url, Func<Task> action)
    {
        const int maxAttempts = 3;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                Console.WriteLine($"Downloading {url}");
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                Console.WriteLine($"Attempt {attempt} to download {url} failed: {ex.Message}, retrying...");
                await Task.Delay(TimeSpan.FromSeconds(attempt)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to download {url}", ex);
            }
        }
    }

    private async Task<bool> ExecuteAsync()
    {
        var driversDirectory = new DirectoryInfo(Path.Combine(BasePath, "src", "Playwright", ".drivers"));
        string stamp = $"driver {DriverVersion} node {NodeVersion}";
        string stampFile = Path.Combine(driversDirectory.FullName, ".stamp");

        if (File.Exists(stampFile) && File.ReadAllText(stampFile) == stamp)
        {
            Console.WriteLine("Drivers are up-to-date");
        }
        else
        {
            if (driversDirectory.Exists)
            {
                driversDirectory.Delete(true);
            }

            driversDirectory.Create();

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36");

            var tasks = new List<Task>
            {
                DownloadPlaywrightPackageAsync(client, driversDirectory.FullName),
            };
            foreach (var (platformId, nodeSuffix) in _platforms)
            {
                tasks.Add(DownloadNodeAsync(client, driversDirectory.FullName, platformId, nodeSuffix));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            File.WriteAllText(stampFile, stamp);
        }

        // update readme
        await UpdateBrowserVersionsAsync(BasePath, DriverVersion).ConfigureAwait(false);

        return true;
    }

    private async Task DownloadPlaywrightPackageAsync(HttpClient client, string driversDirectory)
    {
        string url = $"https://registry.npmjs.org/playwright-core/-/playwright-core-{DriverVersion}.tgz";
        await WithRetriesAsync(url, async () =>
        {
            using var stream = await GetStreamAsync(client, url).ConfigureAwait(false);
            await ExtractPlaywrightPackageAsync(stream, driversDirectory).ConfigureAwait(false);
        }).ConfigureAwait(false);
        Console.WriteLine($"Extracted playwright-core {DriverVersion}");
    }

    private async Task DownloadNodeAsync(HttpClient client, string driversDirectory, string platformId, string nodeSuffix)
    {
        bool isWindows = nodeSuffix.StartsWith("win-", StringComparison.Ordinal);
        string archiveDir = $"node-v{NodeVersion}-{nodeSuffix}";
        string url = $"https://nodejs.org/dist/v{NodeVersion}/{archiveDir}.{(isWindows ? "zip" : "tar.gz")}";
        string nodeEntry = isWindows ? $"{archiveDir}/node.exe" : $"{archiveDir}/bin/node";
        string nodeDestination = Path.Combine(driversDirectory, "node", platformId, isWindows ? "node.exe" : "node");
        // Every Node.js archive carries the same LICENSE; extract it from a single
        // designated platform to avoid parallel writes to the same file.
        string licenseDestination = platformId == "linux-x64" ? Path.Combine(driversDirectory, "node", "LICENSE") : null;

        await WithRetriesAsync(url, async () =>
        {
            bool extractedNode = false;
            bool extractedLicense = licenseDestination == null;
            var stagingRoot = CreateStagingDirectory(driversDirectory, platformId);
            var stagedNodeDestination = Path.Combine(stagingRoot, "node", platformId, isWindows ? "node.exe" : "node");
            var stagedLicenseDestination = licenseDestination == null ? null : Path.Combine(stagingRoot, "node", "LICENSE");

            try
            {
                if (isWindows)
                {
                    // ZipArchive needs a seekable stream, so buffer the archive in memory.
                    using var buffer = new MemoryStream();
                    using (var stream = await GetStreamAsync(client, url).ConfigureAwait(false))
                    {
                        await stream.CopyToAsync(buffer).ConfigureAwait(false);
                    }
                    buffer.Position = 0;
                    using var zip = new ZipArchive(buffer);
                    ExtractZipEntry(zip, nodeEntry, stagedNodeDestination);
                    extractedNode = true;
                    if (!extractedLicense)
                    {
                        ExtractZipEntry(zip, $"{archiveDir}/LICENSE", stagedLicenseDestination);
                        extractedLicense = true;
                    }
                }
                else
                {
                    using var stream = await GetStreamAsync(client, url).ConfigureAwait(false);
                    using var gzip = new GZipStream(stream, CompressionMode.Decompress);
                    using var tar = new TarReader(gzip);
                    while ((!extractedNode || !extractedLicense) && await tar.GetNextEntryAsync().ConfigureAwait(false) is { } entry)
                    {
                        if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
                        {
                            continue;
                        }
                        if (entry.Name == nodeEntry)
                        {
                            await ExtractEntryAsync(entry, stagedNodeDestination).ConfigureAwait(false);
                            extractedNode = true;
                        }
                        else if (!extractedLicense && entry.Name == $"{archiveDir}/LICENSE")
                        {
                            await ExtractEntryAsync(entry, stagedLicenseDestination).ConfigureAwait(false);
                            extractedLicense = true;
                        }
                    }
                }

                if (!extractedNode || !extractedLicense)
                {
                    throw new Exception($"Could not find the Node.js binary or LICENSE in {url}");
                }

                MoveFileIntoPlace(stagedNodeDestination, nodeDestination);
                if (licenseDestination != null)
                {
                    MoveFileIntoPlace(stagedLicenseDestination, licenseDestination);
                }
            }
            finally
            {
                if (Directory.Exists(stagingRoot))
                {
                    Directory.Delete(stagingRoot, recursive: true);
                }
            }
        }).ConfigureAwait(false);
        Console.WriteLine($"Extracted Node.js {NodeVersion} for {platformId}");
    }

    private async Task UpdateBrowserVersionsAsync(string basePath, string driverVersion)
    {
        try
        {
            string readmePath = Path.Combine(basePath, "README.md");
            string playwrightVersion = driverVersion.Contains("-") ? driverVersion.Substring(0, driverVersion.IndexOf("-")) : driverVersion;

            var regex = new Regex("<!-- GEN:(.*?) -->(.*?)<!-- GEN:stop -->", RegexOptions.Compiled);

            var basePlaywrightDir = Environment.GetEnvironmentVariable("PW_SRC_DIR") ?? Path.Combine(Environment.CurrentDirectory, "..", "playwright");
            var readme = File.ReadAllText(Path.Combine(basePlaywrightDir, "README.md"));
            static string ReplaceBrowserVersion(string content, MatchCollection browserMatches)
            {
                foreach (Match match in browserMatches)
                {
                    content = new Regex($"<!-- GEN:{match.Groups[1].Value} -->.*?<!-- GEN:stop -->")
                        .Replace(content, $"<!-- GEN:{match.Groups[1].Value} -->{match.Groups[2].Value}<!-- GEN:stop -->");
                }

                return content;
            }

            var browserMatches = regex.Matches(readme);
            string readmeText = await File.ReadAllTextAsync(readmePath).ConfigureAwait(false);
            await File.WriteAllTextAsync(readmePath, ReplaceBrowserVersion(readmeText, browserMatches)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: Could not update the browser versions in the README file.");
            Console.WriteLine($"This is usually due to the readme file not yet existing for {driverVersion}.");
            Console.WriteLine(e.Message);
        }
    }
}

[Verb("download-drivers")]
internal class DownloadDriversOptions
{
    [Option(Required = true, HelpText = "Solution path.")]
    public string BasePath { get; set; }
}
