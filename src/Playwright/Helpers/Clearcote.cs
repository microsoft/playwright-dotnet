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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Helpers;

internal static partial class Clearcote
{
    private const string VerifiedFileName = ".verified";
    private const string DefaultAcceptLanguage = "en-US,en";
    private const string SigningKeyFingerprint = "CA96F185F96A693AEDB3AC1FCB00D851B7A86B0F";
    internal const string Repo = "clearcotelabs/clearcote-browser";
    private static readonly SemaphoreSlim _downloadLock = new(1, 1);
    private static readonly HashSet<string> _seenCoherenceNotes = new(StringComparer.Ordinal);

    private static readonly ReleaseInfo _windows = new(
        Tag: "v0.1.0-pre.18",
        Version: "149.0.7827.114",
        Asset: "clearcote-149.0.7827.114-windows-x64.zip",
        Url: "https://github.com/clearcotelabs/clearcote-browser/releases/download/v0.1.0-pre.18/clearcote-149.0.7827.114-windows-x64.zip",
        Sha256: "935d43daba8ce6f336b4ede21b326744f7804c2fb4f0384bafa2d2a80a71a88c",
        ExeSha256: "09a9f5ed46be45b54babc91872256fcdd5ef61cef6bf65cbec3928cbb38ee17a",
        Size: 242649591,
        Archive: "zip",
        Binary: "chrome.exe",
        AssetGlob: "windows-x64");

    private static readonly ReleaseInfo _linux = new(
        Tag: "v0.1.0-pre.18",
        Version: "149.0.7827.114",
        Asset: "clearcote-149.0.7827.114-linux-x64.tar.xz",
        Url: "https://github.com/clearcotelabs/clearcote-browser/releases/download/v0.1.0-pre.18/clearcote-149.0.7827.114-linux-x64.tar.xz",
        Sha256: "fd96497e921b4fc9f384a5c1377896c8ee7e8a3a1991835c0256b010811e97aa",
        ExeSha256: "f4e8c1161938769d6a6c50aee1497c76b4f6723a6a0a3f02a19e0b5a9d4b141a",
        Size: 142700100,
        Archive: "tar.xz",
        Binary: "chrome",
        AssetGlob: "linux-x64");

    private static readonly string[] _privacySandboxFeatures =
    {
        "BrowsingTopics",
        "BrowsingTopicsDocumentAPI",
        "Fledge",
        "InterestGroupStorage",
        "PrivateAggregationApi",
        "SharedStorageAPI",
        "FencedFrames",
        "WebUSB",
    };

    private enum GpgVerdict
    {
        Ok,
        Skipped,
        Failed,
    }

    internal static bool ShouldPatchLaunch(string browserName, BrowserTypeLaunchOptions options)
        => browserName == "chromium"
        && string.IsNullOrEmpty(options.ExecutablePath)
        && string.IsNullOrEmpty(options.Channel)
        && (options is ClearcoteLaunchOptions || EnvironmentOptIn());

    internal static bool ShouldPatchLaunch(string browserName, BrowserTypeLaunchPersistentContextOptions options)
        => browserName == "chromium"
        && string.IsNullOrEmpty(options.ExecutablePath)
        && string.IsNullOrEmpty(options.Channel)
        && (options is ClearcoteLaunchPersistentContextOptions || EnvironmentOptIn());

