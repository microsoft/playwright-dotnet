using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>permissions.spec.js</playwright-file>
    ///<playwright-describe>Permissions</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PermissionsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PermissionsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should be prompt by default</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBePromptByDefault()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should deny permission when not listed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDenyPermissionWhenNotListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(Array.Empty<ContextPermission>(), TestConstants.EmptyPage);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should fail when bad permission is given</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldFailWhenBadPermissionIsGiven() { }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should grant geolocation permission when listed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldGrantGeolocationPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should grant notifications permission when listed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldGrantNotificationsPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should accumulate when adding</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAccumulateWhenAdding()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            await Context.GrantPermissionsAsync(ContextPermission.Notifications);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(Page, "notifications"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should clear permissions</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should grant permission when listed for all domains</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldGrantPermissionWhenListedForAllDomains()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should grant permission when creating context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should reset permissions</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldResetPermissions()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.GrantPermissionsAsync(ContextPermission.Geolocation, TestConstants.EmptyPage);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should trigger permission onchange</playwright-it>
        [Fact(Skip = "Skipped in Playwright")]
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

        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should trigger permission onchange</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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


        ///<playwright-file>permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should support clipboard read</playwright-it>
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
