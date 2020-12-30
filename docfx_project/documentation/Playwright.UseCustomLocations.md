# How to adjust some common settings, including set BrowsersPath and DriverExecutablePath but not limited
_Contributors: [Scott Huang](https://github.com/ScottHuangZL), [Dario Kondratiuk](https://www.hardkoded.com/)_

## Problem

You want to take the playwright driver or the browsers from a custom location.

## Solution

You can set a custom path for the driver or browsers passing a `browserPath` or `driverExecutable` to the `Playwright.CreateAsync` function.

```cs
using var playwright = await Playwright.CreateAsync(
    browsersPath: "customBrowserPath", 
    driverExecutablePath: "customDriversPath");
```

You could also alter the default location of these two artifacts using environment variables without changing your code.
You can use the environment variable `PLAYWRIGHT_BROWSERS_PATH` to set the default browsers path and `PLAYWRIGHT_DRIVER_PATH`  to set the driver location.