    private static bool EnvironmentOptIn()
    {
        var env = Environment.GetEnvironmentVariable("CLEARCOTE");
        return string.Equals(env, "1", StringComparison.Ordinal)
            || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task<LaunchPatch> PatchAsync(BrowserTypeLaunchOptions options)
    {
        ApplyProfile(options);
        var settings = ClearcoteSettings.From(options);
        settings = await ApplyGeoipAsync(settings, options.Proxy).ConfigureAwait(false);
        EmitCoherenceWarnings(settings, options.Proxy, options.Headless, options.Args);
        var (proxyArgs, proxy) = ResolveProxy(options.Proxy, settings.Quiet);
        var executablePath = await ResolveExecutablePathAsync(settings.CacheDir, settings.Quiet, settings.AutoUpdate, honorEnvironmentBinary: true).ConfigureAwait(false);
        var args = AssembleArgs(settings, proxyArgs, options.Args, options.Proxy);
        return new LaunchPatch(
            executablePath,
            args,
            proxy,
            DefaultIgnoreDefaultArgs(options.IgnoreDefaultArgs, options.IgnoreAllDefaultArgs),
            false,
            options.Headless == false,
            settings.Humanize,
            settings.ShowCursor);
    }

    internal static async Task<LaunchPatch> PatchAsync(string userDataDir, BrowserTypeLaunchPersistentContextOptions options)
    {
        ApplyProfile(options);
        var settings = ClearcoteSettings.From(options);
        settings = await ApplyGeoipAsync(settings, options.Proxy).ConfigureAwait(false);
        EmitCoherenceWarnings(settings, options.Proxy, options.Headless, options.Args);
        var (proxyArgs, proxy) = ResolveProxy(options.Proxy, settings.Quiet);
        var executablePath = await ResolveExecutablePathAsync(settings.CacheDir, settings.Quiet, settings.AutoUpdate, honorEnvironmentBinary: true).ConfigureAwait(false);
        var userArgs = options.Args;
        var ignoreDefaultArgs = DefaultIgnoreDefaultArgs(options.IgnoreDefaultArgs, options.IgnoreAllDefaultArgs);
        if (options is ClearcoteLaunchPersistentContextOptions clearcote && clearcote.Widevine == true)
        {
            try
            {
                await SeedWidevineAsync(userDataDir, new() { Quiet = clearcote.Quiet }).ConfigureAwait(false);
                var widevine = ApplyWidevineArgs(ignoreDefaultArgs, options.IgnoreAllDefaultArgs, userArgs);
                ignoreDefaultArgs = widevine.IgnoreDefaultArgs;
                userArgs = widevine.Args;
            }
            catch (Exception ex)
            {
                Log(settings.Quiet, "[widevine] setup failed (continuing without DRM): " + ex.Message);
            }
        }

        var args = AssembleArgs(settings, proxyArgs, userArgs, options.Proxy);
        return new LaunchPatch(
            executablePath,
            args,
            proxy,
            ignoreDefaultArgs,
            options.Headless == false && options.ViewportSize == null,
            options.Headless == false,
            settings.Humanize,
            settings.ShowCursor);
    }

    private static IEnumerable<string>? DefaultIgnoreDefaultArgs(IEnumerable<string>? ignoreDefaultArgs, bool? ignoreAllDefaultArgs)
    {
        if (ignoreDefaultArgs != null || ignoreAllDefaultArgs == true)
        {
            return ignoreDefaultArgs;
        }

        return new[] { "--enable-automation" };
    }

    private static IEnumerable<string> AssembleArgs(
        ClearcoteSettings settings,
        IEnumerable<string> proxyArgs,
        IEnumerable<string>? userArgs,
        Proxy? proxy)
    {
        var args = new List<string>();
        args.AddRange(FingerprintArgs(settings));
        args.AddRange(AgentArgs(settings));
        args.AddRange(ExtensionArgs(settings.Extensions));
        args.AddRange(proxyArgs);
        if (!string.IsNullOrEmpty(proxy?.Server))
        {
            args.Add("--disable-quic");
        }

        if (settings.DisablePrivacySandbox != false)
        {
            args.Add("--disable-features=" + string.Join(",", _privacySandboxFeatures));
        }

        var userArgsList = userArgs?.ToArray() ?? Array.Empty<string>();
        if (string.IsNullOrEmpty(settings.WebrtcIp)
            && !args.Concat(userArgsList).Any(static a => a.StartsWith("--webrtc-ip-handling-policy", StringComparison.Ordinal)
                || a.StartsWith("--force-webrtc-ip-handling-policy", StringComparison.Ordinal)))
        {
            args.Add("--webrtc-ip-handling-policy=disable_non_proxied_udp");
        }

        args.AddRange(userArgsList);
        return MergeFeatureFlags(args);
    }

    private static void ApplyProfile(BrowserTypeLaunchOptions options)
    {
        if (options is not ClearcoteLaunchOptions clearcote || string.IsNullOrEmpty(clearcote.Profile))
        {
            return;
        }

        var merged = ToLaunchOptions(LoadProfile(clearcote.Profile!).Options);
        MergeInto(merged, clearcote, overrideExisting: true);
        MergeInto(clearcote, merged, overrideExisting: true);
    }

    private static void ApplyProfile(BrowserTypeLaunchPersistentContextOptions options)
    {
        if (options is not ClearcoteLaunchPersistentContextOptions clearcote || string.IsNullOrEmpty(clearcote.Profile))
        {
            return;
        }

        var merged = new ClearcoteLaunchPersistentContextOptions(LoadProfile(clearcote.Profile!).Options);
        MergeInto(merged, clearcote, overrideExisting: true);
        MergeInto(clearcote, merged, overrideExisting: true);
    }

    private static IEnumerable<string> ExtensionArgs(IEnumerable<string>? extensions)
    {
        if (extensions == null)
        {
            return Array.Empty<string>();
        }

        var paths = extensions.Where(static path => !string.IsNullOrWhiteSpace(path)).ToArray();
        if (paths.Length == 0)
        {
            return Array.Empty<string>();
        }

        var joined = string.Join(",", paths);
        return new[] { "--load-extension=" + joined, "--disable-extensions-except=" + joined };
    }

    private static IEnumerable<string> FingerprintArgs(ClearcoteSettings settings)
    {
        var args = new List<string>();
        AddFlag(args, "fingerprint", settings.Fingerprint);
        AddFlag(args, "fingerprint-platform", settings.ClearcotePlatform ?? HostPersonaPlatform());
        AddFlag(args, "fingerprint-platform-version", settings.PlatformVersion);
        AddFlag(args, "fingerprint-brand", settings.Brand ?? "chrome");
        AddFlag(args, "fingerprint-brand-version", settings.BrandVersion);
        AddFlag(args, "fingerprint-gpu-vendor", settings.GpuVendor);
        AddFlag(args, "fingerprint-gpu-renderer", settings.GpuRenderer);
        AddFlag(args, "fingerprint-hardware-concurrency", settings.HardwareConcurrency?.ToString(CultureInfo.InvariantCulture));
        AddFlag(args, "fingerprint-location", settings.Location);
        AddFlag(args, "fingerprint-storage-quota", settings.StorageQuota?.ToString(CultureInfo.InvariantCulture));
        AddFlag(args, "fingerprint-tls-profile", settings.TlsProfile);
        AddFlag(args, "timezone", settings.Timezone);

        var cleanLang = CleanAcceptLanguage(settings.AcceptLanguage ?? DefaultAcceptLanguage);
        args.Add("--accept-lang=" + cleanLang);
        var primaryLang = cleanLang.Split(',')[0];
        if (!string.IsNullOrEmpty(primaryLang))
        {
            args.Add("--lang=" + primaryLang);
        }

        AddFlag(args, "webrtc-ip", settings.WebrtcIp);
        if (settings.DisableGpuFingerprint == true)
        {
            args.Add("--disable-gpu-fingerprint");
        }

        if (settings.FingerprintNoise == false)
        {
            args.Add("--disable-fingerprint-noise");
        }

        if (!string.IsNullOrEmpty(settings.FingerprintProfile))
        {
            args.Add("--fingerprint-profile=" + EncodeProfile(settings.FingerprintProfile));
        }

        if (!string.IsNullOrEmpty(settings.CanvasBridge?.Url))
        {
            var bridge = settings.CanvasBridge;
            AddFlag(args, "canvas-bridge-url", bridge.Url);
            AddFlag(args, "canvas-bridge-auth", bridge.Auth);
            AddFlag(args, "canvas-bridge-mode", bridge.Mode);
            AddFlag(args, "canvas-bridge-allow", JoinCsv(bridge.Allow));
            AddFlag(args, "canvas-bridge-deny", JoinCsv(bridge.Deny));
            AddFlag(args, "canvas-bridge-fallback", bridge.Fallback);
            if (!args.Contains("--no-sandbox", StringComparer.Ordinal))
            {
                args.Add("--no-sandbox");
            }
        }

        return args;
    }

    private static IEnumerable<string> AgentArgs(ClearcoteSettings settings)
    {
        var args = new List<string>();
        if (settings.AgentLlmKey == null && settings.AgentLlmUrl == null)
        {
            return args;
        }

        args.Add("--agent-llm-url=" + (settings.AgentLlmUrl ?? OpenRouterBaseUrl));
        AddFlag(args, "agent-llm-key", settings.AgentLlmKey);
        AddFlag(args, "agent-model", settings.AgentModel);
        AddFlag(args, "agent-tool-mode", settings.AgentToolMode);
        args.Add(AgentTypingFeature(settings.AgentTyping ?? "human"));
        return args;
    }

    private static string AgentTypingFeature(string speed)
    {
        if (string.Equals(speed, "instant", StringComparison.OrdinalIgnoreCase))
        {
            return "--disable-features=GlicActorIncrementalTyping";
        }

        if (string.Equals(speed, "fast", StringComparison.OrdinalIgnoreCase))
        {
            return "--enable-features=GlicActorIncrementalTyping:glic-actor-incremental-typing-key-down-duration/8ms/glic-actor-incremental-typing-key-up-duration/8ms";
        }

        return "--enable-features=GlicActorIncrementalTyping:"
            + "glic-actor-incremental-typing-key-down-duration/45ms/"
            + "glic-actor-incremental-typing-key-up-duration/60ms/"
            + "glic-actor-incremental-typing-long-multiplier/0.7/"
            + "glic-actor-incremental-typing-long-text-threshold/80/"
            + "glic-actor-long-text-paste-threshold/100000";
    }

    internal static string CursorOverlayScript()
        => @"(() => {
  if (window.__clearcoteCursor) return; window.__clearcoteCursor = 1;
  const make = () => {
    if (document.getElementById('__clearcote_cursor')) return;
    const d = document.createElement('div'); d.id = '__clearcote_cursor';
    d.style.cssText = 'position:fixed;left:0;top:0;width:20px;height:20px;margin:-10px 0 0 -10px;' +
      'border-radius:50%;border:2px solid #ff3b3b;background:rgba(255,59,59,.22);' +
      'box-shadow:0 0 10px rgba(255,59,59,.6);pointer-events:none;z-index:2147483647';
    (document.body || document.documentElement).appendChild(d);
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', make); else make();
  document.addEventListener('mousemove', (e) => {
    const d = document.getElementById('__clearcote_cursor');
    if (d) { d.style.left = e.clientX + 'px'; d.style.top = e.clientY + 'px'; }
  }, true);
})();";

