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
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using DriverDownloader.Linux;
using Playwright.Tooling.Options;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Playwright.Tooling;

[Description("Downloads the playwright drivers for all supported platforms.")]
internal class DriverDownloader : AsyncCommand<DownloadDriversOptions>
{
    private static readonly string[] _platforms = new[]
    {
            "mac",
            "mac-arm64",
            "linux",
            "linux-arm64",
            "win32_x64",
    };

    private readonly IAnsiConsole _console;

    public DriverDownloader(IAnsiConsole console)
    {
        _console = console;
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
            _console.WriteLine("WARNING: Could not update the browser versions in the README file.", Color.Yellow);
            _console.WriteLine($"This is usually due to the readme file not yet existing for {driverVersion}.", Color.Yellow);
            _console.WriteLine(e.Message, Color.Yellow);
        }
    }

    private async Task DownloadDriverAsync(DirectoryInfo destinationDirectory, string driverVersion, string platform)
    {
        string cdn = "https://playwright.azureedge.net/builds/driver";
        if (
            driverVersion.Contains("-alpha")
            || driverVersion.Contains("-beta")
            || driverVersion.Contains("-next"))
        {
            cdn += "/next";
        }

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        // Azure seems to not like requests without a user agent.
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36");
        string url = $"{cdn}/playwright-{driverVersion}-{platform}.zip";

        try
        {
            var response = await client.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception($"Failed to download driver from {url} with status {response.StatusCode} and content {content}.");
            }

            var directory = new DirectoryInfo(Path.Combine(destinationDirectory.FullName, platform));

            if (directory.Exists)
            {
                directory.Delete(true);
            }

            new ZipArchive(await response.Content.ReadAsStreamAsync().ConfigureAwait(false)).ExtractToDirectory(directory.FullName);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var executables = new[] { "playwright.sh", "node", "package/third_party/ffmpeg/ffmpeg-linux", "package/third_party/ffmpeg/ffmpeg-mac" }
                    .Select(f => Path.Combine(directory.FullName, f));

                foreach (string executable in executables)
                {
                    if (new FileInfo(executable).Exists && LinuxSysCall.Chmod(executable, LinuxSysCall.ExecutableFilePermissions) != 0)
                    {
                        throw new($"Unable to chmod {executable} ({Marshal.GetLastWin32Error()})");
                    }
                }
            }

            _console.WriteLine($"Driver for {platform} downloaded");
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Unable to download driver for {driverVersion} using url {url}");
            throw new($"Unable to download driver for {driverVersion} using url {url}", ex);
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DownloadDriversOptions options)
    {
        var versionProps = Path.GetFullPath(Path.Combine(options.BasePath, "src", "Common", "Version.props"));
        var document = XDocument.Load(versionProps);
        var driverVersion = document.XPathSelectElement("/Project/PropertyGroup/DriverVersion")?.Value ?? throw new($"The DriverVersion element was not found in {versionProps}");

        var destinationDirectory = new DirectoryInfo(Path.Combine(options.BasePath, "src", "Playwright", ".drivers"));
        var dedupeDirectory = new DirectoryInfo(Path.Combine(options.BasePath, "src", "Playwright", ".playwright"));

        var versionFile = new FileInfo(Path.Combine(destinationDirectory.FullName, driverVersion));

        if (!versionFile.Exists)
        {
            if (destinationDirectory.Exists)
            {
                Directory.Delete(destinationDirectory.FullName, true);
            }

            if (dedupeDirectory.Exists)
            {
                Directory.Delete(dedupeDirectory.FullName, true);
            }

            destinationDirectory.Create();

            var tasks = new List<Task>();

            foreach (var platform in _platforms)
            {
                tasks.Add(DownloadDriverAsync(destinationDirectory, driverVersion, platform));
            }

            await _console.Status().StartAsync($"Downloading drivers for {string.Join(", ", _platforms)}", _ => Task.WhenAll(tasks)).ConfigureAwait(false);
            versionFile.CreateText();
        }
        else
        {
            _console.WriteLine("Drivers are up-to-date");
        }

        // update readme
        await UpdateBrowserVersionsAsync(options.BasePath, driverVersion).ConfigureAwait(false);

        return 0;
    }
}
