# Playwright for .NET — NativeAOT + Clearcote

<p align="center">
  <img src="https://img.shields.io/badge/Chromium-149.0.7827-45ba4b" alt="Chromium 149" />
  <img src="https://img.shields.io/badge/Firefox-151.0-45ba4b" alt="Firefox 151" />
  <img src="https://img.shields.io/badge/WebKit-26.5-45ba4b" alt="WebKit 26.5" />
  <img src="https://img.shields.io/badge/Clearcote-v0.1.0--pre.20-blue" alt="Clearcote v0.1.0-pre.20" />
  <img src="https://img.shields.io/badge/NativeAOT-✓-brightgreen" alt="NativeAOT" />
  <img src="https://img.shields.io/badge/TrimMode-full-blueviolet" alt="TrimMode=full" />
</p>

A **NativeAOT-compatible** fork of [playwright-dotnet](https://github.com/microsoft/playwright-dotnet) targeting `net10.0` with full trimming and ahead-of-time compilation. Zero reflection, zero build warnings, zero runtime dynamic code — and first-class support for the [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser) fingerprint-resistant Chromium build.

---

## Table of Contents

- [Browser support](#browser-support)
- [Installation](#installation)
- [Quick start](#quick-start)
- [NativeAOT deep-dive](#nativeaot-deep-dive)
- [Clearcote Browser](#clearcote-browser)
  - [Complete example](#complete-example)
  - [Feature catalog](#feature-catalog)
  - [API reference](#api-reference)
  - [Platform personas](#platform-personas)
  - [TLS profiles](#tls-profiles)
  - [Environment variables](#environment-variables)
- [Samples](#samples)
- [Resources](#resources)

---

## Browser support

| Browser | Linux | macOS | Windows |
|---------|:-----:|:-----:|:-------:|
| Chromium 149 | ✅ | ✅ | ✅ |
| WebKit 26.5 | ✅ | ✅ | ✅ |
| Firefox 151 | ✅ | ✅ | ✅ |
| [Clearcote] v0.1.0-pre.20 | ✅ | — | ✅ |

[Clearcote]: https://github.com/clearcotelabs/clearcote-browser

---

## Installation

```bash
dotnet add package Playwright.Clearcote
```

Then download the browser drivers:

```bash
./build.sh --download-driver
```

> The Clearcote browser binary is downloaded automatically on first use via
> `ClearcoteBrowser.DownloadAsync()` or `ClearcoteBrowser.ExecutablePathAsync()`.
> No separate driver download is needed.

---

## Quick start

```bash
# Build the full solution
dotnet build ./src

# Build with AOT validation
dotnet build src/Playwright/Playwright.csproj \
  -p:TargetFramework=net10.0 \
  -p:PublishAot=true \
  -p:TrimMode=full

# Run AOT sample
dotnet run --project samples/Playwright.AotSample

# Run Clearcote sample
dotnet run --project samples/Playwright.AotSample.Clearcote
```

---

## NativeAOT deep-dive

This fork eliminates every reflection-heavy pattern in the upstream
playwright-dotnet library. The JSON wire protocol uses `System.Text.Json`
source generation (`PlaywrightJsonContext`) with 272+ generated option types
and all common system types registered. The library compiles at `IlcTrimmed`
with **zero IL2026 / IL3050 warnings** and **zero suppressions**.

| What was removed | Where | Replacement |
|---|---|---|
| `Activator.CreateInstance` + `MakeGenericType` | `ChannelOwnerConverterFactory.cs` | Typed `WriteOnlyJsonConverter<ChannelOwner>` |
| `Activator.CreateInstance` + `MakeGenericType` | `JsonStringEnumMemberConverter` | Explicit per-enum `JsonConverter<T>` |
| `Activator.CreateInstance`, `TypeDescriptor.GetProperties`, `ExpandoObject` | `EvaluateArgumentValueConverter.cs` | `JsonNode` tree parsing + source-gen deserialize |
| `typeof(T).GetProperties()` | `ClassUtils.cs` | JSON round-trip via `PlaywrightJsonContext` |
| `typeof(T).GetProperties()` | `Locator.cs` | JSON round-trip + `JsonObject` property check |
| `Assembly.GetName().Version` | `Driver.cs` | Hardcoded version string |
| 431 IL2026 / IL3050 suppressions | `CompatibilitySuppressions.xml` | **Deleted** — zero needed |

---

## Clearcote Browser

[Clearcote](https://github.com/clearcotelabs/clearcote-browser) is an
open-source Chromium build engineered for bot / automation use cases:

- **Fingerprint resistance** — WebGL, Canvas, Audio, Fonts, WebRTC, TLS
  ClientHello all spoofed to match a claimed persona
- **Platform personas** — Windows, macOS, Linux, Android with full UA,
  screen, touch, and TLS suite matching
- **Canvas bridge** — remote real-GPU rendering via WebSocket relay
  (no software rasterizer)
- **Humanized input** — Bezier mouse paths, multi-peak velocity, fat-finger
  typos
- **AI agent** — in-browser autonomous task execution via OpenRouter LLMs
- **Widevine DRM** — auto-fetch and seed Google's CDM for DRM playback
- **GeoIP** — auto-resolve timezone/language/location from proxy egress IP

Pinned release: **v0.1.0-pre.20** (Chromium 149.0.7827.114)

---

### Complete example

```csharp
using Microsoft.Playwright;

// 1. Download the browser binary (cached after first use)
var downloadPath = await ClearcoteBrowser.DownloadAsync(new()
{
    CacheDir = "/tmp/clearcote-cache",
    Quiet = true,
});
Console.WriteLine($"Downloaded: {downloadPath}");

// 2. Launch with full fingerprint configuration
var playwright = await Playwright.CreateAsync();
var browser = await ClearcoteBrowser.LaunchAsync(playwright, new()
{
    Headless = false,
    ClearcotePlatform = ClearcotePlatform.Windows,
    Brand = "Chrome",
    BrandVersion = "149",
    Fingerprint = "my-fingerprint-seed",

    // TLS ClientHello matching
    TlsProfile = ClearcoteTlsProfile.MatchPersona,

    // Hardware concurrency
    HardwareConcurrency = 8,
    DisableGpuFingerprint = false,

    // Geo-location (auto-resolve or manual)
    Geoip = true,
    Location = "US,New York",
    Timezone = "America/New_York",
    AcceptLanguage = "en-US,en;q=0.9",

    // WebRTC
    WebrtcIp = "192.168.1.100",

    // Humanize input
    Humanize = true,
    ShowCursor = true,

    // Canvas bridge (remote GPU rendering)
    CanvasBridge = new()
    {
        Url = "ws://canvas-bridge:9090",
        Auth = "token-secret",
        Mode = "passthrough",
    },

    // Storage limit
    StorageQuota = 500_000_000,          // 500 MB

    // Cache
    CacheDir = "/tmp/clearcote-cache",
    Quiet = true,
});

var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");

// 3. Check render coherence
var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
Console.WriteLine($"Coherent: {verdict.Coherent}");
Console.WriteLine($"  Vendor:   {verdict.Vendor}");
Console.WriteLine($"  Renderer: {verdict.Renderer}");
Console.WriteLine($"  WebGL:    {verdict.Webgl}");

await page.ScreenshotAsync(new() { Path = "screenshot.png" });
await browser.CloseAsync();

// 4. Save and load profiles
var profile = new ClearcoteProfile("work-profile", new()
{
    Fingerprint = "work-laptop",
    Brand = "Chrome",
    BrandVersion = "149",
    Humanize = true,
});
profile.Save();

var loaded = ClearcoteProfile.Load("work-profile");
var browser2 = await loaded.LaunchAsync(playwright.Chromium, new()
{
    Headless = true,
    CacheDir = "/tmp/clearcote-cache",
});
await browser2.CloseAsync();

// 5. Persistent context with Widevine DRM
var context = await ClearcoteBrowser.LaunchPersistentContextAsync(
    playwright, "/tmp/clearcote-userdata", new()
    {
        Fingerprint = "drm-profile",
        Widevine = true,
        Headless = false,
    });
var drmPage = await context.NewPageAsync();
await drmPage.GotoAsync("https://bitmovin.com/demos/drm");
await context.CloseAsync();

// 6. AI browser agent
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
await agentCtx.CloseAsync();

playwright.Dispose();
```

> **Tip:** Pass `ClearcoteLaunchOptions` to standard `playwright.Chromium.LaunchAsync()`
> — patching is automatic. Set env var `CLEARCOTE=1` to opt-in without changing code.

---

### Feature catalog

| Feature | API | Description |
|---|---|---|
| **Fingerprint masking** | `ClearcoteLaunchOptions` (`Fingerprint`, `Brand`, `BrandVersion`, `Platform`) | Seed-derived persona covering WebGL, Canvas, Audio, Fonts, Timezone, Geolocation, WebRTC, TLS ClientHello |
| **Platform personas** | `ClearcotePlatform` enum (Windows, Linux, Macos, Android) | Full platform suite — UA, TLS, screen resolution, touch support |
| **Canvas bridge** | `ClearcoteCanvasBridgeOptions` | Remote real-GPU rendering via WebSocket relay |
| **Humanized input** | `Humanize = true` / `ShowCursor = true` | Bezier curves, multi-peak velocity, fat-finger typos |
| **AI agent** | `LaunchAgentAsync` / `RunAgentTaskAsync` | In-browser autonomous tasks via OpenRouter LLMs |
| **Persistent profiles** | `ClearcoteProfile` (Save, Load, List) | Save/restore full persona config |
| **Render coherence** | `CheckRenderCoherenceAsync` | Detect software rasterizer / GPU mismatches |
| **Widevine DRM** | `Widevine = true` | Auto-fetch and seed Google's CDM (Chrome 149+) |
| **GeoIP auto-resolve** | `Geoip = true` | Timezone, language, location, WebRTC IP from proxy egress |
| **Fingerprint noise** | `FingerprintNoise = true` | Per-session sub-pixel jitter on Canvas/WebGL/Audio |
| **Storage quota** | `StorageQuota` (long) | Limit IndexedDB / localStorage to N bytes |
| **Privacy sandbox** | `DisablePrivacySandbox = true` | Disable Topics API, FLEDGE, Private Aggregation, etc. |

---

### API reference

#### `ClearcoteBrowser` static methods

| Method | Description |
|---|---|
| `ExecutablePathAsync(options?)` | Resolve the Clearcote binary path (downloads if missing) |
| `DownloadAsync(options?)` | Download, verify SHA-256, extract, return exe path |
| `LaunchAsync(playwright\|chromium, options?)` | Launch Clearcote with full fingerprint patching |
| `LaunchPersistentContextAsync(playwright\|chromium, userDataDir, options?)` | Launch with persistent user data directory |
| `LaunchAgentAsync(playwright\|chromium, options?)` | Launch for AI agent (auto-temp user data dir) |
| `RunAgentTaskAsync(page, goal, options?)` | Run in-browser AI agent task |
| `CheckRenderCoherenceAsync(page, claimedGpu?)` | Probe WebGL renderer coherence |
| `FetchWidevineAsync(options?)` | Download and verify Widevine CDM |
| `SeedWidevineAsync(userDataDir, options?)` | Seed Widevine into a profile directory |

#### `ClearcoteLaunchOptions` key properties

Inherits all `BrowserTypeLaunchOptions`. Adds:

| Property | Type | Description |
|---|---|---|
| `Fingerprint` | `string?` | Seed for deterministic fingerprint generation |
| `ClearcotePlatform` | `ClearcotePlatform?` | Target platform persona |
| `PlatformVersion` | `string?` | OS version override (e.g. `"10.0.19045"`) |
| `Brand` | `string?` | Browser brand override (e.g. `"Chrome"`, `"Edge"`) |
| `BrandVersion` | `string?` | Brand version override |
| `GpuVendor` | `string?` | GPU vendor override |
| `GpuRenderer` | `string?` | GPU renderer override |
| `HardwareConcurrency` | `int?` | `navigator.hardwareConcurrency` |
| `Location` | `string?` | Geolocation (e.g. `"US,New York"`) |
| `Timezone` | `string?` | IANA timezone ID |
| `AcceptLanguage` | `string?` | Accept-Language header |
| `WebrtcIp` | `string?` | WebRTC IP address |
| `TlsProfile` | `ClearcoteTlsProfile?` | TLS ClientHello profile |
| `TlsProfileCustom` | `string?` | Raw TLS profile JSON |
| `DisableGpuFingerprint` | `bool?` | Skip GPU fingerprint patching |
| `FingerprintNoise` | `bool?` | Add per-session fingerprint jitter |
| `FingerprintProfile` | `string?` | Pre-built fingerprint JSON |
| `StorageQuota` | `long?` | Storage limit in bytes |
| `CanvasBridge` | `ClearcoteCanvasBridgeOptions?` | Remote GPU bridge config |
| `DisablePrivacySandbox` | `bool?` | Disable Privacy Sandbox APIs |
| `Extensions` | `IEnumerable<string>?` | Chrome extension paths/IDs |
| `Geoip` | `bool?` | Auto-resolve geo from proxy egress |
| `CacheDir` | `string?` | Download cache directory |
| `Quiet` | `bool?` | Suppress console logging |
| `AutoUpdate` | `bool?` | Opt-in to unpinned latest release |
| `Humanize` | `bool?` | Enable humanized input |
| `ShowCursor` | `bool?` | Show click cursor |
| `AgentLlmUrl` | `string?` | LLM API endpoint for AI agent |
| `AgentLlmKey` | `string?` | LLM API key |
| `AgentModel` | `string?` | LLM model name |
| `AgentToolMode` | `string?` | Agent tool execution mode |
| `AgentTyping` | `string?` | Agent typing style |
| `Widevine` | `bool?` | Auto-fetch and seed Widevine CDM |

#### `ClearcoteProfile`

| Member | Description |
|---|---|
| `new ClearcoteProfile(name, options?)` | Create a named profile |
| `Name` | Profile name |
| `Options` | Saved launch options |
| `Path` | Resolved JSON file path |
| `Save(path?)` | Persist to JSON |
| `Load(nameOrPath)` | Load from JSON |
| `ListProfiles()` | List saved profile names |
| `Set(options)` | Merge additional options |
| `LaunchAsync(chromium, overrides?)` | Launch saved persona |
| `LaunchPersistentContextAsync(chromium, userDataDir, overrides?)` | Launch with user data dir |

---

### Platform personas

| `ClearcotePlatform` | TLS | User-Agent | Window | Touch |
|---|---|---|---|---|
| `Windows` | Windows Chrome | `Windows NT 10.0; Win64; x64` | 1280×720 | — |
| `Linux` | Linux Chrome | `X11; Linux x86_64` | 1280×720 | — |
| `Macos` | macOS Chrome | `Macintosh; Intel Mac OS X` | 1280×720 | — |
| `Android` | Android Chrome | `Linux; Android 14` | 412×915 | Multi-touch |

---

### TLS profiles

| `ClearcoteTlsProfile` | Effect |
|---|---|
| `MatchPersona` / `Auto` | Grease + cipher suites match the persona's claimed Chrome version |
| `Native` | Keep the build's native TLS ClientHello unchanged |

### Canvas bridge options

| Property | Type | Description |
|---|---|---|
| `Url` | `string?` | WebSocket URL of the remote GPU bridge |
| `Auth` | `string?` | Authentication token |
| `Mode` | `string?` | Bridge mode (`"passthrough"`, etc.) |
| `Allow` | `IEnumerable<string>?` | Allowed origin patterns |
| `Deny` | `IEnumerable<string>?` | Denied origin patterns |
| `Fallback` | `string?` | Fallback behavior |

---

### Environment variables

| Variable | Description |
|---|---|
| `CLEARCOTE=1` | Opt-in without `ClearcoteLaunchOptions` |
| `CLEARCOTE_BINARY` | Direct path to Clearcote binary (bypasses download) |
| `CLEARCOTE_CACHE` | Override download cache directory |
| `CLEARCOTE_AUTO_UPDATE=1` | Auto-resolve latest GitHub release |
| `CLEARCOTE_PROFILE_DIR` | Override profile storage directory (default `~/.clearcote/profiles`) |
| `CLEARCOTE_NO_WARN=1` | Suppress coherence warnings |
| `PLAYWRIGHT_BROWSERS_PATH` | Common Playwright browser cache (fallback for `CacheDir`) |

---

## Samples

| Sample | Description | Build command |
|---|---|---|
| [`samples/Playwright.AotSample`](samples/Playwright.AotSample) | 186 validation groups covering all core Playwright APIs (navigation, locators, evaluate, binding, routes, clock, API request context, etc.) | `dotnet publish -c Release -r linux-x64 -p:PublishAot=true` |
| [`samples/Playwright.AotSample.Clearcote`](samples/Playwright.AotSample.Clearcote) | Clearcote Browser validation: download, profile round-trip, launch, page evaluate, render coherence probe, screenshot | `dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true` |

Build and run the AOT samples:

```bash
# Regular Playwright API sample
dotnet publish samples/Playwright.AotSample \
  -c Release -r linux-x64 \
  -p:PublishAot=true

./samples/Playwright.AotSample/bin/Release/net10.0/linux-x64/publish/Playwright.AotSample

# Clearcote Browser sample
dotnet publish samples/Playwright.AotSample.Clearcote \
  -c Release -r linux-x64 \
  -p:PublishAot=true -p:SelfContained=true

./samples/Playwright.AotSample.Clearcote/bin/Release/net10.0/linux-x64/publish/Playwright.AotSample.Clearcote
```

---

## Resources

- [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser) — fingerprint-resistant Chromium
- [playwright-dotnet (upstream)](https://github.com/microsoft/playwright-dotnet) — original Microsoft project
- [Playwright for .NET docs](https://playwright.dev/dotnet/) — upstream API documentation
- [NativeAOT overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) — .NET NativeAOT documentation
