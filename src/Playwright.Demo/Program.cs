using System;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightSharp.Demo
{
    /// <summary>
    /// Contains the code for the quick snippets on the Readme page.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            await GettingStarted();
            await GeolocationExample();
            await EvaluateInBrowserContext();
            await RouteIntercept();

        }

        static async Task GettingStarted()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });
            var page = await browser.NewPageAsync();
            await page.GotoAsync("https://example.com");
            Console.ReadLine();
            await page.ScreenshotAsync(new PageScreenshotOptions() { Path = "bing.png" });
        }

        static async Task GeolocationExample()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions() { Headless = false });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions()
            {
                IsMobile = true,
                Locale = "en-US",
                Geolocation = new Geolocation { Longitude = 12.492507f, Latitude = 41.889938f },
                Permissions = new[] { ContextPermissions.Geolocation }
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync("https://www.bing.com/maps");

            await page.ClickAsync(".bnp_btn_accept");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.ScreenshotAsync(new PageScreenshotOptions() { Path = "colosseum-iphone.png" });
        }

        static async Task EvaluateInBrowserContext()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Firefox.LaunchAsync();

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync("https://www.bing.com/");
            var dimensions = await page.EvaluateAsync<Size>(@"() => {
    return {
        width: document.documentElement.clientWidth,
        height: document.documentElement.clientHeight,
    }}");
            Console.WriteLine($"Dimensions: {dimensions.Width} x {dimensions.Height}");
        }

        static async Task RouteIntercept()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Firefox.LaunchAsync();

            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            // Log and continue all network requests
            await page.RouteAsync("**", (route) =>
            {
                Console.WriteLine($"Route intercepted: ${route.Request.Url}");
                route.ContinueAsync();
            });

            await page.GotoAsync("http://todomvc.com");
        }
    }
}
