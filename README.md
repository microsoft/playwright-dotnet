# Playwright for .NET 🎭
[![NuGet version](https://img.shields.io/nuget/vpre/Microsoft.Playwright?color=%2345ba4b)](https://www.nuget.org/packages/Microsoft.Playwright) [![Join Slack](https://img.shields.io/badge/join-slack-infomational)](https://aka.ms/playwright-slack)

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| <!-- GEN:chromium-version -->93.0.4576.0<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| <!-- GEN:webkit-version -->14.2<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| <!-- GEN:firefox-version -->90.0<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |

Playwright for .NET is the official language port of [Playwright](https://playwright.dev), the library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) with a single API. Playwright is built to enable cross-browser web automation that is **ever-green**, **capable**, **reliable** and **fast**.

## Documentation

[https://playwright.dev/dotnet/docs/intro](https://playwright.dev/dotnet/docs/intro) 

## API Reference
[https://playwright.dev/dotnet/docs/api/class-playwright](https://playwright.dev/dotnet/docs/api/class-playwright)


```cs
using System.Threading.Tasks;
using Microsoft.Playwright;

class Program
{
    public static async Task Main()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://playwright.dev/dotnet");
        await page.ScreenshotAsync(new() { Path = "screenshot.png" });
    }
}
```

## Other languages

More comfortable in another programming language? [Playwright](https://playwright.dev) is also available in
- [TypeScript](https://playwright.dev/docs/intro),
- [Python](https://playwright.dev/python/docs/intro)
- [Java](https://playwright.dev/java/docs/intro).
