using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

using static Microsoft.Playwright.Assertions;

namespace Playwright.TestingHarnessTest.NUnit
{

    public class SimpleSmokeTests : PageTest
    {
        [Test]
        public async Task ShouldOpenPlaywright()
        {
            var path = Path.GetFullPath("index.html");
            Assert.IsNotNull(Page);
            await Page.GotoAsync("file://" + path);
            var h1 = await Page.TextContentAsync("h1");
            Assert.AreEqual("Getting started.", h1);

            var title = await Page.EvaluateAsync<string>("() => document.title");
            Assert.AreEqual("This is a website.", title);

            await Expect(Page.Locator("h1")).ToBeVisibleAsync();
        }
    }
}
