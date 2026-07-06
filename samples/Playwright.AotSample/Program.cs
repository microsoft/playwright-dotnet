using Microsoft.Playwright;

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
var page = await browser.NewPageAsync();
await page.GotoAsync("https://www.google.com");
var title = await page.TitleAsync();
Console.WriteLine($"Page title: {title}");
await page.ScreenshotAsync(new() { Path = "google.png" });
Console.WriteLine("Screenshot saved to google.png");
await browser.CloseAsync();
