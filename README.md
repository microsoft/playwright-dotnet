# Playwright for .NET — NativeAOT

![Chromium 149](https://img.shields.io/badge/Chromium-149.0.7827-45ba4b)
![Firefox 151](https://img.shields.io/badge/Firefox-151.0-45ba4b)
![WebKit 26.5](https://img.shields.io/badge/WebKit-26.5-45ba4b)
![Clearcote v0.1.0-pre.18](https://img.shields.io/badge/Clearcote-v0.1.0--pre.18-blue)

A **NativeAOT-compatible** fork of [playwright-dotnet](https://github.com/microsoft/playwright-dotnet) targeting `net10.0` with full trimming and ahead-of-time compilation — zero reflection, zero build warnings, zero runtime dynamic code.

| Browser | Linux | macOS | Windows |
|---------|:-----:|:-----:|:-------:|
| Chromium 149 | ✅ | ✅ | ✅ |
| WebKit 26.5 | ✅ | ✅ | ✅ |
| Firefox 151 | ✅ | ✅ | ✅ |
| [Clearcote] v0.1.0-pre.18 | ✅ | — | ✅ |

[Clearcote]: https://github.com/clearcotelabs/clearcote-browser

## Install

```bash
dotnet add package Playwright.Clearcote --version 1.0.0
```

## Quick start

```bash
./build.sh --download-driver
dotnet build ./src
```

Build with AOT validation:

```bash
dotnet build src/Playwright/Playwright.csproj \
  -p:TargetFramework=net10.0 \
  -p:PublishAot=true \
  -p:TrimMode=full
```

## NativeAOT features

Every `Activator.CreateInstance`, `MakeGenericType`, `GetProperties()`, `Convert.ChangeType`, and `Assembly.GetName()` call has been removed. The JSON wire protocol uses `System.Text.Json` source generation (`PlaywrightJsonContext`) with 272+ generated option types and all common system types registered. The library compiles at `IlcTrimmed` with zero IL2026/IL3050 warnings and zero suppressions.

### What's replaced

| File | Before | After |
|------|--------|-------|
| `EvaluateArgumentValueConverter.cs` | `Activator.CreateInstance`, `TypeDescriptor.GetProperties`, `ExpandoObject`, `MakeGenericType` | `JsonNode` tree parsing + source-gen `Deserialize` |
| `ClassUtils.cs` | `typeof(T).GetProperties()` | JSON round-trip via `PlaywrightJsonContext` |
| `Locator.cs` | `typeof(T).GetProperties()` + reflection for Strict default | JSON round-trip + `JsonObject` property check |
| `Driver.cs` | `Assembly.GetName().Version` | Hardcoded version string |
| `AotEnumMemberConverter.cs` | `MakeGenericType` in `JsonConverterFactory` | Explicit per-enum `JsonConverter<T>` |
| `ChannelOwnerConverterFactory.cs` | `Activator.CreateInstance` + `MakeGenericType` | Typed `WriteOnlyJsonConverter<ChannelOwner>` |
| `CompatibilitySuppressions.xml` | 431 IL2026/IL3050 suppressions | **Deleted** — zero suppressions needed |

## Clearcote Browser

This fork bundles [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser) — an open-source, fingerprint-resistant Chromium build. The binary is auto-downloaded, SHA-256 verified, and cached on first use.

```cs
using System;
using System.IO;
using Microsoft.Playwright;

Console.WriteLine("Playwright .NET + Clearcote Browser — NativeAOT sample");
Console.WriteLine();

var cacheDir = Path.Combine(Path.GetTempPath(), "clearcote-cache");
var downloadPath = await ClearcoteBrowser.DownloadAsync(new()
{
    CacheDir = cacheDir,
    Quiet = true,
});
Console.WriteLine($"Clearcote browser binary: {downloadPath}");

var launchOptions = new ClearcoteLaunchOptions
{
    Headless = true,
    Fingerprint = "playwright-aot-sample",
    Brand = "Chrome",
    BrandVersion = "149",
    TlsProfile = ClearcoteTlsProfile.MatchPersona,
    HardwareConcurrency = 8,
    DisableGpuFingerprint = true,
    FingerprintNoise = false,
    Humanize = true,
    CacheDir = cacheDir,
    Quiet = true,
};

var playwright = await Playwright.CreateAsync();
var browser = await ClearcoteBrowser.LaunchAsync(playwright, launchOptions);
var page = await browser.NewPageAsync();

var html = """
<!doctype html>
<title>Clearcote NativeAOT</title>
<main>
  <h1>Clearcote NativeAOT smoke</h1>
  <p>Offline page used to validate launch, JavaScript evaluation, WebGL probing, and screenshots.</p>
</main>
""";
await page.GotoAsync(
    "data:text/html;charset=utf-8," + Uri.EscapeDataString(html),
    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
Console.WriteLine($"Page title: {await page.TitleAsync()}");
Console.WriteLine($"User agent: {await page.EvaluateAsync<string>("() => navigator.userAgent")}");

var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
Console.WriteLine($"Render coherent on this host: {verdict.Coherent}");
Console.WriteLine($"  Vendor:   {verdict.Vendor}");
Console.WriteLine($"  Renderer: {verdict.Renderer}");
Console.WriteLine($"  WebGL:    {verdict.Webgl}");
foreach (var warning in verdict.Warnings)
{
    Console.WriteLine($"  Warning:  {warning}");
}

await page.ScreenshotAsync(new() { Path = "clearcote-screenshot.png", FullPage = true });
Console.WriteLine("Screenshot saved to clearcote-screenshot.png");

await browser.CloseAsync();
Console.WriteLine("Done.");
```

Use `ClearcoteBrowser.LaunchAsync(playwright, options)` for the shortest path, or pass `ClearcoteLaunchOptions` to standard `playwright.Chromium.LaunchAsync()`; patching is automatic. Set `CLEARCOTE=1` to use Clearcote without code changes.

The pinned browser release is `v0.1.0-pre.18`. `CLEARCOTE_AUTO_UPDATE=1` / `AutoUpdate = true` is opt-in and falls back to the pinned browser when the latest GitHub release is SDK-only or has no compatible browser asset.

### Clearcote features

- **Fingerprint masking** — seed-derived persona for WebGL, Canvas, audio, fonts, timezone, geolocation, WebRTC, TLS ClientHello
- **Canvas bridge** — remote real-GPU rendering for canvas/WebGL readbacks
- **Humanized input** — Bezier mouse paths, multi-peak velocity profile, fat-finger typos
- **In-browser AI agent** — autonomous browser tasks via OpenRouter LLM
- **Persistent profiles** — save/load personas with full fingerprint + canvas bridge config
- **Render coherence probing** — detect software rasterizer / GPU family mismatches
- **Widevine CDM** — fetch and seed Google's CDM for DRM playback
- **Geo-IP auto-resolution** — fill timezone/language/location/WebRTC IP from proxy egress

### Environment variables

| Variable | Purpose |
|----------|---------|
| `CLEARCOTE=1` | Opt-in without ClearcoteLaunchOptions |
| `CLEARCOTE_BINARY` | Direct path to Clearcote binary (bypasses download) |
| `CLEARCOTE_CACHE` | Override download cache directory |
| `CLEARCOTE_AUTO_UPDATE=1` | Auto-resolve latest GitHub release |
| `CLEARCOTE_PROFILE_DIR` | Override profile storage directory |
| `CLEARCOTE_NO_WARN=1` | Suppress coherence warnings |

## Samples

| Sample | Description | Build |
|--------|-------------|-------|
| [`samples/Playwright.AotSample`](samples/Playwright.AotSample) | Basic Chromium launch + screenshot | `dotnet publish … -p:PublishAot=true` |
| [`samples/Playwright.AotSample.Clearcote`](samples/Playwright.AotSample.Clearcote) | Full Clearcote sample (above) | `dotnet publish … -p:PublishAot=true` |

```bash
dotnet publish samples/Playwright.AotSample.Clearcote \
  -c Release -r linux-x64 \
  -p:PublishAot=true -p:SelfContained=true

./samples/Playwright.AotSample.Clearcote/bin/Release/net10.0/linux-x64/publish/Playwright.AotSample.Clearcote
```

## Resources

- [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser)
- [playwright-dotnet (upstream)](https://github.com/microsoft/playwright-dotnet)
