using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Playwright.Tests.MSTest
{
        
    public class SimpleSmokeTestsNUnit : PageTest
    {
        [Test]
        public async Task ShouldOpenPlaywright()
        {
            Assert.IsNotNull(Page);
            await Page.GotoAsync("https://www.playwright.dev");
            var h1 = await Page.TextContentAsync("h1");
            Assert.AreEqual("Playwright enables reliable end-to-end testing for modern web apps.", h1);
        }

        [Test]
        public async Task ShouldNavigate()
        {
            Assert.IsNotNull(Page);
            await Page.GotoAsync("https://www.playwright.dev");
            await Page.ClickAsync("text=Get started");
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.DOMContentLoaded);
            Assert.AreEqual("Getting Started", await Page.TextContentAsync("h1"));
        }
    }
}
