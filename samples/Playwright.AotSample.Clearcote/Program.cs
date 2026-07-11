using Microsoft.Playwright;

Console.WriteLine("Playwright .NET + Clearcote Browser - NativeAOT validation");
Console.WriteLine();

var cacheDir = Path.Combine(Path.GetTempPath(), "clearcote-cache");
var profilePath = Path.Combine(Path.GetTempPath(), "clearcote-aot-profile.json");
string? downloadPath = null;
IPlaywright? playwright = null;
IBrowser? browser = null;
IPage? page = null;

try
{
    await RunGroupAsync("clearcote-download", async () =>
    {
        downloadPath = await ClearcoteBrowser.DownloadAsync(new()
        {
            CacheDir = cacheDir,
            Quiet = true,
        });
        Assert(File.Exists(downloadPath), "Clearcote executable was not resolved.");
        Console.WriteLine($"  Binary: {downloadPath}");
    });

    await RunGroupAsync("clearcote-profile-json", () =>
    {
        var profile = new ClearcoteProfile("aot-sample", new()
        {
            Fingerprint = "playwright-aot-sample",
            Brand = "Chrome",
            BrandVersion = "149",
            TlsProfile = ClearcoteTlsProfile.MatchPersona,
            HardwareConcurrency = 8,
            Humanize = true,
            DisableGpuFingerprint = true,
            FingerprintNoise = false,
        });

        profile.Save(profilePath);
        var loaded = ClearcoteProfile.Load(profilePath);
        Assert(loaded.Options.Fingerprint == "playwright-aot-sample", "Profile fingerprint did not round-trip.");
        Assert(loaded.Options.Brand == "Chrome", "Profile brand did not round-trip.");
        return Task.CompletedTask;
    });

    playwright = await Playwright.CreateAsync();

    await RunGroupAsync("clearcote-launch-profile", async () =>
    {
        browser = await ClearcoteBrowser.LaunchAsync(playwright, new()
        {
            Profile = profilePath,
            Headless = true,
            CacheDir = cacheDir,
            Quiet = true,
        });
        Assert(!string.IsNullOrWhiteSpace(browser.Version), "Clearcote browser version should be available.");
        page = await browser.NewPageAsync();
        Assert(page != null, "Clearcote page should be created.");
        Console.WriteLine($"  Browser version: {browser.Version}");
    });

    await RunGroupAsync("clearcote-page-evaluate", async () =>
    {
        var activePage = page ?? throw new InvalidOperationException("Clearcote page was not created.");
        var html = """
<!doctype html>
<title>Clearcote NativeAOT</title>
<main>
  <h1>Clearcote NativeAOT validation</h1>
  <p>Offline page used to validate launch, JavaScript evaluation, WebGL probing, and screenshots.</p>
</main>
""";
        await activePage.GotoAsync(
            "data:text/html;charset=utf-8," + Uri.EscapeDataString(html),
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await activePage.TitleAsync() == "Clearcote NativeAOT", "Unexpected Clearcote page title.");

        var userAgent = await activePage.EvaluateAsync<string>("() => navigator.userAgent");
        Assert(userAgent.Contains("Chrome/149", StringComparison.Ordinal), "User agent should reflect the pinned Chrome major.");
        Console.WriteLine($"  User agent: {userAgent}");
    });

    await RunGroupAsync("clearcote-render-probe", async () =>
    {
        var activePage = page ?? throw new InvalidOperationException("Clearcote page was not created.");
        var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(activePage);
        Assert(verdict.Webgl, "WebGL should be available.");
        Assert(!string.IsNullOrWhiteSpace(verdict.Renderer), "Renderer string should be available.");
        Console.WriteLine($"  Render coherent on this host: {verdict.Coherent}");
        Console.WriteLine($"  Vendor:   {verdict.Vendor}");
        Console.WriteLine($"  Renderer: {verdict.Renderer}");
        foreach (var warning in verdict.Warnings)
        {
            Console.WriteLine($"  Warning:  {warning}");
        }
    });

    await RunGroupAsync("clearcote-screenshot", async () =>
    {
        var activePage = page ?? throw new InvalidOperationException("Clearcote page was not created.");
        var screenshotPath = "clearcote-screenshot.png";
        var bytes = await activePage.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
        Assert(bytes.Length > 0, "Clearcote screenshot bytes should not be empty.");
        Assert(File.Exists(screenshotPath), "Clearcote screenshot file was not written.");
        Console.WriteLine($"  Screenshot: {screenshotPath}");
    });
}
finally
{
    if (browser != null)
    {
        await browser.CloseAsync();
    }

    playwright?.Dispose();

    if (File.Exists(profilePath))
    {
        File.Delete(profilePath);
    }
}

Console.WriteLine("Clearcote NativeAOT validation complete.");

static async Task RunGroupAsync(string name, Func<Task> action)
{
    try
    {
        await action().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
        throw;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
