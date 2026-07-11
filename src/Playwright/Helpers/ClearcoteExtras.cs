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
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Converters;

#pragma warning disable SA1201
#pragma warning disable SA1203
#pragma warning disable SA1501
#pragma warning disable RCS1007

namespace Microsoft.Playwright.Helpers;

internal static partial class Clearcote
{
    private const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1";
    private const string WidevineAppId = "oimompecagnajdejgnnjijobebaeigek";
    private const string OmahaUrl = "https://update.googleapis.com/service/update2/json";
    private const string WidevineHintFile = "latest-component-updated-widevine-cdm";
    private const string MmdbUrl = "https://github.com/daijro/geoip-all-in-one/releases/latest/download/geoip-aio-all.mmdb.zip";
    private const int MmdbMaxAgeDays = 30;

    private static readonly Regex _safeProfileName = new("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.CultureInvariant);
    private static readonly string[] _ipEchoUrls = { "https://api.ipify.org", "https://checkip.amazonaws.com" };
    private static ClearcoteMmdbReader? _mmdbReader;
    private static string? _mmdbReaderPath;
    private static Task<string?>? _mmdbInflight;

    internal static string ProfileDirectory
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("CLEARCOTE_PROFILE_DIR");
            return string.IsNullOrEmpty(env)
                ? ResolveDirectoryRoot(Path.Combine(HomeDirectory(), ".clearcote", "profiles"), "Clearcote profile directory")
                : ResolveEnvironmentDirectoryRoot(env, "CLEARCOTE_PROFILE_DIR");
        }
    }

    internal static string ProfilePath(string nameOrPath)
    {
        if (nameOrPath.Contains(Path.DirectorySeparatorChar)
            || nameOrPath.Contains(Path.AltDirectorySeparatorChar)
            || nameOrPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return SecurityHelpers.ResolveAndValidatePath(nameOrPath, "Clearcote profile path");
        }

        if (!_safeProfileName.IsMatch(nameOrPath))
        {
            throw new PlaywrightException($"invalid profile name '{nameOrPath}' - use [A-Za-z0-9._-] or pass an explicit path");
        }

        return SecurityHelpers.ResolveAndValidatePath(Path.Combine(ProfileDirectory, nameOrPath + ".json"), "Clearcote profile path");
    }

    internal static string SaveProfile(ClearcoteProfile profile, string? path)
    {
        var dest = SecurityHelpers.ResolveAndValidatePath(path ?? profile.Path, "Clearcote profile path");
        var parentDirectory = Path.GetDirectoryName(dest);
        if (!string.IsNullOrEmpty(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }
        using var stream = File.Create(dest);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteString("name", profile.Name);
        writer.WritePropertyName("options");
        WriteProfileOptions(writer, profile.Options);
        writer.WriteEndObject();
        writer.Flush();
        TryChmod(dest, "600");
        if (!string.IsNullOrEmpty(parentDirectory))
        {
            TryChmod(parentDirectory, "700");
        }
        return dest;
    }

    internal static ClearcoteProfile LoadProfile(string nameOrPath)
    {
        var path = ProfilePath(nameOrPath);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString() ?? baseName
            : baseName;
        var options = new ClearcoteLaunchPersistentContextOptions();
        if (root.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Object)
        {
            ReadProfileOptions(optionsElement, options);
        }

        return new ClearcoteProfile(name, options);
    }

    internal static IReadOnlyList<string> ListProfiles()
    {
        if (!Directory.Exists(ProfileDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(ProfileDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(static name => !string.IsNullOrEmpty(name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray()!;
    }

    internal static Task<IBrowser> LaunchProfileAsync(
        IBrowserType chromium,
        ClearcoteProfile profile,
        ClearcoteLaunchOptions? overrides)
    {
        var options = ToLaunchOptions(profile.Options);
        if (overrides != null)
        {
            MergeInto(options, overrides, overrideExisting: true);
        }

        return chromium.LaunchAsync(options);
    }

    internal static Task<IBrowserContext> LaunchPersistentProfileAsync(
        IBrowserType chromium,
        string userDataDir,
        ClearcoteProfile profile,
        ClearcoteLaunchPersistentContextOptions? overrides)
    {
        var options = new ClearcoteLaunchPersistentContextOptions(profile.Options);
        if (overrides != null)
        {
            MergeInto(options, overrides, overrideExisting: true);
        }

        return chromium.LaunchPersistentContextAsync(userDataDir, options);
    }

    internal static async Task<IBrowserContext> LaunchAgentAsync(IBrowserType chromium, ClearcoteLaunchAgentOptions? options)
    {
        options ??= new();
        var userDataDir = options.UserDataDir;
        if (string.IsNullOrEmpty(userDataDir))
        {
            userDataDir = Path.Combine(Path.GetTempPath(), "clearcote-agent-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(userDataDir);
        }

        return await chromium.LaunchPersistentContextAsync(userDataDir, options).ConfigureAwait(false);
    }

    internal static async Task<ClearcoteAgentTaskResult> RunAgentTaskAsync(
        IPage page,
        string goal,
        ClearcoteAgentTaskOptions? options)
    {
        var browser = page.Context.Browser
            ?? throw new PlaywrightException("RunClearcoteAgentTaskAsync: page is not attached to a Browser");
        var targetSession = await page.Context.NewCDPSessionAsync(page).ConfigureAwait(false);
        var targetInfo = await targetSession.SendAsync("Target.getTargetInfo").ConfigureAwait(false);
        var targetId = targetInfo?.GetProperty("targetInfo").GetProperty("targetId").GetString()
            ?? throw new PlaywrightException("RunClearcoteAgentTaskAsync: could not resolve page target id");
        var browserSession = await browser.NewBrowserCDPSessionAsync().ConfigureAwait(false);
        var args = new Dictionary<string, object>
        {
            ["targetId"] = targetId,
            ["goal"] = goal,
        };
        if (options?.MaxSteps != null)
        {
            args["maxSteps"] = options.MaxSteps.Value;
        }
        if (!string.IsNullOrEmpty(options?.Model))
        {
            args["model"] = options.Model!;
        }
        if (!string.IsNullOrEmpty(options?.PlanJson))
        {
            args["planJson"] = options.PlanJson!;
        }

        JsonElement? result;
        try
        {
            result = await browserSession.SendAsync("Browser.agentRunTask", args).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new PlaywrightException(
                "Browser.agentRunTask failed - make sure this is a Clearcote build with the AI agent and that " +
                "the browser was launched with AgentLlmKey/AgentLlmUrl set. Underlying error: " + ex.Message);
        }

        var root = result ?? default;
        var stepsJson = root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("stepsJson", out var stepsElement)
            && stepsElement.ValueKind == JsonValueKind.String
                ? stepsElement.GetString() ?? "[]"
                : "[]";
        return new()
        {
            Success = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("success", out var successElement)
                && successElement.ValueKind == JsonValueKind.True,
            FinalText = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("finalText", out var textElement)
                && textElement.ValueKind == JsonValueKind.String
                    ? textElement.GetString() ?? string.Empty
                    : string.Empty,
            StepsJson = stepsJson,
            Steps = ParseAgentSteps(stepsJson),
        };
    }

    internal static async Task<ClearcoteRenderVerdict> CheckRenderCoherenceAsync(IPage page, string? claimedGpu)
    {
        var info = await page.EvaluateAsync<JsonElement>(RenderProbeScript).ConfigureAwait(false);
        return EvaluateRenderInfo(info, claimedGpu);
    }

    private static async Task<string?> ResolveExitIpAsync(Proxy? proxy, bool quiet)
    {
        foreach (var url in _ipEchoUrls)
        {
            try
            {
                using var client = CreateGeoHttpClient(proxy);
                var body = await client.GetStringAsync(SecurityHelpers.ValidateHttpsUri(url, "GeoIP exit IP probe")).ConfigureAwait(false);
                var ip = body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (ip != null && IPAddress.TryParse(ip, out _))
                {
                    return ip;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UriFormatException)
            {
            }
        }

        Log(quiet, "geoip: could not determine the exit IP");
        return null;
    }

    private static async Task<ClearcoteGeo?> ResolveMmdbGeoAsync(string ip, bool quiet)
    {
        var file = await EnsureMmdbAsync(quiet).ConfigureAwait(false);
        if (file == null)
        {
            return null;
        }

        try
        {
            if (_mmdbReader == null || !string.Equals(_mmdbReaderPath, file, StringComparison.Ordinal))
            {
                _mmdbReader?.Dispose();
                _mmdbReader = new ClearcoteMmdbReader(file);
                _mmdbReaderPath = file;
            }

            var record = _mmdbReader.Find(IPAddress.Parse(ip));
            if (record == null)
            {
                return null;
            }

            var country = GetNestedString(record, "country", "iso_code");
            var timezone = GetNestedString(record, "location", "time_zone");
            var lat = GetNestedDouble(record, "location", "latitude");
            var lon = GetNestedDouble(record, "location", "longitude");
            if (timezone == null && lat == null)
            {
                return null;
            }

            var location = lat != null && lon != null
                ? lat.Value.ToString(CultureInfo.InvariantCulture) + "," + lon.Value.ToString(CultureInfo.InvariantCulture)
                : null;
            return new(
                Ip: ip,
                Country: country,
                Timezone: timezone,
                AcceptLanguage: AcceptLanguageForCountry(country),
                Location: location);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException or FormatException)
        {
            Log(quiet, "geoip: mmdb read failed (" + ex.Message + ")");
            return null;
        }
    }

    private static async Task<string?> EnsureMmdbAsync(bool quiet)
    {
        var directory = Path.Combine(DefaultCacheRoot(), "geoip");
        var file = Path.Combine(directory, "geoip-aio-all.mmdb");
        if (File.Exists(file))
        {
            var ageDays = (DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(file)).TotalDays;
            if (ageDays < MmdbMaxAgeDays)
            {
                return file;
            }
        }

        if (_mmdbInflight != null)
        {
            return await _mmdbInflight.ConfigureAwait(false);
        }

        _mmdbInflight = FetchMmdbAsync(directory, file, quiet);
        try
        {
            return await _mmdbInflight.ConfigureAwait(false);
        }
        finally
        {
            _mmdbInflight = null;
        }
    }

    private static async Task<string?> FetchMmdbAsync(string directory, string file, bool quiet)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var suffix = Guid.NewGuid().ToString("N");
            var zip = Path.Combine(directory, "geoip-aio-all.mmdb.zip.download-" + suffix);
            var extract = Path.Combine(directory, ".extract-" + suffix);
            var staging = Path.Combine(directory, ".geoip-aio-all.mmdb-" + suffix);

            Log(quiet, "geoip: downloading the geoip-all-in-one database (~52 MB, first run only)");
            try
            {
                using (var client = CreateHttpClient(TimeSpan.FromMinutes(5)))
                using (var response = await client.GetAsync(SecurityHelpers.ValidateHttpsUri(MmdbUrl, "GeoIP MMDB download"), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using var output = File.Create(zip);
                    await input.CopyToAsync(output).ConfigureAwait(false);
                }

                SecurityHelpers.ExtractZipToDirectorySafely(zip, extract, overwriteFiles: true);
                var found = FindMmdb(extract);
                if (found == null)
                {
                    throw new PlaywrightException("no .mmdb in archive");
                }

                File.Move(found, staging, overwrite: false);
                File.Move(staging, file, overwrite: true);
            }
            finally
            {
                TryDeleteFile(staging);
                TryDeleteFile(zip);
                TryDeleteDirectory(extract);
            }

            Log(quiet, "geoip: database ready");
            return file;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException or PlaywrightException)
        {
            Log(quiet, "geoip: database fetch failed (" + ex.Message + ") - falling back to HTTPS IP lookup");
            return null;
        }
    }

    private static string? GetNestedString(Dictionary<string, object> record, string section, string key)
        => record.TryGetValue(section, out var sectionValue)
        && sectionValue is Dictionary<string, object> map
        && map.TryGetValue(key, out var value)
            ? value as string
            : null;

    private static string? FindMmdb(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(current))
            {
                if (Directory.Exists(entry))
                {
                    pending.Push(entry);
                }
                else if (entry.EndsWith(".mmdb", StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
        }

        return null;
    }

    private static double? GetNestedDouble(Dictionary<string, object> record, string section, string key)
    {
        if (!record.TryGetValue(section, out var sectionValue)
            || sectionValue is not Dictionary<string, object> map
            || !map.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            double d => d,
            float f => f,
            decimal d => (double)d,
            int i => i,
            long l => l,
            _ => null,
        };
    }

    internal static async Task<string> FetchWidevineAsync(ClearcoteWidevineOptions? options)
    {
        options ??= new();
        var update = ParseWidevineUpdate(await PostOmahaAsync().ConfigureAwait(false));
        var root = options.Dest
            ?? Environment.GetEnvironmentVariable("CLEARCOTE_WIDEVINE_DIR")
            ?? Path.Combine(HomeDirectory(), ".clearcote", "WidevineCdm");
        var version = SecurityHelpers.ValidatePathSegment(string.IsNullOrEmpty(update.Version) ? "current" : update.Version, "Widevine version");
        var versionDir = Path.Combine(root, version);
        var platform = WidevinePlatform();
        var libraryPath = Path.Combine(versionDir, "_platform_specific", platform.Subdir, platform.FileName);
        if (File.Exists(libraryPath) && File.Exists(Path.Combine(versionDir, "manifest.json")))
        {
            WidevineLog(options.Quiet == true, "already present: " + versionDir);
            return versionDir;
        }

        WidevineLog(options.Quiet == true, "fetching CDM " + version);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        using var request = new HttpRequestMessage(HttpMethod.Get, SecurityHelpers.ValidateHttpsUri(update.Url, "Widevine CDM download"));
        request.Headers.UserAgent.ParseAdd(WidevineUserAgent());
        using var response = await client.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var blob = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(update.Sha256))
        {
            throw new PlaywrightException("Widevine update response had no sha256 - refusing to install an unverified CDM");
        }

        var actual = Convert.ToHexString(SHA256.HashData(blob)).ToLowerInvariant();
        if (!string.Equals(actual, update.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new PlaywrightException("Widevine CDM sha256 mismatch - refusing to install");
        }

        Directory.CreateDirectory(versionDir);
        var temp = Path.Combine(Path.GetTempPath(), "cc-wv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var zipPath = Path.Combine(temp, "cdm.zip");
            await File.WriteAllBytesAsync(zipPath, Crx3ToZip(blob)).ConfigureAwait(false);
            SecurityHelpers.ExtractZipToDirectorySafely(zipPath, versionDir, overwriteFiles: true);
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch { }
        }

        if (!File.Exists(libraryPath))
        {
            throw new PlaywrightException($"extracted CDM but {platform.FileName} not at {libraryPath}");
        }

        WidevineLog(options.Quiet == true, "installed: " + versionDir);
        return versionDir;
    }

    internal static async Task<string> SeedWidevineAsync(string userDataDir, ClearcoteWidevineOptions? options)
    {
        options ??= new();
        var source = await FetchWidevineAsync(options).ConfigureAwait(false);
        var version = Path.GetFileName(source);
        var root = Path.Combine(SecurityHelpers.ResolveAndValidatePath(userDataDir, "Widevine user data directory"), "WidevineCdm");
        var target = Path.Combine(root, version);
        var platform = WidevinePlatform();
        if (!File.Exists(Path.Combine(target, "_platform_specific", platform.Subdir, platform.FileName)))
        {
            Directory.CreateDirectory(root);
            if (Directory.Exists(target))
            {
                throw new PlaywrightException($"existing Widevine CDM directory is incomplete: {target}");
            }

            var staging = Path.Combine(root, "." + version + ".seed-" + Guid.NewGuid().ToString("N"));
            try
            {
                CopyDirectorySafely(source, staging);
                Directory.Move(staging, target);
            }
            finally
            {
                TryDeleteDirectory(staging);
            }
        }

        try
        {
            var hint = Path.Combine(root, WidevineHintFile);
            var stagingHint = Path.Combine(root, "." + WidevineHintFile + "-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(stagingHint, "{\"Path\":\"" + JsonEscape(target) + "\"}");
                File.Move(stagingHint, hint, overwrite: true);
            }
            finally
            {
                TryDeleteFile(stagingHint);
            }
        }
        catch
        {
        }

        WidevineLog(options.Quiet == true, "seeded into " + root);
        return target;
    }

    internal static (IEnumerable<string>? IgnoreDefaultArgs, IEnumerable<string> Args) ApplyWidevineArgs(
        IEnumerable<string>? ignoreDefaultArgs,
        bool? ignoreAllDefaultArgs,
        IEnumerable<string>? userArgs)
    {
        var args = userArgs?.ToArray() ?? Array.Empty<string>();
        IEnumerable<string>? newIgnoreDefaultArgs = ignoreDefaultArgs;
        if (ignoreDefaultArgs != null)
        {
            var ignore = ignoreDefaultArgs.ToArray();
            newIgnoreDefaultArgs = ignore.Contains("--disable-component-update", StringComparer.Ordinal)
                ? ignore
                : ignore.Concat(new[] { "--disable-component-update" }).ToArray();
        }
        else if (ignoreAllDefaultArgs != true)
        {
            newIgnoreDefaultArgs = new[] { "--enable-automation", "--disable-component-update" };
        }

        if (!OperatingSystem.IsLinux() && !args.Any(static a => a.Contains("component-updater", StringComparison.Ordinal)))
        {
            args = args.Concat(new[] { "--component-updater=fast-update" }).ToArray();
        }

        return (newIgnoreDefaultArgs, args);
    }

    private static IReadOnlyList<ClearcoteAgentStep> ParseAgentSteps(string stepsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<ClearcoteAgentStep>();
            }

            var steps = new List<ClearcoteAgentStep>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                steps.Add(new()
                {
                    Action = item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("action", out var action)
                        && action.ValueKind == JsonValueKind.String
                            ? action.GetString()
                            : null,
                    Status = item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("status", out var status)
                        && status.ValueKind == JsonValueKind.String
                            ? status.GetString()
                            : null,
                    Raw = item.Clone(),
                });
            }

            return steps;
        }
        catch
        {
            return Array.Empty<ClearcoteAgentStep>();
        }
    }

    private static void WriteProfileOptions(Utf8JsonWriter writer, ClearcoteLaunchPersistentContextOptions options)
    {
        writer.WriteStartObject();
        WriteString(writer, "fingerprint", options.Fingerprint);
        WriteString(writer, "platform", options.ClearcotePlatform?.ToString().ToLowerInvariant());
        WriteString(writer, "platformVersion", options.PlatformVersion);
        WriteString(writer, "brand", options.Brand);
        WriteString(writer, "brandVersion", options.BrandVersion);
        WriteString(writer, "gpuVendor", options.GpuVendor);
        WriteString(writer, "gpuRenderer", options.GpuRenderer);
        WriteNumber(writer, "hardwareConcurrency", options.HardwareConcurrency);
        WriteString(writer, "location", options.Location);
        WriteString(writer, "timezone", options.Timezone);
        WriteString(writer, "acceptLanguage", options.AcceptLanguage);
        WriteString(writer, "webrtcIp", options.WebrtcIp);
        WriteString(writer, "tlsProfile", options.TlsProfile?.ToString());
        WriteString(writer, "tlsProfileCustom", options.TlsProfileCustom);
        WriteBool(writer, "disableGpuFingerprint", options.DisableGpuFingerprint);
        WriteBool(writer, "fingerprintNoise", options.FingerprintNoise);
        WriteString(writer, "fingerprintProfile", options.FingerprintProfile);
        WriteNumber(writer, "storageQuota", options.StorageQuota);
        WriteCanvasBridge(writer, options.CanvasBridge);
        WriteBool(writer, "disablePrivacySandbox", options.DisablePrivacySandbox);
        WriteStrings(writer, "extensions", options.Extensions);
        WriteBool(writer, "geoip", options.Geoip);
        WriteString(writer, "cacheDir", options.CacheDir);
        WriteBool(writer, "quiet", options.Quiet);
        WriteBool(writer, "autoUpdate", options.AutoUpdate);
        WriteBool(writer, "humanize", options.Humanize);
        WriteBool(writer, "showCursor", options.ShowCursor);
        WriteString(writer, "agentLlmUrl", options.AgentLlmUrl);
        WriteString(writer, "agentLlmKey", options.AgentLlmKey);
        WriteString(writer, "agentModel", options.AgentModel);
        WriteString(writer, "agentToolMode", options.AgentToolMode);
        WriteString(writer, "agentTyping", options.AgentTyping);
        WriteBool(writer, "widevine", options.Widevine);
        WriteBool(writer, "headless", options.Headless);
        WriteString(writer, "executablePath", options.ExecutablePath);
        WriteString(writer, "channel", options.Channel);
        WriteStrings(writer, "args", options.Args);
        WriteString(writer, "artifactsDir", options.ArtifactsDir);
        WriteBool(writer, "chromiumSandbox", options.ChromiumSandbox);
        WriteString(writer, "downloadsPath", options.DownloadsPath);
        WriteStringPairs(writer, "env", options.Env);
        WriteObjectPairs(writer, "firefoxUserPrefs", options.FirefoxUserPrefs);
        WriteBool(writer, "handleSIGHUP", options.HandleSIGHUP);
        WriteBool(writer, "handleSIGINT", options.HandleSIGINT);
        WriteBool(writer, "handleSIGTERM", options.HandleSIGTERM);
        WriteBool(writer, "ignoreAllDefaultArgs", options.IgnoreAllDefaultArgs);
        WriteStrings(writer, "ignoreDefaultArgs", options.IgnoreDefaultArgs);
        WriteNumber(writer, "slowMo", options.SlowMo);
        WriteNumber(writer, "timeout", options.Timeout);
        WriteString(writer, "tracesDir", options.TracesDir);
        WriteBool(writer, "acceptDownloads", options.AcceptDownloads);
        WriteString(writer, "baseURL", options.BaseURL);
        WriteBool(writer, "bypassCSP", options.BypassCSP);
        WriteEnum(writer, "colorScheme", options.ColorScheme);
        WriteEnum(writer, "contrast", options.Contrast);
        WriteNumber(writer, "deviceScaleFactor", options.DeviceScaleFactor);
        WriteStringPairs(writer, "extraHTTPHeaders", options.ExtraHTTPHeaders);
        WriteEnum(writer, "forcedColors", options.ForcedColors);
        WriteGeolocation(writer, "geolocation", options.Geolocation);
        WriteBool(writer, "hasTouch", options.HasTouch);
        WriteHttpCredentials(writer, "httpCredentials", options.HttpCredentials);
        WriteBool(writer, "ignoreHTTPSErrors", options.IgnoreHTTPSErrors);
        WriteBool(writer, "isMobile", options.IsMobile);
        WriteBool(writer, "javaScriptEnabled", options.JavaScriptEnabled);
        WriteString(writer, "locale", options.Locale);
        WriteBool(writer, "offline", options.Offline);
        WriteStrings(writer, "permissions", options.Permissions);
        WriteProxy(writer, "proxy", options.Proxy);
        WriteEnum(writer, "reducedMotion", options.ReducedMotion);
        WriteScreenSize(writer, "screenSize", options.ScreenSize);
        WriteEnum(writer, "serviceWorkers", options.ServiceWorkers);
        WriteBool(writer, "strictSelectors", options.StrictSelectors);
        WriteString(writer, "timezoneId", options.TimezoneId);
        WriteString(writer, "userAgent", options.UserAgent);
        WriteViewportSize(writer, "viewportSize", options.ViewportSize);
        writer.WriteEndObject();
    }

    private static void ReadProfileOptions(JsonElement options, ClearcoteLaunchPersistentContextOptions target)
    {
        foreach (var property in options.EnumerateObject())
        {
            var key = ToCamel(property.Name);
            var value = property.Value;
            switch (key)
            {
                case "fingerprint": target.Fingerprint = ReadString(value); break;
                case "platform":
                case "clearcotePlatform": target.ClearcotePlatform = ReadEnum<ClearcotePlatform>(value); break;
                case "platformVersion": target.PlatformVersion = ReadString(value); break;
                case "brand": target.Brand = ReadString(value); break;
                case "brandVersion": target.BrandVersion = ReadString(value); break;
                case "gpuVendor": target.GpuVendor = ReadString(value); break;
                case "gpuRenderer": target.GpuRenderer = ReadString(value); break;
                case "hardwareConcurrency": target.HardwareConcurrency = ReadInt(value); break;
                case "location": target.Location = ReadString(value); break;
                case "timezone": target.Timezone = ReadString(value); break;
                case "acceptLanguage": target.AcceptLanguage = ReadString(value); break;
                case "webrtcIp": target.WebrtcIp = ReadString(value); break;
                case "tlsProfile": target.TlsProfile = ReadEnum<ClearcoteTlsProfile>(value); break;
                case "tlsProfileCustom": target.TlsProfileCustom = ReadString(value); break;
                case "disableGpuFingerprint": target.DisableGpuFingerprint = ReadBool(value); break;
                case "fingerprintNoise": target.FingerprintNoise = ReadBool(value); break;
                case "fingerprintProfile": target.FingerprintProfile = ReadString(value); break;
                case "storageQuota": target.StorageQuota = ReadLong(value); break;
                case "canvasBridge": target.CanvasBridge = ReadCanvasBridge(value); break;
                case "disablePrivacySandbox": target.DisablePrivacySandbox = ReadBool(value); break;
                case "extensions": target.Extensions = ReadStrings(value); break;
                case "geoip": target.Geoip = ReadBool(value); break;
                case "cacheDir": target.CacheDir = ReadString(value); break;
                case "quiet": target.Quiet = ReadBool(value); break;
                case "autoUpdate": target.AutoUpdate = ReadBool(value); break;
                case "humanize": target.Humanize = ReadBool(value); break;
                case "showCursor": target.ShowCursor = ReadBool(value); break;
                case "agentLlmUrl": target.AgentLlmUrl = ReadString(value); break;
                case "agentLlmKey": target.AgentLlmKey = ReadString(value); break;
                case "agentModel": target.AgentModel = ReadString(value); break;
                case "agentToolMode": target.AgentToolMode = ReadString(value); break;
                case "agentTyping": target.AgentTyping = ReadString(value); break;
                case "widevine": target.Widevine = ReadBool(value); break;
                case "headless": target.Headless = ReadBool(value); break;
                case "executablePath": target.ExecutablePath = ReadString(value); break;
                case "channel": target.Channel = ReadString(value); break;
                case "args": target.Args = ReadStrings(value); break;
                case "artifactsDir": target.ArtifactsDir = ReadString(value); break;
                case "chromiumSandbox": target.ChromiumSandbox = ReadBool(value); break;
                case "downloadsPath": target.DownloadsPath = ReadString(value); break;
                case "env": target.Env = ReadStringPairs(value); break;
                case "firefoxUserPrefs": target.FirefoxUserPrefs = ReadObjectPairs(value); break;
                case "handleSIGHUP": target.HandleSIGHUP = ReadBool(value); break;
                case "handleSIGINT": target.HandleSIGINT = ReadBool(value); break;
                case "handleSIGTERM": target.HandleSIGTERM = ReadBool(value); break;
                case "ignoreAllDefaultArgs": target.IgnoreAllDefaultArgs = ReadBool(value); break;
                case "ignoreDefaultArgs": target.IgnoreDefaultArgs = ReadStrings(value); break;
                case "slowMo": target.SlowMo = ReadFloat(value); break;
                case "timeout": target.Timeout = ReadFloat(value); break;
                case "tracesDir": target.TracesDir = ReadString(value); break;
                case "acceptDownloads": target.AcceptDownloads = ReadBool(value); break;
                case "baseURL": target.BaseURL = ReadString(value); break;
                case "bypassCSP": target.BypassCSP = ReadBool(value); break;
                case "colorScheme": target.ColorScheme = ReadEnum<ColorScheme>(value); break;
                case "contrast": target.Contrast = ReadEnum<Contrast>(value); break;
                case "deviceScaleFactor": target.DeviceScaleFactor = ReadFloat(value); break;
                case "extraHTTPHeaders": target.ExtraHTTPHeaders = ReadStringPairs(value); break;
                case "forcedColors": target.ForcedColors = ReadEnum<ForcedColors>(value); break;
                case "geolocation": target.Geolocation = ReadGeolocation(value); break;
                case "hasTouch": target.HasTouch = ReadBool(value); break;
                case "httpCredentials": target.HttpCredentials = ReadHttpCredentials(value); break;
                case "ignoreHTTPSErrors": target.IgnoreHTTPSErrors = ReadBool(value); break;
                case "isMobile": target.IsMobile = ReadBool(value); break;
                case "javaScriptEnabled": target.JavaScriptEnabled = ReadBool(value); break;
                case "locale": target.Locale = ReadString(value); break;
                case "offline": target.Offline = ReadBool(value); break;
                case "permissions": target.Permissions = ReadStrings(value); break;
                case "proxy": target.Proxy = ReadProxy(value); break;
                case "reducedMotion": target.ReducedMotion = ReadEnum<ReducedMotion>(value); break;
                case "screenSize": target.ScreenSize = ReadScreenSize(value); break;
                case "serviceWorkers": target.ServiceWorkers = ReadEnum<ServiceWorkerPolicy>(value); break;
                case "strictSelectors": target.StrictSelectors = ReadBool(value); break;
                case "timezoneId": target.TimezoneId = ReadString(value); break;
                case "userAgent": target.UserAgent = ReadString(value); break;
                case "viewportSize": target.ViewportSize = ReadViewportSize(value); break;
            }
        }
    }

    internal static void MergeInto(
        ClearcoteLaunchPersistentContextOptions target,
        ClearcoteLaunchPersistentContextOptions source,
        bool overrideExisting)
    {
        target.Fingerprint = Pick(target.Fingerprint, source.Fingerprint, overrideExisting);
        target.ClearcotePlatform = Pick(target.ClearcotePlatform, source.ClearcotePlatform, overrideExisting);
        target.PlatformVersion = Pick(target.PlatformVersion, source.PlatformVersion, overrideExisting);
        target.Brand = Pick(target.Brand, source.Brand, overrideExisting);
        target.BrandVersion = Pick(target.BrandVersion, source.BrandVersion, overrideExisting);
        target.GpuVendor = Pick(target.GpuVendor, source.GpuVendor, overrideExisting);
        target.GpuRenderer = Pick(target.GpuRenderer, source.GpuRenderer, overrideExisting);
        target.HardwareConcurrency = Pick(target.HardwareConcurrency, source.HardwareConcurrency, overrideExisting);
        target.Location = Pick(target.Location, source.Location, overrideExisting);
        target.Timezone = Pick(target.Timezone, source.Timezone, overrideExisting);
        target.AcceptLanguage = Pick(target.AcceptLanguage, source.AcceptLanguage, overrideExisting);
        target.WebrtcIp = Pick(target.WebrtcIp, source.WebrtcIp, overrideExisting);
        target.TlsProfile = Pick(target.TlsProfile, source.TlsProfile, overrideExisting);
        target.TlsProfileCustom = Pick(target.TlsProfileCustom, source.TlsProfileCustom, overrideExisting);
        target.DisableGpuFingerprint = Pick(target.DisableGpuFingerprint, source.DisableGpuFingerprint, overrideExisting);
        target.FingerprintNoise = Pick(target.FingerprintNoise, source.FingerprintNoise, overrideExisting);
        target.FingerprintProfile = Pick(target.FingerprintProfile, source.FingerprintProfile, overrideExisting);
        target.StorageQuota = Pick(target.StorageQuota, source.StorageQuota, overrideExisting);
        target.CanvasBridge = Pick(target.CanvasBridge, source.CanvasBridge, overrideExisting);
        target.DisablePrivacySandbox = Pick(target.DisablePrivacySandbox, source.DisablePrivacySandbox, overrideExisting);
        target.Extensions = Pick(target.Extensions, source.Extensions, overrideExisting);
        target.Geoip = Pick(target.Geoip, source.Geoip, overrideExisting);
        target.CacheDir = Pick(target.CacheDir, source.CacheDir, overrideExisting);
        target.Quiet = Pick(target.Quiet, source.Quiet, overrideExisting);
        target.AutoUpdate = Pick(target.AutoUpdate, source.AutoUpdate, overrideExisting);
        target.Profile = Pick(target.Profile, source.Profile, overrideExisting);
        target.Humanize = Pick(target.Humanize, source.Humanize, overrideExisting);
        target.ShowCursor = Pick(target.ShowCursor, source.ShowCursor, overrideExisting);
        target.AgentLlmUrl = Pick(target.AgentLlmUrl, source.AgentLlmUrl, overrideExisting);
        target.AgentLlmKey = Pick(target.AgentLlmKey, source.AgentLlmKey, overrideExisting);
        target.AgentModel = Pick(target.AgentModel, source.AgentModel, overrideExisting);
        target.AgentToolMode = Pick(target.AgentToolMode, source.AgentToolMode, overrideExisting);
        target.AgentTyping = Pick(target.AgentTyping, source.AgentTyping, overrideExisting);
        target.Widevine = Pick(target.Widevine, source.Widevine, overrideExisting);
        target.Headless = Pick(target.Headless, source.Headless, overrideExisting);
        target.ExecutablePath = Pick(target.ExecutablePath, source.ExecutablePath, overrideExisting);
        target.Channel = Pick(target.Channel, source.Channel, overrideExisting);
        target.Args = Pick(target.Args, source.Args, overrideExisting);
        target.AcceptDownloads = Pick(target.AcceptDownloads, source.AcceptDownloads, overrideExisting);
        target.BaseURL = Pick(target.BaseURL, source.BaseURL, overrideExisting);
        target.BypassCSP = Pick(target.BypassCSP, source.BypassCSP, overrideExisting);
        target.ColorScheme = Pick(target.ColorScheme, source.ColorScheme, overrideExisting);
        target.Contrast = Pick(target.Contrast, source.Contrast, overrideExisting);
        target.DeviceScaleFactor = Pick(target.DeviceScaleFactor, source.DeviceScaleFactor, overrideExisting);
        target.ExtraHTTPHeaders = Pick(target.ExtraHTTPHeaders, source.ExtraHTTPHeaders, overrideExisting);
        target.ForcedColors = Pick(target.ForcedColors, source.ForcedColors, overrideExisting);
        target.Geolocation = Pick(target.Geolocation, source.Geolocation, overrideExisting);
        target.HasTouch = Pick(target.HasTouch, source.HasTouch, overrideExisting);
        target.HttpCredentials = Pick(target.HttpCredentials, source.HttpCredentials, overrideExisting);
        target.IgnoreHTTPSErrors = Pick(target.IgnoreHTTPSErrors, source.IgnoreHTTPSErrors, overrideExisting);
        target.IsMobile = Pick(target.IsMobile, source.IsMobile, overrideExisting);
        target.JavaScriptEnabled = Pick(target.JavaScriptEnabled, source.JavaScriptEnabled, overrideExisting);
        target.Locale = Pick(target.Locale, source.Locale, overrideExisting);
        target.Offline = Pick(target.Offline, source.Offline, overrideExisting);
        target.Permissions = Pick(target.Permissions, source.Permissions, overrideExisting);
        target.Proxy = Pick(target.Proxy, source.Proxy, overrideExisting);
        target.ReducedMotion = Pick(target.ReducedMotion, source.ReducedMotion, overrideExisting);
        target.ScreenSize = Pick(target.ScreenSize, source.ScreenSize, overrideExisting);
        target.ServiceWorkers = Pick(target.ServiceWorkers, source.ServiceWorkers, overrideExisting);
        target.StrictSelectors = Pick(target.StrictSelectors, source.StrictSelectors, overrideExisting);
        target.TimezoneId = Pick(target.TimezoneId, source.TimezoneId, overrideExisting);
        target.UserAgent = Pick(target.UserAgent, source.UserAgent, overrideExisting);
        target.ViewportSize = Pick(target.ViewportSize, source.ViewportSize, overrideExisting);
    }

    internal static void MergeInto(ClearcoteLaunchOptions target, ClearcoteLaunchOptions source, bool overrideExisting)
    {
        target.Fingerprint = Pick(target.Fingerprint, source.Fingerprint, overrideExisting);
        target.ClearcotePlatform = Pick(target.ClearcotePlatform, source.ClearcotePlatform, overrideExisting);
        target.PlatformVersion = Pick(target.PlatformVersion, source.PlatformVersion, overrideExisting);
        target.Brand = Pick(target.Brand, source.Brand, overrideExisting);
        target.BrandVersion = Pick(target.BrandVersion, source.BrandVersion, overrideExisting);
        target.GpuVendor = Pick(target.GpuVendor, source.GpuVendor, overrideExisting);
        target.GpuRenderer = Pick(target.GpuRenderer, source.GpuRenderer, overrideExisting);
        target.HardwareConcurrency = Pick(target.HardwareConcurrency, source.HardwareConcurrency, overrideExisting);
        target.Location = Pick(target.Location, source.Location, overrideExisting);
        target.Timezone = Pick(target.Timezone, source.Timezone, overrideExisting);
        target.AcceptLanguage = Pick(target.AcceptLanguage, source.AcceptLanguage, overrideExisting);
        target.WebrtcIp = Pick(target.WebrtcIp, source.WebrtcIp, overrideExisting);
        target.TlsProfile = Pick(target.TlsProfile, source.TlsProfile, overrideExisting);
        target.TlsProfileCustom = Pick(target.TlsProfileCustom, source.TlsProfileCustom, overrideExisting);
        target.DisableGpuFingerprint = Pick(target.DisableGpuFingerprint, source.DisableGpuFingerprint, overrideExisting);
        target.FingerprintNoise = Pick(target.FingerprintNoise, source.FingerprintNoise, overrideExisting);
        target.FingerprintProfile = Pick(target.FingerprintProfile, source.FingerprintProfile, overrideExisting);
        target.StorageQuota = Pick(target.StorageQuota, source.StorageQuota, overrideExisting);
        target.CanvasBridge = Pick(target.CanvasBridge, source.CanvasBridge, overrideExisting);
        target.DisablePrivacySandbox = Pick(target.DisablePrivacySandbox, source.DisablePrivacySandbox, overrideExisting);
        target.Extensions = Pick(target.Extensions, source.Extensions, overrideExisting);
        target.Geoip = Pick(target.Geoip, source.Geoip, overrideExisting);
        target.CacheDir = Pick(target.CacheDir, source.CacheDir, overrideExisting);
        target.Quiet = Pick(target.Quiet, source.Quiet, overrideExisting);
        target.AutoUpdate = Pick(target.AutoUpdate, source.AutoUpdate, overrideExisting);
        target.Profile = Pick(target.Profile, source.Profile, overrideExisting);
        target.Humanize = Pick(target.Humanize, source.Humanize, overrideExisting);
        target.ShowCursor = Pick(target.ShowCursor, source.ShowCursor, overrideExisting);
        target.AgentLlmUrl = Pick(target.AgentLlmUrl, source.AgentLlmUrl, overrideExisting);
        target.AgentLlmKey = Pick(target.AgentLlmKey, source.AgentLlmKey, overrideExisting);
        target.AgentModel = Pick(target.AgentModel, source.AgentModel, overrideExisting);
        target.AgentToolMode = Pick(target.AgentToolMode, source.AgentToolMode, overrideExisting);
        target.AgentTyping = Pick(target.AgentTyping, source.AgentTyping, overrideExisting);
        MergeLaunchBaseInto(target, source, overrideExisting);
    }

    internal static ClearcoteLaunchOptions ToLaunchOptions(ClearcoteLaunchPersistentContextOptions source)
    {
        var target = new ClearcoteLaunchOptions
        {
            Fingerprint = source.Fingerprint,
            ClearcotePlatform = source.ClearcotePlatform,
            PlatformVersion = source.PlatformVersion,
            Brand = source.Brand,
            BrandVersion = source.BrandVersion,
            GpuVendor = source.GpuVendor,
            GpuRenderer = source.GpuRenderer,
            HardwareConcurrency = source.HardwareConcurrency,
            Location = source.Location,
            Timezone = source.Timezone,
            AcceptLanguage = source.AcceptLanguage,
            WebrtcIp = source.WebrtcIp,
            TlsProfile = source.TlsProfile,
            TlsProfileCustom = source.TlsProfileCustom,
            DisableGpuFingerprint = source.DisableGpuFingerprint,
            FingerprintNoise = source.FingerprintNoise,
            FingerprintProfile = source.FingerprintProfile,
            StorageQuota = source.StorageQuota,
            CanvasBridge = source.CanvasBridge,
            DisablePrivacySandbox = source.DisablePrivacySandbox,
            Extensions = source.Extensions,
            Geoip = source.Geoip,
            CacheDir = source.CacheDir,
            Quiet = source.Quiet,
            AutoUpdate = source.AutoUpdate,
            Profile = source.Profile,
            Humanize = source.Humanize,
            ShowCursor = source.ShowCursor,
            AgentLlmUrl = source.AgentLlmUrl,
            AgentLlmKey = source.AgentLlmKey,
            AgentModel = source.AgentModel,
            AgentToolMode = source.AgentToolMode,
            AgentTyping = source.AgentTyping,
            Args = source.Args,
            ArtifactsDir = source.ArtifactsDir,
            Channel = source.Channel,
            ChromiumSandbox = source.ChromiumSandbox,
            DownloadsPath = source.DownloadsPath,
            Env = source.Env,
            ExecutablePath = source.ExecutablePath,
            FirefoxUserPrefs = source.FirefoxUserPrefs,
            HandleSIGHUP = source.HandleSIGHUP,
            HandleSIGINT = source.HandleSIGINT,
            HandleSIGTERM = source.HandleSIGTERM,
            Headless = source.Headless,
            IgnoreAllDefaultArgs = source.IgnoreAllDefaultArgs,
            IgnoreDefaultArgs = source.IgnoreDefaultArgs,
            Proxy = source.Proxy,
            SlowMo = source.SlowMo,
            Timeout = source.Timeout,
            TracesDir = source.TracesDir,
        };
        return target;
    }

    private static void MergeLaunchBaseInto<TTarget, TSource>(TTarget target, TSource source, bool overrideExisting)
        where TTarget : BrowserTypeLaunchOptions
        where TSource : BrowserTypeLaunchOptions
    {
        target.Args = Pick(target.Args, source.Args, overrideExisting);
        target.ArtifactsDir = Pick(target.ArtifactsDir, source.ArtifactsDir, overrideExisting);
        target.Channel = Pick(target.Channel, source.Channel, overrideExisting);
        target.ChromiumSandbox = Pick(target.ChromiumSandbox, source.ChromiumSandbox, overrideExisting);
        target.DownloadsPath = Pick(target.DownloadsPath, source.DownloadsPath, overrideExisting);
        target.Env = Pick(target.Env, source.Env, overrideExisting);
        target.ExecutablePath = Pick(target.ExecutablePath, source.ExecutablePath, overrideExisting);
        target.FirefoxUserPrefs = Pick(target.FirefoxUserPrefs, source.FirefoxUserPrefs, overrideExisting);
        target.HandleSIGHUP = Pick(target.HandleSIGHUP, source.HandleSIGHUP, overrideExisting);
        target.HandleSIGINT = Pick(target.HandleSIGINT, source.HandleSIGINT, overrideExisting);
        target.HandleSIGTERM = Pick(target.HandleSIGTERM, source.HandleSIGTERM, overrideExisting);
        target.Headless = Pick(target.Headless, source.Headless, overrideExisting);
        target.IgnoreAllDefaultArgs = Pick(target.IgnoreAllDefaultArgs, source.IgnoreAllDefaultArgs, overrideExisting);
        target.IgnoreDefaultArgs = Pick(target.IgnoreDefaultArgs, source.IgnoreDefaultArgs, overrideExisting);
        target.Proxy = Pick(target.Proxy, source.Proxy, overrideExisting);
        target.SlowMo = Pick(target.SlowMo, source.SlowMo, overrideExisting);
        target.Timeout = Pick(target.Timeout, source.Timeout, overrideExisting);
        target.TracesDir = Pick(target.TracesDir, source.TracesDir, overrideExisting);
    }

    private static T? Pick<T>(T? target, T? source, bool overrideExisting)
        where T : class
        => source != null && (overrideExisting || target == null) ? source : target;

    private static T? Pick<T>(T? target, T? source, bool overrideExisting)
        where T : struct
        => source.HasValue && (overrideExisting || !target.HasValue) ? source : target;

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value != null) writer.WriteString(name, value);
    }

    private static void WriteBool(Utf8JsonWriter writer, string name, bool? value)
    {
        if (value.HasValue) writer.WriteBoolean(name, value.Value);
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue) writer.WriteNumber(name, value.Value);
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, long? value)
    {
        if (value.HasValue) writer.WriteNumber(name, value.Value);
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, float? value)
    {
        if (value.HasValue) writer.WriteNumber(name, value.Value);
    }

    private static void WriteEnum<T>(Utf8JsonWriter writer, string name, T? value)
        where T : struct, Enum
    {
        if (value.HasValue) writer.WriteString(name, AotEnumMemberConverter.ToWireString(value.Value));
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string>? values)
    {
        if (values == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }
        writer.WriteEndArray();
    }

    private static void WriteStringPairs(Utf8JsonWriter writer, string name, IEnumerable<KeyValuePair<string, string>>? values)
    {
        if (values == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var pair in values)
        {
            writer.WriteString(pair.Key, pair.Value);
        }
        writer.WriteEndObject();
    }

    private static void WriteObjectPairs(Utf8JsonWriter writer, string name, IEnumerable<KeyValuePair<string, object>>? values)
    {
        if (values == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var pair in values)
        {
            writer.WritePropertyName(pair.Key);
            WriteProfileJsonValue(writer, pair.Value);
        }
        writer.WriteEndObject();
    }

    private static void WriteProfileJsonValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            case byte byteValue:
                writer.WriteNumberValue(byteValue);
                break;
            case sbyte sbyteValue:
                writer.WriteNumberValue(sbyteValue);
                break;
            case short shortValue:
                writer.WriteNumberValue(shortValue);
                break;
            case ushort ushortValue:
                writer.WriteNumberValue(ushortValue);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case uint uintValue:
                writer.WriteNumberValue(uintValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case ulong ulongValue:
                writer.WriteNumberValue(ulongValue);
                break;
            case float floatValue:
                writer.WriteNumberValue(floatValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            case JsonElement jsonElement:
                jsonElement.WriteTo(writer);
                break;
            case JsonNode jsonNode:
                jsonNode.WriteTo(writer);
                break;
            case Enum enumValue:
                writer.WriteStringValue(AotEnumMemberConverter.ToWireString(enumValue));
                break;
            default:
                throw new PlaywrightException(
                    $"Clearcote profile values do not support '{value.GetType().FullName}'. " +
                    "Use JSON primitives or JsonNode values.");
        }
    }

    private static void WriteProxy(Utf8JsonWriter writer, string name, Proxy? proxy)
    {
        if (proxy == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        WriteString(writer, "server", proxy.Server);
        WriteString(writer, "bypass", proxy.Bypass);
        WriteString(writer, "username", proxy.Username);
        WriteString(writer, "password", proxy.Password);
        writer.WriteEndObject();
    }

    private static void WriteHttpCredentials(Utf8JsonWriter writer, string name, HttpCredentials? credentials)
    {
        if (credentials == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        WriteString(writer, "username", credentials.Username);
        WriteString(writer, "password", credentials.Password);
        WriteString(writer, "origin", credentials.Origin);
        WriteEnum(writer, "send", credentials.Send);
        writer.WriteEndObject();
    }

    private static void WriteGeolocation(Utf8JsonWriter writer, string name, Geolocation? geolocation)
    {
        if (geolocation == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        writer.WriteNumber("latitude", geolocation.Latitude);
        writer.WriteNumber("longitude", geolocation.Longitude);
        WriteNumber(writer, "accuracy", geolocation.Accuracy);
        writer.WriteEndObject();
    }

    private static void WriteViewportSize(Utf8JsonWriter writer, string name, ViewportSize? size)
    {
        if (size == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        writer.WriteNumber("width", size.Width);
        writer.WriteNumber("height", size.Height);
        writer.WriteEndObject();
    }

    private static void WriteScreenSize(Utf8JsonWriter writer, string name, ScreenSize? size)
    {
        if (size == null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        writer.WriteNumber("width", size.Width);
        writer.WriteNumber("height", size.Height);
        writer.WriteEndObject();
    }

    private static void WriteCanvasBridge(Utf8JsonWriter writer, ClearcoteCanvasBridgeOptions? options)
    {
        if (options == null) return;
        writer.WritePropertyName("canvasBridge");
        writer.WriteStartObject();
        WriteString(writer, "url", options.Url);
        WriteString(writer, "auth", options.Auth);
        WriteString(writer, "mode", options.Mode);
        WriteStrings(writer, "allow", options.Allow);
        WriteStrings(writer, "deny", options.Deny);
        WriteString(writer, "fallback", options.Fallback);
        writer.WriteEndObject();
    }

    private static string? ReadString(JsonElement value)
        => value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool? ReadBool(JsonElement value)
        => value.ValueKind == JsonValueKind.True ? true : value.ValueKind == JsonValueKind.False ? false : null;

    private static int? ReadInt(JsonElement value)
        => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    private static long? ReadLong(JsonElement value)
        => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : null;

    private static float? ReadFloat(JsonElement value)
        => value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out var number) ? number : null;

    private static T? ReadEnum<T>(JsonElement value)
        where T : struct, Enum
    {
        var s = ReadString(value);
        if (s == null) return null;
        try
        {
            return (T)AotEnumMemberConverter.FromWireString(typeof(T), s);
        }
        catch (JsonException)
        {
        }
        if (Enum.TryParse<T>(s.Replace("-", string.Empty), ignoreCase: true, out var result))
        {
            return result;
        }
        return null;
    }

    private static IEnumerable<string>? ReadStrings(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                values.Add(item.GetString()!);
            }
        }

        return values;
    }

    private static IEnumerable<KeyValuePair<string, string>>? ReadStringPairs(JsonElement value)
    {
        var values = new List<KeyValuePair<string, string>>();
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    values.Add(new(property.Name, property.Value.GetString()!));
                }
            }

            return values;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = item.TryGetProperty("name", out var nameValue)
                && nameValue.ValueKind == JsonValueKind.String
                    ? nameValue.GetString()
                    : null;
            var pairValue = item.TryGetProperty("value", out var valueElement)
                && valueElement.ValueKind == JsonValueKind.String
                    ? valueElement.GetString()
                    : null;
            if (name != null && pairValue != null)
            {
                values.Add(new(name, pairValue));
            }
        }

        return values;
    }

    private static IEnumerable<KeyValuePair<string, object>>? ReadObjectPairs(JsonElement value)
    {
        var values = new List<KeyValuePair<string, object>>();
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                values.Add(new(property.Name, ReadProfileJsonValue(property.Value)!));
            }

            return values;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = item.TryGetProperty("name", out var nameValue)
                && nameValue.ValueKind == JsonValueKind.String
                    ? nameValue.GetString()
                    : null;
            if (name != null && item.TryGetProperty("value", out var pairValue))
            {
                values.Add(new(name, ReadProfileJsonValue(pairValue)!));
            }
        }

        return values;
    }

    private static object? ReadProfileJsonValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Number:
                if (value.TryGetInt32(out var intValue))
                {
                    return intValue;
                }
                if (value.TryGetInt64(out var longValue))
                {
                    return longValue;
                }
                return value.GetDouble();
            case JsonValueKind.Object:
            case JsonValueKind.Array:
                return JsonNode.Parse(value.GetRawText())!;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return null;
        }
    }

    private static Proxy? ReadProxy(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var server = value.TryGetProperty("server", out var serverValue)
            ? ReadString(serverValue)
            : null;
        if (string.IsNullOrEmpty(server))
        {
            return null;
        }

        return new()
        {
            Server = server,
            Bypass = value.TryGetProperty("bypass", out var bypassValue) ? ReadString(bypassValue) : null,
            Username = value.TryGetProperty("username", out var usernameValue) ? ReadString(usernameValue) : null,
            Password = value.TryGetProperty("password", out var passwordValue) ? ReadString(passwordValue) : null,
        };
    }

    private static HttpCredentials? ReadHttpCredentials(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var username = value.TryGetProperty("username", out var usernameValue)
            ? ReadString(usernameValue)
            : null;
        var password = value.TryGetProperty("password", out var passwordValue)
            ? ReadString(passwordValue)
            : null;
        if (username == null || password == null)
        {
            return null;
        }

        return new()
        {
            Username = username,
            Password = password,
            Origin = value.TryGetProperty("origin", out var originValue) ? ReadString(originValue) : null,
            Send = value.TryGetProperty("send", out var sendValue) ? ReadEnum<HttpCredentialsSend>(sendValue) : null,
        };
    }

    private static Geolocation? ReadGeolocation(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty("latitude", out var latitudeValue)
            || !value.TryGetProperty("longitude", out var longitudeValue))
        {
            return null;
        }

        var latitude = ReadFloat(latitudeValue);
        var longitude = ReadFloat(longitudeValue);
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        return new()
        {
            Latitude = latitude.Value,
            Longitude = longitude.Value,
            Accuracy = value.TryGetProperty("accuracy", out var accuracyValue) ? ReadFloat(accuracyValue) : null,
        };
    }

    private static ViewportSize? ReadViewportSize(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty("width", out var widthValue)
            || !value.TryGetProperty("height", out var heightValue))
        {
            return null;
        }

        var width = ReadInt(widthValue);
        var height = ReadInt(heightValue);
        return width.HasValue && height.HasValue
            ? new() { Width = width.Value, Height = height.Value }
            : null;
    }

    private static ScreenSize? ReadScreenSize(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty("width", out var widthValue)
            || !value.TryGetProperty("height", out var heightValue))
        {
            return null;
        }

        var width = ReadInt(widthValue);
        var height = ReadInt(heightValue);
        return width.HasValue && height.HasValue
            ? new() { Width = width.Value, Height = height.Value }
            : null;
    }

    private static ClearcoteCanvasBridgeOptions? ReadCanvasBridge(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var options = new ClearcoteCanvasBridgeOptions();
        foreach (var property in value.EnumerateObject())
        {
            switch (ToCamel(property.Name))
            {
                case "url": options.Url = ReadString(property.Value); break;
                case "auth": options.Auth = ReadString(property.Value); break;
                case "mode": options.Mode = ReadString(property.Value); break;
                case "allow": options.Allow = ReadStrings(property.Value); break;
                case "deny": options.Deny = ReadStrings(property.Value); break;
                case "fallback": options.Fallback = ReadString(property.Value); break;
            }
        }

        return options;
    }

    private static string ToCamel(string value)
        => Regex.Replace(value, "_([a-z0-9])", static match => match.Groups[1].Value.ToUpperInvariant());

    private static string HomeDirectory()
        => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private static void TryChmod(string path, string mode)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var unixMode = mode == "700"
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite;
            if (File.Exists(path) || Directory.Exists(path))
            {
                File.SetUnixFileMode(path, unixMode);
            }
        }
        catch
        {
        }
    }

    private static string JsonEscape(string value)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStringValue(value);
        writer.Flush();
        var json = Encoding.UTF8.GetString(stream.ToArray());
        return json.Substring(1, json.Length - 2);
    }

    private sealed record WidevineUpdate(string Url, string Sha256, string Version);

    private sealed record WidevinePlatformInfo(string AtOs, string OsPlatform, string OsVersion, string Subdir, string FileName);

    private static WidevinePlatformInfo WidevinePlatform()
        => OperatingSystem.IsLinux()
            ? new("Linux", "Linux", "6.1.0", "linux_x64", "libwidevinecdm.so")
            : new("win", "Windows", "10.0.19045.0", "win_x64", "widevinecdm.dll");

    private static string WidevineUserAgent()
        => OperatingSystem.IsLinux()
            ? "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36"
            : "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36";

    private static string OmahaBody()
    {
        var platform = WidevinePlatform();
        return "{\"request\":{\"@os\":\"" + platform.AtOs + "\",\"@updater\":\"clearcote\",\"acceptformat\":\"crx3\",\"protocol\":\"3.1\",\"arch\":\"x64\",\"nacl_arch\":\"x86-64\",\"prodversion\":\"149.0.0.0\",\"updaterversion\":\"149.0.0.0\",\"dedup\":\"cr\",\"os\":{\"arch\":\"x86_64\",\"platform\":\"" + platform.OsPlatform + "\",\"version\":\"" + platform.OsVersion + "\"},\"app\":[{\"appid\":\"" + WidevineAppId + "\",\"version\":\"0.0.0.0\",\"updatecheck\":{},\"ping\":{\"r\":-2}}]}}";
    }

    private static async Task<JsonDocument> PostOmahaAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        using var request = new HttpRequestMessage(HttpMethod.Post, OmahaUrl)
        {
            Content = new StringContent(OmahaBody(), Encoding.UTF8, "application/json"),
        };
        request.Headers.UserAgent.ParseAdd(WidevineUserAgent());
        using var response = await client.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (raw.StartsWith(")]}'", StringComparison.Ordinal))
        {
            var newline = raw.IndexOf('\n', StringComparison.Ordinal);
            raw = newline >= 0 ? raw.Substring(newline + 1) : raw.Substring(4);
        }

        return JsonDocument.Parse(raw);
    }

    private static WidevineUpdate ParseWidevineUpdate(JsonDocument document)
    {
        var app = document.RootElement.GetProperty("response").GetProperty("app")[0];
        var updateCheck = app.GetProperty("updatecheck");
        if (!updateCheck.TryGetProperty("status", out var status) || status.GetString() != "ok")
        {
            throw new PlaywrightException("Widevine update check status: " + (status.GetString() ?? "<missing>"));
        }

        if (updateCheck.TryGetProperty("pipelines", out var pipelines) && pipelines.ValueKind == JsonValueKind.Array)
        {
            foreach (var pipeline in pipelines.EnumerateArray())
            {
                if (!pipeline.TryGetProperty("operations", out var operations) || operations.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
                foreach (var operation in operations.EnumerateArray())
                {
                    if (operation.TryGetProperty("urls", out var urls)
                        && urls.ValueKind == JsonValueKind.Array
                        && urls.GetArrayLength() > 0
                        && operation.TryGetProperty("out", out var output)
                        && output.TryGetProperty("sha256", out var sha))
                    {
                        var version = app.TryGetProperty("nextversion", out var nextVersion)
                            ? nextVersion.GetString() ?? string.Empty
                            : updateCheck.TryGetProperty("nextversion", out var updateVersion)
                                ? updateVersion.GetString() ?? string.Empty
                                : string.Empty;
                        return new(urls[0].GetProperty("url").GetString()!, sha.GetString() ?? string.Empty, version);
                    }
                }
            }
        }

        var baseUrl = updateCheck.GetProperty("urls").GetProperty("url")[0].GetProperty("codebase").GetString();
        var package = updateCheck.GetProperty("manifest").GetProperty("packages").GetProperty("package")[0];
        var name = package.GetProperty("name").GetString();
        var packageHash = package.TryGetProperty("hash_sha256", out var hash) ? hash.GetString() ?? string.Empty : string.Empty;
        var manifestVersion = updateCheck.GetProperty("manifest").TryGetProperty("version", out var manifest)
            ? manifest.GetString() ?? string.Empty
            : string.Empty;
        if (baseUrl != null && name != null)
        {
            return new(baseUrl.TrimEnd('/') + "/" + name, packageHash, manifestVersion);
        }

        throw new PlaywrightException("could not find a CDM download URL in the update response");
    }

    private static byte[] Crx3ToZip(byte[] bytes)
    {
        if (bytes.Length < 4 || Encoding.Latin1.GetString(bytes, 0, 4) != "Cr24")
        {
            return bytes;
        }
        if (bytes.Length < 12)
        {
            throw new PlaywrightException("malformed CRX3 (truncated header)");
        }

        var headerLength = BitConverter.ToUInt32(bytes, 8);
        if (12 + headerLength > bytes.Length)
        {
            throw new PlaywrightException("malformed CRX3 (header overruns buffer)");
        }

        var zipLength = bytes.Length - (int)(12 + headerLength);
        var rented = ArrayPool<byte>.Shared.Rent(zipLength);
        Buffer.BlockCopy(bytes, (int)(12 + headerLength), rented, 0, zipLength);
        var output = new byte[zipLength];
        Buffer.BlockCopy(rented, 0, output, 0, zipLength);
        ArrayPool<byte>.Shared.Return(rented);
        return output;
    }

    internal static void CopyDirectorySafely(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in new DirectoryInfo(source).EnumerateFiles())
        {
            if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new PlaywrightException($"Refusing to copy reparse point from Widevine directory: {file.FullName}");
            }

            using var input = file.OpenRead();
            using var output = new FileStream(Path.Combine(target, file.Name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }

        foreach (var directory in new DirectoryInfo(source).EnumerateDirectories())
        {
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new PlaywrightException($"Refusing to copy reparse point from Widevine directory: {directory.FullName}");
            }

            CopyDirectorySafely(directory.FullName, Path.Combine(target, directory.Name));
        }
    }

    private static void WidevineLog(bool quiet, string message)
    {
        if (!quiet)
        {
            Console.Error.WriteLine("[clearcote] [widevine] " + message);
        }
    }

    private const string RenderProbeScript = @"() => {
  const out = { webgl: false, webgl2: false, vendor: '', renderer: '',
                unmaskedVendor: '', unmaskedRenderer: '', maxTextureSize: 0 };
  try {
    const c = document.createElement('canvas');
    const gl2 = c.getContext('webgl2');
    const gl = gl2 || c.getContext('webgl') || c.getContext('experimental-webgl');
    if (!gl) return out;
    out.webgl = true;
    out.webgl2 = !!gl2;
    out.vendor = gl.getParameter(gl.VENDOR) || '';
    out.renderer = gl.getParameter(gl.RENDERER) || '';
    const dbg = gl.getExtension('WEBGL_debug_renderer_info');
    if (dbg) {
      out.unmaskedVendor = gl.getParameter(dbg.UNMASKED_VENDOR_WEBGL) || '';
      out.unmaskedRenderer = gl.getParameter(dbg.UNMASKED_RENDERER_WEBGL) || '';
    }
    out.maxTextureSize = gl.getParameter(gl.MAX_TEXTURE_SIZE) || 0;
  } catch (e) { out.error = String(e); }
  return out;
}";

    private static ClearcoteRenderVerdict EvaluateRenderInfo(JsonElement info, string? claimedGpu)
    {
        static string GetString(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? string.Empty
                    : string.Empty;
        static int GetInt(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out var number)
                    ? number
                    : 0;
        static bool GetBool(JsonElement element, string name)
            => element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.True;

        var renderer = FirstNonEmpty(GetString(info, "unmaskedRenderer"), GetString(info, "renderer"));
        var vendor = FirstNonEmpty(GetString(info, "unmaskedVendor"), GetString(info, "vendor"));
        var warnings = new List<string>();
        var hasWebgl = GetBool(info, "webgl");
        if (!hasWebgl)
        {
            warnings.Add("WebGL is unavailable - a hard tell for a real desktop browser.");
        }

        var software = ContainsAny(renderer, "swiftshader", "google swiftshader", "llvmpipe", "softpipe", "mesa offscreen", "microsoft basic render", "software adapter")
            || ContainsAny(vendor, "swiftshader", "google swiftshader", "llvmpipe", "softpipe", "mesa offscreen", "microsoft basic render", "software adapter");
        if (software)
        {
            warnings.Add("software rasterizer detected in the WebGL renderer (" + renderer + ") - a headless/no-GPU tell.");
        }

        var rendererFamily = GpuFamily(renderer);
        var vendorFamily = GpuFamily(vendor);
        if (!string.IsNullOrEmpty(rendererFamily) && !string.IsNullOrEmpty(vendorFamily) && rendererFamily != vendorFamily)
        {
            warnings.Add($"WebGL vendor and renderer disagree on GPU family (vendor~{vendorFamily}, renderer~{rendererFamily}).");
        }

        if (!string.IsNullOrEmpty(claimedGpu))
        {
            var claimedFamily = GpuFamily(claimedGpu);
            if (!string.IsNullOrEmpty(claimedFamily) && !string.IsNullOrEmpty(rendererFamily) && claimedFamily != rendererFamily)
            {
                warnings.Add($"the claimed GPU ({claimedGpu}, family ~{claimedFamily}) does not match the WebGL renderer family (~{rendererFamily}).");
            }
        }

        return new()
        {
            Vendor = vendor,
            Renderer = renderer,
            Webgl = hasWebgl,
            Webgl2 = GetBool(info, "webgl2"),
            MaxTextureSize = GetInt(info, "maxTextureSize"),
            SoftwareSuspected = software,
            Coherent = hasWebgl && !software && !warnings.Any(static warning => warning.Contains("disagree", StringComparison.Ordinal) || warning.Contains("does not match", StringComparison.Ordinal)),
            Warnings = warnings,
        };
    }

    private static string FirstNonEmpty(string first, string second)
        => string.IsNullOrEmpty(first) ? second : first;

    private static bool ContainsAny(string value, params string[] markers)
    {
        var lower = value.ToLowerInvariant();
        return markers.Any(lower.Contains);
    }

    private static string GpuFamily(string value)
    {
        var lower = value.ToLowerInvariant();
        if (ContainsAny(lower, "nvidia", "geforce", "rtx", "gtx", "quadro")) return "nvidia";
        if (ContainsAny(lower, "radeon", "amd", "ati ")) return "amd";
        if (ContainsAny(lower, "intel", "iris", "uhd graphics", "hd graphics")) return "intel";
        if (ContainsAny(lower, "apple", "m1", "m2", "m3", "m4")) return "apple";
        if (lower.Contains("mali", StringComparison.Ordinal)) return "mali";
        if (lower.Contains("adreno", StringComparison.Ordinal)) return "adreno";
        if (lower.Contains("powervr", StringComparison.Ordinal)) return "powervr";
        return string.Empty;
    }
}
