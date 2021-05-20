# Playwright for .NET 🎭
[![NuGet version](https://img.shields.io/nuget/vpre/Microsoft.Playwright?color=%2345ba4b)](https://www.nuget.org/packages/Microsoft.Playwright) [![Join Slack](https://img.shields.io/badge/join-slack-infomational)](https://aka.ms/playwright-slack)

Playwright for .NET is the official language port of [Playwright](https://playwright.dev), a Node.js based library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) with a single API. Playwright is built to enable cross-browser web automation that is **ever-green**, **capable**, **reliable** and **fast**.

* Website: [https://playwright.dev/](https://playwright.dev/) 
* .NET API Reference: [https://playwright.dev/dotnet/docs/api/class-playwright](https://playwright.dev/dotnet/docs/api/class-playwright) Note: 🚧🏗 _we're still working on this, so it might not be 100% up to date_
* Getting Started: [https://playwright.dev/dotnet/docs/intro](https://playwright.dev/dotnet/docs/intro)

# Getting Started
Install the `Microsoft.Playwright` package from NuGet in Visual Studio, or directly from the CLI in your project root directory:

```powershell
dotnet add package Microsoft.Playwright
```

You can use the following 

```cs
using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightSharp.Demo
{
    class Program
    {
        static async Task Main()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });
            var page = await browser.NewPageAsync();
            await page.GotoAsync("https://bing.com");
            Console.ReadLine();
            await page.ScreenshotAsync(new PageScreenshotOptions() { Path = "bing.png" });
        }
    }
}
```

## Dependencies
The .NET port relies on two external components, Playwright itself, and the browsers. Because Playwright is a Node.js library, required assemblies (including `node`) are downloaded and added to the `bin` folder. 

### Playwright Driver

Playwright drivers will be copied to the `bin` folder at build time. Nuget will rely on the [RuntimeIdentifier](https://docs.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props?WT.mc_id=DT-MVP-5003814#runtimeidentifier) to copy a platform-specific driver, or on the [runtime used on dotnet publish](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish?WT.mc_id=DT-MVP-5003814).
If the [RuntimeIdentifier](https://docs.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props?WT.mc_id=DT-MVP-5003814#runtimeidentifier) is not set, all runtimes will be copied inside a runtimes folder. Then, the platform-specific driver will be picked at run-time.

### Browsers

For most scenarios, browsers are installed automatically. See [this upstream documentation](https://playwright.dev/docs/installation#managing-browser-binaries) for more information.

> Note: we're still working on some changes, and we'll update the Readme as we continue. For now, stuff like remote connect is not documented. 

> If none of the scenarios mentioned above cover your scenario, you can install the browsers programmatically using `await Playwright.InstallAsync();`

# Capabilities

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
This snippet emulates Mobile Safari on a device at a given geolocation, navigates to Bing Maps, performs an action, and takes a screenshot.

```cs 
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });

var context = await browser.NewContextAsync(new BrowserNewContextOptions()
{
    IsMobile = true,
    Locale = "en-US",
    Geolocation = new Geolocation { Longitude = 12.492507f, Latitude = 41.889938f },
    Permissions = new[] { ContextPermissions.Geolocation }
});

var page = await context.NewPageAsync();
await page.GotoAsync("https://www.bing.com/maps");

await page.ClickAsync(".bnp_btn_accept");
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

await page.ScreenshotAsync(new PageScreenshotOptions() { Path = "colosseum-iphone.png" });
```

## Evaluate in browser context
This code snippet navigates to bing.com in Firefox and executes a script in the page context. This also demonstrates how to interact with the javascript context and get values from it, converting them (where possible) to .NET objects (i.e. `Rect` in our example).

```cs
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
await page.GotoAsync("https://www.bing.com/");
var dimensions = await page.EvaluateAsync<Size>(@"() => {
return {
    width: document.documentElement.clientWidth,
    height: document.documentElement.clientHeight,
}}");
Console.WriteLine($"Dimensions: {dimensions.Width} x {dimensions.Height}");
```

## Intercept network requests

This code snippet sets up request routing for a WebKit page to log all network requests.

```cs 
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
// Log and continue all network requests
await page.RouteAsync("**", (route) =>
{
    Console.WriteLine($"Route intercepted: ${route.Request.Url}");
    route.ContinueAsync();
});

await page.GotoAsync("http://todomvc.com");
```

# Useful links

* [StackOverflow](https://stackoverflow.com/search?q=playwright-sharp)
* [Issues](https://github.com/microsoft/playwright-sharp/issues?utf8=%E2%9C%93&q=is%3Aissue)
* [Documentation](https://playwright.dev/dotnet/docs/intro/)
* [API reference](https://playwright.dev/dotnet/docs/api/class-playwright/)
* [Community showcase](https://playwright.dev/dotnet/docs/showcase/)
* [Contribution guide](CONTRIBUTING.md)
* [Changelog](https://github.com/microsoft/playwright/releases)

## Browser Support/Current Versions

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| <!-- GEN:chromium-version -->92.0.4498.0<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| <!-- GEN:webkit-version -->14.2<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| <!-- GEN:firefox-version -->89.0b6<!-- GEN:stop --> | :white_check_mark: | :white_check_mark: | :white_check_mark: |

Headless execution is supported for all browsers on all platforms.