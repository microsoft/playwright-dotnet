using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PermissionsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PermissionsTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("permissions.spec.ts", "should be prompt by default")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldBePromptByDefault()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should deny permission when not listed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldDenyPermissionWhenNotListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(Array.Empty<string>(), TestConstants.EmptyPage);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should fail when bad permission is given")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldFailWhenBadPermissionIsGiven() { }

        [PlaywrightTest("permissions.spec.ts", "should grant geolocation permission when listed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantGeolocationPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermissions.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should grant notifications permission when listed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantNotificationsPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermissions.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "should accumulate when adding")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldAccumulateWhenAdding()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermissions.Geolocation);
            await Context.GrantPermissionsAsync(ContextPermissions.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "should clear permissions")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldClearPermissions()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermissions.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            await Context.GrantPermissionsAsync(ContextPermissions.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
            Assert.NotEqual("granted", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "should grant permission when listed for all domains")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenListedForAllDomains()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermissions.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should grant permission when creating context")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenCreatingContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Permissions = new[] { ContextPermissions.Geolocation },
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("granted", await GetPermissionAsync(page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should reset permissions")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldResetPermissions()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(new[] { ContextPermissions.Geolocation }, TestConstants.EmptyPage);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "should trigger permission onchange")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldTriggerPermissionOnchange()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                window.events = [];
                return navigator.permissions.query({ name: 'geolocation'}).then(function(result) {
                    window.events.push(result.state);
                    result.onchange = function() {
                        window.events.push(result.state);
                    };
                });
            }");
            Assert.Equal(new[] { "prompt" }, await Page.EvaluateAsync<string[]>("window.events"));
            await Context.GrantPermissionsAsync(Array.Empty<string>(), TestConstants.EmptyPage);
            Assert.Equal(new[] { "prompt", "denied" }, await Page.EvaluateAsync<string[]>("window.events"));
            await Context.GrantPermissionsAsync(new[] { ContextPermissions.Geolocation }, TestConstants.EmptyPage);
            Assert.Equal(
                new[] { "prompt", "denied", "granted" },
                await Page.EvaluateAsync<string[]>("window.events"));
            await Context.ClearPermissionsAsync();
            Assert.Equal(
                new[] { "prompt", "denied", "granted", "prompt" },
                await Page.EvaluateAsync<string[]>("window.events"));
        }

        [PlaywrightTest("permissions.spec.ts", "should trigger permission onchange")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldIsolatePermissionsBetweenBrowserContexts()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await using var otherContext = await Browser.NewContextAsync();
            var otherPage = await otherContext.NewPageAsync();
            await otherPage.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("prompt", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.GrantPermissionsAsync(Array.Empty<string>(), TestConstants.EmptyPage);
            await otherContext.GrantPermissionsAsync(new[] { ContextPermissions.Geolocation }, TestConstants.EmptyPage);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await otherContext.CloseAsync();
        }


        [PlaywrightTest("permissions.spec.ts", "should support clipboard read")]
        [Fact(Skip = "Skipped in Playwright")]
        public void ShouldSupportClipboardRead()
        {
        }

        private static Task<string> GetPermissionAsync(IPage page, string name)
            => page.EvaluateAsync<string>(
                "name => navigator.permissions.query({ name }).then(result => result.state)",
                name);
    }
}
