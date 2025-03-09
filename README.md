# Playwright for .NET 🎭
[![NuGet version](https://img.shields.io/nuget/v/Microsoft.Playwright?color=%2345ba4b)](https://www.nuget.org/packages/Microsoft.Playwright) [![Join Discord](https://img.shields.io/badge/join-discord-infomational)](https://aka.ms/playwright/discord)

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| Chromium <!-- GEN:chromium-version -->134.0.6998.35<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| WebKit <!-- GEN:webkit-version -->18.4<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| Firefox <!-- GEN:firefox-version -->135.0<!-- GEN:stop --> | ✅ | ✅ | ✅ |

Playwright for .NET is the official language port of [Playwright](https://playwright.dev), the library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) with a single API. Playwright is built to enable cross-browser web automation that is **ever-green**, **capable**, **reliable** and **fast**.

## Documentation

[https://playwright.dev/dotnet/docs/intro](https://playwright.dev/dotnet/docs/intro) 

## API Reference
[https://playwright.dev/dotnet/docs/api/class-playwright](https://playwright.dev/dotnet/docs/api/class-playwright)

## Build and Publish
```bash
# 1. Update the version from `/playwright-dotnet/src/Common/Version.props` file
# 2. Delete the .drivers folder from `/playwright-dotnet/src/Playwright/.drivers`
# 3. Go to the root folder of the repo. And run the dotnet command to download the drivers again.
cd playwright-dotnet
dotnet run --project ./src/tools/Playwright.Tooling/Playwright.Tooling.csproj -- download-drivers --basepath .

# 4. Go to src and run build
cd src
dotnet build

# 5. Now publish a release for nuget.
dotnet pack -c Release

# 6. Push to Nuget registry
dotnet nuget push ./Playwright/bin/Release/WitcherPro.Playwright.nupkg -k <api-key-here> -s https://api.nuget.org/v3/index.json 
```


```cs
using System.Threading.Tasks;
using Microsoft.Playwright;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
var page = await browser.NewPageAsync();
await page.GotoAsync("https://playwright.dev/dotnet");
await page.ScreenshotAsync(new() { Path = "screenshot.png" });
```

## Other languages

More comfortable in another programming language? [Playwright](https://playwright.dev) is also available in
- [TypeScript](https://playwright.dev/docs/intro),
- [Python](https://playwright.dev/python/docs/intro),
- [Java](https://playwright.dev/java/docs/intro).
