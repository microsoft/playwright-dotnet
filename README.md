# Playwright for .NET — NativeAOT

[!INCLUDE[Chromium](https://img.shields.io/badge/Chromium-149.0.7827-45ba4b)]()
[!INCLUDE[Firefox](https://img.shields.io/badge/Firefox-151.0-45ba4b)]()
[!INCLUDE[WebKit](https://img.shields.io/badge/WebKit-26.5-45ba4b)]()
[!INCLUDE[Clearcote](https://img.shields.io/badge/Clearcote-v0.1.0--pre.18-blue)]()

A **NativeAOT-compatible** fork of [Playwright](https://playwright.dev) targeting `net10.0` with full trimming and ahead-of-time compilation — zero reflection, zero build warnings, zero runtime dynamic code.

| Browser | Linux | macOS | Windows |
|---------|:-----:|:-----:|:-------:|
| Chromium 149 | ✅ | ✅ | ✅ |
| WebKit 26.5 | ✅ | ✅ | ✅ |
| Firefox 151 | ✅ | ✅ | ✅ |
| [Clearcote] v0.1.0-pre.18 | ✅ | — | ✅ |

[Clearcote]: https://github.com/clearcotelabs/clearcote-browser

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
using Microsoft.Playwright;

var playwright = await Playwright.CreateAsync();

// Launch with a Windows persona.
var browser = await ClearcoteBrowser.LaunchAsync(playwright.Chromium, new()
{
    Headless = true,
    Fingerprint = "my-persona",
    ClearcotePlatform = "windows",
    Humanize = true,
});

var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");

// Clearcote-specific APIs.
var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
var result = await ClearcoteBrowser.RunAgentTaskAsync(page, "click the login button");
```

Pass `ClearcoteLaunchOptions` to standard `chromium.LaunchAsync()` — patching is automatic. Set `CLEARCOTE=1` to enable Clearcote without code changes.

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
| [`samples/Playwright.AotSample.Clearcote`](samples/Playwright.AotSample.Clearcote) | Clearcote launch + render coherence + screenshot | `dotnet publish … -p:PublishAot=true` |

```bash
dotnet publish samples/Playwright.AotSample.Clearcote \
  -c Release -r linux-x64 \
  -p:PublishAot=true -p:SelfContained=true

./samples/Playwright.AotSample.Clearcote/bin/Release/net10.0/linux-x64/publish/Playwright.AotSample.Clearcote
```

## Documentation

- [Playwright .NET docs](https://playwright.dev/dotnet/docs/intro)
- [API reference](https://playwright.dev/dotnet/docs/api/class-playwright)
- [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser)

## Other languages

- [TypeScript](https://playwright.dev/docs/intro)
- [Python](https://playwright.dev/python/docs/intro)
- [Java](https://playwright.dev/java/docs/intro)
