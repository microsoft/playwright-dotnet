using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Helpers;

internal static partial class Clearcote
{
    internal const string RunTokenEnv = "CLEARCOTE_RUN_TOKEN";
    private const string LicenseApiDefault = "https://www.clearcotelabs.com";

    internal static string? ResolveLicenseKey(string? explicitKey = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            return explicitKey.Trim();
        }

        var env = Environment.GetEnvironmentVariable("CLEARCOTE_LICENSE_KEY");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var p = Path.Combine(home, ".clearcote", "license.key");
            if (File.Exists(p))
            {
                var v = File.ReadAllText(p).Trim();
                if (v.Length > 0)
                {
                    return v;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    internal static string ResolveInstanceId()
    {
        var env = Environment.GetEnvironmentVariable("CLEARCOTE_INSTANCE_ID");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = Path.Combine(home, ".clearcote");
        var p = Path.Combine(dir, "instance_id");
        try
        {
            if (File.Exists(p))
            {
                var v = File.ReadAllText(p).Trim();
                if (v.Length > 0)
                {
                    return v;
                }
            }
        }
        catch
        {
        }

        var id = Guid.NewGuid().ToString();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(p, id + "\n");
        }
        catch
        {
        }

        return id;
    }

    private static string LicenseApiBase(string? apiBase)
        => (apiBase ?? Environment.GetEnvironmentVariable("CLEARCOTE_LICENSE_API") ?? LicenseApiDefault).TrimEnd('/');

    private static string LicenseCachePath(string licenseKey)
    {
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(licenseKey))).ToLowerInvariant()[..16];
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".clearcote", $"lease-{id}.json");
    }

    private static (string Token, long Exp)? LicenseReadCache(string licenseKey)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(LicenseCachePath(licenseKey)));
            var root = doc.RootElement;
            if (root.TryGetProperty("token", out var t) && t.ValueKind == JsonValueKind.String
                && root.TryGetProperty("exp", out var e) && e.TryGetInt64(out var exp))
            {
                return (t.GetString()!, exp);
            }
        }
        catch
        {
        }

        return null;
    }

    private static void LicenseWriteCache(string licenseKey, string token, long exp)
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Directory.CreateDirectory(Path.Combine(home, ".clearcote"));
            File.WriteAllText(
                LicenseCachePath(licenseKey),
                new JsonObject { ["token"] = token, ["exp"] = exp }.ToJsonString());
        }
        catch
        {
        }
    }

    private static async Task<HttpResponseMessage> LicensePostJsonAsync(string url, string licenseKey, JsonObject body)
    {
        using var client = CreateHttpClient(TimeSpan.FromSeconds(15));
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", licenseKey);
        return await client.SendAsync(req).ConfigureAwait(false);
    }

    private static async Task LicenseThrowForStatusAsync(HttpResponseMessage res)
    {
        string? error = null, code = null;
        try
        {
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
            if (doc.RootElement.TryGetProperty("error", out var e))
            {
                error = e.GetString();
            }

            if (doc.RootElement.TryGetProperty("code", out var c))
            {
                code = c.GetString();
            }
        }
        catch
        {
        }

        var status = (int)res.StatusCode;
        var msg = error ?? $"License request failed ({status}).";
        if (status == 429 || code == "CONCURRENCY_LIMIT_EXCEEDED")
        {
            throw new ConcurrencyLimitError(msg);
        }

        if (status == 403 || code == "LICENSE_REVOKED" || code == "LICENSE_EXPIRED")
        {
            throw new LicenseRevokedError(msg);
        }

        throw new LicenseError(msg, code ?? $"HTTP_{status}");
    }

    private static long NowSec() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    internal static async Task<LeaseSession?> AcquireLeaseAsync(
        string? licenseKey, string? apiBase, string? sdkVersion = null, bool quiet = false)
    {
        var resolvedKey = ResolveLicenseKey(licenseKey);
        if (resolvedKey is null)
        {
            return null;
        }

        var baseUrl = LicenseApiBase(apiBase);
        var instanceId = ResolveInstanceId();
        void Warn(string m)
        {
            if (!quiet)
            {
                Console.Error.WriteLine($"[clearcote] [license] {m}");
            }
        }

        JsonElement checkout;
        try
        {
            using var res = await LicensePostJsonAsync(
                $"{baseUrl}/api/v1/lease/checkout",
                resolvedKey,
                new JsonObject { ["instance_id"] = instanceId, ["os"] = OsTag(), ["sdk_version"] = sdkVersion }).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                await LicenseThrowForStatusAsync(res).ConfigureAwait(false);
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
            checkout = doc.RootElement.Clone();
            LicenseWriteCache(resolvedKey, checkout.GetProperty("token").GetString()!, checkout.GetProperty("exp").GetInt64());
        }
        catch (LicenseError)
        {
            throw;
        }
        catch (ConcurrencyLimitError)
        {
            throw;
        }
        catch (LicenseRevokedError)
        {
            throw;
        }
        catch (Exception e)
        {
            var cached = LicenseReadCache(resolvedKey);
            if (cached is { } c && c.Exp > NowSec() + 60)
            {
                Warn($"backend unreachable ({e.Message}); using cached run-token (offline grace).");
                return new LeaseSession(c.Token, "cached", static () => Task.CompletedTask);
            }
            throw new LicenseError($"Could not reach the license server and no valid cached token: {e.Message}");
        }

        var leaseId = checkout.GetProperty("lease_id").GetString()!;
        var token = checkout.GetProperty("token").GetString()!;
        var hbSec = checkout.TryGetProperty("heartbeat_interval_sec", out var hb) && hb.TryGetInt32(out var v) ? v : 30;
        var hbMs = Math.Max(5, hbSec) * 1000;

        var cts = new CancellationTokenSource();
        LeaseSession session = null!;
        async Task HeartbeatAsync()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(hbMs, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    using var res = await LicensePostJsonAsync(
                        $"{baseUrl}/api/v1/lease/heartbeat",
                        resolvedKey,
                        new JsonObject { ["lease_id"] = session.LeaseId, ["nonce"] = Guid.NewGuid().ToString() }).ConfigureAwait(false);
                    if ((int)res.StatusCode == 409)
                    {
                        using var co = await LicensePostJsonAsync(
                            $"{baseUrl}/api/v1/lease/checkout",
                            resolvedKey,
                            new JsonObject { ["instance_id"] = instanceId, ["os"] = OsTag(), ["sdk_version"] = sdkVersion }).ConfigureAwait(false);
                        if (co.IsSuccessStatusCode)
                        {
                            using var d = JsonDocument.Parse(await co.Content.ReadAsStringAsync().ConfigureAwait(false));
                            session.LeaseId = d.RootElement.GetProperty("lease_id").GetString()!;
                            session.SetToken(d.RootElement.GetProperty("token").GetString()!);
                            LicenseWriteCache(resolvedKey, session.Token, d.RootElement.GetProperty("exp").GetInt64());
                        }
                        continue;
                    }
                    if (res.IsSuccessStatusCode)
                    {
                        using var d = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
                        session.SetToken(d.RootElement.GetProperty("token").GetString()!);
                        LicenseWriteCache(resolvedKey, session.Token, d.RootElement.GetProperty("exp").GetInt64());
                    }
                }
                catch
                {
                }
            }
        }

        async Task StopAsync()
        {
            await cts.CancelAsync().ConfigureAwait(false);
            try
            {
                using var resp = await LicensePostJsonAsync(
                    $"{baseUrl}/api/v1/lease/checkin",
                    resolvedKey,
                    new JsonObject { ["lease_id"] = session.LeaseId }).ConfigureAwait(false);
            }
            catch
            {
            }

            cts.Dispose();
        }

        session = new LeaseSession(token, leaseId, StopAsync);
        _ = Task.Run(HeartbeatAsync);
        return session;
    }

    internal static Dictionary<string, string?> WithRunToken(string token, IReadOnlyDictionary<string, string?>? baseEnv)
    {
        var outEnv = new Dictionary<string, string?>();
        if (baseEnv is not null)
        {
            foreach (var (k, v) in baseEnv)
            {
                if (v is not null)
                {
                    outEnv[k] = v;
                }
            }
        }
        else
        {
            foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                if (e.Value is string sv)
                {
                    outEnv[(string)e.Key] = sv;
                }
            }
        }

        outEnv[RunTokenEnv] = token;
        return outEnv;
    }

    private static string OsTag()
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

        return "unknown";
    }
}

