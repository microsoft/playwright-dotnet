using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PermissionsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PermissionsTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should be prompt by default")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldBePromptByDefault()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should deny permission when not listed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldDenyPermissionWhenNotListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(Array.Empty<ContextPermission>(), TestConstants.EmptyPage);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should fail when bad permission is given")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldFailWhenBadPermissionIsGiven() { }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should grant geolocation permission when listed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantGeolocationPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should grant notifications permission when listed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantNotificationsPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should accumulate when adding")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldAccumulateWhenAdding()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            await Context.GrantPermissionsAsync(ContextPermission.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should clear permissions")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldClearPermissions()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            await Context.GrantPermissionsAsync(ContextPermission.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
            Assert.NotEqual("granted", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should grant permission when listed for all domains")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenListedForAllDomains()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should grant permission when creating context")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenCreatingContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Permissions = new[] { ContextPermission.Geolocation },
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("granted", await GetPermissionAsync(page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should reset permissions")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldResetPermissions()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation, TestConstants.EmptyPage);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should trigger permission onchange")]
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
            await Context.GrantPermissionsAsync(Array.Empty<ContextPermission>(), TestConstants.EmptyPage);
            Assert.Equal(new[] { "prompt", "denied" }, await Page.EvaluateAsync<string[]>("window.events"));
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation, TestConstants.EmptyPage);
            Assert.Equal(
                new[] { "prompt", "denied", "granted" },
                await Page.EvaluateAsync<string[]>("window.events"));
            await Context.ClearPermissionsAsync();
            Assert.Equal(
                new[] { "prompt", "denied", "granted", "prompt" },
                await Page.EvaluateAsync<string[]>("window.events"));
        }

        [PlaywrightTest("permissions.spec.ts", "permissions", "should isolate permissions between browser contexts")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldIsolatePermissionsBetweenBrowserContexts()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await using var otherContext = await Browser.NewContextAsync();
            var otherPage = await otherContext.NewPageAsync();
            await otherPage.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("prompt", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.GrantPermissionsAsync(Array.Empty<ContextPermission>(), TestConstants.EmptyPage);
            await otherContext.GrantPermissionsAsync(ContextPermission.Geolocation, TestConstants.EmptyPage);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await otherContext.CloseAsync();
        }


        [PlaywrightTest("permissions.spec.ts", "permissions", "should support clipboard read")]
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
