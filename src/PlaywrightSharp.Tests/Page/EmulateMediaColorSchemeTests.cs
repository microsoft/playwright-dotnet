using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>Page.emulateMedia colorScheme</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class EmulateMediaColorSchemeTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public EmulateMediaColorSchemeTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulateMedia colorScheme</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.EmulateMediaAsync(new EmulateMedia { ColorScheme = ColorScheme.Light });
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: no-preference)').matches"));

            await Page.EmulateMediaAsync(new EmulateMedia { ColorScheme = ColorScheme.Dark });
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: no-preference)').matches"));

            if (!TestConstants.IsWebKit)
            {
                await Page.EmulateMediaAsync(new EmulateMedia { ColorScheme = ColorScheme.NoPreference });

                Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
                Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
                Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: no-preference)').matches"));
            }
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulateMedia colorScheme</playwright-describe>
        ///<playwright-it>should throw in case of bad type argument</playwright-it>
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldThrowInCaseOfBadTypeArgument() { }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulateMedia colorScheme</playwright-describe>
        ///<playwright-it>should work during navigation</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkDuringNavigation()
        {
            await Page.EmulateMediaAsync(new EmulateMedia { ColorScheme = ColorScheme.Light });
            var navigated = Page.GoToAsync(TestConstants.EmptyPage);

            for (int i = 0; i < 9; i++)
            {
                await Page.EmulateMediaAsync(new EmulateMedia { ColorScheme = i % 2 == 0 ? ColorScheme.Dark : ColorScheme.Light });
                await Task.Delay(1);
            }
            await navigated;

            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }
    }
}
