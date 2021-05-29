using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PermissionsTests : PageTestEx
    {
        [PlaywrightTest("permissions.spec.ts", "should be prompt by default")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldBePromptByDefault()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should deny permission when not listed")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldDenyPermissionWhenNotListed()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(Array.Empty<string>(), new BrowserContextGrantPermissionsOptions { Origin = TestConstants.EmptyPage });
            Assert.AreEqual("denied", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should fail when bad permission is given")]
        [Test, Ignore("We don't need this test")]
        public void ShouldFailWhenBadPermissionIsGiven() { }

        [PlaywrightTest("permissions.spec.ts", "should grant geolocation permission when listed")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldGrantGeolocationPermissionWhenListed()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should grant notifications permission when listed")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldGrantNotificationsPermissionWhenListed()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { "notifications" });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "should accumulate when adding")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldAccumulateWhenAdding()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            await Context.GrantPermissionsAsync(new[] { "notifications" });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "should clear permissions")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldClearPermissions()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            await Context.GrantPermissionsAsync(new[] { "notifications" });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
            Assert.That("granted", Is.Not.EqualTo(await GetPermissionAsync(Page, "geolocation")));
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "should grant permission when listed for all domains")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenListedForAllDomains()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { "geolocation" });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should grant permission when creating context")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenCreatingContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Permissions = new[] { "geolocation" },
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual("granted", await GetPermissionAsync(page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should reset permissions")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldResetPermissions()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { "geolocation" }, new BrowserContextGrantPermissionsOptions { Origin = TestConstants.EmptyPage });
            Assert.AreEqual("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should trigger permission onchange")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldTriggerPermissionOnchange()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                window.events = [];
                return navigator.permissions.query({ name: 'geolocation'}).then(function(result) {
                    window.events.push(result.state);
                    result.onchange = function() {
                        window.events.push(result.state);
                    };
                });
            }");
            Assert.AreEqual(new[] { "prompt" }, await Page.EvaluateAsync<string[]>("window.events"));
            await Context.GrantPermissionsAsync(Array.Empty<string>(), new BrowserContextGrantPermissionsOptions { Origin = TestConstants.EmptyPage });
            Assert.AreEqual(new[] { "prompt", "denied" }, await Page.EvaluateAsync<string[]>("window.events"));
            await Context.GrantPermissionsAsync(new[] { "geolocation" }, new BrowserContextGrantPermissionsOptions { Origin = TestConstants.EmptyPage });
            Assert.AreEqual(
                new[] { "prompt", "denied", "granted" },
                await Page.EvaluateAsync<string[]>("window.events"));
            await Context.ClearPermissionsAsync();
            Assert.AreEqual(
                new[] { "prompt", "denied", "granted", "prompt" },
                await Page.EvaluateAsync<string[]>("window.events"));
        }

        [PlaywrightTest("permissions.spec.ts", "should trigger permission onchange")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldIsolatePermissionsBetweenBrowserContexts()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await using var otherContext = await Browser.NewContextAsync();
            var otherPage = await otherContext.NewPageAsync();
            await otherPage.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.AreEqual("prompt", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.GrantPermissionsAsync(Array.Empty<string>(), new BrowserContextGrantPermissionsOptions { Origin = TestConstants.EmptyPage });
            await otherContext.GrantPermissionsAsync(new[] { "geolocation" }, new BrowserContextGrantPermissionsOptions { Origin = TestConstants.EmptyPage });
            Assert.AreEqual("denied", await GetPermissionAsync(Page, "geolocation"));
            Assert.AreEqual("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.ClearPermissionsAsync();
            Assert.AreEqual("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.AreEqual("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await otherContext.CloseAsync();
        }


        [PlaywrightTest("permissions.spec.ts", "should support clipboard read")]
        [Test, Ignore("Skipped in Playwright")]
        public void ShouldSupportClipboardRead()
        {
        }

        private static Task<string> GetPermissionAsync(IPage page, string name)
            => page.EvaluateAsync<string>(
                "name => navigator.permissions.query({ name }).then(result => result.state)",
                name);
    }
}
