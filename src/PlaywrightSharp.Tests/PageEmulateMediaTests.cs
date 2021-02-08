using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEmulateMediaTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEmulateMediaTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.EmulateMediaAsync(ColorScheme.Light);
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            await Page.EmulateMediaAsync(ColorScheme.Dark);
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should default to light")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDefaultToLight()
        {
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            await Page.EmulateMediaAsync(ColorScheme.Dark);
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));

            await Page.EmulateMediaAsync(colorScheme: null);
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should throw in case of bad media argument")]
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldThrowInCaseOfBadMediaArgument() { }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work during navigation")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkDuringNavigation()
        {
            await Page.EmulateMediaAsync(ColorScheme.Light);
            var navigated = Page.GoToAsync(TestConstants.EmptyPage);

            for (int i = 0; i < 9; i++)
            {
                await Page.EmulateMediaAsync(i % 2 == 0 ? ColorScheme.Dark : ColorScheme.Light);
                await Task.Delay(1);
            }
            await navigated;

            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work in popup")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkInPopup()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                ColorScheme = ColorScheme.Dark,
            }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                var popupTask = page.WaitForEventAsync(PageEvent.Popup);

                await TaskUtils.WhenAll(
                    popupTask,
                    page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

                var popup = popupTask.Result.Page;

                Assert.True(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
                Assert.False(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                ColorScheme = ColorScheme.Light,
            }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                var popupTask = page.WaitForEventAsync(PageEvent.Popup);

                await TaskUtils.WhenAll(
                    popupTask,
                    page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

                var popup = popupTask.Result.Page;

                Assert.False(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
                Assert.True(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            }
        }

        [PlaywrightTest("page-emulate-media.spec.ts", "should work in cross-process iframe")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkInCrossProcessIframe()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                ColorScheme = ColorScheme.Dark,
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(page, "frame1", TestConstants.CrossProcessHttpPrefix + "/empty.html");
            var frame = page.Frames[1];

            Assert.True(await frame.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
        }

        [PlaywrightTest("page-emulate-media.spec.js", "should emulate type")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmulateType()
        {
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(MediaType.Print);
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync();
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(MediaType.Null);
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
        }

        [PlaywrightTest("emulation.spec.js", "Page.emulateMedia type", "should throw in case of bad colorScheme argument")]
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldThrowInCaseOfBadColorSchemeArgument() { }
    }
}
