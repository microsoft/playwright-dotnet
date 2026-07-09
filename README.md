# Playwright for .NET — NativeAOT

![Chromium 149](https://img.shields.io/badge/Chromium-149.0.7827-45ba4b)
![Firefox 151](https://img.shields.io/badge/Firefox-151.0-45ba4b)
![WebKit 26.5](https://img.shields.io/badge/WebKit-26.5-45ba4b)
![Clearcote v0.1.0-pre.19](https://img.shields.io/badge/Clearcote-v0.1.0--pre.19-blue)

A **NativeAOT-compatible** fork of [playwright-dotnet](https://github.com/microsoft/playwright-dotnet) targeting `net10.0` with full trimming and ahead-of-time compilation — zero reflection, zero build warnings, zero runtime dynamic code.

| Browser | Linux | macOS | Windows |
|---------|:-----:|:-----:|:-------:|
| Chromium 149 | ✅ | ✅ | ✅ |
| WebKit 26.5 | ✅ | ✅ | ✅ |
| Firefox 151 | ✅ | ✅ | ✅ |
| [Clearcote] v0.1.0-pre.19 | ✅ | — | ✅ |

[Clearcote]: https://github.com/clearcotelabs/clearcote-browser

## Install

```bash
dotnet add package Playwright.Clearcote
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

This fork bundles [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser) — an open-source, fingerprint-resistant Chromium build targeting `net10.0` NativeAOT.

```cs
using Microsoft.Playwright;

// 1. Download browser binary (cached after first use)
var downloadPath = await ClearcoteBrowser.DownloadAsync(new()
{
    CacheDir = "/tmp/clearcote-cache",
    Quiet = true,
});
Console.WriteLine($"Downloaded: {downloadPath}");

// 2. Create Playwright and launch Clearcote with full fingerprint config
var playwright = await Playwright.CreateAsync();
var browser = await ClearcoteBrowser.LaunchAsync(playwright, new()
{
    Headless = false,
    ClearcotePlatform = ClearcotePlatform.Windows,
    Brand = "Chrome",
    BrandVersion = "149",
    Fingerprint = "my-fingerprint-seed",

    // TLS ClientHello
    TlsProfile = ClearcoteTlsProfile.MatchPersona,

    // Hardware
    HardwareConcurrency = 8,
    DisableGpuFingerprint = false,

    // Geo-location
    Geoip = true,                       // auto-resolve from proxy egress
    Location = "US,New York",           // or manual
    Timezone = "America/New_York",
    AcceptLanguage = "en-US,en;q=0.9",

    // WebRTC
    WebrtcIp = "192.168.1.100",

    // Humanized input
    Humanize = true,
    ShowCursor = true,

    // Canvas bridge (remote GPU)
    CanvasBridge = new()
    {
        Url = "ws://canvas-bridge:9090",
        Auth = "token-secret",
        Mode = "passthrough",
    },

    // Storage
    StorageQuota = 500_000_000,          // 500 MB

    // Cache & downloads
    CacheDir = "/tmp/clearcote-cache",
    Quiet = true,
});

var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");

// 3. Check render coherence (software vs GPU)
var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
Console.WriteLine($"Coherent: {verdict.Coherent}");
Console.WriteLine($"  Vendor:   {verdict.Vendor}");
Console.WriteLine($"  Renderer: {verdict.Renderer}");
Console.WriteLine($"  WebGL:    {verdict.Webgl}");
foreach (var w in verdict.Warnings) Console.WriteLine($"  Warning:  {w}");

// 4. Screenshot
await page.ScreenshotAsync(new() { Path = "screenshot.png" });
await browser.CloseAsync();

// 5. Save / load profiles
var profile = new ClearcoteProfile("work-profile", new()
{
    Fingerprint = "work-laptop",
    Brand = "Chrome",
    BrandVersion = "149",
    Humanize = true,
});
profile.Save();                          // persists to CLEARCOTE_PROFILE_DIR
var loaded = ClearcoteProfile.Load("work-profile");
var browser2 = await loaded.LaunchAsync(playwright, new()
{
    Headless = true,
    CacheDir = "/tmp/clearcote-cache",
});
Console.WriteLine($"Loaded profile: {loaded.Name}");
await browser2.CloseAsync();

// 6. Persistent context with Widevine DRM
var context = await ClearcoteBrowser.LaunchPersistentContextAsync(
    playwright, "/tmp/clearcote-userdata", new()
    {
        Fingerprint = "drm-profile",
        Widevine = true,            // auto-fetch + seed Widevine CDM
        Headless = false,
    });
var drmPage = await context.NewPageAsync();
await drmPage.GotoAsync("https://bitmovin.com/demos/drm");
Console.WriteLine($"DRM page: {await drmPage.TitleAsync()}");
await context.CloseAsync();

// 7. AI browser agent
var agentCtx = await ClearcoteBrowser.LaunchAgentAsync(playwright, new()
{
    Headless = true,
    AgentLlmUrl = "https://openrouter.ai/api/v1/chat/completions",
    AgentLlmKey = "sk-or-v1-...",
    AgentModel = "openai/gpt-4o",
    AgentToolMode = "auto",
    AgentTyping = "human",
});
var agentPage = await agentCtx.NewPageAsync();
var result = await ClearcoteBrowser.RunAgentTaskAsync(agentPage,
    "Navigate to github.com and search for playwright-dotnet",
    new() { MaxSteps = 20 });
Console.WriteLine($"Agent success: {result.Success}");
Console.WriteLine($"  Final: {result.FinalText}");
foreach (var step in result.Steps)
    Console.WriteLine($"  [{step.Status}] {step.Action}");
await agentCtx.CloseAsync();

playwright.Dispose();
```

Use `ClearcoteBrowser.LaunchAsync(playwright, options)` for the shortest path, or pass `ClearcoteLaunchOptions` to standard `playwright.Chromium.LaunchAsync()`; patching is automatic. Set `CLEARCOTE=1` to use Clearcote without code changes.

The pinned browser release is `v0.1.0-pre.19`. `CLEARCOTE_AUTO_UPDATE=1` / `AutoUpdate = true` is opt-in and falls back to the pinned browser when the latest GitHub release is SDK-only or has no compatible browser asset.

### Clearcote features

| Feature | API | Description |
|---------|-----|-------------|
| **Fingerprint masking** | `ClearcoteLaunchOptions` (`Fingerprint`, `Brand`, `BrandVersion`, `Platform`) | Seed-derived persona for WebGL, Canvas, audio, fonts, timezone, geolocation, WebRTC, TLS ClientHello |
| **Platform personas** | `ClearcotePlatform` (Windows/Linux/Macos/Android) | Full platform fingerprint suite (UA, TLS, screen, touch, window size) |
| **Canvas bridge** | `ClearcoteCanvasBridgeOptions` | Remote real-GPU rendering for canvas/WebGL readbacks (WebSocket relay) |
| **Humanized input** | `Humanize = true` | Bezier mouse paths, multi-peak velocity profile, fat-finger typos |
| **AI browser agent** | `LaunchAgentAsync` / `RunAgentTaskAsync` | Autonomous browser tasks via OpenRouter LLM (GPT-4o, Claude, etc.) |
| **Persistent profiles** | `ClearcoteProfile` (Save/Load/ListProfiles) | Save/load personas with full fingerprint + canvas bridge config |
| **Render coherence** | `CheckRenderCoherenceAsync` | Detect software rasterizer / GPU family mismatches |
| **Widevine DRM** | `Widevine = true` | Auto-fetch and seed Google's CDM for DRM playback (Chrome ≥ 149 required) |
| **GeoIP auto-resolution** | `Geoip = true` | Fill timezone/language/location/WebRTC IP from proxy egress IP |
| **Fingerprint noise** | `FingerprintNoise` | Add sub-pixel jitter to Canvas/WebGL/audio fingerprints per session |
| **Storage quota** | `StorageQuota` | Limit IndexedDB / localStorage to N bytes |
| **Screenshot probe** | `CheckRenderCoherenceAsync(page).Screenshot` | Visual renderer detection via WebGL + Canvas2D snapshots |

### Environment variables

| Variable | Purpose |
|----------|---------|
| `CLEARCOTE=1` | Opt-in without `ClearcoteLaunchOptions` |
| `CLEARCOTE_BINARY` | Direct path to Clearcote binary (bypasses download) |
| `CLEARCOTE_CACHE` | Override download cache directory |
| `CLEARCOTE_AUTO_UPDATE=1` | Auto-resolve latest GitHub release |
| `CLEARCOTE_PROFILE_DIR` | Override profile storage directory (default `~/.clearcote/profiles`) |
| `CLEARCOTE_NO_WARN=1` | Suppress coherence warnings |
| `PLAYWRIGHT_BROWSERS_PATH` | Common Playwright browser cache (fallback for `CacheDir`) |

### Platform support

| `ClearcotePlatform` | TLS ClientHello | User-Agent | Window size | Touch |
|---------------------|:---------------:|:----------:|:-----------:|:-----:|
| `Windows` | Windows Chrome | `Windows NT 10.0` | `1280,720` | — |
| `Linux` | Linux Chrome | `X11; Linux x86_64` | `1280,720` | — |
| `Macos` | macOS Chrome | `Macintosh; Intel Mac OS X` | `1280,720` | — |
| `Android` | Android Chrome | `Linux; Android 14` | `412,915` | Multi-touch |

### TLS profiles

| `ClearcoteTlsProfile` | Effect |
|-----------------------|--------|
| `MatchPersona` / `Auto` | Grease + cipher suites match the persona's claimed Chrome version |
| `Native` | Keep the build's native TLS ClientHello unchanged |

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
