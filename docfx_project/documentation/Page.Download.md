# How to get download files
_Contributors: [Dario Kondratiuk](https://www.hardkoded.com/)_

## Problem

You want to download and process a file.

## Solution

[Page](https://playwrightsharp.dev/api/PlaywrightSharp.IPage.html) will raise a [Download](https://playwrightsharp.dev/api/PlaywrightSharp.IPage.html#PlaywrightSharp_IPage_Download) event when the browser downloads a file.  
To enable this behavior, you will need to set `acceptDownload` to true when you create a new BrowserContext.  

```cs
var context = await browser.NewContextAsync(acceptDownloads: true);
var persistentContext = await browser.LaunchPersistentContextAsync(userDataDir, acceptDownloads: true);
```

Or 

```cs 
var page = await browser.NewPageAsync(acceptDownloads: true);
```

Once `acceptDownloads` is enabled, you can wait for the download event.

```cs 
page.Download += (sender, e) => Console.WriteLine(e.Download.SuggestedFilename);
```

You can also use the [WaitForEvent](https://playwrightsharp.dev/api/PlaywrightSharp.IPage.html#PlaywrightSharp_IPage_WaitForEvent__1_PlaywrightSharp_PlaywrightEvent___0__Func___0_System_Boolean__System_Nullable_System_Int32__) method, to wait for a download.
```cs 
var downloadTask = page.WaitForEvent(PageEvent.Download);
```

[DownloadEventArgs](https://playwrightsharp.dev/api/PlaywrightSharp.DownloadEventArgs.html) has a property call [Download](https://playwrightsharp.dev/api/PlaywrightSharp.Download.html), which will give you access to all the download information.
From there, you can read the file, delete it, or save it in another location.

```cs 
using var playwright = await Playwright.CreateAsync();
var chromium = playwright.Chromium;
await using var browser = await chromium.LaunchAsync(new LaunchOptions { Headless = false });
var page = await browser.NewPageAsync(acceptDownloads: true);
await page.GoToAsync("https://github.com/microsoft/playwright-sharp/releases/tag/v0.151.0");

var downloadTask = page.WaitForEvent(PageEvent.Download);

await page.ClickAsync("text=Source Code");

var downloadEvent = await downloadTask;
string filePath = await downloadEvent.Download.GetPathAsync();
Console.WriteLine($"Original path: {filePath}");

await downloadEvent.Download.SaveAsAsync("version.zip");
Console.WriteLine($"New file exists: {new FileInfo("version.zip").Exists}");

Console.ReadLine();
```

