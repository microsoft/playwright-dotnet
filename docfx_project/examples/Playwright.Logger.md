# How to get internal logs
_Contributors: [Dario Kondratiuk](https://www.hardkoded.com/)_

## Problem

You need to get the internal log information to debug a problem.

## Solution

Playwright Sharp uses a [.NET Logger Factory](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.iloggerfactory?view=dotnet-plat-ext-3.1&WT.mc_id=DT-MVP-5003814) to log the communication between the library and the playwright driver.

It uses two categories:
 * All the communication between the library and the driver is under the `PlaywrightSharp.Transport.Connection` category.
 * The debug information coming from the driver, when `debug: true` is set, is under the `PlaywrightSharp.Playwright` category.

You create an `ILoggerFactory` in the same way you would build a logger in ASP.NET. You can also use extension methods, like [AddDebug](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.debugloggerfactoryextensions.adddebug?view=dotnet-plat-ext-3.1&WT.mc_id=DT-MVP-5003814) to send the log in the output window, or [AddConsole](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.consoleloggerextensions.addconsole?view=dotnet-plat-ext-3.1&WT.mc_id=DT-MVP-5003814) to send it to the console.

```cs
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddDebug();
    builder.AddFilter((f, _) => f == "PlaywrightSharp.Playwright");
});

using var playwright = await Playwright.CreateAsync(loggerFactory, debug: true);
```

The `debug` is only needed for the `PlaywrightSharp.Playwright` category. `PlaywrightSharp.Transport.Connection` logs will come with no extra argument.