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
using System.Diagnostics;
using System.Formats.Tar;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using CommandLine;

namespace Playwright.Tooling;

/// <summary>
/// Builds the Playwright driver into src/Playwright/.drivers. The driver is the
/// playwright-core package of microsoft/playwright at the commit in DriverVersion
/// (src/Common/Version.props), plus the official Node.js binaries of the latest
/// release of DriverNodeMajorVersion. This:
///   1. clones microsoft/playwright into .upstream/ (gitignored) the first time,
///      and checks out the commit;
///   2. builds it with `npm ci` and `npm run build` (once per commit);
///   3. packs packages/playwright-core with `npm pack`, and adds the Node.js
///      binary of each platform.
/// The resulting directory layout matches the .playwright folder shipped in the
/// NuGet package:
///   .drivers/package/**                  - playwright-core npm package contents
///   .drivers/node/LICENSE                - Node.js license
///   .drivers/node/&lt;platform&gt;/node[.exe]  - per-platform Node.js binary.
/// A driver that is built is not built again until DriverVersion changes or
/// Node.js DriverNodeMajorVersion gets a new release. Do not commit, branch or
/// edit in .upstream/: the driver is built only from a checkout with no local changes.
/// </summary>
internal class DriverBuilder
{
    private const string UpstreamUrl = "https://github.com/microsoft/playwright.git";

    private const string NodeIndexUrl = "https://nodejs.org/dist/index.json";

    private static readonly Regex _commitRegex = new("^[0-9a-f]{40}$", RegexOptions.Compiled);

    private static readonly Regex _nodeReleaseRegex = new(@"^v([0-9]+\.[0-9]+\.[0-9]+)$", RegexOptions.Compiled);

    // Directory names under .drivers/node/ are the platform ids that Driver.cs
    // resolves at runtime; the suffixes name the Node.js download for that platform,
    // and the index files name it in the "files" of a release in the Node.js index.
    private static readonly (string PlatformId, string NodeSuffix, string NodeIndexFile)[] _platforms = new[]
    {
            ("darwin-x64", "darwin-x64", "osx-x64-tar"),
            ("darwin-arm64", "darwin-arm64", "osx-arm64-tar"),
            ("linux-x64", "linux-x64", "linux-x64"),
            ("linux-arm64", "linux-arm64", "linux-arm64"),
            ("win32_x64", "win-x64", "win-x64-zip"),
    };

    public string BasePath { get; set; }

    /// <summary>The microsoft/playwright commit that the driver is built from.</summary>
    public string DriverVersion { get; set; }

    /// <summary>The Node.js major version that the driver bundles the latest release of.</summary>
    public int NodeMajorVersion { get; set; }

    private string NodeVersion { get; set; }

    private string UpstreamPath => Path.Combine(BasePath, ".upstream");

    internal static Task RunAsync(BuildDriverOptions o)
    {
        string propsPath = Path.Combine(o.BasePath, "src", "Common", "Version.props");
        var props = new XmlDocument();
        props.Load(propsPath);
        string driverVersion = props.DocumentElement.SelectSingleNode("/Project/PropertyGroup/DriverVersion").FirstChild.Value.Trim();
        string nodeMajorVersion = props.DocumentElement.SelectSingleNode("/Project/PropertyGroup/DriverNodeMajorVersion").FirstChild.Value.Trim();
        if (!_commitRegex.IsMatch(driverVersion))
        {
            throw new Exception($"DriverVersion in {propsPath} must be a microsoft/playwright commit (40 hex digits), got \"{driverVersion}\"");
        }
        if (!int.TryParse(nodeMajorVersion, NumberStyles.None, CultureInfo.InvariantCulture, out int nodeMajor))
        {
            throw new Exception($"DriverNodeMajorVersion in {propsPath} must be a Node.js major version such as 26, got \"{nodeMajorVersion}\"");
        }

        return new DriverBuilder()
        {
            BasePath = Path.GetFullPath(o.BasePath),
            DriverVersion = driverVersion,
            NodeMajorVersion = nodeMajor,
        }.ExecuteAsync();
    }