/// <summary>A definitive licensing failure. Never silently downgraded to the free build.</summary>
public class LicenseError : Exception
{
    private string _code = "LICENSE_ERROR";

    public LicenseError()
    {
    }

    public LicenseError(string message)
        : base(message)
    {
    }

    public LicenseError(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LicenseError(string message, string code)
        : base(message)
    {
        _code = code;
    }

    public string Code => _code;
}

/// <summary>The license has no free concurrency slot right now.</summary>
public sealed class ConcurrencyLimitError : Exception
{
    private const string CodeValue = "CONCURRENCY_LIMIT_EXCEEDED";

    public ConcurrencyLimitError()
    {
    }

    public ConcurrencyLimitError(string message)
        : base(message)
    {
    }

    public ConcurrencyLimitError(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string Code => CodeValue;
}

/// <summary>The license was revoked or has expired.</summary>
public sealed class LicenseRevokedError : Exception
{
    private const string CodeValue = "LICENSE_REVOKED";

    public LicenseRevokedError()
    {
    }

    public LicenseRevokedError(string message)
        : base(message)
    {
    }

    public LicenseRevokedError(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string Code => CodeValue;
}

/// <summary>A live floating-concurrency lease. Keep it until the browser closes, then call <see cref="LeaseSession.StopAsync"/>.</summary>
public sealed class LeaseSession
{
    private readonly Func<Task> _stop;
    private volatile string _token;
    private int _stopped;

    internal LeaseSession(string token, string leaseId, Func<Task> stop)
    {
        _token = token;
        LeaseId = leaseId;
        _stop = stop;
    }

    public string Token => _token;

    public string LeaseId { get; internal set; }

    internal void SetToken(string token)
    {
        _token = token;
    }

    public Task StopAsync()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return Task.CompletedTask;
        }

        return _stop();
    }
}
