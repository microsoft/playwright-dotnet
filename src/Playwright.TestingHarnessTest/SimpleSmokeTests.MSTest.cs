using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Playwright.TestingHarnessTest.MSTest
{
    [TestClass]
    [TestCategory("Smoke")]
    public class SimpleSmokeTests : PageTest
    {
        [TestMethod]
        public async Task ShouldOpenPlaywright()
        {
            var path = Path.GetFullPath("index.html");
            await Page.GotoAsync("file://" + path);
            var h1 = await Page.TextContentAsync("h1");
            Assert.AreEqual("Getting started.", h1);

            var title = await Page.EvaluateAsync<string>("() => document.title");
            Assert.AreEqual("This is a website.", title);

            await Expect(Page.Locator("h1")).ToBeVisibleAsync();


            Assert.AreEqual(1920, await Page.EvaluateAsync<int>("() => window.innerWidth"));
            Assert.AreEqual(1080, await Page.EvaluateAsync<int>("() => window.innerHeight"));

            Assert.AreEqual("Foobar", await Page.EvaluateAsync<string>("() => navigator.userAgent"));

            await Page.GotoAsync("https://httpbin.org/headers");
            Assert.AreEqual("KekStarValue", await Page.EvaluateAsync<string>("() => JSON.parse(document.body.innerText).headers.Kekstar"));

        }

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions()
            {
                ColorScheme = ColorScheme.Light,
                UserAgent = "Foobar",
                ViewportSize = new()
                {
                    Width = 1920,
                    Height = 1080
                },
                ExtraHTTPHeaders = new Dictionary<string, string> {
                    { "Kekstar", "KekStarValue" }
                }
            };
        }
    }
}
