# 🎭 [Playwright](https://playwright.dev) for .NET [![NuGet version](https://buildstats.info/nuget/PlaywrightSharp)](https://www.nuget.org/packages/PlaywrightSharp) [![Join Slack](https://img.shields.io/badge/join-slack-infomational)](https://join.slack.com/t/playwright/shared_invite/enQtOTEyMTUxMzgxMjIwLThjMDUxZmIyNTRiMTJjNjIyMzdmZDA3MTQxZWUwZTFjZjQwNGYxZGM5MzRmNzZlMWI5ZWUyOTkzMjE5Njg1NDg)

PlaywrightSharp is a .Net library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) browsers with a single API. Playwright delivers automation that is **ever-green**, **capable**, **reliable** and **fast**. [See how Playwright is better](https://playwright.dev/#path=docs%2Fwhy-playwright.md&q=).

|          | Linux | macOS | Windows |
|   :---   | :---: | :---: | :---:   |
| Chromium <!-- GEN:chromium-version -->86.0.4238.0<!-- GEN:stop --> | ✅ | ✅ | ✅ |
| WebKit 14.0 | ✅ | ✅ | ✅ |
| Firefox <!-- GEN:firefox-version -->80.0b8<!-- GEN:stop --> | ✅ | ✅ | ✅ |

Headless execution is supported for all browsers on all platforms.

#Usage 
Playwright Sharp relies on two external components: The browsers and the Playwright driver.

```cs 
await Playwright.InstallAsync();
var playwright = await Playwright.CreateAsync();
```

`Playwright.InstallAsync()` will download the required browsers. `Playwright.CreateAsync()` will create and launch the playwright driver.

```cs
await Playwright.InstallAsync();
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync();
var page = await browser.NewPageAsync();
await page.GoToAsync("http://www.bing.com");
await page.ScreenshotAsync(path: outputFile);
```

To avoid downloading the browsers in runtime, you can install the playwright-sharp dotnet tool:

```
dotnet tool install playwright-sharp-tool -g
playwright-sharp install-browsers
```

By running these two commands, you can avoid having the `await Playwright.InstallAsync();` line in your code.

# Examples
## Mobile and geolocation
This snippet emulates Mobile Safari on a device at a given geolocation, navigates to maps.google.com, performs an action, and takes a screenshot.

```cs 
await Playwright.InstallAsync();

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Webkit.LaunchAsync(false);

var contextOptions = playwright.Devices["iPhone 11 Pro"].ToBrowserContextOptions();
contextOptions.Locale = "en-US";
contextOptions.Geolocation = new Geolocation { Longitude = 12.492507m, Latitude = 41.889938m };
contextOptions.Permissions = new[] { ContextPermission.Geolocation };

var context = await browser.NewContextAsync(contextOptions);
var page = await context.NewPageAsync();
await page.GoToAsync("https://maps.google.com");
page.ClickAsync("text='Your location'"); //
await page.ScreenshotAsync("colosseum-iphone.png");
```

## Evaluate in browser context
This code snippet navigates to example.com in Firefox, and executes a script in the page context.

```cs
await Playwright.InstallAsync();
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync();

var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
await page.GoToAsync("https://www.example.com/");
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
await Playwright.InstallAsync();
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

await page.GoToAsync("http://todomvc.com");
```

# Monthly reports
 * [October 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-oct-2020)
 * [September 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-sep-2020)
 * [July 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-jul-2020)
 * [June 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-jun-2020)
 * [May 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-may-2020)
 * [April 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-apr-2020)
 * [March 2020](https://www.hardkoded.com/blog/playwright-sharp-monthly-march-2020)

# Useful links

* [StackOverflow](https://stackoverflow.com/search?q=playwright-sharp)
* [Issues](https://github.com/microsoft/playwright-sharp/issues?utf8=%E2%9C%93&q=is%3Aissue)
