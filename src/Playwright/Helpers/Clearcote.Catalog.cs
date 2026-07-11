using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Helpers;

internal static partial class Clearcote
{
    public const string CatalogUrl = "https://www.clearcotelabs.com/api/v1/versions";

    private static string? PlatKey()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux";
        }

        return null;
    }

    private static int VerCmp(string a, string b)
    {
        int[] Parts(string v)
        {
            var matches = Regex.Matches(v ?? string.Empty, @"\d+");
            return matches.Take(4).Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture)).ToArray();
        }

        var x = Parts(a);
        var y = Parts(b);
        for (var i = 0; i < 4; i++)
        {
            var d = (i < x.Length ? x[i] : 0) - (i < y.Length ? y[i] : 0);
            if (d != 0)
            {
                return d;
            }
        }

        return 0;
    }

    private static async Task<Catalog> FetchCatalogAsync(bool quiet)
    {
        try
        {
            var json = await FetchTextAsync(CreateHttpClient(TimeSpan.FromSeconds(30)), CatalogUrl).ConfigureAwait(false);
            var cat = JsonSerializer.Deserialize(json, ClearcoteJsonContext.Default.Catalog);
            if (cat is { Builds.Count: > 0 })
            {
                return cat;
            }
        }
        catch (Exception e)
        {
            Log(quiet, $"version catalog unreachable ({e.Message}); using the bundled snapshot");
        }

        return CatalogFallback;
    }

    public static async Task<VersionPlan> ResolveVersionAsync(string selector, bool hasLicense, bool quiet = false)
    {
        var cat = await FetchCatalogAsync(quiet).ConfigureAwait(false);
        return ResolveFromCatalog(cat, selector, hasLicense);
    }

    public static VersionPlan ResolveFromCatalog(Catalog cat, string selector, bool hasLicense)
    {
        var plat = PlatKey() ?? throw new PlaywrightException("Clearcote ships Windows x64 and Linux x64 only.");
        var builds = cat.Builds.Where(b => b.Platforms.ContainsKey(plat)).ToList();
        var sel = (selector ?? string.Empty).Trim();

        List<CatalogBuild> cands;
        if (Regex.IsMatch(sel, "^(latest|newest)$", RegexOptions.IgnoreCase))
        {
            cands = builds.Where(b => b.Tier == "free" || hasLicense).ToList();
        }
        else if (Regex.IsMatch(sel, @"^\d+$"))
        {
            cands = builds.Where(b => b.Major.ToString(CultureInfo.InvariantCulture) == sel).ToList();
        }
        else
        {
            cands = builds.Where(b => b.Version == sel).ToList();
        }

        if (cands.Count == 0)
        {
            var avail = string.Join(", ", builds.Select(b => $"{b.Version} ({b.Tier})"));
            throw new PlaywrightException($"No Clearcote build matches version '{selector}' for {plat}. Available: {(avail.Length > 0 ? avail : "none")}.");
        }

        var pick = cands.Aggregate((a, b) => VerCmp(b.Version, a.Version) > 0 ? b : a);

        if (pick.Tier == "pro" && !hasLicense)
        {
            var free = string.Join(", ", builds.Where(b => b.Tier == "free").Select(b => b.Version));
            throw new PlaywrightException(
                $"Clearcote {pick.Version} is a PRO build and isn't public yet — set a license key (CLEARCOTE_LICENSE_KEY, or pass LicenseKey) to use it.\n" +
                $"  Free versions you can use without a key: {(free.Length > 0 ? free : "none")}.");
        }

        if (pick.Tier == "pro")
        {
            return new VersionPlan("pro", null, pick.Version);
        }

        var p = pick.Platforms[plat];
        if (string.IsNullOrEmpty(p.Url) || string.IsNullOrEmpty(p.Sha256))
        {
            throw new PlaywrightException($"Clearcote {pick.Version} is marked free but the catalog has no download for {plat}.");
        }

        var rel = new ResolvedRelease(
            Tag: string.IsNullOrEmpty(pick.Tag) ? $"v-{pick.Version}" : pick.Tag,
            Version: pick.Version,
            Asset: p.Asset ?? $"clearcote-{pick.Version}-{plat}-x64.{(p.Archive == "zip" ? "zip" : "tar.xz")}",
            Url: p.Url!,
            Sha256: p.Sha256!,
            ExeSha256: p.ExeSha256 ?? string.Empty,
            Size: p.Size,
            Archive: p.Archive,
            Binary: p.Binary,
            AssetGlob: $"{plat}-x64",
            AscUrl: null,
            KeyUrl: null,
            SumsUrl: null,
            Unpinned: false);
        return new VersionPlan("free", rel, null);
    }

    public static async Task<string> EnsureVersionAsync(
        string selector,
        string? licenseKey = null,
        string? apiBase = null,
        string? cacheDir = null,
        bool quiet = false)
    {
        var plan = await ResolveVersionAsync(selector, !string.IsNullOrEmpty(licenseKey), quiet).ConfigureAwait(false);
        if (plan.Kind == "pro")
        {
            return await ProEnsureBinaryAsync(
                licenseKey!,
                new ProDownloadOptions { ApiBase = apiBase, CacheDir = cacheDir, Quiet = quiet, Version = plan.Version }).ConfigureAwait(false);
        }

        var rel = plan.Rel!;
        var @base = Path.Combine(cacheDir ?? DefaultCacheRoot(), rel.Tag);
        if (File.Exists(Path.Combine(@base, VerifiedFileName)))
        {
            var cached = FindFile(Path.Combine(@base, "browser"), rel.Binary);
            if (cached is not null)
            {
                return cached;
            }
        }

        return await FetchAndVerifyAsync(rel, @base, quiet).ConfigureAwait(false);
    }

    public static async Task<string> ProEnsureBinaryAsync(string licenseKey, ProDownloadOptions? opts = null)
    {
        opts ??= new ProDownloadOptions();
        var baseUrl = (opts.ApiBase ?? Environment.GetEnvironmentVariable("CLEARCOTE_LICENSE_API") ?? "https://www.clearcotelabs.com").TrimEnd('/');
        var plat = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsLinux() ? "linux" : null;
        if (plat is null)
        {
            throw new PlaywrightException("Clearcote PRO ships Windows x64 and Linux x64 only.");
        }

        using var client = CreateHttpClient(TimeSpan.FromSeconds(30));
        var proUrl = $"{baseUrl}/api/v1/download/pro?platform={plat}";
        if (!string.IsNullOrEmpty(opts.Version))
        {
            proUrl += $"&version={Uri.EscapeDataString(opts.Version)}";
        }

        var req = new HttpRequestMessage(HttpMethod.Get, proUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", licenseKey);
        req.Headers.UserAgent.ParseAdd("clearcote-sdk");
        using var res = await client.SendAsync(req).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (body.Length > 200)
            {
                body = body[..200];
            }

            throw new PlaywrightException($"Clearcote PRO download not authorized (HTTP {(int)res.StatusCode}): {body}\n" +
                                "Check your license key and that your plan is active.");
        }

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
        var meta = doc.RootElement;
        string? Get(string k) => meta.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        var url = Get("url");
        var sha = Get("sha256");
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(sha))
        {
            throw new PlaywrightException($"Clearcote PRO build is not currently available for {plat} (the server returned no download).");
        }

        var version = Get("version") ?? string.Empty;
        var rel = new ResolvedRelease(
            Tag: Get("tag") ?? $"pro-{version}",
            Version: version,
            Asset: Get("asset") ?? $"clearcote-pro-{version}-{plat}-x64.{(plat == "windows" ? "zip" : "tar.xz")}",
            Url: url!,
            Sha256: sha!,
            ExeSha256: Get("exe_sha256") ?? string.Empty,
            Size: meta.TryGetProperty("size", out var s) && s.TryGetInt64(out var sv) ? sv : 0,
            Archive: Get("archive") ?? (plat == "windows" ? "zip" : "tar.xz"),
            Binary: Get("binary") ?? (plat == "windows" ? "chrome.exe" : "chrome"),
            AssetGlob: $"{plat}-x64",
            AscUrl: null,
            KeyUrl: null,
            SumsUrl: null,
            Unpinned: false);

        var @base = Path.Combine(opts.CacheDir ?? DefaultCacheRoot(), rel.Tag);
        if (File.Exists(Path.Combine(@base, VerifiedFileName)))
        {
            var cached = FindFile(Path.Combine(@base, "browser"), rel.Binary);
            if (cached is not null)
            {
                return cached;
            }
        }

        return await FetchAndVerifyAsync(rel, @base, opts.Quiet).ConfigureAwait(false);
    }
}

