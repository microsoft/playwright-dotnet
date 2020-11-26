using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp;

namespace PdfDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new LaunchOptions { Headless = true });

            Console.WriteLine("Navigating microsoft");
            var page = await browser.NewPageAsync();
            await page.GoToAsync("http://www.microsoft.com");

            Console.WriteLine("Taking Screenshot");
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "microsoft.png"), await page.ScreenshotAsync());

            Console.WriteLine("Export completed");
        }
    }
}