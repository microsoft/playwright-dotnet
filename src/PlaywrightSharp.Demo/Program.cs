using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var playwright = new Playwright();
            var chromium = await playwright.GetChromiumBrowserAsync();
            var browser = await chromium.LaunchAsync(new LaunchOptions{ Headless = false });
            var page = await browser.NewPageAsync();
            await page.GoToAsync("https://example.com");
        }
    }
}
