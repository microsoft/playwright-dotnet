using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var playwright = await Playwright.CreateAsync();
            var chromium = playwright.Chromium;
            var browser = await chromium.LaunchAsync(new LaunchOptions { Headless = false });
            var page = await browser.NewPageAsync();
            await page.GoToAsync("https://example.com");
        }
    }
}
