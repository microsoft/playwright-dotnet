# 🎭 [Playwright](https://playwright.dev/dotnet) for .NET 
[![NuGet version](https://buildstats.info/nuget/PlaywrightSharp)](https://www.nuget.org/packages/PlaywrightSharp) [![Join Slack](https://img.shields.io/badge/join-slack-infomational)](https://aka.ms/playwright-slack)

## [Documentation](https://playwright.dev/dotnet) | [API reference](https://playwright.dev/dotnet/docs/api/class-playwright/)

Playwright is a .NET library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) with a single API. Playwright is built to enable cross-browser web automation that is **ever-green**, **capable**, **reliable** and **fast**.

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| <!-- GEN:chromium-version -->92.0.4498.0<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| <!-- GEN:webkit-version -->14.2<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| <!-- GEN:firefox-version -->89.0b6<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |

Headless execution is supported for all the browsers on all platforms. Check out [system requirements](https://playwright.dev/docs/intro/#system-requirements) for details.

# Usage 

* [Getting started](https://playwright.dev/dotnet/docs/intro)
* [Installation configuration](https://playwright.dev/dotnet/docs/installation)
* [API reference](https://playwright.dev/dotnet/docs/api/class-playwright)

## Capabilities

Playwright is built to automate the broad and growing set of web browser capabilities used by Single Page Apps and Progressive Web Apps.

* Scenarios that span multiple page, domains and iframes
* Auto-wait for elements to be ready before executing actions (like click, fill)
* Intercept network activity for stubbing and mocking network requests
* Emulate mobile devices, geolocation, permissions
* Support for web components via shadow-piercing selectors
* Native input events for mouse and keyboard
* Upload and download files

# Examples

## Mobile and geolocation

This snippet emulates Mobile Safari on a device at a given geolocation, navigates to maps.google.com, performs an action, and takes a screenshot.

```cs 
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Webkit.LaunchAsync(headless: false);

var contextOptions = playwright.Devices["iPhone 11 Pro"].ToBrowserContextOptions();
contextOptions.Locale = "en-US";
contextOptions.Geolocation = new Geolocation { Longitude = 12.492507m, Latitude = 41.889938m };
contextOptions.Permissions = new[] { ContextPermission.Geolocation };

var context = await browser.NewContextAsync(contextOptions);
var page = await context.NewPageAsync();
await page.GotoAsync("https://www.google.com/maps");

await page.ClickAsync(".ml-button-my-location-fab");
await page.WaitForLoadStateAsync(LifecycleEvent.Networkidle);

if ((await page.QuerySelectorAsync(".ml-promotion-no-thanks")) != null)
{
    await page.ClickAsync(".ml-promotion-no-thanks");
}

await page.ScreenshotAsync("colosseum-iphone.png");
```

## Evaluate in browser context

This code snippet navigates to example.com in Firefox and executes a script in the page context.

```cs
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
await page.GotoAsync("https://www.example.com/");
var dimensions = await page.EvaluateAsync<Size>(@"() => {
    return {
        width: document.documentElement.clientWidth,
        height: document.documentElement.clientHeight,
    }
}");
Console.WriteLine(dimensions);
```

## Intercept network requests

This code snippet sets up request routing for a WebKit page to log all network requests.

```cs 
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
// Log and continue all network requests
await page.RouteAsync("**", (route, _) =>
{
    Console.WriteLine(route.Request.Url);
    route.ContinueAsync();
});

await page.GotoAsync("http://todomvc.com");
```

## CLI & Codegen

Playwright conveniently comes with a built-in [`codegen` facility](https://playwright.dev/dotnet/docs/cli) which lets you open an inspector and a browser window, then record the 
actions you perform, generating the relevant source code as you do.

To use it, you need to install the dotnet tool:

```powershell
dotnet-install --global microsoft.playwright.cli
```

Then, you can simply call:

```powershell
playwrightcli codegen https://www.microsoft.com
```

### Driver Discoverability

By default, the tool will attempt to look at `%USERPROFILE%\.nuget\packages\microsoft.playwright` for the location of the driver. If that behaviour
is undesired, you can set the `PW_CLI_DRIVERPATH` environment variable, to specify the location. The location needs to point to the
folder containing the `Drivers` folder, e.g. `C:\Users\username\.nuget\packages\microsoft.playwright\1.11.0-alpha\`.

## Resources

* [Documentation](https://playwright.dev/dotnet/docs/intro/)
* [API reference](https://playwright.dev/dotnet/docs/api/class-playwright/)
* [Community showcase](https://playwright.dev/dotnet/docs/showcase/)
* [Contribution guide](CONTRIBUTING.md)
* [Changelog](https://github.com/microsoft/playwright/releases)
