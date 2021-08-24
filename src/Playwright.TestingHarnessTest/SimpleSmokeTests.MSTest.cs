using System.Threading.Tasks;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Playwright.Tests.MSTest
{
    [TestClass]
    [TestCategory("Smoke")]
    public class SimpleSmokeTestsMSTest : PageTest
    {
        [TestMethod]
        public async Task ShouldOpenPlaywright()
        {
            Assert.IsNotNull(Page);
            await Page.GotoAsync("https://www.playwright.dev");
            var h1 = await Page.TextContentAsync("h1");
            Assert.AreEqual("Playwright enables reliable end-to-end testing for modern web apps.", h1);
        }

        [TestMethod]
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
