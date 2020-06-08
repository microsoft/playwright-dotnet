using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp;
using PlaywrightSharp.Chromium;

namespace PdfDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = new LaunchOptions
            {
                Headless = true
            };

            Console.WriteLine("Downloading chromium");
            var chromium = new ChromiumBrowserType();

            await chromium.CreateBrowserFetcher().DownloadAsync();

            Console.WriteLine("Navigating google");
            await using var browser = await chromium.LaunchAsync(options);
            var page = await browser.DefaultContext.NewPageAsync();
            await page.GoToAsync("http://www.google.com");

            Console.WriteLine("Generating PDF");
            await page.GetPdfAsync(Path.Combine(Directory.GetCurrentDirectory(), "google.pdf"));

            Console.WriteLine("Export completed");
        }
    }
}