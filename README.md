# Playwright for .NET — NativeAOT fork 🎭

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| Chromium <!-- GEN:chromium-version -->149.0.7827.55<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| WebKit <!-- GEN:webkit-version -->26.5<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| Firefox <!-- GEN:firefox-version -->151.0<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| [Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser) v0.1.0-pre.18 (Chromium 149) | ✅ | — | ✅ |

This is a **NativeAOT-compatible fork** of [Playwright](https://playwright.dev) for .NET. It targets `net10.0` with full trimming and AOT compilation — zero reflection, zero warnings, zero runtime dynamic code.

## Quick start

```bash
./build.sh --download-driver
dotnet build ./src
```

## Clearcote Browser support

[Clearcote Browser](https://github.com/clearcotelabs/clearcote-browser) is a fingerprint-resistant Chromium fork by [Clearcote Labs](https://clearcotelabs.com/). This fork bundles full Clearcote support:

```cs
using Microsoft.Playwright;

var playwright = await Playwright.CreateAsync();

// Launch with a realistic Windows fingerprint (headless-safe).
var browser = await ClearcoteBrowser.LaunchAsync(playwright.Chromium, new()
{
    Headless = true,
    Fingerprint = "my-persona",
    ClearcotePlatform = "windows",
    Humanize = true,
});

var page = await browser.NewPageAsync();
await page.GotoAsync("https://example.com");

// Clearcote-specific APIs:
var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
var result = await ClearcoteBrowser.RunAgentTaskAsync(page, "click the login button");
```

You can also pass `ClearcoteLaunchOptions` directly to the standard `chromium.LaunchAsync()` — the patching is automatic.

### Feature highlights

- **Fingerprint masking** — spoof WebGL, Canvas, audio, fonts, timezone, geolocation, WebRTC, and more
- **Canvas bridge** — inject real canvas output from a remote rendering service
- **Humanized input** — Bezier-curved mouse movements, human typing cadence with typos
- **In-browser AI agent** — run autonomous browser tasks via LLM integration
- **Persistent profiles** — save/load browser personas with all fingerprint settings
- **Render coherence probing** — detect WebGL software/hardware mismatches
- **Widevine CDM** — fetch and seed the Widevine Content Decryption Module
- **Geo-IP auto-resolution** — auto-fill location/timezone from proxy egress

### NativeAOT samples

| Sample | Description |
|--------|-------------|
| [`samples/Playwright.AotSample`](samples/Playwright.AotSample) | Basic Playwright + Chromium (headless) |
| [`samples/Playwright.AotSample.Clearcote`](samples/Playwright.AotSample.Clearcote) | Clearcote Browser with fingerprint, render coherence, screenshot |

```bash
dotnet publish samples/Playwright.AotSample.Clearcote -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true
./samples/Playwright.AotSample.Clearcote/bin/Release/net10.0/linux-x64/publish/Playwright.AotSample.Clearcote
```

## Documentation

[https://playwright.dev/dotnet/docs/intro](https://playwright.dev/dotnet/docs/intro)

## API Reference

[https://playwright.dev/dotnet/docs/api/class-playwright](https://playwright.dev/dotnet/docs/api/class-playwright)

## Other languages

Playwright is also available in
- [TypeScript](https://playwright.dev/docs/intro),
- [Python](https://playwright.dev/python/docs/intro),
- [Java](https://playwright.dev/java/docs/intro).
