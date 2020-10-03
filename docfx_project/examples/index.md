# 🎭 [Playwright](https://playwright.dev) for .NET [![NuGet version](https://buildstats.info/nuget/PlaywrightSharp)](https://www.nuget.org/packages/PlaywrightSharp) [![Join Slack](https://img.shields.io/badge/join-slack-infomational)](https://join.slack.com/t/playwright/shared_invite/enQtOTEyMTUxMzgxMjIwLThjMDUxZmIyNTRiMTJjNjIyMzdmZDA3MTQxZWUwZTFjZjQwNGYxZGM5MzRmNzZlMWI5ZWUyOTkzMjE5Njg1NDg)

PlaywrightSharp is a .Net library to automate [Chromium](https://www.chromium.org/Home), [Firefox](https://www.mozilla.org/en-US/firefox/new/) and [WebKit](https://webkit.org/) browsers with a single API. Playwright delivers automation that is **ever-green**, **capable**, **reliable** and **fast**. [See how Playwright is better](https://playwright.dev/#path=docs%2Fwhy-playwright.md&q=).

## Take screenshots

```cs
await Playwright.InstallAsync();
var playwright = await Playwright.CreateAsync();

await using var browser = await playwright.Chromium.LaunchAsync(headless: false);
var page = await browser.NewPageAsync();
await page.GoToAsync("http://www.google.com");
await page.ScreenshotAsync(path: outputFile);
```

You can also change the view port before generating the screenshot

```cs
await page.SetViewportSizeAsync(width: 500, height: 500);
```

## Generate PDF files

```cs
await Playwright.InstallAsync();
var playwright = await Playwright.CreateAsync();

await using var browser = await playwright.Chromium.LaunchAsync(headless: true);
var page = await browser.NewPageAsync();

await page.GoToAsync("http://www.google.com");
await page.GetPdfAsync(path: outputFile);
```

## Inject HTML

```cs
var page = await browser.NewPageAsync();
await page.SetContentAsync("<div>My Receipt</div>");
var result = await page.GetContentAsync();
await page.GetPdfAsync(outputFile);
SaveHtmlToDB(result);
```

## Evaluate Javascript

```cs
var page = await browser.NewPageAsync();
var seven = await page.EvaluateAsync<int>("4 + 3");
var someObject = await page.EvaluateAsync<dynamic>("(value) => ({a: value})", 5);
Console.WriteLine(someObject.a);
```

## Wait For Selector

```cs
var page = await browser.NewPageAsync();
await page.GoToAsync("http://www.spapage.com");
await page.WaitForSelectorAsync("div.main-content");
await page.GetPdfAsync(outputFile);
```

## Wait For Function

```cs
var page = await Browser.NewPageAsync();
await page.GoToAsync("http://www.spapage.com");
var watchDog = page.WaitForFunctionAsync(" () => window.innerWidth < 100");
await page.SetViewportSizeAsync(width: 50, height: 50);
await watchDog;
```

## Connect to a remote browser

```cs
var options = new ConnectOptions()
{
    BrowserWSEndpoint = 
};

var url = "https://www.google.com/";

await using (var browser = await playwright.Chromium.ConnectAsync($"wss://www.externalbrowser.io?token={apikey}"))
{
    var page = await browser.NewPageAsync();
    await page.GoToAsync(url);
    await page.GetPdfAsync("wot.pdf");
}
```
