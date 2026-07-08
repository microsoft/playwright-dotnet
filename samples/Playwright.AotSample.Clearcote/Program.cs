using Microsoft.Playwright;

Console.WriteLine("Playwright .NET + Clearcote Browser — NativeAOT sample");
Console.WriteLine();

// 1. Ensure the Clearcote browser binary is downloaded.
var downloadPath = await ClearcoteBrowser.DownloadAsync(new()
{
    Quiet = true,
});
Console.WriteLine($"Clearcote browser binary: {downloadPath}");

// 2. Launch with a realistic Windows fingerprint in headless mode.
var launchOptions = new ClearcoteLaunchOptions
{
    Headless = true,
    Fingerprint = "playwright-aot-sample",
    ClearcotePlatform = "windows",
    Brand = "Chrome",
    BrandVersion = "149",
    GpuVendor = "Google",
    GpuRenderer = "ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device))",
    HardwareConcurrency = 8,
    Humanize = true,
};

var playwright = await Playwright.CreateAsync();
var browser = await ClearcoteBrowser.LaunchAsync(playwright.Chromium, launchOptions);
var page = await browser.NewPageAsync();

await page.GotoAsync("https://www.whatsmyua.info");
Console.WriteLine($"Page title: {await page.TitleAsync()}");

// 3. Check render coherence (Clearcote-specific API).
var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
Console.WriteLine($"Render coherent: {verdict.Coherent}");
Console.WriteLine($"  Vendor:   {verdict.Vendor}");
Console.WriteLine($"  Renderer: {verdict.Renderer}");
Console.WriteLine($"  WebGL:    {verdict.Webgl}");

// 4. Screenshot.
var screenshotPath = "clearcote-screenshot.png";
await page.ScreenshotAsync(new() { Path = screenshotPath });
Console.WriteLine($"Screenshot saved to {screenshotPath}");

await browser.CloseAsync();
Console.WriteLine("Done.");