    private static void AddFlag(List<string> args, string flag, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            args.Add("--" + flag + "=" + value);
        }
    }

    private static string? JoinCsv(IEnumerable<string>? values)
    {
        if (values == null)
        {
            return null;
        }

        var items = values.Where(static value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return items.Length == 0 ? null : string.Join(",", items);
    }

    private static string HostPersonaPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macos";
        }

        return "windows";
    }

    private static string CleanAcceptLanguage(string value)
        => string.Join(
            ",",
            value.Split(',')
                .Select(static token => token.Split(';')[0].Trim())
                .Where(static token => token.Length > 0));

    private static string EncodeProfile(string value)
    {
        byte[] raw = File.Exists(value) ? File.ReadAllBytes(value) : Encoding.UTF8.GetBytes(value);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(raw, 0, raw.Length);
        }

        return Convert.ToBase64String(output.ToArray());
    }

    private static IReadOnlyList<string> MergeFeatureFlags(IEnumerable<string> args)
    {
        var enabled = new List<string>();
        var disabled = new List<string>();
        var rest = new List<string>();
        foreach (var arg in args)
        {
            if (arg.StartsWith("--enable-features=", StringComparison.Ordinal))
            {
                enabled.AddRange(arg.Substring("--enable-features=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            else if (arg.StartsWith("--disable-features=", StringComparison.Ordinal))
            {
                disabled.AddRange(arg.Substring("--disable-features=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                rest.Add(arg);
            }
        }

        if (enabled.Count > 0)
        {
            rest.Add("--enable-features=" + string.Join(",", enabled.Distinct(StringComparer.Ordinal)));
        }

        if (disabled.Count > 0)
        {
            rest.Add("--disable-features=" + string.Join(",", disabled.Distinct(StringComparer.Ordinal)));
        }

        return rest;
    }

    private static (IEnumerable<string> Args, Proxy? Proxy) ResolveProxy(Proxy? proxy, bool quiet)
    {
        if (proxy == null || string.IsNullOrWhiteSpace(proxy.Server))
        {
            return (Array.Empty<string>(), proxy);
        }

        var hasCredentials = !string.IsNullOrEmpty(proxy.Username) || !string.IsNullOrEmpty(proxy.Password);
        if (proxy.Server.StartsWith("socks", StringComparison.OrdinalIgnoreCase) && hasCredentials)
        {
            Log(quiet, "routed credentialed SOCKS proxy via --proxy-server; Chromium cannot authenticate SOCKS credentials directly.");
            return (new[] { "--proxy-server=" + proxy.Server.Trim() }, null);
        }

        return (Array.Empty<string>(), proxy);
    }

    private static async Task<ClearcoteSettings> ApplyGeoipAsync(ClearcoteSettings settings, Proxy? proxy)
    {
        if (!settings.Geoip)
        {
            return settings;
        }

        var geo = await ResolveGeoAsync(proxy, settings.Quiet).ConfigureAwait(false);
        return geo == null ? settings : settings.WithGeo(geo);
    }

    private static async Task<ClearcoteGeo?> ResolveGeoAsync(Proxy? proxy, bool quiet)
    {
        if (IsSocksProxy(proxy))
        {
            Log(quiet, "geoip: SOCKS proxy cannot be used for geo lookup; set timezone, accept language, location, and WebRTC IP explicitly.");
            return null;
        }

        var ip = await ResolveExitIpAsync(proxy, quiet).ConfigureAwait(false);
        var geo = ip == null ? null : await ResolveMmdbGeoAsync(ip, quiet).ConfigureAwait(false);
        if (geo == null)
        {
            geo = await ResolveIpApiGeoAsync(proxy, quiet).ConfigureAwait(false);
        }

        if (geo != null)
        {
            Log(quiet, $"geoip: {geo.Ip} -> {geo.Country} tz={geo.Timezone} lang={geo.AcceptLanguage}");
        }

        return geo;
    }

    private static async Task<ClearcoteGeo?> ResolveIpApiGeoAsync(Proxy? proxy, bool quiet)
    {
        try
        {
            using var client = CreateGeoHttpClient(proxy);
            using var response = await client.GetAsync(new Uri("http://ip-api.com/json/?fields=status,message,countryCode,timezone,lat,lon,query")).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Log(quiet, $"geoip: ip-api returned HTTP {(int)response.StatusCode}");
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            var root = document.RootElement;
            if (!string.Equals(GetStringProperty(root, "status"), "success", StringComparison.Ordinal))
            {
                Log(quiet, "geoip: ip-api did not return a successful lookup");
                return null;
            }

            var country = GetStringProperty(root, "countryCode");
            var location = TryGetDoubleProperty(root, "lat", out var lat) && TryGetDoubleProperty(root, "lon", out var lon)
                ? lat.ToString(CultureInfo.InvariantCulture) + "," + lon.ToString(CultureInfo.InvariantCulture)
                : null;
            var geo = new ClearcoteGeo(
                Ip: GetStringProperty(root, "query"),
                Country: country,
                Timezone: GetStringProperty(root, "timezone"),
                AcceptLanguage: AcceptLanguageForCountry(country),
                Location: location);
            return geo;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException or UriFormatException)
        {
            Log(quiet, $"geoip: lookup failed ({ex.Message})");
            return null;
        }
    }

    private static HttpClient CreateGeoHttpClient(Proxy? proxy)
    {
        var handler = new HttpClientHandler { CheckCertificateRevocationList = true };
        var uri = NormalizedProxyUri(proxy);
        if (uri != null)
        {
            var webProxy = new WebProxy(uri);
            if (!string.IsNullOrEmpty(proxy?.Username))
            {
                webProxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password ?? string.Empty);
            }

            handler.UseProxy = true;
            handler.Proxy = webProxy;
        }

        var client = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("clearcote-dotnet");
        client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        return client;
    }

    private static Uri? NormalizedProxyUri(Proxy? proxy)
    {
        var server = proxy?.Server?.Trim();
        if (string.IsNullOrEmpty(server))
        {
            return null;
        }

        if (!server.Contains("://", StringComparison.Ordinal))
        {
            server = "http://" + server;
        }

        return new Uri(server);
    }

    private static bool IsSocksProxy(Proxy? proxy)
    {
        var server = proxy?.Server;
        if (string.IsNullOrWhiteSpace(server))
        {
            return false;
        }

        return server.StartsWith("socks", StringComparison.OrdinalIgnoreCase)
            || server.Contains("://socks", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetDoubleProperty(JsonElement element, string name, out double value)
    {
        if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static void EmitCoherenceWarnings(ClearcoteSettings settings, Proxy? proxy, bool? headless, IEnumerable<string>? userArgs)
    {
        if (settings.Quiet || Environment.GetEnvironmentVariable("CLEARCOTE_NO_WARN") == "1")
        {
            return;
        }

        var warnings = CoherenceWarnings(settings, proxy, headless, userArgs);
        foreach (var warning in warnings)
        {
            if (warning.Note)
            {
                lock (_seenCoherenceNotes)
                {
                    if (!_seenCoherenceNotes.Add(warning.Code))
                    {
                        continue;
                    }
                }
            }

            Console.Error.WriteLine("clearcote: " + (warning.Note ? "note: " : "warning: ") + warning.Message);
        }
    }

    private static IEnumerable<(bool Note, string Code, string Message)> CoherenceWarnings(ClearcoteSettings settings, Proxy? proxy, bool? headless, IEnumerable<string>? userArgs)
    {
        var warnings = new List<(bool Note, string Code, string Message)>();
        var server = proxy?.Server ?? string.Empty;
        if (!string.IsNullOrEmpty(server) && !settings.Geoip && string.IsNullOrEmpty(settings.Timezone) && string.IsNullOrEmpty(settings.AcceptLanguage))
        {
            warnings.Add((false, "proxy-no-geo", "proxy set without geoip and no timezone/acceptLanguage; browser timezone and language may reflect this host instead of the proxy exit region."));
        }

        if (!string.IsNullOrEmpty(server) && settings.Geoip && IsSocksProxy(proxy))
        {
            warnings.Add((false, "socks-geoip", "geoip cannot resolve a SOCKS proxy exit IP; set timezone, acceptLanguage, location, and webrtcIp manually."));
        }

        var hostFamily = HostPersonaPlatform();
        if (!string.IsNullOrEmpty(settings.ClearcotePlatform)
            && !string.Equals(settings.ClearcotePlatform, hostFamily, StringComparison.Ordinal)
            && string.IsNullOrEmpty(settings.FingerprintProfile))
        {
            warnings.Add((false, "platform-host-fonts", $"platform='{settings.ClearcotePlatform}' on a {hostFamily} host without a fingerprintProfile may leave host-native font/canvas metrics."));
        }

        if (!string.IsNullOrEmpty(settings.GpuRenderer) && !string.IsNullOrEmpty(settings.ClearcotePlatform))
        {
            var gpuMismatch = GpuPlatformMismatch(settings.GpuRenderer, settings.ClearcotePlatform);
            if (!string.IsNullOrEmpty(gpuMismatch))
            {
                warnings.Add((false, "gpu-platform", $"gpuRenderer is incoherent with platform='{settings.ClearcotePlatform}' ({gpuMismatch}): '{settings.GpuRenderer}'."));
            }
        }

        if (!string.IsNullOrEmpty(settings.GpuRenderer) && IsSoftwareGpu(settings.GpuRenderer))
        {
            warnings.Add((false, "gpu-software", $"gpuRenderer is a software renderer ('{settings.GpuRenderer}'); a real consumer machine usually reports a hardware GPU."));
        }

        if (!string.IsNullOrEmpty(settings.Brand) && !IsChromeBrand(settings.Brand))
        {
            warnings.Add((false, "brand-mismatch", $"brand='{settings.Brand}' is advertised in UA-CH, but the binary transport remains Chrome."));
        }

        var buildMajor = CurrentRelease().Version.Split('.')[0];
        if (!string.IsNullOrEmpty(settings.BrandVersion) && settings.BrandVersion.Split('.')[0] != buildMajor)
        {
            warnings.Add((false, "version-mismatch", $"brandVersion major {settings.BrandVersion.Split('.')[0]} differs from the build's Chrome {buildMajor}."));
        }

        if (settings.DisableGpuFingerprint == true && settings.FingerprintNoise != false)
        {
            warnings.Add((false, "gpu-noise", "disableGpuFingerprint presents the real GPU, but fingerprintNoise is still enabled; pair it with fingerprintNoise:false for coherent real pixels."));
        }

        if (headless != false && string.IsNullOrEmpty(settings.CanvasBridge?.Url) && settings.DisableGpuFingerprint != true && string.IsNullOrEmpty(settings.FingerprintProfile))
        {
            warnings.Add((true, "headless-render", "headless with no canvasBridge, disableGpuFingerprint, or fingerprintProfile may expose render-vs-GPU mismatch on canvas-scored sites."));
        }

        if (!string.IsNullOrEmpty(settings.CanvasBridge?.Url) && string.IsNullOrEmpty(settings.GpuRenderer) && string.IsNullOrEmpty(settings.GpuVendor) && string.IsNullOrEmpty(settings.FingerprintProfile))
        {
            warnings.Add((true, "bridge-no-gpu", "canvasBridge is set but gpuVendor/gpuRenderer are not pinned; WebGL renderer strings may not match bridge pixels."));
        }

        if (userArgs != null && userArgs.Any(static arg => arg.Contains("--enable-automation", StringComparison.Ordinal) || arg.StartsWith("--remote-debugging-port", StringComparison.Ordinal)))
        {
            warnings.Add((false, "automation-arg", "args reintroduce an automation flag that the Clearcote patch strips by default."));
        }

        return warnings;
    }

    private static string? GpuPlatformMismatch(string renderer, string platform)
    {
        var value = renderer.ToLowerInvariant();
        return platform switch
        {
            "macos" when value.Contains("direct3d", StringComparison.Ordinal) || value.Contains("d3d", StringComparison.Ordinal) => "macOS uses Metal/OpenGL, never Direct3D",
            "windows" when value.Contains("metal", StringComparison.Ordinal) => "Windows uses Direct3D/ANGLE, never Metal",
            "linux" when value.Contains("direct3d", StringComparison.Ordinal) || value.Contains("d3d", StringComparison.Ordinal) || value.Contains("metal", StringComparison.Ordinal) => "Linux uses OpenGL/Vulkan, never Direct3D/Metal",
            _ => null,
        };
    }

    private static bool IsSoftwareGpu(string renderer)
    {
        var value = renderer.ToLowerInvariant();
        return value.Contains("swiftshader", StringComparison.Ordinal)
            || value.Contains("llvmpipe", StringComparison.Ordinal)
            || value.Contains("microsoft basic render", StringComparison.Ordinal)
            || value.Contains("software adapter", StringComparison.Ordinal)
            || value.Contains("software", StringComparison.Ordinal);
    }

    private static bool IsChromeBrand(string brand)
        => string.Equals(brand, "chrome", StringComparison.OrdinalIgnoreCase)
        || string.Equals(brand, "google chrome", StringComparison.OrdinalIgnoreCase);

    private static string AcceptLanguageForCountry(string? country)
        => country?.ToUpperInvariant() switch
        {
            "US" => "en-US,en",
            "GB" => "en-GB,en",
            "CA" => "en-CA,en,fr-CA",
            "AU" => "en-AU,en",
            "NZ" => "en-NZ,en",
            "IE" => "en-IE,en",
            "IN" => "en-IN,en,hi",
            "ZA" => "en-ZA,en",
            "SG" => "en-SG,en",
            "DE" => "de-DE,de,en",
            "AT" => "de-AT,de,en",
            "CH" => "de-CH,de,fr,en",
            "FR" => "fr-FR,fr,en",
            "BE" => "nl-BE,nl,fr,en",
            "NL" => "nl-NL,nl,en",
            "ES" => "es-ES,es,en",
            "MX" => "es-MX,es,en",
            "AR" => "es-AR,es,en",
            "CL" => "es-CL,es,en",
            "CO" => "es-CO,es,en",
            "PT" => "pt-PT,pt,en",
            "BR" => "pt-BR,pt,en",
            "IT" => "it-IT,it,en",
            "PL" => "pl-PL,pl,en",
            "RU" => "ru-RU,ru,en",
            "UA" => "uk-UA,uk,ru,en",
            "SE" => "sv-SE,sv,en",
            "NO" => "nb-NO,no,en",
            "DK" => "da-DK,da,en",
            "FI" => "fi-FI,fi,en",
            "CZ" => "cs-CZ,cs,en",
            "RO" => "ro-RO,ro,en",
            "HU" => "hu-HU,hu,en",
            "GR" => "el-GR,el,en",
            "TR" => "tr-TR,tr,en",
            "IL" => "he-IL,he,en",
            "SA" => "ar-SA,ar,en",
            "AE" => "ar-AE,ar,en",
            "EG" => "ar-EG,ar,en",
            "JP" => "ja-JP,ja,en",
            "KR" => "ko-KR,ko,en",
            "CN" => "zh-CN,zh,en",
            "HK" => "zh-HK,zh,en",
            "TW" => "zh-TW,zh,en",
            "TH" => "th-TH,th,en",
            "VN" => "vi-VN,vi,en",
            "ID" => "id-ID,id,en",
            "MY" => "ms-MY,ms,en",
            "PH" => "en-PH,en,fil",
            _ => DefaultAcceptLanguage,
        };

    internal static Task<string> ExecutablePathAsync(string? executablePath, string? cacheDir, bool quiet, bool autoUpdate)
    {
        if (!string.IsNullOrEmpty(executablePath))
        {
            return Task.FromResult(executablePath);
        }

        return ResolveExecutablePathAsync(cacheDir, quiet, autoUpdate, honorEnvironmentBinary: true);
    }

    internal static Task<string> DownloadAsync(string? cacheDir, bool quiet, bool autoUpdate)
        => ResolveExecutablePathAsync(cacheDir, quiet, autoUpdate, honorEnvironmentBinary: false);

    private static async Task<string> ResolveExecutablePathAsync(string? cacheDir, bool quiet, bool autoUpdate, bool honorEnvironmentBinary)
    {
        var envBinary = honorEnvironmentBinary ? Environment.GetEnvironmentVariable("CLEARCOTE_BINARY") : null;
        if (!string.IsNullOrEmpty(envBinary))
        {
            return envBinary;
        }

        var rel = await ResolveReleaseAsync(quiet, autoUpdate).ConfigureAwait(false);
        var cacheRoot = !string.IsNullOrEmpty(cacheDir) ? cacheDir : DefaultCacheRoot();
        var basePath = Path.Combine(cacheRoot, rel.Tag);
        var browserDir = Path.Combine(basePath, "browser");
        var verifiedPath = Path.Combine(basePath, VerifiedFileName);

        if (File.Exists(verifiedPath))
        {
            var cached = FindFile(browserDir, rel.Binary);
            if (!string.IsNullOrEmpty(cached))
            {
                return cached;
            }
        }

        await _downloadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (File.Exists(verifiedPath))
            {
                var cached = FindFile(browserDir, rel.Binary);
                if (!string.IsNullOrEmpty(cached))
                {
                    return cached;
                }
            }

            return await FetchAndVerifyAsync(rel, basePath, quiet).ConfigureAwait(false);
        }
        finally
        {
            _downloadLock.Release();
        }
    }

    private static async Task<ResolvedRelease> ResolveReleaseAsync(bool quiet, bool autoUpdate)
    {
        var pinned = CurrentRelease();
        if (!autoUpdate)
        {
            return ResolvedRelease.FromPinned(pinned);
        }

        var latest = await ResolveLatestReleaseAsync(pinned, quiet).ConfigureAwait(false);
        if (latest == null || string.Equals(latest.Tag, pinned.Tag, StringComparison.Ordinal))
        {
            return ResolvedRelease.FromPinned(pinned);
        }

        return latest;
    }

    private static async Task<ResolvedRelease?> ResolveLatestReleaseAsync(ReleaseInfo pinned, bool quiet)
    {
        using var client = CreateHttpClient(TimeSpan.FromSeconds(30));
        JsonDocument document;
        try
        {
            using var response = await client.GetAsync(new Uri($"https://api.github.com/repos/{Repo}/releases?per_page=30")).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Log(quiet, $"auto-update: GitHub API returned HTTP {(int)response.StatusCode}; using pinned {pinned.Tag}");
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
        {
            Log(quiet, $"auto-update: could not reach GitHub ({ex.Message}); using pinned {pinned.Tag}");
            return null;
        }

        using (document)
        {
            var releases = new List<GitHubRelease>();
            foreach (var release in document.RootElement.EnumerateArray())
            {
                if (release.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True)
                {
                    continue;
                }

                var tag = GetStringProperty(release, "tag_name");
                if (string.IsNullOrEmpty(tag) || !release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                releases.Add(new GitHubRelease(tag, GetStringProperty(release, "published_at") ?? string.Empty, assets.Clone()));
            }

            releases.Sort(static (left, right) => string.CompareOrdinal(right.PublishedAt, left.PublishedAt));
            foreach (var release in releases)
            {
                var candidate = await TryResolveLatestReleaseAsync(client, pinned, release).ConfigureAwait(false);
                if (candidate != null)
                {
                    return candidate;
                }
            }
        }

        Log(quiet, $"auto-update: no compatible release asset found; using pinned {pinned.Tag}");
        return null;
    }

    private static async Task<ResolvedRelease?> TryResolveLatestReleaseAsync(HttpClient client, ReleaseInfo pinned, GitHubRelease release)
    {
        GitHubAsset? browserAsset = null;
        GitHubAsset? sumsAsset = null;
        GitHubAsset? ascAsset = null;
        GitHubAsset? keyAsset = null;

        foreach (var assetElement in release.Assets.EnumerateArray())
        {
            var name = GetStringProperty(assetElement, "name");
            var url = GetStringProperty(assetElement, "browser_download_url");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                continue;
            }

            var asset = new GitHubAsset(name, url, GetInt64Property(assetElement, "size"));
            if (IsPlatformAsset(name, pinned.AssetGlob))
            {
                browserAsset = asset;
            }
            else if (name == "SHA256SUMS.txt")
            {
                sumsAsset = asset;
            }
            else if (name == "SHA256SUMS.txt.asc")
            {
                ascAsset = asset;
            }
            else if (name == "clearcote-signing-key.asc")
            {
                keyAsset = asset;
            }
        }

        if (browserAsset == null || sumsAsset == null)
        {
            return null;
        }

        string sums;
        try
        {
            sums = await FetchTextAsync(client, sumsAsset.Url).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return null;
        }

        var (archiveSha, exeSha) = ParseSums(sums, browserAsset.Name, pinned.Binary);
        if (string.IsNullOrEmpty(archiveSha))
        {
            return null;
        }

        return new ResolvedRelease(
            Tag: release.Tag,
            Version: ExtractVersion(browserAsset.Name, pinned.AssetGlob) ?? release.Tag,
            Asset: browserAsset.Name,
            Url: browserAsset.Url,
            Sha256: archiveSha,
            ExeSha256: exeSha ?? string.Empty,
            Size: browserAsset.Size,
            Archive: browserAsset.Name.EndsWith(".tar.xz", StringComparison.Ordinal) ? "tar.xz" : "zip",
            Binary: pinned.Binary,
            AssetGlob: pinned.AssetGlob,
            Unpinned: true,
            SumsUrl: sumsAsset.Url,
            AscUrl: ascAsset?.Url,
            KeyUrl: keyAsset?.Url);
    }

    private static HttpClient CreateHttpClient(TimeSpan timeout)
    {
        var client = new HttpClient { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("clearcote-dotnet");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static async Task<string> FetchTextAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(new Uri(url)).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new PlaywrightException($"Clearcote metadata fetch failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase} for {url}");
        }

        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    private static string? GetStringProperty(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static long GetInt64Property(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number) ? number : 0;

    private static bool IsPlatformAsset(string name, string assetGlob)
        => name.StartsWith("clearcote-", StringComparison.Ordinal)
        && (name.EndsWith("-" + assetGlob + ".zip", StringComparison.Ordinal)
            || name.EndsWith("-" + assetGlob + ".tar.xz", StringComparison.Ordinal));

    private static string? ExtractVersion(string assetName, string assetGlob)
    {
        const string prefix = "clearcote-";
        if (!assetName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var suffixZip = "-" + assetGlob + ".zip";
        var suffixTar = "-" + assetGlob + ".tar.xz";
        string suffix;
        if (assetName.EndsWith(suffixZip, StringComparison.Ordinal))
        {
            suffix = suffixZip;
        }
        else if (assetName.EndsWith(suffixTar, StringComparison.Ordinal))
        {
            suffix = suffixTar;
        }
        else
        {
            return null;
        }

        var length = assetName.Length - prefix.Length - suffix.Length;
        return length > 0 ? assetName.Substring(prefix.Length, length) : null;
    }

    private static (string? ArchiveSha, string? ExeSha) ParseSums(string text, string assetName, string binaryName)
    {
        string? archiveSha = null;
        string? exeSha = null;
        foreach (var raw in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length < 66)
            {
                continue;
            }

            var hash = line.Substring(0, 64);
            if (!IsHexSha256(hash))
            {
                continue;
            }

            var path = line.Substring(64).TrimStart();
            if (path.StartsWith('*'))
            {
                path = path.Substring(1);
            }

            var fileName = Path.GetFileName(path.Replace('\\', Path.DirectorySeparatorChar));
            if (fileName == assetName)
            {
                archiveSha = hash.ToLowerInvariant();
            }
            else if (fileName == binaryName)
            {
                exeSha = hash.ToLowerInvariant();
            }
        }

        return (archiveSha, exeSha);
    }

    private static bool IsHexSha256(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (var ch in value)
        {
            var isHex = (ch >= '0' && ch <= '9')
                || (ch >= 'a' && ch <= 'f')
                || (ch >= 'A' && ch <= 'F');
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool AutoUpdateRequested(bool? option)
    {
        if (option.HasValue)
        {
            return option.Value;
        }

        var env = Environment.GetEnvironmentVariable("CLEARCOTE_AUTO_UPDATE");
        return string.Equals(env, "1", StringComparison.Ordinal) || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<GpgVerdict> GpgVerifyAsync(HttpClient client, ResolvedRelease rel, string sumsBody, string tempRoot, bool quiet)
    {
        if (string.IsNullOrEmpty(rel.AscUrl) || string.IsNullOrEmpty(rel.KeyUrl) || !HasGpg())
        {
            if (!quiet)
            {
                Log(quiet, "auto-update: gpg not found or signature assets missing; skipping signature check");
            }

            return GpgVerdict.Skipped;
        }

        var home = Path.Combine(tempRoot, "ccgpg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(home);
        var keyPath = Path.Combine(home, "key.asc");
        var sumsPath = Path.Combine(home, "SHA256SUMS.txt");
        var ascPath = Path.Combine(home, "SHA256SUMS.txt.asc");
        try
        {
            File.WriteAllText(sumsPath, sumsBody);
            await File.WriteAllTextAsync(keyPath, await FetchTextAsync(client, rel.KeyUrl).ConfigureAwait(false)).ConfigureAwait(false);
            await File.WriteAllTextAsync(ascPath, await FetchTextAsync(client, rel.AscUrl).ConfigureAwait(false)).ConfigureAwait(false);

            if (RunGpg(home, "--import", keyPath).ExitCode != 0)
            {
                return GpgVerdict.Failed;
            }

            var fingerprint = RunGpg(home, "--with-colons", "--fingerprint");
            if (fingerprint.ExitCode != 0 || !ContainsSigningFingerprint(fingerprint.Stdout))
            {
                return GpgVerdict.Failed;
            }

            var verified = RunGpg(home, "--verify", ascPath, sumsPath);
            return verified.ExitCode == 0 ? GpgVerdict.Ok : GpgVerdict.Failed;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or TaskCanceledException)
        {
            Log(quiet, $"auto-update: GPG verification failed before signature check ({ex.Message})");
            return GpgVerdict.Failed;
        }
        finally
        {
            try
            {
                Directory.Delete(home, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log(quiet, $"auto-update: could not remove temporary GPG directory: {ex.Message}");
            }
        }
    }

    private static bool HasGpg()
    {
        try
        {
            return RunProcess("gpg", "--version").ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunGpg(string home, params string[] args)
    {
        var allArgs = new List<string> { "--homedir", home, "--batch" };
        allArgs.AddRange(args);
        return RunProcess("gpg", allArgs.ToArray());
    }

    private static (int ExitCode, string Stdout, string Stderr) RunProcess(string fileName, params string[] args)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi) ?? throw new PlaywrightException($"Could not start {fileName}.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdout, stderr);
    }

    private static bool ContainsSigningFingerprint(string gpgColonOutput)
    {
        foreach (var line in gpgColonOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith("fpr:", StringComparison.Ordinal))
            {
                continue;
            }

            var fields = line.Split(':');
            if (fields.Length > 9 && string.Equals(fields[9], SigningKeyFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static ReleaseInfo CurrentRelease()
    {
        if (OperatingSystem.IsWindows() && System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
        {
            return _windows;
        }

        if (OperatingSystem.IsLinux() && System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
        {
            return _linux;
        }

        throw new PlaywrightException("Clearcote currently ships Windows x64 and Linux x64 binaries. Pass ExecutablePath or set CLEARCOTE_BINARY to use a compatible build.");
    }

    private static string DefaultCacheRoot()
    {
        var env = Environment.GetEnvironmentVariable("CLEARCOTE_CACHE");
        if (!string.IsNullOrEmpty(env))
        {
            return env;
        }

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local))
            {
                local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local");
            }

            return Path.Combine(local, "clearcote", "Cache");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches", "clearcote");
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        return Path.Combine(string.IsNullOrEmpty(xdg) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache") : xdg, "clearcote");
    }

    private static async Task<string> FetchAndVerifyAsync(ResolvedRelease rel, string basePath, bool quiet)
    {
        Directory.CreateDirectory(basePath);
        var browserDir = Path.Combine(basePath, "browser");
        var archivePath = Path.Combine(basePath, rel.Asset);

        if (Directory.Exists(browserDir))
        {
            Directory.Delete(browserDir, recursive: true);
        }

        Log(quiet, $"fetching Clearcote {rel.Version} ({rel.Tag}, ~{rel.Size / 1_000_000} MB)");
        using var client = CreateHttpClient(TimeSpan.FromMinutes(10));
        await DownloadToAsync(client, rel.Url, archivePath, rel.Size, quiet).ConfigureAwait(false);

        Log(quiet, "verifying SHA-256");
        var archiveHash = await Sha256FileAsync(archivePath).ConfigureAwait(false);
        if (!string.Equals(archiveHash, rel.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(archivePath);
            throw new PlaywrightException($"Clearcote archive SHA-256 mismatch. Expected {rel.Sha256}, got {archiveHash}.");
        }

        if (rel.Unpinned && !string.IsNullOrEmpty(rel.AscUrl) && !string.IsNullOrEmpty(rel.KeyUrl) && !string.IsNullOrEmpty(rel.SumsUrl))
        {
            var sumsBody = await FetchTextAsync(client, rel.SumsUrl).ConfigureAwait(false);
            var verdict = await GpgVerifyAsync(client, rel, sumsBody, basePath, quiet).ConfigureAwait(false);
            if (verdict == GpgVerdict.Failed)
            {
                File.Delete(archivePath);
                throw new PlaywrightException($"Clearcote {rel.Tag}: GPG signature verification failed against the pinned key {SigningKeyFingerprint}.");
            }

            if (verdict == GpgVerdict.Ok)
            {
                Log(quiet, $"auto-update: GPG signature OK (key {SigningKeyFingerprint})");
            }
        }

        Log(quiet, "extracting");
        Directory.CreateDirectory(browserDir);
        await ExtractArchiveAsync(rel, archivePath, browserDir).ConfigureAwait(false);

        var exe = FindFile(browserDir, rel.Binary);
        if (string.IsNullOrEmpty(exe))
        {
            throw new PlaywrightException($"Clearcote archive verified but {rel.Binary} was not found inside it.");
        }

        var exeHash = await Sha256FileAsync(exe).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(rel.ExeSha256) && !string.Equals(exeHash, rel.ExeSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new PlaywrightException($"Clearcote {rel.Binary} SHA-256 mismatch. Expected {rel.ExeSha256}, got {exeHash}.");
        }

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                Log(quiet, $"could not chmod Clearcote binary: {ex.Message}");
            }
        }

        File.WriteAllText(Path.Combine(basePath, VerifiedFileName), rel.Sha256 + Environment.NewLine);
        File.Delete(archivePath);
        Log(quiet, "ready: " + exe);
        return exe;
    }

    private static async Task DownloadToAsync(HttpClient client, string url, string destination, long expectedSize, bool quiet)
    {
        using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new PlaywrightException($"Clearcote download failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase} for {url}");
        }

        var total = response.Content.Headers.ContentLength ?? expectedSize;
        using var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var output = File.Create(destination);
        var buffer = new byte[1024 * 128];
        long seen = 0;
        var lastPct = -1;
        while (true)
        {
            var read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await output.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            seen += read;
            if (!quiet && total > 0)
            {
                var pct = (int)(seen * 100 / total);
                if (pct != lastPct && pct % 10 == 0)
                {
                    lastPct = pct;
                    await Console.Error.WriteLineAsync($"[clearcote] downloading {pct}% ({seen / 1_000_000}/{total / 1_000_000} MB)").ConfigureAwait(false);
                }
            }
        }
    }

    private static async Task ExtractArchiveAsync(ResolvedRelease rel, string archivePath, string destination)
    {
        if (rel.Archive == "zip")
        {
            ZipFile.ExtractToDirectory(archivePath, destination);
            return;
        }

        var psi = new ProcessStartInfo("tar")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        psi.ArgumentList.Add("-xf");
        psi.ArgumentList.Add(archivePath);
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(destination);

        using var process = Process.Start(psi) ?? throw new PlaywrightException("Could not start tar to extract Clearcote archive.");
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new PlaywrightException($"tar failed to extract Clearcote archive: {await stderrTask.ConfigureAwait(false)}");
        }
    }

    private static async Task<string> Sha256FileAsync(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = await sha.ComputeHashAsync(stream).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? FindFile(string root, string fileName)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(current);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (Directory.Exists(entry))
                {
                    pending.Push(entry);
                }
                else if (string.Equals(Path.GetFileName(entry), fileName, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    return entry;
                }
            }
        }

        return null;
    }

    private static void Log(bool quiet, string message)
    {
        if (!quiet && Environment.GetEnvironmentVariable("CLEARCOTE_NO_WARN") != "1")
        {
            Console.Error.WriteLine("[clearcote] " + message);
        }
    }

    internal sealed record LaunchPatch(
        string ExecutablePath,
        IEnumerable<string> Args,
        Proxy? Proxy,
        IEnumerable<string>? IgnoreDefaultArgs,
        bool NoDefaultViewport,
        bool Headed,
        bool Humanize,
        bool ShowCursor);

    private sealed record ReleaseInfo(
        string Tag,
        string Version,
        string Asset,
        string Url,
        string Sha256,
        string ExeSha256,
        long Size,
        string Archive,
        string Binary,
        string AssetGlob);

    private sealed record ResolvedRelease(
        string Tag,
        string Version,
        string Asset,
        string Url,
        string Sha256,
        string ExeSha256,
        long Size,
        string Archive,
        string Binary,
        string AssetGlob,
        bool Unpinned,
        string? SumsUrl,
        string? AscUrl,
        string? KeyUrl)
    {
        internal static ResolvedRelease FromPinned(ReleaseInfo release)
            => new(
                release.Tag,
                release.Version,
                release.Asset,
                release.Url,
                release.Sha256,
                release.ExeSha256,
                release.Size,
                release.Archive,
                release.Binary,
                release.AssetGlob,
                false,
                null,
                null,
                null);
    }

    private sealed record GitHubRelease(string Tag, string PublishedAt, JsonElement Assets);

    private sealed record GitHubAsset(string Name, string Url, long Size);

    private sealed record ClearcoteGeo(string? Ip, string? Country, string? Timezone, string? AcceptLanguage, string? Location);

    private sealed class ClearcoteSettings
    {
        internal string? Fingerprint { get; private init; }

        internal string? ClearcotePlatform { get; private init; }

        internal string? PlatformVersion { get; private init; }

        internal string? Brand { get; private init; }

        internal string? BrandVersion { get; private init; }

        internal string? GpuVendor { get; private init; }

        internal string? GpuRenderer { get; private init; }

        internal int? HardwareConcurrency { get; private init; }

        internal string? Location { get; private init; }

        internal string? Timezone { get; private init; }

        internal string? AcceptLanguage { get; private init; }

        internal string? WebrtcIp { get; private init; }

        internal string? TlsProfile { get; private init; }

        internal bool? DisableGpuFingerprint { get; private init; }

        internal bool? FingerprintNoise { get; private init; }

        internal string? FingerprintProfile { get; private init; }

        internal long? StorageQuota { get; private init; }

        internal ClearcoteCanvasBridgeOptions? CanvasBridge { get; private init; }

        internal bool? DisablePrivacySandbox { get; private init; }

        internal IEnumerable<string>? Extensions { get; private init; }

        internal bool Geoip { get; private init; }

        internal string? CacheDir { get; private init; }

        internal bool Quiet { get; private init; }

        internal bool AutoUpdate { get; private init; }

        internal bool Humanize { get; private init; }

        internal bool ShowCursor { get; private init; }

        internal string? AgentLlmUrl { get; private init; }

        internal string? AgentLlmKey { get; private init; }

        internal string? AgentModel { get; private init; }

        internal string? AgentToolMode { get; private init; }

        internal string? AgentTyping { get; private init; }

        internal static ClearcoteSettings From(BrowserTypeLaunchOptions options)
            => options is ClearcoteLaunchOptions clearcote ? FromClearcote(clearcote) : FromEnvironment();

        internal static ClearcoteSettings From(BrowserTypeLaunchPersistentContextOptions options)
            => options is ClearcoteLaunchPersistentContextOptions clearcote ? FromClearcote(clearcote) : FromEnvironment();

        private static ClearcoteSettings FromEnvironment()
            => new() { AutoUpdate = AutoUpdateRequested(null) };

        internal ClearcoteSettings WithGeo(ClearcoteGeo geo)
            => new()
            {
                Fingerprint = Fingerprint,
                ClearcotePlatform = ClearcotePlatform,
                PlatformVersion = PlatformVersion,
                Brand = Brand,
                BrandVersion = BrandVersion,
                GpuVendor = GpuVendor,
                GpuRenderer = GpuRenderer,
                HardwareConcurrency = HardwareConcurrency,
                Location = Location ?? geo.Location,
                Timezone = Timezone ?? geo.Timezone,
                AcceptLanguage = AcceptLanguage ?? geo.AcceptLanguage,
                WebrtcIp = WebrtcIp ?? geo.Ip,
                TlsProfile = TlsProfile,
                DisableGpuFingerprint = DisableGpuFingerprint,
                FingerprintNoise = FingerprintNoise,
                FingerprintProfile = FingerprintProfile,
                StorageQuota = StorageQuota,
                CanvasBridge = CanvasBridge,
                DisablePrivacySandbox = DisablePrivacySandbox,
                Extensions = Extensions,
                Geoip = Geoip,
                CacheDir = CacheDir,
                Quiet = Quiet,
                AutoUpdate = AutoUpdate,
                Humanize = Humanize,
                ShowCursor = ShowCursor,
                AgentLlmUrl = AgentLlmUrl,
                AgentLlmKey = AgentLlmKey,
                AgentModel = AgentModel,
                AgentToolMode = AgentToolMode,
                AgentTyping = AgentTyping,
            };

        private static ClearcoteSettings FromClearcote(ClearcoteLaunchOptions options)
            => new()
            {
                Fingerprint = options.Fingerprint,
                ClearcotePlatform = options.ClearcotePlatform,
                PlatformVersion = options.PlatformVersion,
                Brand = options.Brand,
                BrandVersion = options.BrandVersion,
                GpuVendor = options.GpuVendor,
                GpuRenderer = options.GpuRenderer,
                HardwareConcurrency = options.HardwareConcurrency,
                Location = options.Location,
                Timezone = options.Timezone,
                AcceptLanguage = options.AcceptLanguage,
                WebrtcIp = options.WebrtcIp,
                TlsProfile = options.TlsProfile,
                DisableGpuFingerprint = options.DisableGpuFingerprint,
                FingerprintNoise = options.FingerprintNoise,
                FingerprintProfile = options.FingerprintProfile,
                StorageQuota = options.StorageQuota,
                CanvasBridge = options.CanvasBridge,
                DisablePrivacySandbox = options.DisablePrivacySandbox,
                Extensions = options.Extensions,
                Geoip = options.Geoip == true,
                CacheDir = options.CacheDir,
                Quiet = options.Quiet == true,
                AutoUpdate = AutoUpdateRequested(options.AutoUpdate),
                Humanize = options.Humanize == true,
                ShowCursor = options.ShowCursor == true,
                AgentLlmUrl = options.AgentLlmUrl,
                AgentLlmKey = options.AgentLlmKey,
                AgentModel = options.AgentModel,
                AgentToolMode = options.AgentToolMode,
                AgentTyping = options.AgentTyping,
            };

        private static ClearcoteSettings FromClearcote(ClearcoteLaunchPersistentContextOptions options)
            => new()
            {
                Fingerprint = options.Fingerprint,
                ClearcotePlatform = options.ClearcotePlatform,
                PlatformVersion = options.PlatformVersion,
                Brand = options.Brand,
                BrandVersion = options.BrandVersion,
                GpuVendor = options.GpuVendor,
                GpuRenderer = options.GpuRenderer,
                HardwareConcurrency = options.HardwareConcurrency,
                Location = options.Location,
                Timezone = options.Timezone,
                AcceptLanguage = options.AcceptLanguage,
                WebrtcIp = options.WebrtcIp,
                TlsProfile = options.TlsProfile,
                DisableGpuFingerprint = options.DisableGpuFingerprint,
                FingerprintNoise = options.FingerprintNoise,
                FingerprintProfile = options.FingerprintProfile,
                StorageQuota = options.StorageQuota,
                CanvasBridge = options.CanvasBridge,
                DisablePrivacySandbox = options.DisablePrivacySandbox,
                Extensions = options.Extensions,
                Geoip = options.Geoip == true,
                CacheDir = options.CacheDir,
                Quiet = options.Quiet == true,
                AutoUpdate = AutoUpdateRequested(options.AutoUpdate),
                Humanize = options.Humanize == true,
                ShowCursor = options.ShowCursor == true,
                AgentLlmUrl = options.AgentLlmUrl,
                AgentLlmKey = options.AgentLlmKey,
                AgentModel = options.AgentModel,
                AgentToolMode = options.AgentToolMode,
                AgentTyping = options.AgentTyping,
            };
    }
}
