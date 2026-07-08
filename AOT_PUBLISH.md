# NativeAOT Fork Publish Notes

## Build the library

```bash
dotnet build src/Playwright/Playwright.csproj -p:UseSharedCompilation=false
dotnet build src/Playwright/Playwright.csproj \
  -p:TargetFramework=net10.0 \
  -p:PublishAot=true \
  -p:TrimMode=full \
  -p:UseSharedCompilation=false
```

Both commands must complete with zero warnings.

## Build the NuGet package

```bash
dotnet pack src/Playwright/Playwright.csproj \
  -c Debug --no-build --no-restore \
  -p:UseSharedCompilation=false \
  -p:BuildInParallel=false \
  -v:minimal
```

Expected package:

```text
src/Playwright/bin/Debug/Playwright.AOTFork.1.61.0.nupkg
```

The package should contain:

```text
lib/net10.0/Microsoft.Playwright.dll
.playwright/node/linux-x64/node
.playwright/node/win32_x64/node.exe
.playwright/package/cli.js
buildTransitive/Playwright.AOTFork.targets
build/Playwright.AOTFork.targets
```

## Validate package consumption

Use a throwaway project and a temp NuGet cache:

```bash
dotnet new console -o /tmp/playwright-aotfork-consumer --framework net10.0
dotnet add /tmp/playwright-aotfork-consumer/playwright-aotfork-consumer.csproj package Playwright.AOTFork \
  --version 1.61.0 \
  --source src/Playwright/bin/Debug
dotnet restore /tmp/playwright-aotfork-consumer/playwright-aotfork-consumer.csproj \
  -p:RestoreAdditionalProjectSources=$PWD/src/Playwright/bin/Debug \
  -p:RestorePackagesPath=/tmp/nuget-packages
dotnet build /tmp/playwright-aotfork-consumer/playwright-aotfork-consumer.csproj \
  --no-restore \
  -p:RestorePackagesPath=/tmp/nuget-packages \
  -p:UseSharedCompilation=false
```

The consumer output should include:

```text
bin/Debug/net10.0/Microsoft.Playwright.dll
bin/Debug/net10.0/playwright.ps1
bin/Debug/net10.0/.playwright/node/linux-x64/node
bin/Debug/net10.0/.playwright/node/win32_x64/node.exe
bin/Debug/net10.0/.playwright/package/cli.js
```

## Clearcote runtime smoke

Use a writable cache directory so the verified browser archive does not touch the user profile:

```csharp
using Microsoft.Playwright;

_ = await ClearcoteBrowser.DownloadAsync(new()
{
    CacheDir = "/tmp/clearcote-cache",
    Quiet = true,
});

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new ClearcoteLaunchOptions
{
    Headless = true,
    Fingerprint = "default",
    CacheDir = "/tmp/clearcote-cache",
    Quiet = true,
    Args = new[] { "--no-sandbox", "--disable-crash-reporter", "--disable-crashpad" },
});
Console.WriteLine(browser.Version);
```

Expected output for the current Linux pin:

```text
149.0.7827.114
```

## Additional Clearcote helper smoke

```csharp
var profile = new ClearcoteProfile("acct-1", new()
{
    Fingerprint = "acct-1",
    Humanize = true,
    ShowCursor = true,
});
profile.Save();

var ctx = await ClearcoteBrowser.LaunchAgentAsync(playwright.Chromium, new()
{
    AgentLlmKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"),
    AgentModel = "openai/gpt-4o-mini",
    Profile = "acct-1",
});

var page = ctx.Pages.Count > 0 ? ctx.Pages[0] : await ctx.NewPageAsync();
var render = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
```

## NativeAOT publish

```bash
dotnet publish samples/Playwright.AotSample/Playwright.AotSample.csproj \
  -c Release -r linux-x64 \
  -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full \
  -p:UseSharedCompilation=false
```

This environment emits a native Linux executable at:

```text
samples/Playwright.AotSample/bin/Release/net10.0/linux-x64/publish/Playwright.AotSample
```
