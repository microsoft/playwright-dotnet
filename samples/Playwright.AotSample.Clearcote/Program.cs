using Microsoft.Playwright;

Console.WriteLine("Playwright .NET + Clearcote Browser — NativeAOT sample");
Console.WriteLine();

var downloadPath = await ClearcoteBrowser.DownloadAsync(new()
{
    Quiet = true,
});
Console.WriteLine($"Clearcote browser binary: {downloadPath}");

var launchOptions = new ClearcoteLaunchOptions
{
    Headless = true,
    Fingerprint = "playwright-aot-sample",
    ClearcotePlatform = ClearcotePlatform.Linux,
    Brand = "Edge",
    BrandVersion = "149",
    TlsProfile = ClearcoteTlsProfile.MatchPersona,
    GpuVendor = "Google",
    GpuRenderer = "ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device))",
    HardwareConcurrency = 8,
    Humanize = true,
};

var playwright = await Playwright.CreateAsync();
var browser = await ClearcoteBrowser.LaunchAsync(playwright.Chromium, launchOptions);
var page = await browser.NewPageAsync();

await page.GotoAsync("https://browserleaks.com/client-hints");
Console.WriteLine($"Page title: {await page.TitleAsync()}");

var verdict = await ClearcoteBrowser.CheckRenderCoherenceAsync(page);
Console.WriteLine($"Render coherent: {verdict.Coherent}");
Console.WriteLine($"  Vendor:   {verdict.Vendor}");
Console.WriteLine($"  Renderer: {verdict.Renderer}");
Console.WriteLine($"  WebGL:    {verdict.Webgl}");

var screenshotPath = "clearcote-screenshot.png";
await page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
Console.WriteLine($"Screenshot saved to {screenshotPath}");

await browser.CloseAsync();
Console.WriteLine("Done.");