    private static async Task ExtractEntryAsync(TarEntry entry, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
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

    private static void ExtractZipEntry(ZipArchive zip, string entryName, string destination)
    {
        var entry = zip.GetEntry(entryName) ?? throw new Exception($"Could not find {entryName} in the archive");
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        using var output = File.Create(destination);
        using var input = entry.Open();
        input.CopyTo(output);
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

    private static string ResolveNpmExecutable()
    {
        string name = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
        // On Windows npm.cmd must be started by its full path: launched by bare name,
        // the script's %~dp0 expands to the working directory instead of its own
        // directory and it fails to find node_modules/npm/bin/npm-cli.js.
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory.Trim('"'), name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return name;
    }

    private static async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string workingDirectory, IEnumerable<string> arguments, bool captureOutput = false, IDictionary<string, string> environment = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = captureOutput,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[name] = value;
        }
        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new Exception($"Failed to start {fileName}");
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new Exception($"{fileName} was not found on PATH; git, Node.js and npm are required to build the driver.", e);
        }
        using (process)
        {
            string output = captureOutput ? await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false) : string.Empty;
            await process.WaitForExitAsync().ConfigureAwait(false);
            return (process.ExitCode, output.Trim());
        }
    }

    private async Task<string> GitAsync(params string[] arguments)
    {
        var (exitCode, output) = await RunProcessAsync("git", UpstreamPath, arguments, captureOutput: true).ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new Exception($"git {string.Join(' ', arguments)} failed with exit code {exitCode}");
        }
        return output;
    }

    private async Task<bool> HasCommitAsync(string commit)
    {
        var (exitCode, _) = await RunProcessAsync("git", UpstreamPath, new[] { "rev-parse", "--verify", "--quiet", commit + "^{commit}" }, captureOutput: true).ConfigureAwait(false);
        return exitCode == 0;
    }

    private async Task<bool> HasLocalChangesAsync()
        => (await GitAsync("status", "--porcelain").ConfigureAwait(false)).Length > 0;

    private async Task RunNpmAsync(string workingDirectory, params string[] arguments)
    {
        var environment = new Dictionary<string, string>();
        string npmrc = Path.Combine(BasePath, ".npmrc");
        if (File.Exists(npmrc))
        {
            // npm does not run in the repository root, so name the root-level .npmrc:
            // the release pipeline writes the registry and its credentials there, to
            // point npm at the internal Azure Artifacts feed.
            environment["npm_config_userconfig"] = npmrc;
        }
        Console.WriteLine($"Running npm {string.Join(' ', arguments)} in {workingDirectory}");
        var (exitCode, _) = await RunProcessAsync(ResolveNpmExecutable(), workingDirectory, arguments, environment: environment).ConfigureAwait(false);
        if (exitCode != 0)
        {
            throw new Exception($"npm {string.Join(' ', arguments)} failed with exit code {exitCode}");
        }
    }

    private async Task<bool> ExecuteAsync()
    {
        await CheckoutUpstreamAsync().ConfigureAwait(false);

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36");

        NodeVersion = await ResolveNodeVersionAsync(client).ConfigureAwait(false);

        var driversDirectory = new DirectoryInfo(Path.Combine(BasePath, "src", "Playwright", ".drivers"));
        string stamp = $"driver {DriverVersion} node {NodeVersion}";
        string stampFile = Path.Combine(driversDirectory.FullName, ".stamp");

        if (File.Exists(stampFile) && File.ReadAllText(stampFile) == stamp)
        {
            Console.WriteLine("The driver is up-to-date");
        }
        else
        {
            if (driversDirectory.Exists)
            {
                driversDirectory.Delete(true);
            }

            driversDirectory.Create();

            var tasks = new List<Task>
            {
                BuildPlaywrightPackageAsync(driversDirectory.FullName),
            };
            foreach (var (platformId, nodeSuffix, _) in _platforms)
            {
                tasks.Add(DownloadNodeAsync(client, driversDirectory.FullName, platformId, nodeSuffix));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            File.WriteAllText(stampFile, stamp);
        }

        // update readme
        await UpdateBrowserVersionsAsync().ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Checks out DriverVersion in .upstream/, after cloning microsoft/playwright there the first time.
    /// </summary>
    private async Task CheckoutUpstreamAsync()
    {
        bool cloned = false;
        if (!Directory.Exists(Path.Combine(UpstreamPath, ".git")))
        {
            Console.WriteLine($"Cloning {UpstreamUrl} into {UpstreamPath}");
            var (exitCode, _) = await RunProcessAsync("git", BasePath, new[] { "clone", "--no-checkout", UpstreamUrl, UpstreamPath }).ConfigureAwait(false);
            if (exitCode != 0)
            {
                throw new Exception($"git clone {UpstreamUrl} failed with exit code {exitCode}");
            }
            cloned = true;
        }
        if (cloned || await GitAsync("rev-parse", "HEAD").ConfigureAwait(false) != DriverVersion)
        {
            if (!cloned && await HasLocalChangesAsync().ConfigureAwait(false))
            {
                throw new Exception($"{UpstreamPath} has local changes. Discard them before checking out {DriverVersion}.");
            }
            if (!await HasCommitAsync(DriverVersion).ConfigureAwait(false))
            {
                await GitAsync("fetch", "--quiet", "--tags", "origin").ConfigureAwait(false);
            }
            if (!await HasCommitAsync(DriverVersion).ConfigureAwait(false))
            {
                // A commit that no branch or tag reaches any more, for example on a deleted branch.
                await GitAsync("fetch", "--quiet", "origin", DriverVersion).ConfigureAwait(false);
            }
            await GitAsync("checkout", "--quiet", "--detach", DriverVersion).ConfigureAwait(false);
        }
        Console.WriteLine($"{UpstreamPath} is at {DriverVersion}");
    }

    /// <summary>
    /// Builds .upstream/ and extracts its playwright-core package into .drivers/package.
    /// </summary>
    private async Task BuildPlaywrightPackageAsync(string driversDirectory)
    {
        if (await HasLocalChangesAsync().ConfigureAwait(false))
        {
            throw new Exception($"{UpstreamPath} has local changes. The driver is stamped with the commit, so build it from a clean checkout.");
        }

        string coreDirectory = Path.Combine(UpstreamPath, "packages", "playwright-core");
        // npm ci removes node_modules, and the stamp with it.
        string buildStamp = Path.Combine(UpstreamPath, "node_modules", ".playwright-dotnet-build");
        bool built = File.Exists(buildStamp)
            && File.ReadAllText(buildStamp).Trim() == DriverVersion
            && Directory.Exists(Path.Combine(coreDirectory, "lib"));
        if (!built)
        {
            await RunNpmAsync(UpstreamPath, "ci", "--no-audit", "--no-fund").ConfigureAwait(false);
            await RunNpmAsync(UpstreamPath, "run", "build").ConfigureAwait(false);
            File.WriteAllText(buildStamp, DriverVersion + "\n");
        }

        string tempDirectory = Directory.CreateTempSubdirectory("playwright-core-").FullName;
        try
        {
            await RunNpmAsync(coreDirectory, "pack", "--pack-destination", tempDirectory).ConfigureAwait(false);
            string[] tgzs = Directory.GetFiles(tempDirectory, "playwright-core-*.tgz");
            if (tgzs.Length != 1)
            {
                throw new Exception($"npm pack did not produce one playwright-core-*.tgz in {tempDirectory}");
            }
            string tgz = tgzs.Single();

            using var stream = File.OpenRead(tgz);
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var tar = new TarReader(gzip);
            bool extractedAnything = false;
            while (await tar.GetNextEntryAsync().ConfigureAwait(false) is { } entry)
            {
                // npm tarballs nest all package contents under a top-level "package/" directory,
                // which conveniently matches the layout we want under .drivers/.
                if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) || !entry.Name.StartsWith("package/", StringComparison.Ordinal))
                {
                    continue;
                }
                await ExtractEntryAsync(entry, Path.Combine(driversDirectory, entry.Name)).ConfigureAwait(false);
                extractedAnything = true;
            }
            if (!extractedAnything)
            {
                throw new Exception($"No files were extracted from {tgz}");
            }
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }

        // Upstream copies these into the package before it publishes it
        // (LICENCE_FILES in utils/workspace.js).
        foreach (string name in new[] { "NOTICE", "LICENSE" })
        {
            File.Copy(Path.Combine(UpstreamPath, name), Path.Combine(driversDirectory, "package", name), overwrite: true);
        }
        Console.WriteLine($"Built playwright-core at {DriverVersion}");
    }

    /// <summary>
    /// Finds the latest release of Node.js NodeMajorVersion that has the binaries of all platforms.
    /// </summary>
    private async Task<string> ResolveNodeVersionAsync(HttpClient client)
    {
        Version latest = null;
        await WithRetriesAsync(NodeIndexUrl, async () =>
        {
            using var stream = await GetStreamAsync(client, NodeIndexUrl).ConfigureAwait(false);
            using var index = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            latest = index.RootElement.EnumerateArray()
                .Where(release => _platforms.All(platform => release.GetProperty("files").EnumerateArray().Any(file => file.GetString() == platform.NodeIndexFile)))
                .Select(release => _nodeReleaseRegex.Match(release.GetProperty("version").GetString()))
                .Where(match => match.Success)
                .Select(match => Version.Parse(match.Groups[1].Value))
                .Where(version => version.Major == NodeMajorVersion)
                .Max();
        }).ConfigureAwait(false);
        if (latest == null)
        {
            throw new Exception($"{NodeIndexUrl} has no Node.js {NodeMajorVersion} release with the binaries of all platforms");
        }
        Console.WriteLine($"The latest Node.js {NodeMajorVersion} release is {latest}");
        return latest.ToString();
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
                ExtractZipEntry(zip, nodeEntry, nodeDestination);
                extractedNode = true;
                if (!extractedLicense)
                {
                    ExtractZipEntry(zip, $"{archiveDir}/LICENSE", licenseDestination);
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
                        await ExtractEntryAsync(entry, nodeDestination).ConfigureAwait(false);
                        extractedNode = true;
                    }
                    else if (!extractedLicense && entry.Name == $"{archiveDir}/LICENSE")
                    {
                        await ExtractEntryAsync(entry, licenseDestination).ConfigureAwait(false);
                        extractedLicense = true;
                    }
                }
            }

            if (!extractedNode || !extractedLicense)
            {
                throw new Exception($"Could not find the Node.js binary or LICENSE in {url}");
            }
        }).ConfigureAwait(false);
        Console.WriteLine($"Extracted Node.js {NodeVersion} for {platformId}");
    }

    private async Task UpdateBrowserVersionsAsync()
    {
        try
        {
            string readmePath = Path.Combine(BasePath, "README.md");

            var regex = new Regex("<!-- GEN:(.*?) -->(.*?)<!-- GEN:stop -->", RegexOptions.Compiled);

            var readme = File.ReadAllText(Path.Combine(UpstreamPath, "README.md"));
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
            string newReadmeText = ReplaceBrowserVersion(readmeText, browserMatches);
            if (newReadmeText != readmeText)
            {
                await File.WriteAllTextAsync(readmePath, newReadmeText).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARNING: Could not update the browser versions in the README file.");
            Console.WriteLine(e.Message);
            Console.ResetColor();
        }
    }
}

[Verb("build-driver")]
internal class BuildDriverOptions
{
    [Option(Required = true, HelpText = "Solution path.")]
    public string BasePath { get; set; }
}
