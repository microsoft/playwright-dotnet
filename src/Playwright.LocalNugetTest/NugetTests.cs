using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PlaywrightSharp.LocalNugetTest
{
    public class NugetTests
    {
        [Fact]
        public async Task ShouldWork()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();

            var page = await browser.NewPageAsync();
            Console.WriteLine("Navigating google");
            await page.GoToAsync("http://www.google.com");

            Assert.Contains("Google", await page.GetTitleAsync());
        }
    }
}
