using System;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;

namespace PlaywrightSharp.Tests.DumpIO
{
    class Program
    {
        public const string ChromiumProduct = "CHROMIUM";
        public const string WebkitProduct = "WEBKIT";
        public const string FirefoxProduct = "FIREFOX";

        public static async Task Main(string[] args)
        {
            var options = new LaunchOptions
            {
                Headless = true,
                DumpIO = true,
                ExecutablePath = args[0]
            };

            var playwright = args[1] switch
            {
                WebkitProduct => null,
                FirefoxProduct => null,
                ChromiumProduct => new ChromiumBrowserType(),
                _ => throw new ArgumentOutOfRangeException($"product {args[1]} does not exist")
            };

            var browser = await playwright.LaunchAsync(options);
            var page = await browser.DefaultContext.NewPageAsync();
            await page.CloseAsync();
            await browser.CloseAsync();
        }
    }
}
