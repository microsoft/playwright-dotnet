using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextClearCookiesTests : PageTestEx
    {
        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should clear cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldClearCookies()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie1",
                    Value = "1"
                }
            });
            Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
            await Context.ClearCookiesAsync();
            Assert.IsEmpty(await Context.CookiesAsync());
            await Page.ReloadAsync();
            Assert.IsEmpty(await Page.EvaluateAsync<string>("document.cookie"));
        }

        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should isolate cookies when clearing")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateWhenClearing()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "page1cookie",
                    Value = "page1value",
                    Url = Server.EmptyPage
                }
            });

            await anotherContext.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "page2cookie",
                    Value = "page2value",
                    Url = Server.EmptyPage
                }
            });

            Assert.That(await Context.CookiesAsync(), Has.Count.EqualTo(1));
            Assert.That(await anotherContext.CookiesAsync(), Has.Count.EqualTo(1));

            await Context.ClearCookiesAsync();
            Assert.IsEmpty((await Context.CookiesAsync()));
            Assert.That((await anotherContext.CookiesAsync()), Has.Count.EqualTo(1));

            await anotherContext.ClearCookiesAsync();
            Assert.IsEmpty(await Context.CookiesAsync());
            Assert.IsEmpty(await anotherContext.CookiesAsync());
        }
    }
}
