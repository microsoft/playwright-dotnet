using System.Threading.Tasks;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Playwright.LocalNugetTest.MTP;

[TestClass]
public class ConfigurationTests : PageTest
{
    [TestMethod]
    public async Task ShouldConfigureThePackagedFixtures()
    {
        Assert.AreEqual("firefox", BrowserName);
        Assert.AreEqual("fr-FR", await Page.EvaluateAsync<string>("() => navigator.language").ConfigureAwait(false));
        Assert.AreEqual("Europe/Madrid", await Page.EvaluateAsync<string>("() => Intl.DateTimeFormat().resolvedOptions().timeZone").ConfigureAwait(false));
        Assert.AreEqual(960, await Page.EvaluateAsync<int>("() => innerWidth").ConfigureAwait(false));
        Assert.AreEqual(540, await Page.EvaluateAsync<int>("() => innerHeight").ConfigureAwait(false));
        await Page.SetContentAsync("<div data-package-id='target'>packaged</div>").ConfigureAwait(false);
        await Expect(Page.GetByTestId("target")).ToHaveTextAsync("packaged").ConfigureAwait(false);
    }
}