/// <summary>A resolved version plan: a free release to download, or a pro version to fetch via the licensed route.</summary>
public sealed record VersionPlan(string Kind, ResolvedRelease? Rel, string? Version);

/// <summary>Options for PRO browser download.</summary>
public sealed class ProDownloadOptions
{
    public string? ApiBase { get; set; }

    public string? CacheDir { get; set; }

    public bool Quiet { get; set; }

    public string? Version { get; set; }
}

/// <summary>One platform's download info within a <see cref="CatalogBuild"/>.</summary>
public sealed record CatalogPlatform
{
    public string? Asset { get; init; }

    public string? Url { get; init; }

    public string? Sha256 { get; init; }

    public string? ExeSha256 { get; init; }

    public long Size { get; init; }

    public string Archive { get; init; } = "zip";

    public string Binary { get; init; } = "chrome";
}

/// <summary>One published build in the version catalog.</summary>
public sealed record CatalogBuild
{
    public int Major { get; init; }

    public string Version { get; init; } = string.Empty;

    public string Tier { get; init; } = "free";

    public string Tag { get; init; } = string.Empty;

    public Dictionary<string, CatalogPlatform> Platforms { get; init; } = new();
}

/// <summary>The public version catalog (the SDK fetches this to answer Version selectors).</summary>
public sealed record Catalog
{
    public int Schema { get; init; } = 1;

    public List<CatalogBuild> Builds { get; init; } = new();
}
