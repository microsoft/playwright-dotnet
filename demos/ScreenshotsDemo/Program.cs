using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp;
using PlaywrightSharp.Firefox;

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

            Console.WriteLine("Downloading Firefox");
            var firefox = new FirefoxBrowserType();

            await firefox.CreateBrowserFetcher().DownloadAsync();

            Console.WriteLine("Navigating google");
            await using var browser = await firefox.LaunchAsync(options);
            var page = await browser.DefaultContext.NewPageAsync();
            await page.GoToAsync("http://www.microsoft.com");

            Console.WriteLine("Taking Screenshot");
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "microsoft.png"), await page.ScreenshotAsync());

            Console.WriteLine("Export completed");
        }
    }
}