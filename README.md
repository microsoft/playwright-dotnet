# Playwright for .NET 🎭
[![NuGet version](https://img.shields.io/nuget/v/Microsoft.Playwright?color=%2345ba4b)](https://www.nuget.org/packages/Microsoft.Playwright) [![Join Discord](https://img.shields.io/badge/join-discord-infomational)](https://aka.ms/playwright/discord)

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| Chromium <!-- GEN:chromium-version -->153.0.8010.12<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| WebKit <!-- GEN:webkit-version -->26.6<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| Firefox <!-- GEN:firefox-version -->155.0<!-- GEN:stop --> | ✅ | ✅ | ✅ |

Playwright for .NET is the official language port of [Playwright](https://playwright.dev), the library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) with a single API. Playwright is built to enable cross-browser web automation that is **ever-green**, **capable**, **reliable** and **fast**.

## Documentation

[https://playwright.dev/dotnet/docs/intro](https://playwright.dev/dotnet/docs/intro) 

## API Reference
[https://playwright.dev/dotnet/docs/api/class-playwright](https://playwright.dev/dotnet/docs/api/class-playwright)


```cs
using System.Threading.Tasks;
using Microsoft.Playwright;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
var page = await browser.NewPageAsync();
await page.GotoAsync("https://playwright.dev/dotnet");
await page.ScreenshotAsync(new() { Path = "screenshot.png" });
```

## Microsoft.Testing.Platform configuration

For tests running on Microsoft.Testing.Platform (MTP) 2.4.1 or later, reference
`Microsoft.Playwright.Testing.Platform` alongside your Playwright test integration
(for example, `Microsoft.Playwright.MSTest.v4`). The configuration extension is
registered automatically by MTP's generated entry point. Applications with a custom
entry point can call
`Microsoft.Playwright.Testing.Platform.TestingPlatformBuilderHook.AddExtensions(builder, args)`
before building their test application.

Use a test framework runner compatible with MTP 2.x, such as `MSTest.Sdk` 4.4.1.
The extension is a separate package so existing VSTest and MTP 1.x projects are
not automatically upgraded.

Add a `testconfig.json` file to your test project:

```json
{
  "playwright": {
    "browserName": "firefox",
    "expectTimeout": 10000,
    "testIdAttribute": "data-testid",
    "launchOptions": {
      "headless": true,
      "slowMo": 50
    },
    "contextOptions": {
      "locale": "en-US",
      "timezoneId": "Europe/Madrid",
      "colorScheme": "dark",
      "viewportSize": { "width": 1280, "height": 720 },
      "extraHTTPHeaders": { "X-Test": "playwright" }
    }
  }
}
```

Let MTP's MSBuild integration copy the configuration to the test application's
output directory, or select a file explicitly with MTP's
`--config-file path/to/testconfig.json` option. Playwright reads the configuration
supplied by MTP, not files from the current working directory.

Launch and context options use camel-cased .NET property names from
`BrowserTypeLaunchOptions` and `BrowserNewContextOptions` (for example,
`ViewportSize` becomes `viewportSize`). Enum values use their
Playwright strings, such as `"dark"` and `"no-preference"`; headers, environment
variables, and Firefox preferences are JSON objects. Unknown properties and invalid
values fail the test session instead of silently falling back to defaults.

`BROWSER` and `HEADED=1` retain precedence over file settings. Code overrides of
`LaunchOptionsAsync()` (MSTest), `LaunchOptions()` (NUnit/xUnit), and
`ContextOptions()` retain precedence too; use
`base.ContextOptions()` to customize configured context defaults. Unspecified
context locale and color scheme retain the fixture defaults (`en-US` and `light`).
Each call returns a new options object.

The optional `"pwDebug"` setting accepts `"0"`, `"1"`, or `"console"` and applies
before the Playwright driver starts. An existing `PWDEBUG` environment variable takes
precedence. `"1"` opens the Playwright Inspector, just like `PWDEBUG=1`.

VSTest continues to support the `<Playwright>` section in `.runsettings` without
this additional package. MTP does not invoke VSTest settings providers, so that
section is **not applied** on MTP. MSTest's existing warnings for unsupported
runsettings entries do not currently cover custom sections such as `<Playwright>`.

## Other languages

More comfortable in another programming language? [Playwright](https://playwright.dev) is also available in
- [TypeScript](https://playwright.dev/docs/intro),
- [Python](https://playwright.dev/python/docs/intro),
- [Java](https://playwright.dev/java/docs/intro).
