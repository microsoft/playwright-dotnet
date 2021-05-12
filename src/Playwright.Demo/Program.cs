using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightSharp.Demo
{
    class Program
    {
        static async Task Main()
        {
            using var playwright = await Playwright.CreateAsync();
            var chromium = playwright.Chromium;
            var browser = await chromium.LaunchAsync(headless: false);
            var page = await browser.NewPageAsync();
            await page.GotoAsync("https://example.com");
            Console.ReadLine();
        }
    }
}
