using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>features/permissions.spec.js</playwright-file>
    ///<playwright-describe>Permissions</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class PermissionsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PermissionsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should be prompt by default</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldBePromptByDefault()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should deny permission when not listed</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldDenyPermissionWhenNotListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetPermissionsAsync(TestConstants.EmptyPage);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should fail when bad permission is given</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldFailWhenBadPermissionIsGiven() { }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should grant permission when listed</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldGrantPermissionWhenListed()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetPermissionsAsync(TestConstants.EmptyPage, ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should reset permissions</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldResetPermissions()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetPermissionsAsync(TestConstants.EmptyPage, ContextPermission.Geolocation);
            Assert.Equal("granted", await GetPermissionAsync(Page, "geolocation"));
            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
        }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should trigger permission onchange</playwright-it>
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
            await Context.SetPermissionsAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { "prompt", "denied" }, await Page.EvaluateAsync<string[]>("window.events"));
            await Context.SetPermissionsAsync(TestConstants.EmptyPage, ContextPermission.Geolocation);
            Assert.Equal(
                new[] { "prompt", "denied", "granted" },
                await Page.EvaluateAsync<string[]>("window.events"));
            await Context.ClearPermissionsAsync();
            Assert.Equal(
                new[] { "prompt", "denied", "granted", "prompt" },
                await Page.EvaluateAsync<string[]>("window.events"));
        }

        ///<playwright-file>features/permissions.spec.js</playwright-file>
        ///<playwright-describe>Permissions</playwright-describe>
        ///<playwright-it>should trigger permission onchange</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldIsolatePermissionsBetweenBrowserContexts()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var otherContext = await NewContextAsync();
            var otherPage = await otherContext.NewPageAsync();
            await otherPage.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("prompt", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.SetPermissionsAsync(TestConstants.EmptyPage);
            await otherContext.SetPermissionsAsync(TestConstants.EmptyPage, ContextPermission.Geolocation);
            Assert.Equal("denied", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await Context.ClearPermissionsAsync();
            Assert.Equal("prompt", await GetPermissionAsync(Page, "geolocation"));
            Assert.Equal("granted", await GetPermissionAsync(otherPage, "geolocation"));

            await otherContext.CloseAsync();
        }

        private static Task<string> GetPermissionAsync(IPage page, string name)
            => page.EvaluateAsync<string>(
                "name => navigator.permissions.query({ name }).then(result => result.state)",
                name);
    }
}
